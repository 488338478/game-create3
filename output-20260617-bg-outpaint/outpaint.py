"""
Background top outpainting - 5 versions using Gemini 3.1 Flash.
Original: 9552x2192 RGBA panoramic game background.
Goal: Extend the top boundary upward, adding more sky/environment.
"""
import requests
import base64
import yaml
import os
from PIL import Image
import io

# ── Config ──
with open(os.path.expanduser("~/.claude/skills/media-generation/config.yaml")) as f:
    _config = yaml.safe_load(f)
INTERNAL_TOKEN = _config["auth"]["token"]

MODEL = "gemini-3.1-flash-image-preview"
API_URL = f"http://api-gateway.vivi-x.ai:3000/proxy/gemini/v1beta/models/{MODEL}:generateContent"

OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))
INPUT_PATH = os.path.join(OUTPUT_DIR, "..", "Assets", "Arts", "UI", "第一关长图素材", "Background.png")

# ── Prepare input image ──
print("Loading original image...")
img = Image.open(INPUT_PATH).convert("RGB")
print(f"Original: {img.size}")

# Resize for API - target width ~2752 (2K 16:9 width) for reasonable quality
# Gemini supports up to 4K, but 2K is more reliable for editing
TARGET_W = 2752
scale = TARGET_W / img.size[0]
new_h = int(img.size[1] * scale)
img_resized = img.resize((TARGET_W, new_h), Image.LANCZOS)
print(f"Resized for API: {img_resized.size}")

# Convert to JPEG bytes
buf = io.BytesIO()
img_resized.save(buf, format="JPEG", quality=92)
img_bytes = buf.getvalue()
img_b64 = base64.b64encode(img_bytes).decode("utf-8")
print(f"Encoded size: {len(img_b64)} chars")

# ── 5 Prompt Variations ──
prompts = [
    # V1: Natural sky extension
    """Extend the top of this game background image upward by adding more sky area above.
    The new extended top section should seamlessly continue the existing sky gradient and cloud patterns.
    Add soft, natural clouds and gentle atmospheric haze that matches the lighting of the original scene.
    The transition between original and new content must be invisible and seamless.
    Preserve all existing content in the middle and bottom of the image exactly as-is.
    The extended image should feel like the same continuous game level background, just with more headroom above.""",

    # V2: Canopy and foliage
    """Extend the top portion of this side-scrolling game background upward, adding taller tree canopy,
    overlapping branches, and leaves at the very top edge. The new foliage should naturally continue
    the existing vegetation style, color palette, and density pattern from the original scene.
    Add depth with foreground silhouettes of leaves/branches at the extreme top.
    The transition from original to new content should be seamless with no visible seam.
    Keep all original image content in the lower portion completely unchanged.
    The result should look like the same forest environment with more vertical space above.""",

    # V3: Atmospheric depth
    """Extend the top boundary of this game background image upward by adding atmospheric depth.
    Include distant mountain silhouettes, layered mist/fog, and a subtle gradient sky above the existing scene.
    The new upper area should create a sense of grand scale and depth while perfectly matching
    the existing art style, color temperature, and lighting direction.
    Add soft atmospheric perspective - elements further up should be lighter and less saturated.
    All original content must remain untouched. The seam should be invisible.
    The extended image should feel like a natural, complete composition.""",

    # V4: Rich environmental detail
    """Extend this game background image upward, enriching the top area with detailed environmental elements.
    Add wispy clouds catching sunlight, birds or floating particles in the distance,
    taller background trees or rock formations, and subtle light rays from above.
    Match the exact color grading, saturation, and contrast of the original artwork.
    The new content should feel like it was always part of the original painting.
    Keep all existing elements in the middle and bottom perfectly preserved.
    The result should be a richer, more vertically spacious version of the same scene.""",

    # V5: Ethereal fantasy atmosphere
    """Extend the top of this game background upward with an ethereal, storybook atmosphere.
    Add a dramatic but soft sky with warm/cool gradient, delicate light rays filtering through,
    subtle sparkling particles or fireflies near the top, and a gentle vignette at the very top edge.
    The extension should add emotional impact and visual interest while perfectly blending with
    the existing art style, brush quality, and color harmony of the original background.
    Preserve all original content below exactly as-is with zero modification.
    The extended section should elevate the entire scene's mood and visual appeal."""
]

# ── Generate 5 versions ──
for i, prompt in enumerate(prompts, 1):
    print(f"\n{'='*60}")
    print(f"Generating version {i}/5...")
    print(f"Prompt: {prompt[:100]}...")

    try:
        resp = requests.post(
            API_URL,
            headers={
                "x-internal-token": INTERNAL_TOKEN,
                "Content-Type": "application/json"
            },
            json={
                "contents": [{
                    "parts": [
                        {"text": prompt},
                        {"inline_data": {"mime_type": "image/jpeg", "data": img_b64}}
                    ]
                }],
                "generationConfig": {
                    "responseModalities": ["IMAGE"],
                    "imageConfig": {
                        "imageSize": "2K"
                    }
                }
            },
            timeout=180
        )
        resp.raise_for_status()
        result = resp.json()

        # Extract image
        parts = result["candidates"][0]["content"]["parts"]
        for part in parts:
            if "inlineData" in part:
                out_b64 = part["inlineData"]["data"]
                out_bytes = base64.b64decode(out_b64)
                out_path = os.path.join(OUTPUT_DIR, f"v{i:01d}_outpaint_top.png")
                with open(out_path, "wb") as f:
                    f.write(out_bytes)
                # Check output size
                out_img = Image.open(out_path)
                print(f"  ✅ Saved: {out_path} ({out_img.size})")
                break
        else:
            # Maybe returned text
            text = "".join(p.get("text", "") for p in parts)
            print(f"  ⚠️ No image in response. Text: {text[:200]}")

    except Exception as e:
        print(f"  ❌ Error: {e}")
        if hasattr(e, 'response') and e.response is not None:
            print(f"  Response: {e.response.text[:500]}")

print(f"\n{'='*60}")
print("Done! Check output-20260617-bg-outpaint/ for results.")
