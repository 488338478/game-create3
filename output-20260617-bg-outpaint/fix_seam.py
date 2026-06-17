"""Fix white seam in composite: use Gemini 3 Pro to blend seamlessly."""
import requests, base64, yaml, os
from PIL import Image
import io

with open(os.path.expanduser("~/.claude/skills/media-generation/config.yaml")) as f:
    _config = yaml.safe_load(f)
TOKEN = _config["auth"]["token"]

MODEL = "gemini-3-pro-image-preview"
API_URL = f"http://api-gateway.vivi-x.ai:3000/proxy/gemini/v1beta/models/{MODEL}:generateContent"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# Load composite
with open(os.path.join(OUTPUT_DIR, "to_fix_seam.jpg"), "rb") as f:
    img_b64 = base64.b64encode(f.read()).decode("utf-8")

prompt = """This is a panoramic game background image that has a visible horizontal white seam/edge where two sections were joined together.

Please fix this image:
1. Remove the white horizontal seam completely
2. Blend the area around the former seam so the transition is invisible
3. The upper area (tree canopy/sky) and lower area (ground scene) should look like one continuous painting
4. Match the exact colors, lighting, brush style, and atmosphere across the entire image
5. Keep all existing content and layout - do NOT change anything except removing the seam and blending
6. The result should look like a single cohesive panoramic game background with no visible join line"""

print("Sending to Gemini 3 Pro...")
resp = requests.post(
    API_URL,
    headers={"x-internal-token": TOKEN, "Content-Type": "application/json"},
    proxies={"http": None, "https": None},
    json={
        "contents": [{
            "parts": [
                {"text": prompt},
                {"inline_data": {"mime_type": "image/jpeg", "data": img_b64}}
            ]
        }],
        "generationConfig": {
            "responseModalities": ["IMAGE"],
            "imageConfig": {"imageSize": "2K"}
        }
    },
    timeout=300
)
resp.raise_for_status()
result = resp.json()

parts = result["candidates"][0]["content"]["parts"]
for part in parts:
    if "inlineData" in part:
        out_bytes = base64.b64decode(part["inlineData"]["data"])
        out_path = os.path.join(OUTPUT_DIR, "bg_seamless.png")
        with open(out_path, "wb") as f:
            f.write(out_bytes)
        img = Image.open(out_path)
        print(f"✅ Saved: bg_seamless.png ({img.size})")
        break
else:
    text = "".join(p.get("text", "") for p in parts)
    print(f"No image, text: {text[:200]}")
