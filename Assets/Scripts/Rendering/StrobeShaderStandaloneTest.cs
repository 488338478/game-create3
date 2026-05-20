using UnityEngine;

namespace GameCreate3.Rendering
{
    /// <summary>
    /// 独立测试：把 Strobe2DTest.shader 套在任意 SpriteRenderer/MeshRenderer 上，
    /// 本脚本驱动参数 + 在 Inspector 上调。不依赖 RendererFeature / Blitter。
    ///
    /// 使用：
    /// 1. 场景里建一个 Quad（3D Object → Quad）或 SpriteRenderer，丢任意带纹理的图。
    /// 2. 给其 Renderer 的材质换成 shader = "Game/Test/Strobe2DTest"。
    /// 3. 把本脚本挂到同一个 GameObject 上，运行。
    /// 4. 看效果是否出现 / 拨 intensity / flicker 测试参数。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrobeShaderStandaloneTest : MonoBehaviour
    {
        [Range(0,1)] public float intensity = 1f;
        [Range(0,1)] public float flickerStrength = 0.6f;
        public float flickerFrequency = 12f;
        [Range(0,1)] public float noiseDensity = 0.3f;
        public float rgbShiftAmount = 8f;
        [Range(0,1)] public float scanlineIntensity = 0.5f;
        public float scanlineFrequency = 400f;
        [Range(0,1)] public float tearAmount = 0.2f;
        public float tearFrequency = 5f;
        public Color colorShift = new Color(1,1,1,0);

        [Header("Auto-pulse intensity (sanity check)")]
        public bool autoPulse = true;
        public float pulseHz = 1f;

        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;

        private static readonly int IntensityId         = Shader.PropertyToID("_Intensity");
        private static readonly int FlickerStrengthId   = Shader.PropertyToID("_FlickerStrength");
        private static readonly int FlickerFrequencyId  = Shader.PropertyToID("_FlickerFrequency");
        private static readonly int NoiseDensityId      = Shader.PropertyToID("_NoiseDensity");
        private static readonly int RGBShiftAmountId    = Shader.PropertyToID("_RGBShiftAmount");
        private static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int ScanlineFrequencyId = Shader.PropertyToID("_ScanlineFrequency");
        private static readonly int TearAmountId        = Shader.PropertyToID("_TearAmount");
        private static readonly int TearFrequencyId     = Shader.PropertyToID("_TearFrequency");
        private static readonly int ColorShiftId        = Shader.PropertyToID("_ColorShift");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            if (_renderer == null)
                Debug.LogError("[StrobeShaderStandaloneTest] 需要 Renderer。挂到 Quad/Sprite 上。");
            else if (_renderer.sharedMaterial != null && _renderer.sharedMaterial.shader != null)
                Debug.Log($"[StrobeShaderStandaloneTest] shader='{_renderer.sharedMaterial.shader.name}'");
        }

        private void Update()
        {
            if (_renderer == null) return;

            float i = intensity;
            if (autoPulse)
                i *= 0.5f + 0.5f * Mathf.Sin(Time.time * pulseHz * Mathf.PI * 2f);

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(IntensityId,         i);
            _mpb.SetFloat(FlickerStrengthId,   flickerStrength);
            _mpb.SetFloat(FlickerFrequencyId,  flickerFrequency);
            _mpb.SetFloat(NoiseDensityId,      noiseDensity);
            _mpb.SetFloat(RGBShiftAmountId,    rgbShiftAmount);
            _mpb.SetFloat(ScanlineIntensityId, scanlineIntensity);
            _mpb.SetFloat(ScanlineFrequencyId, scanlineFrequency);
            _mpb.SetFloat(TearAmountId,        tearAmount);
            _mpb.SetFloat(TearFrequencyId,     tearFrequency);
            _mpb.SetColor(ColorShiftId,        colorShift);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
