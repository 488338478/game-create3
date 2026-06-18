using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorApplyTarget : MonoBehaviour
    {
        private sealed class VisualSnapshot
        {
            public Color[] imageColors = Array.Empty<Color>();
            public Color[] spriteColors = Array.Empty<Color>();
            public Sprite[] imageSprites = Array.Empty<Sprite>();
            public Sprite[] rendererSprites = Array.Empty<Sprite>();
            public bool[] activeStates = Array.Empty<bool>();
            public Vector3 scale = Vector3.one;
        }

        [Serializable]
        private sealed class VisualVariant
        {
            public int variantId = -1;
            public string colorId;
            public Sprite[] imageSprites = Array.Empty<Sprite>();
            public Sprite[] spriteRendererSprites = Array.Empty<Sprite>();
            public GameObject[] activeObjects = Array.Empty<GameObject>();
        }

        [SerializeField] private Image[] images;
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [SerializeField] private List<VisualVariant> variants = new List<VisualVariant>();
        [SerializeField] private Transform pulseRoot;
        [SerializeField] private float wrongAlpha = 0.9f;
        [SerializeField] private float correctWhiteLift = 0.2f;
        [SerializeField] private float correctScaleBoost = 0.08f;
        [SerializeField] private float correctPulseDuration = 0.26f;
        [SerializeField] private float hintWhiteLift = 0.35f;
        [SerializeField] private float hintPulseDuration = 0.22f;
        [Tooltip("颜色提示：彩色图 ↔ 无色图(base) 来回切换的间隔(秒)")]
        [SerializeField] private float hintToggleInterval = 0.4f;

        private Color[] baseImageColors = System.Array.Empty<Color>();
        private Color[] baseSpriteColors = System.Array.Empty<Color>();
        private Sprite[] baseImageSprites = System.Array.Empty<Sprite>();
        private Sprite[] baseRendererSprites = System.Array.Empty<Sprite>();
        private Vector3 baseScale = Vector3.one;
        private Coroutine pulseRoutine;
        private Coroutine hintRoutine;
        private VisualSnapshot preHintSnapshot;
        private bool baseStateCached;
        private Coroutine hintColorRoutine;
        private Color[] savedHintImageColors;
        private Color[] savedHintSpriteColors;
        private Sprite[] savedHintImageSprites;
        private Sprite[] savedHintSpriteRendererSprites;
        private Sprite[] hintColorImageSprites;
        private Sprite[] hintColorRendererSprites;
        private GameObject[] trackedVariantObjects = Array.Empty<GameObject>();
        private bool[] trackedVariantObjectStates = Array.Empty<bool>();

        private void Awake()
        {
            if (images == null || images.Length == 0)
            {
                images = GetComponentsInChildren<Image>(true);
            }

            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (pulseRoot == null)
            {
                pulseRoot = transform;
            }

            CacheBaseState(forceRefresh: true);
        }

        public void ApplyColor(Color color, bool isCorrect)
        {
            ApplyVariant(new PaletteColorOption
            {
                fallbackColor = color
            }, isCorrect);
        }

        public void ApplyVariant(PaletteColorOption option, bool isCorrect)
        {
            // 已经点击涂色，停止提示闪烁，避免协程继续切回 sprite 覆盖涂色结果
            StopHintColorPulse();
            ClearHintPreview();
            CacheBaseState();
            RestoreBaseState();

            var appliedVariant = ApplyConfiguredVariant(option);
            if (!appliedVariant)
            {
                if (option.paletteSprite != null)
                {
                    ApplySpriteFallback(option.paletteSprite, isCorrect);
                }
                else
                {
                    ApplyTintFallback(option.fallbackColor, isCorrect);
                }
            }

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (!isCorrect || pulseRoot == null)
            {
                if (pulseRoot != null)
                {
                    pulseRoot.localScale = baseScale;
                }
                return;
            }

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseCorrectTarget());
        }

        public void ResetTarget()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            StopHintColorPulse();
            ClearHintPreview();
            RestoreBaseState();
        }

        public void PlayHintPulse(PaletteColorOption option)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            // 清除上一次的 hint 预览
            ClearHintPreview();

            // 记录闪烁前的原始状态
            preHintSnapshot = CaptureCurrentState();

            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
            }

            hintRoutine = StartCoroutine(HintPulseRoutine(option));
        }

        public void PlayHintColorPulse(PaletteColorOption option)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            StopHintColorPulse();

            // 记录"无色图"基准帧(当前 sprite + color)
            savedHintImageColors = new Color[images.Length];
            savedHintImageSprites = new Sprite[images.Length];
            for (var i = 0; i < images.Length; i++)
            {
                savedHintImageColors[i] = images[i] != null ? images[i].color : Color.white;
                savedHintImageSprites[i] = images[i] != null ? images[i].sprite : null;
            }

            savedHintSpriteColors = new Color[spriteRenderers.Length];
            savedHintSpriteRendererSprites = new Sprite[spriteRenderers.Length];
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                savedHintSpriteColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
                savedHintSpriteRendererSprites[i] = spriteRenderers[i] != null ? spriteRenderers[i].sprite : null;
            }

            // 解析"彩色图"帧：优先配置 variant 的 imageSprites，否则统一用 option.paletteSprite
            var matched = FindMatchedVariant(option);
            hintColorImageSprites = new Sprite[images.Length];
            for (var i = 0; i < images.Length; i++)
            {
                Sprite cs = null;
                if (matched != null && i < matched.imageSprites.Length) cs = matched.imageSprites[i];
                if (cs == null) cs = option.paletteSprite;
                hintColorImageSprites[i] = cs;
            }

            hintColorRendererSprites = new Sprite[spriteRenderers.Length];
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                Sprite cs = null;
                if (matched != null && i < matched.spriteRendererSprites.Length) cs = matched.spriteRendererSprites[i];
                if (cs == null) cs = option.paletteSprite;
                hintColorRendererSprites[i] = cs;
            }

            var hasColorFrame = System.Array.Exists(hintColorImageSprites, s => s != null)
                                || System.Array.Exists(hintColorRendererSprites, s => s != null);
            if (!hasColorFrame)
            {
                return;
            }

            hintColorRoutine = StartCoroutine(HintSpriteToggleRoutine());
        }

        public void StopHintColorPulse()
        {
            if (hintColorRoutine != null)
            {
                StopCoroutine(hintColorRoutine);
                hintColorRoutine = null;
            }

            if (savedHintImageColors != null)
            {
                for (var i = 0; i < images.Length && i < savedHintImageColors.Length; i++)
                {
                    if (images[i] == null) continue;
                    images[i].color = savedHintImageColors[i];
                    if (savedHintImageSprites != null && i < savedHintImageSprites.Length)
                        images[i].sprite = savedHintImageSprites[i];
                }
            }

            if (savedHintSpriteColors != null)
            {
                for (var i = 0; i < spriteRenderers.Length && i < savedHintSpriteColors.Length; i++)
                {
                    if (spriteRenderers[i] == null) continue;
                    spriteRenderers[i].color = savedHintSpriteColors[i];
                    if (savedHintSpriteRendererSprites != null && i < savedHintSpriteRendererSprites.Length)
                        spriteRenderers[i].sprite = savedHintSpriteRendererSprites[i];
                }
            }

            savedHintImageColors = null;
            savedHintSpriteColors = null;
            savedHintImageSprites = null;
            savedHintSpriteRendererSprites = null;
            hintColorImageSprites = null;
            hintColorRendererSprites = null;
        }

        // 颜色提示：按固定频率在"彩色图"和"无色图(base)"之间来回切换 sprite。
        private System.Collections.IEnumerator HintSpriteToggleRoutine()
        {
            var interval = Mathf.Max(0.05f, hintToggleInterval);
            var showColor = true;

            while (true)
            {
                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i] == null) continue;
                    if (showColor)
                    {
                        if (hintColorImageSprites[i] != null)
                        {
                            images[i].sprite = hintColorImageSprites[i];
                            images[i].color = Color.white; // 彩色帧露出原色，避免被 base tint 压暗
                        }
                    }
                    else
                    {
                        if (savedHintImageSprites != null && i < savedHintImageSprites.Length)
                            images[i].sprite = savedHintImageSprites[i];
                        if (i < savedHintImageColors.Length)
                            images[i].color = savedHintImageColors[i];
                    }
                }

                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] == null) continue;
                    if (showColor)
                    {
                        if (hintColorRendererSprites[i] != null)
                        {
                            spriteRenderers[i].sprite = hintColorRendererSprites[i];
                            spriteRenderers[i].color = Color.white;
                        }
                    }
                    else
                    {
                        if (savedHintSpriteRendererSprites != null && i < savedHintSpriteRendererSprites.Length)
                            spriteRenderers[i].sprite = savedHintSpriteRendererSprites[i];
                        if (i < savedHintSpriteColors.Length)
                            spriteRenderers[i].color = savedHintSpriteColors[i];
                    }
                }

                showColor = !showColor;
                yield return new WaitForSeconds(interval);
            }
        }

        private VisualVariant FindMatchedVariant(PaletteColorOption option)
        {
            if (!option.IsValid || variants == null) return null;
            for (var i = 0; i < variants.Count; i++)
            {
                if (variants[i] != null &&
                    PaletteColorOption.Matches(variants[i].variantId, variants[i].colorId, option.variantId, option.colorId))
                {
                    return variants[i];
                }
            }
            return null;
        }

        public void ClearHintPreview()
        {
            if (preHintSnapshot == null) return;
            RestoreSnapshot(preHintSnapshot);
            preHintSnapshot = null;
        }

        private IEnumerator HintPulseRoutine(PaletteColorOption option)
        {
            // 先恢复到闪烁前的状态，再应用预览（每次切换颜色时重新设基准）
            if (preHintSnapshot != null)
            {
                RestoreSnapshot(preHintSnapshot);
            }
            ApplyHintPreview(option);

            var previewSnapshot = CaptureCurrentState();
            var previewColor = ResolveHintPreviewColor(option);
            var previewHighlight = Color.Lerp(previewColor, Color.white, hintWhiteLift);
            previewHighlight.a = 1f;

            var elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                var normalized = (elapsed % hintPulseDuration) / Mathf.Max(0.01f, hintPulseDuration);
                var wave = Mathf.Sin(normalized * Mathf.PI);

                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i] != null)
                    {
                        var tintedColor = previewHighlight;
                        tintedColor.a = i < previewSnapshot.imageColors.Length ? previewSnapshot.imageColors[i].a : 1f;
                        images[i].color = Color.Lerp(
                            i < previewSnapshot.imageColors.Length ? previewSnapshot.imageColors[i] : Color.white,
                            tintedColor,
                            wave);
                    }
                }

                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        var tintedColor = previewHighlight;
                        tintedColor.a = i < previewSnapshot.spriteColors.Length ? previewSnapshot.spriteColors[i].a : 1f;
                        spriteRenderers[i].color = Color.Lerp(
                            i < previewSnapshot.spriteColors.Length ? previewSnapshot.spriteColors[i] : Color.white,
                            tintedColor,
                            wave);
                    }
                }

                yield return null;
            }
        }

        private IEnumerator PulseCorrectTarget()
        {
            var elapsed = 0f;

            while (elapsed < correctPulseDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, correctPulseDuration));
                var wave = Mathf.Sin(normalized * Mathf.PI);
                pulseRoot.localScale = baseScale * (1f + correctScaleBoost * wave);
                yield return null;
            }

            pulseRoot.localScale = baseScale;
            pulseRoutine = null;
        }

        private static Color ResolveHintPreviewColor(PaletteColorOption option)
        {
            if (option.paletteSprite != null && TrySampleSpriteColor(option.paletteSprite, out var spriteColor))
            {
                return spriteColor;
            }

            if (option.fallbackColor.a > 0.001f || option.fallbackColor.maxColorComponent > 0.001f)
            {
                var fallbackColor = option.fallbackColor;
                fallbackColor.a = 1f;
                return fallbackColor;
            }

            return Color.white;
        }

        private static bool TrySampleSpriteColor(Sprite sprite, out Color sampledColor)
        {
            sampledColor = default;
            if (sprite == null)
            {
                return false;
            }

            var texture = sprite.texture;
            if (texture == null || !texture.isReadable)
            {
                return false;
            }

            var rect = sprite.rect;
            var startX = Mathf.RoundToInt(rect.xMin);
            var endX = Mathf.RoundToInt(rect.xMax);
            var startY = Mathf.RoundToInt(rect.yMin);
            var endY = Mathf.RoundToInt(rect.yMax);
            var stepX = Mathf.Max(1, Mathf.RoundToInt(rect.width / 8f));
            var stepY = Mathf.Max(1, Mathf.RoundToInt(rect.height / 8f));

            var weightedSum = Vector4.zero;
            var alphaWeight = 0f;

            for (var y = startY; y < endY; y += stepY)
            {
                for (var x = startX; x < endX; x += stepX)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a <= 0.05f)
                    {
                        continue;
                    }

                    weightedSum += (Vector4)pixel * pixel.a;
                    alphaWeight += pixel.a;
                }
            }

            if (alphaWeight <= 0.001f)
            {
                return false;
            }

            sampledColor = weightedSum / alphaWeight;
            sampledColor.a = 1f;
            return true;
        }

        private void CacheBaseState(bool forceRefresh = false)
        {
            if (images == null)
            {
                images = System.Array.Empty<Image>();
            }

            if (spriteRenderers == null)
            {
                spriteRenderers = System.Array.Empty<SpriteRenderer>();
            }

            if (!forceRefresh &&
                baseStateCached &&
                baseImageColors.Length == images.Length &&
                baseSpriteColors.Length == spriteRenderers.Length)
            {
                return;
            }

            if (baseImageColors.Length != images.Length)
            {
                baseImageColors = new Color[images.Length];
            }

            if (baseImageSprites.Length != images.Length)
            {
                baseImageSprites = new Sprite[images.Length];
            }

            if (baseSpriteColors.Length != spriteRenderers.Length)
            {
                baseSpriteColors = new Color[spriteRenderers.Length];
            }

            if (baseRendererSprites.Length != spriteRenderers.Length)
            {
                baseRendererSprites = new Sprite[spriteRenderers.Length];
            }

            for (var i = 0; i < images.Length; i++)
            {
                baseImageColors[i] = images[i] != null ? images[i].color : Color.white;
                baseImageSprites[i] = images[i] != null ? images[i].sprite : null;
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                baseSpriteColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
                baseRendererSprites[i] = spriteRenderers[i] != null ? spriteRenderers[i].sprite : null;
            }

            baseScale = pulseRoot != null ? pulseRoot.localScale : Vector3.one;
            CacheTrackedVariantObjects();
            baseStateCached = true;
        }

        private void RestoreBaseState()
        {
            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] != null && i < baseImageColors.Length)
                {
                    images[i].color = baseImageColors[i];
                    if (i < baseImageSprites.Length)
                    {
                        images[i].sprite = baseImageSprites[i];
                    }
                }
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && i < baseSpriteColors.Length)
                {
                    spriteRenderers[i].color = baseSpriteColors[i];
                    if (i < baseRendererSprites.Length)
                    {
                        spriteRenderers[i].sprite = baseRendererSprites[i];
                    }
                }
            }

            for (var i = 0; i < trackedVariantObjects.Length; i++)
            {
                if (trackedVariantObjects[i] != null && i < trackedVariantObjectStates.Length)
                {
                    trackedVariantObjects[i].SetActive(trackedVariantObjectStates[i]);
                }
            }

            if (pulseRoot != null)
            {
                pulseRoot.localScale = baseScale;
            }
        }

        private VisualSnapshot CaptureCurrentState()
        {
            var snapshot = new VisualSnapshot
            {
                imageColors = new Color[images.Length],
                spriteColors = new Color[spriteRenderers.Length],
                imageSprites = new Sprite[images.Length],
                rendererSprites = new Sprite[spriteRenderers.Length],
                activeStates = new bool[trackedVariantObjects.Length],
                scale = pulseRoot != null ? pulseRoot.localScale : Vector3.one
            };

            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                snapshot.imageColors[i] = images[i].color;
                snapshot.imageSprites[i] = images[i].sprite;
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                snapshot.spriteColors[i] = spriteRenderers[i].color;
                snapshot.rendererSprites[i] = spriteRenderers[i].sprite;
            }

            for (var i = 0; i < trackedVariantObjects.Length; i++)
            {
                snapshot.activeStates[i] = trackedVariantObjects[i] != null && trackedVariantObjects[i].activeSelf;
            }

            return snapshot;
        }

        private void RestoreSnapshot(VisualSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] == null || i >= snapshot.imageColors.Length)
                {
                    continue;
                }

                images[i].color = snapshot.imageColors[i];
                if (i < snapshot.imageSprites.Length)
                {
                    images[i].sprite = snapshot.imageSprites[i];
                }
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null || i >= snapshot.spriteColors.Length)
                {
                    continue;
                }

                spriteRenderers[i].color = snapshot.spriteColors[i];
                if (i < snapshot.rendererSprites.Length)
                {
                    spriteRenderers[i].sprite = snapshot.rendererSprites[i];
                }
            }

            for (var i = 0; i < trackedVariantObjects.Length && i < snapshot.activeStates.Length; i++)
            {
                if (trackedVariantObjects[i] != null)
                {
                    trackedVariantObjects[i].SetActive(snapshot.activeStates[i]);
                }
            }

            if (pulseRoot != null)
            {
                pulseRoot.localScale = snapshot.scale;
            }
        }

        private bool ApplyConfiguredVariant(PaletteColorOption option)
        {
            if (!option.IsValid || variants == null || variants.Count == 0)
            {
                return false;
            }

            VisualVariant matchedVariant = null;
            for (var i = 0; i < variants.Count; i++)
            {
                if (variants[i] != null && PaletteColorOption.Matches(variants[i].variantId, variants[i].colorId, option.variantId, option.colorId))
                {
                    matchedVariant = variants[i];
                    break;
                }
            }

            if (matchedVariant == null)
            {
                return false;
            }

            for (var i = 0; i < trackedVariantObjects.Length; i++)
            {
                if (trackedVariantObjects[i] != null)
                {
                    trackedVariantObjects[i].SetActive(false);
                }
            }

            for (var i = 0; i < matchedVariant.activeObjects.Length; i++)
            {
                if (matchedVariant.activeObjects[i] != null)
                {
                    matchedVariant.activeObjects[i].SetActive(true);
                }
            }

            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                images[i].color = i < baseImageColors.Length ? baseImageColors[i] : Color.white;
                if (i < matchedVariant.imageSprites.Length && matchedVariant.imageSprites[i] != null)
                {
                    images[i].sprite = matchedVariant.imageSprites[i];
                }
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                spriteRenderers[i].color = i < baseSpriteColors.Length ? baseSpriteColors[i] : Color.white;
                if (i < matchedVariant.spriteRendererSprites.Length && matchedVariant.spriteRendererSprites[i] != null)
                {
                    spriteRenderers[i].sprite = matchedVariant.spriteRendererSprites[i];
                }
            }

            return true;
        }

        private void ApplyHintPreview(PaletteColorOption option)
        {
            if (ApplyConfiguredVariant(option))
            {
                return;
            }

            if (option.paletteSprite != null)
            {
                ApplySpriteFallback(option.paletteSprite, true);
                return;
            }

            ApplyTintFallback(option.fallbackColor, false);
        }

        private void ApplyTintFallback(Color color, bool isCorrect)
        {
            var targetColor = isCorrect ? Color.Lerp(color, Color.white, correctWhiteLift) : color;
            targetColor.a = isCorrect ? 1f : wrongAlpha;

            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    images[i].color = targetColor;
                }
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = targetColor;
                }
            }
        }

        private void ApplySpriteFallback(Sprite sprite, bool isCorrect)
        {
            var spriteColor = Color.white;
            spriteColor.a = isCorrect ? 1f : wrongAlpha;

            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                {
                    continue;
                }

                images[i].sprite = sprite;
                images[i].color = spriteColor;
                images[i].preserveAspect = true;
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                spriteRenderers[i].sprite = sprite;
                spriteRenderers[i].color = spriteColor;
            }
        }

        private void CacheTrackedVariantObjects()
        {
            if (variants == null || variants.Count == 0)
            {
                trackedVariantObjects = Array.Empty<GameObject>();
                trackedVariantObjectStates = Array.Empty<bool>();
                return;
            }

            var uniqueObjects = new List<GameObject>();
            for (var i = 0; i < variants.Count; i++)
            {
                var variant = variants[i];
                if (variant == null || variant.activeObjects == null)
                {
                    continue;
                }

                for (var j = 0; j < variant.activeObjects.Length; j++)
                {
                    var target = variant.activeObjects[j];
                    if (target != null && !uniqueObjects.Contains(target))
                    {
                        uniqueObjects.Add(target);
                    }
                }
            }

            trackedVariantObjects = uniqueObjects.ToArray();
            trackedVariantObjectStates = new bool[trackedVariantObjects.Length];
            for (var i = 0; i < trackedVariantObjects.Length; i++)
            {
                trackedVariantObjectStates[i] = trackedVariantObjects[i] != null && trackedVariantObjects[i].activeSelf;
            }
        }
    }
}
