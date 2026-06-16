using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DreamColorResonanceTarget : MonoBehaviour
    {
        [Serializable]
        private sealed class ResonanceVariant
        {
            public int variantId = -1;
            public string colorId;
            public Sprite[] sprites = Array.Empty<Sprite>();
        }

        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [SerializeField] private List<ResonanceVariant> variants = new List<ResonanceVariant>();
        [SerializeField] private Transform pulseRoot;
        [SerializeField] private float colorBlend = 0.72f;
        [SerializeField] private float whiteLift = 0.28f;
        [SerializeField] private float scaleBoost = 0.06f;
        [SerializeField] private AnimationCurve pulseCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        private Color[] baseColors = System.Array.Empty<Color>();
        private Sprite[] baseSprites = System.Array.Empty<Sprite>();
        private Vector3 baseScale = Vector3.one;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (pulseRoot == null)
            {
                pulseRoot = transform;
            }

            CacheBaseState();
        }

        public void Pulse(Color accentColor, float duration)
        {
            Pulse(new PaletteColorOption
            {
                fallbackColor = accentColor
            }, duration);
        }

        public void Pulse(PaletteColorOption option, float duration)
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                RestoreBaseState();
            }

            CacheBaseState();

            if (!isActiveAndEnabled || duration <= 0f)
            {
                ApplyPulse(option, 1f);
                RestoreBaseState();
                return;
            }

            pulseRoutine = StartCoroutine(PulseRoutine(option, duration));
        }

        private IEnumerator PulseRoutine(PaletteColorOption option, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = Mathf.Clamp01(elapsed / duration);
                ApplyPulse(option, pulseCurve.Evaluate(normalizedTime));
                yield return null;
            }

            RestoreBaseState();
            pulseRoutine = null;
        }

        private void ApplyPulse(PaletteColorOption option, float intensity)
        {
            var appliedVariant = ApplyVariantSprites(option);

            if (!appliedVariant && spriteRenderers != null)
            {
                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    var renderer = spriteRenderers[i];
                    if (renderer == null || i >= baseColors.Length)
                    {
                        continue;
                    }

                    var targetColor = Color.Lerp(baseColors[i], option.fallbackColor, colorBlend);
                    targetColor = Color.Lerp(targetColor, Color.white, whiteLift);
                    renderer.color = Color.Lerp(baseColors[i], targetColor, intensity);
                }
            }

            if (pulseRoot != null)
            {
                pulseRoot.localScale = baseScale * (1f + scaleBoost * intensity);
            }
        }

        private void CacheBaseState()
        {
            if (spriteRenderers == null)
            {
                spriteRenderers = System.Array.Empty<SpriteRenderer>();
            }

            if (baseColors.Length != spriteRenderers.Length)
            {
                baseColors = new Color[spriteRenderers.Length];
            }

            if (baseSprites.Length != spriteRenderers.Length)
            {
                baseSprites = new Sprite[spriteRenderers.Length];
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                baseColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
                baseSprites[i] = spriteRenderers[i] != null ? spriteRenderers[i].sprite : null;
            }

            baseScale = pulseRoot != null ? pulseRoot.localScale : Vector3.one;
        }

        private void RestoreBaseState()
        {
            if (spriteRenderers != null)
            {
                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null && i < baseColors.Length)
                    {
                        spriteRenderers[i].color = baseColors[i];
                        if (i < baseSprites.Length)
                        {
                            spriteRenderers[i].sprite = baseSprites[i];
                        }
                    }
                }
            }

            if (pulseRoot != null)
            {
                pulseRoot.localScale = baseScale;
            }
        }

        private bool ApplyVariantSprites(PaletteColorOption option)
        {
            if (!option.IsValid || variants == null || variants.Count == 0)
            {
                return false;
            }

            ResonanceVariant matched = null;
            for (var i = 0; i < variants.Count; i++)
            {
                if (variants[i] != null && PaletteColorOption.Matches(variants[i].variantId, variants[i].colorId, option.variantId, option.colorId))
                {
                    matched = variants[i];
                    break;
                }
            }

            if (matched == null)
            {
                return false;
            }

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null)
                {
                    continue;
                }

                spriteRenderers[i].color = i < baseColors.Length ? baseColors[i] : Color.white;
                if (i < matched.sprites.Length && matched.sprites[i] != null)
                {
                    spriteRenderers[i].sprite = matched.sprites[i];
                }
            }

            return true;
        }
    }
}
