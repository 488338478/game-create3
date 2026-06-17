"""
Use Gemini multi-image composition to blend V2 top + original bottom seamlessly.
Ref 1: V2 output (good top canopy/sky)
Ref 2: Original background (preserve bottom content)
"""
import requests
import base64
import yaml
import os
from PIL import Image
import io
import time

# ── Config ──
with open(os.path.expanduser("~/.claude/skills/media-generation/config.yaml")) as f:
    _config = yaml.safe_load(f)
INTERNAL_TOKEN = _config["auth"]["token"]

MODEL = "gemini-3.1-flash-image-preview"
API_URL = f"http://api-gateway.vivi-x.ai:3000/proxy/gemini/v1beta/models/{MODEL}:generateContent"

OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# ── Load and encode both reference images ──
ref_v2_path = os.path.join(OUTPUT_DIR, "ref_v2_top.png")
ref_orig_path = os.path.join(OUTPUT_DIR, "ref_orig_bottom.png")

with open(ref_v2_path, "rb") as f:
    ref_v2_b64 = base64.b64encode(f.read()).decode("utf-8")
with open(ref_orig_path, "rb") as f:
    ref_orig_b64 = base64.b64encode(f.read()).decode("utf-8")

print(f"Ref V2 size: {len(ref_v2_b64)} chars")
print(f"Ref Orig size: {len(ref_orig_b64)} chars")

# ── Prompt ──
# Describe the task clearly: use V2's top + original's bottom, blend seamlessly
prompts = [
    # V1: Balanced blend
    """You are given two reference images of a side-scrolling game background (a forest/nature scene).
Image 1 (first image) shows a version with richer, taller tree canopy at the top.
Image 2 (second image) is the original version that needs to be preserved at the bottom.

Create ONE new image that combines the best of both:
- The TOP ~40% of the new image should use Image 1's extended tree canopy, sky, and upper foliage as reference for style and content.
- The BOTTOM ~60% of the new image should faithfully reproduce Image 2's original ground-level content, layout, and details.
- The transition between the two regions must be completely invisible - like they were always one painting.
- Match the exact color grading, lighting, brush style, saturation, and atmospheric perspective of Image 2 throughout.
- The extended top area should naturally continue the environment upward: more sky, taller trees, overlapping branches, soft clouds.
- Output a single seamless panoramic game background image.
- Do NOT change any existing elements from Image 2's lower portion.""",

    # V2: More canopy emphasis
    """Reference images are two versions of the same panoramic game level background.
Image 1 (first): has richer upper tree canopy and sky extension.
Image 2 (second): the original baseline to preserve below.

Create a single blended panoramic background:
- Use Image 1 as reference for the UPPER section - extending the tree canopy taller, adding layered foliage, branches, and sky above.
- Use Image 2 as reference for the LOWER section - keep the ground, midground elements, and overall scene layout exactly as in Image 2.
- Blend the upper and lower sections seamlessly with no visible boundary or seam.
- The upper extension should feel like a natural vertical continuation of Image 2's environment - same forest, same trees, same lighting, just more headroom.
- All color values, lighting direction, shadow consistency, and art style must match Image 2 perfectly.
- Output: one unified panoramic game background, taller than Image 2, with the extended canopy top from Image 1's aesthetic.""",

    # V3: Atmospheric depth focus
    """Two reference panoramas of a forest game background are provided.
Image 1: extended version with more sky and canopy at top.
Image 2: original version to preserve at bottom.

Generate one seamless extended panoramic background:
- TOP SECTION: inspired by Image 1's upper atmosphere - more sky gradient, distant clouds, taller tree silhouettes, soft atmospheric haze creating depth.
- BOTTOM SECTION: faithfully keep Image 2's original ground plane, path, foliage, and all scene elements unchanged.
- The middle transition zone should feature trees and foliage that bridge both sections naturally.
- Ensure perfect color continuity - the new top must share Image 2's exact warm/cool balance, saturation level, and contrast.
- Lighting must be consistent: if Image 2 has dappled sunlight, the extended top should have matching light rays and shadow patterns.
- Result should look like the original artist simply painted a taller canvas to begin with.""",

    # V4: Subtle extension
    """Two panoramic game background images are provided as reference.
Image 1: has an extended tree canopy top area.
Image 2: the original scene to preserve.

Create a naturally extended version:
- The new top area (~35% of total height) should be guided by Image 1's canopy density, tree shapes, and sky treatment.
- The remaining area should match Image 2's original content exactly.
- The extension should be subtle and natural - not dramatically different, just a gentle continuation of the existing treeline upward with more sky and branch detail.
- No sharp edges, no visible cut line, no style mismatch anywhere in the image.
- Output one cohesive panoramic background image.
- All artistic qualities (texture, brushwork, color palette) must be identical to Image 2.""",

    # V5: Rich environment
    """Reference images: two versions of a horizontal game level background.
Image 1: shows richer upper foliage and extended sky.
Image 2: the original baseline.

Synthesize one final seamless panoramic background:
- UPPER AREA: take creative direction from Image 1's treetop canopy - extend the forest upward with layered leaves, dappled sky peeking through branches, subtle light rays, and atmospheric depth.
- LOWER AREA: preserve Image 2's complete original scene as-is - every rock, path, bush, and ground detail stays exactly where it is.
- The join between upper and lower must be invisible - use overlapping foliage, graduated atmospheric haze, and continuous tree trunks to bridge the transition.
- Color grade, shadow temperature, highlight tone, and overall mood must be 100% consistent with Image 2.
- The final image should read as "the same painting, just with more sky and taller trees" - not as two images stitched together.
- Output the complete extended panoramic background."""
]

for i, prompt in enumerate(prompts, 1):
    print(f"\n{'='*60}")
    print(f"Generating blended version {i}/5...")

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
                        {"inline_data": {"mime_type": "image/jpeg", "data": ref_v2_b64}},
                        {"inline_data": {"mime_type": "image/jpeg", "data": ref_orig_b64}}
                    ]
                }],
                "generationConfig": {
                    "responseModalities": ["IMAGE"],
                    "imageConfig": {
                        "imageSize": "2K"
                    }
                }
            },
            timeout=300
        )
        resp.raise_for_status()
        result = resp.json()

        parts = result["candidates"][0]["content"]["parts"]
        for part in parts:
            if "inlineData" in part:
                out_b64 = part["inlineData"]["data"]
                out_bytes = base64.b64decode(out_b64)
                out_path = os.path.join(OUTPUT_DIR, f"blend_v{i:01d}.png")
                with open(out_path, "wb") as f:
                    f.write(out_bytes)
                out_img = Image.open(out_path)
                print(f"  ✅ Saved: blend_v{i:01d}.png ({out_img.size})")
                break
        else:
            text = "".join(p.get("text", "") for p in parts)
            print(f"  ⚠️ No image. Text: {text[:300]}")

    except Exception as e:
        print(f"  ❌ Error: {e}")
        if hasattr(e, 'response') and e.response is not None:
            print(f"  Response: {e.response.text[:500]}")

print(f"\n{'='*60}")
print("Done! Check output-20260617-bg-outpaint/blend_v1~v5.png")
