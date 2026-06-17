"""Use Gemini 3.1 Flash to outpaint white edges with scene content."""
import requests, base64, yaml, os, io
from PIL import Image
import numpy as np

with open(os.path.expanduser("~/.claude/skills/media-generation/config.yaml")) as f:
    _config = yaml.safe_load(f)
TOKEN = _config["auth"]["token"]

MODEL = "gemini-3.1-flash-image-preview"
API_URL = f"http://api-gateway.vivi-x.ai:3000/proxy/gemini/v1beta/models/{MODEL}:generateContent"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# Load image
img = Image.open(os.path.join(OUTPUT_DIR, "bg_seamless.png")).convert("RGB")
print(f"Input: {img.size}")

# Encode
buf = io.BytesIO()
img.save(buf, format="JPEG", quality=92)
img_b64 = base64.b64encode(buf.getvalue()).decode("utf-8")
print(f"Encoded: {len(img_b64)} chars")

prompt = """This is a panoramic game background image that has white empty borders on the bottom, left, and right sides.

Please extend the actual scene content to fill ALL white/empty border areas:
- Bottom: Extend the ground, foliage, and environment downward to fill the white strip at the bottom.
- Left: Extend the left side scenery to fill the white strip on the left.
- Right: Extend the right side scenery to fill the white strip on the right.
- Top: The top edge is fine, do not change it.

The extensions must seamlessly match the existing art style, colors, lighting, and brushwork. The filled areas should look like they were always part of the original painting.
The result should be a complete full-frame panoramic game background with no white edges, no borders, no empty space - just the scene filling the entire canvas edge to edge.
Do NOT crop or resize - keep the full canvas and fill in all white areas with matching scene content."""

print("Sending to Gemini 3.1 Flash...")
resp = requests.post(
    API_URL,
    headers={"x-internal-token": TOKEN, "Content-Type": "application/json"},
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
        out_path = os.path.join(OUTPUT_DIR, "bg_no_white_edges.png")
        with open(out_path, "wb") as f:
            f.write(out_bytes)
        out_img = Image.open(out_path)
        print(f"✅ Saved: bg_no_white_edges.png ({out_img.size})")

        # Check white edges
        arr = np.array(out_img)
        for name, strip in [('top', arr[0,:,:]), ('bottom', arr[-1,:,:]), ('left', arr[:,0,:]), ('right', arr[:,-1,:])]:
            white_px = ((strip[:,0] > 240) & (strip[:,1] > 240) & (strip[:,2] > 240)).sum() if len(strip.shape) > 1 else 0
            total = strip.shape[0] if len(strip.shape) > 1 else strip.size
            pct = 100*white_px/total if total > 0 else 0
            print(f"  {name}: {white_px}/{total} white ({pct:.1f}%)")
        break
else:
    text = "".join(p.get("text", "") for p in parts)
    print(f"No image, text: {text[:300]}")
