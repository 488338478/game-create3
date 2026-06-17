"""Qwen Image 2.0 Pro: ultra-clean sketch refinement — preserve style, only clean lines & colors."""
import requests, base64, yaml, os, io
from PIL import Image

with open(os.path.expanduser("~/.claude/skills/media-generation/config.yaml")) as f:
    _config = yaml.safe_load(f)
TOKEN = _config["auth"]["token"]

API_URL = "http://api-gateway.vivi-x.ai:3000/proxy/dashscope-intl/api/v1/services/aigc/multimodal-generation/generation"
MODEL = "qwen-image-2.0-pro"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# Prepare input
img = Image.open(os.path.join(OUTPUT_DIR, "L3_sketch_input.jpg"))
w, h = img.size
print(f"Input: {w}x{h}")

# Encode as data URL
buf = io.BytesIO()
img.save(buf, format="JPEG", quality=92)
data_url = "data:image/jpeg;base64," + base64.b64encode(buf.getvalue()).decode()

# Three restrained prompts
prompts = [
    # V1: 最保守 — 线条精修
    """保留这张手绘草图的全部元素、构图和手绘风格不变。
仅做以下细微处理：
1. 让线条稍微更干净流畅一点
2. 让颜色填充稍微更均匀一点
3. 去除明显杂点和涂改痕迹
整体仍然是一张手绘草图，不能变成渲染图或成品画。保持原有的铅笔/笔刷质感。""",

    # V2: 色彩微调
    """保持这张草图的手绘风格、线条质感和全部构图元素完全不变。
仅微调颜色：让色块更均匀一些，减少涂色不均的斑驳感。
不要改变任何形状和位置，不要添加光影效果，不要渲染。
结果必须仍然是手绘草图的质感。""",

    # V3: 整体清洁
    """对这张手绘游戏场景草图做轻度清洁处理：
- 保持所有元素、位置、风格不变
- 让粗糙线条稍微平滑
- 让颜色稍微干净
- 去除杂点
不要添加任何光影、渲染、渐变、新元素。保持手绘线稿的感觉。""",
]

for i, prompt in enumerate(prompts, 1):
    print(f"\n{'='*50}")
    print(f"V{i}/3...")

    payload = {
        "model": MODEL,
        "input": {
            "messages": [{
                "role": "user",
                "content": [
                    {"image": data_url},
                    {"text": prompt}
                ]
            }]
        },
        "parameters": {
            "n": 1,
            "negative_prompt": "渲染图, 3D渲染, 写实照片, 过度光滑, AI感, 变形, 添加物体, 风格改变, 光影特效, 渐变, 雾化",
            "prompt_extend": False,
            "watermark": False,
        }
    }

    try:
        resp = requests.post(
            API_URL,
            headers={"x-internal-token": TOKEN, "Content-Type": "application/json"},
            proxies={"http": None, "https": None},
            json=payload,
            timeout=180
        )
        resp.raise_for_status()
        result = resp.json()

        urls = [
            item["image"]
            for item in result["output"]["choices"][0]["message"]["content"]
            if item.get("image")
        ]
        if urls:
            # Download
            img_resp = requests.get(urls[0], timeout=60)
            img_resp.raise_for_status()
            out_path = os.path.join(OUTPUT_DIR, f"L3_qwen_v{i:01d}.png")
            with open(out_path, "wb") as f:
                f.write(img_resp.content)
            out_img = Image.open(out_path)
            print(f"  ✅ {out_img.size}")
        else:
            print("  ⚠️ No image URL")

    except Exception as e:
        print(f"  ❌ {e}")
        if hasattr(e, 'response') and e.response is not None:
            print(f"  {e.response.text[:300]}")

print(f"\nDone → L3_qwen_v1~v3.png")
