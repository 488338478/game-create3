using System.Collections;
using System.Collections.Generic;
using GameCreate3.Rendering;
using UnityEngine;

namespace GameCreate3.SideScroll.VFX
{
    /// <summary>
    /// 频闪效果运行时控制器。挂在 BootScene 或 DontDestroyOnLoad 容器上。
    /// </summary>
    public sealed class StrobeEffectController : MonoBehaviour
    {
        private static readonly int IntensityId          = Shader.PropertyToID("_Intensity");
        private static readonly int FlickerStrengthId    = Shader.PropertyToID("_FlickerStrength");
        private static readonly int FlickerFrequencyId   = Shader.PropertyToID("_FlickerFrequency");
        private static readonly int NoiseDensityId       = Shader.PropertyToID("_NoiseDensity");
        private static readonly int RGBShiftAmountId     = Shader.PropertyToID("_RGBShiftAmount");
        private static readonly int ScanlineIntensityId  = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int ScanlineFrequencyId  = Shader.PropertyToID("_ScanlineFrequency");
        private static readonly int TearAmountId         = Shader.PropertyToID("_TearAmount");
        private static readonly int TearFrequencyId      = Shader.PropertyToID("_TearFrequency");
        private static readonly int ColorShiftId         = Shader.PropertyToID("_ColorShift");
        private static readonly int TimeSeedId           = Shader.PropertyToID("_TimeSeed");

        public static StrobeEffectController Instance { get; private set; }

        [Tooltip("跨场景常驻。如果在每个场景手动放，建议关掉。")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private Coroutine activeRoutine;
        private StrobeConfig currentConfig;
        private float runtimeIntensityMul = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Disable();
                Instance = null;
            }
        }

        // ---------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------

        public void Play(StrobeConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[StrobeEffectController] 传入的 StrobeConfig 为空。");
                return;
            }   
            Debug.Log($"[StrobeEffectController] Play StrobeConfig: {config.name}");
            StopActiveRoutine();
            currentConfig = config;
            runtimeIntensityMul = 1f;
            activeRoutine = StartCoroutine(PlayRoutine(config));
        }

        public void Play(StrobeConfig basePreset, IReadOnlyDictionary<string, string> overrides)
        {
            if (basePreset == null && (overrides == null || overrides.Count == 0)) return;

            var working = basePreset != null
                ? basePreset.CloneRuntime()
                : ScriptableObject.CreateInstance<StrobeConfig>();

            if (overrides != null && overrides.Count > 0)
            {
                ApplyOverrides(working, overrides);
            }
            Play(working);
        }

        public void PlayInline(IReadOnlyDictionary<string, string> overrides)
        {
            var c = ScriptableObject.CreateInstance<StrobeConfig>();
            if (overrides != null) ApplyOverrides(c, overrides);
            Play(c);
        }

        public void SetIntensity(float mul)
        {
            runtimeIntensityMul = Mathf.Clamp01(mul);
        }

        public void Stop()
        {
            StopActiveRoutine();
            Disable();
        }

        public void FadeOut(float duration)
        {
            if (duration <= 0f) { Stop(); return; }
            if (currentConfig == null) { Disable(); return; }
            StopActiveRoutine();
            activeRoutine = StartCoroutine(FadeOutRoutine(duration, currentConfig));
        }

        // ---------------------------------------------------------------------
        // Routines
        // ---------------------------------------------------------------------

        private IEnumerator PlayRoutine(StrobeConfig c)
        {
            Enable(c);

            // Fade in
            if (c.fadeIn > 0f)
            {
                float t = 0f;
                while (t < c.fadeIn)
                {
                    t += Time.unscaledDeltaTime;
                    float w = Mathf.Clamp01(t / c.fadeIn);
                    ApplyToMaterial(c, w * c.intensity * runtimeIntensityMul);
                    yield return null;
                }
            }

            if (c.duration > 0f)
            {
                float holdTime = Mathf.Max(0f, c.duration - c.fadeIn - c.fadeOut);
                float held = 0f;
                while (held < holdTime)
                {
                    held += Time.unscaledDeltaTime;
                    ApplyToMaterial(c, c.intensity * runtimeIntensityMul);
                    yield return null;
                }

                if (c.fadeOut > 0f)
                {
                    float fo = 0f;
                    while (fo < c.fadeOut)
                    {
                        fo += Time.unscaledDeltaTime;
                        float w = 1f - Mathf.Clamp01(fo / c.fadeOut);
                        ApplyToMaterial(c, w * c.intensity * runtimeIntensityMul);
                        yield return null;
                    }
                }

                Disable();
                activeRoutine = null;
            }
            else
            {
                // 无限持续 — 等 Stop()
                while (true)
                {
                    ApplyToMaterial(c, c.intensity * runtimeIntensityMul);
                    yield return null;
                }
            }
        }

        private IEnumerator FadeOutRoutine(float duration, StrobeConfig c)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float w = 1f - Mathf.Clamp01(t / duration);
                ApplyToMaterial(c, w * c.intensity * runtimeIntensityMul);
                yield return null;
            }
            Disable();
            activeRoutine = null;
        }

        private void StopActiveRoutine()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
        }

        // ---------------------------------------------------------------------
        // Material I/O
        // ---------------------------------------------------------------------

        private void Enable(StrobeConfig c)
        {
            var f = StrobeRendererFeature.Instance;
            if (f == null)
            {
                Debug.LogWarning("[StrobeEffectController] StrobeRendererFeature 未注册到 Renderer2DData，效果无法显示。请见 README_StrobeEffect.md。");
                return;
            }
            f.IsActive = true;
            ApplyStaticParams(c);
        }

        private void Disable()
        {
            var f = StrobeRendererFeature.Instance;
            if (f != null) f.IsActive = false;
            currentConfig = null;
        }

        private void ApplyStaticParams(StrobeConfig c)
        {
            var mat = StrobeRendererFeature.Instance?.Material;
            if (mat == null) return;
            mat.SetFloat(FlickerFrequencyId,  c.flickerFrequency);
            mat.SetFloat(ScanlineFrequencyId, c.scanlineFrequency);
            mat.SetFloat(TearFrequencyId,     c.tearFrequency);
            mat.SetFloat(TimeSeedId,          Random.Range(0f, 1000f));
        }

        private void ApplyToMaterial(StrobeConfig c, float effectiveIntensity)
        {
            var mat = StrobeRendererFeature.Instance?.Material;
            if (mat == null) return;
            mat.SetFloat(IntensityId,         effectiveIntensity);
            mat.SetFloat(FlickerStrengthId,   c.flickerStrength);
            mat.SetFloat(NoiseDensityId,      c.noiseDensity);
            mat.SetFloat(RGBShiftAmountId,    c.rgbShiftAmount);
            mat.SetFloat(ScanlineIntensityId, c.scanlineIntensity);
            mat.SetFloat(TearAmountId,        c.tearAmount);
            mat.SetColor(ColorShiftId,        c.colorShift);
        }

        // ---------------------------------------------------------------------
        // Overrides parser
        // ---------------------------------------------------------------------

        private static void ApplyOverrides(StrobeConfig c, IReadOnlyDictionary<string, string> overrides)
        {
            foreach (var kv in overrides)
            {
                var key = kv.Key.ToLowerInvariant();

                if (key == "color")
                {
                    if (ColorUtility.TryParseHtmlString(kv.Value, out var col)) c.colorShift = col;
                    continue;
                }

                if (!float.TryParse(kv.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
                    continue;

                switch (key)
                {
                    case "duration":     c.duration          = f; break;
                    case "fadein":       c.fadeIn            = f; break;
                    case "fadeout":      c.fadeOut           = f; break;
                    case "intensity":    c.intensity         = Mathf.Clamp01(f); break;
                    case "flicker":      c.flickerStrength   = Mathf.Clamp01(f); break;
                    case "frequency":
                    case "flickerfreq":  c.flickerFrequency  = f; break;
                    case "noise":        c.noiseDensity      = Mathf.Clamp01(f); break;
                    case "rgb":          c.rgbShiftAmount    = f; break;
                    case "scanline":     c.scanlineIntensity = Mathf.Clamp01(f); break;
                    case "scanlinefreq": c.scanlineFrequency = f; break;
                    case "tear":         c.tearAmount        = Mathf.Clamp01(f); break;
                    case "tearfreq":     c.tearFrequency     = f; break;
                }
            }
        }
    }
}
