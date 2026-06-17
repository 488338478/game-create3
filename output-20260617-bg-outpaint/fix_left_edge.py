"""One more pass: fix remaining white strip on left edge."""
import requests, base64, yaml, os, io
from PIL import Image
import numpy as np

with open(os.path.expanduser("~/.claude/skills/media-generation/config.yaml")) as f:
    _config = yaml.safe_load(f)
TOKEN = _config["auth"]["token"]

MODEL = "gemini-3.1-flash-image-preview"
API_URL = f"http://api-gateway.vivi-x.ai:3000/proxy/gemini/v1beta/models/{MODEL}:generateContent"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

img = Image.open(os.path.join(OUTPUT_DIR, "bg_no_white_edges.png")).convert("RGB")
print(f"Input: {img.size}")

buf = io.BytesIO()
img.save(buf, format="JPEG", quality=92)
img_b64 = base64.b64encode(buf.getvalue()).decode("utf-8")

prompt = """This game background image has a white strip only on the LEFT edge. The top, bottom, and right edges are already perfect.

Please ONLY extend the scene content to fill the white strip on the LEFT edge. Everything else must stay exactly as-is.
- Extend the trees, foliage, and environment elements from the left side scene to fill the white gap.
- Match the existing art style, colors, and lighting perfectly.
- The left extension must blend seamlessly with the adjacent content.
- Do NOT modify the top, bottom, right, or center of the image at all.
- Result: a complete full-frame image with zero white edges."""

print("Sending...")
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
        out_path = os.path.join(OUTPUT_DIR, "bg_no_white_edges_v2.png")
        with open(out_path, "wb") as f:
            f.write(out_bytes)
        out_img = Image.open(out_path)
        print(f"✅ Saved: bg_no_white_edges_v2.png ({out_img.size})")
        arr = np.array(out_img)
        for name, strip in [('top', arr[0,:,:]), ('bottom', arr[-1,:,:]), ('left', arr[:,0,:]), ('right', arr[:,-1,:])]:
            white = ((strip[:,0] > 245) & (strip[:,1] > 245) & (strip[:,2] > 245)).sum()
            total = strip.shape[0]
            print(f"  {name}: {white}/{total} white ({100*white/total:.1f}%)")
        break
