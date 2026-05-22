using UnityEngine;

namespace GameCreate3.SideScroll.VFX
{
    /// <summary>
    /// 频闪效果预设。放在 Resources/VFX/Strobe/ 下以便 StoryPlayer 通过名字查找。
    /// </summary>
    [CreateAssetMenu(fileName = "StrobePreset_", menuName = "GameCreate3/VFX/Strobe Preset")]
    public sealed class StrobeConfig : ScriptableObject
    {
        [Header("时长")]
        [Tooltip("总时长（秒）。<= 0 表示需要手动 Stop。")]
        public float duration = 2.0f;

        [Tooltip("入场渐显（秒）")]
        public float fadeIn = 0.1f;

        [Tooltip("出场渐隐（秒）")]
        public float fadeOut = 0.3f;

        [Header("主强度")]
        [Range(0f, 1f)] public float intensity = 1f;

        [Header("亮度闪烁")]
        [Range(0f, 1f)] public float flickerStrength = 0.5f;

        [Tooltip("闪烁频率（Hz）")]
        public float flickerFrequency = 12f;

        [Header("雪花噪点")]
        [Range(0f, 1f)] public float noiseDensity = 0.4f;

        [Header("RGB 偏移")]
        [Tooltip("像素偏移量")]
        public float rgbShiftAmount = 6f;

        [Header("扫描线")]
        [Range(0f, 1f)] public float scanlineIntensity = 0.3f;

        [Tooltip("扫描线数量（垂直方向条数）")]
        public float scanlineFrequency = 200f;

        [Header("画面撕裂")]
        [Range(0f, 1f)] public float tearAmount = 0.15f;

        [Tooltip("撕裂触发频率（Hz）")]
        public float tearFrequency = 8f;

        [Header("色调偏移")]
        [Tooltip("叠加偏色 — A 通道控制混合权重")]
        public Color colorShift = new Color(1f, 1f, 1f, 0f);

        public StrobeConfig CloneRuntime()
        {
            var c = CreateInstance<StrobeConfig>();
            c.duration          = duration;
            c.fadeIn            = fadeIn;
            c.fadeOut           = fadeOut;
            c.intensity         = intensity;
            c.flickerStrength   = flickerStrength;
            c.flickerFrequency  = flickerFrequency;
            c.noiseDensity      = noiseDensity;
            c.rgbShiftAmount    = rgbShiftAmount;
            c.scanlineIntensity = scanlineIntensity;
            c.scanlineFrequency = scanlineFrequency;
            c.tearAmount        = tearAmount;
            c.tearFrequency     = tearFrequency;
            c.colorShift        = colorShift;
            return c;
        }
    }
}
