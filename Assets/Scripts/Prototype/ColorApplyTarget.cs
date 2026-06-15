using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorApplyTarget : MonoBehaviour
    {
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
        [SerializeField] private float hintScaleBoost = 0.06f;
        [SerializeField] private float hintPulseDuration = 0.22f;

        private Color[] baseImageColors = System.Array.Empty<Color>();
        private Color[] baseSpriteColors = System.Array.Empty<Color>();
        private Sprite[] baseImageSprites = System.Array.Empty<Sprite>();
        private Sprite[] baseRendererSprites = System.Array.Empty<Sprite>();
        private Vector3 baseScale = Vector3.one;
        private Coroutine pulseRoutine;
        private Coroutine hintRoutine;
        private bool baseStateCached;
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

            RestoreBaseState();
        }

        public void PlayHintPulse()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
            }

            hintRoutine = StartCoroutine(HintPulseRoutine());
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

        private IEnumerator HintPulseRoutine()
        {
            var currentImageColors = new Color[images.Length];
            var currentSpriteColors = new Color[spriteRenderers.Length];
            var currentScale = pulseRoot != null ? pulseRoot.localScale : Vector3.one;

            for (var i = 0; i < images.Length; i++)
            {
                currentImageColors[i] = images[i] != null ? images[i].color : Color.white;
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                currentSpriteColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
            }

            var elapsed = 0f;
            while (elapsed < hintPulseDuration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, hintPulseDuration));
                var wave = Mathf.Sin(normalized * Mathf.PI);

                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i] != null)
                    {
                        images[i].color = Color.Lerp(currentImageColors[i], Color.white, hintWhiteLift * wave);
                    }
                }

                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        spriteRenderers[i].color = Color.Lerp(currentSpriteColors[i], Color.white, hintWhiteLift * wave);
                    }
                }

                if (pulseRoot != null)
                {
                    pulseRoot.localScale = currentScale * (1f + hintScaleBoost * wave);
                }

                yield return null;
            }

            for (var i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    images[i].color = currentImageColors[i];
                }
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = currentSpriteColors[i];
                }
            }

            if (pulseRoot != null)
            {
                pulseRoot.localScale = currentScale;
            }

            hintRoutine = null;
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
