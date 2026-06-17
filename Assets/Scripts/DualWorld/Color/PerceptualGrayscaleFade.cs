using UnityEngine;

namespace GameCreate3.DualWorld
{
    /// <summary>
    /// 感知灰度映射算法 — 将 RGBA 彩色平滑过渡到纯灰度（2通道：亮度 + Alpha）。
    ///
    /// 设计目标：
    ///   1. t=0 保持原色，t=1 完全仅剩黑白灰 + Alpha
    ///   2. 色调→明暗映射符合人眼直觉：
    ///      - 黄/橙/浅绿 → 亮灰（人眼对中长波敏感，且暖色感知上更"前进"）
    ///      - 蓝/紫 → 深灰（短波视锥贡献低）
    ///      - 高饱和色适当提亮（Helmholtz–Kohlrausch 效应：鲜艳色看起来更亮）
    ///   3. 保持 alpha 通道不变
    ///
    /// 管线（逐像素）：
    ///   sRGB decode → CIE 1931 相对亮度 → H-K 饱和度补偿 → sRGB encode → lerp
    /// </summary>
    public static class PerceptualGrayscaleFade
    {
        // ──────────────────────────────────────────────
        // CIE 1931 相对亮度权重（比 BT.601 更贴合现代显示器和人眼光谱敏感度）
        // 绿 71.52% + 红 21.26% + 蓝 7.22%
        // ──────────────────────────────────────────────
        private const float LumaR = 0.2126f;
        private const float LumaG = 0.7152f;
        private const float LumaB = 0.0722f;

        // Helmholtz–Kohlrausch 补偿上限：高饱和色最多额外提亮 12%
        // 值越高，鲜艳色在灰度下越突出；0 则完全按物理亮度
        private const float HkCompensation = 0.12f;

        // 柔和 S 曲线对比度保留：在中间调施加微小的对比拉伸
        // 0 = 线性映射，~0.08 = 轻微增强中间调对比防止"糊成一片"
        private const float ContrastPreserve = 0.06f;

        // ──────────────────────────────────────────────
        // 公开入口
        // ──────────────────────────────────────────────

        /// <summary>
        /// 对单个颜色做感知灰度褪色。
        /// </summary>
        /// <param name="color">原始 sRGB 颜色（0-1 范围）</param>
        /// <param name="t">褪色进度 0=原色, 1=纯灰度</param>
        /// <returns>插值后的 sRGB 颜色，alpha 保持原值</returns>
        public static Color Fade(Color color, float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return color;

            // 1. sRGB → 线性空间
            float rLin = SRgbToLinear(color.r);
            float gLin = SRgbToLinear(color.g);
            float bLin = SRgbToLinear(color.b);

            // 2. CIE 1931 相对亮度
            float luminance = LumaR * rLin + LumaG * gLin + LumaB * bLin;

            // 3. Helmholtz–Kohlrausch 补偿
            //    饱和度 = (max - min) / max，在 RGB 线性空间近似
            float maxC = Mathf.Max(rLin, gLin, bLin);
            float minC = Mathf.Min(rLin, gLin, bLin);
            float saturation = maxC > 0.0001f ? (maxC - minC) / maxC : 0f;

            //    鲜艳色感知亮度高于物理亮度，补偿量随饱和度增加
            float perceived = luminance * (1f + saturation * HkCompensation);

            // 4. 柔和 S 曲线（中间调对比保留）
            //    y = x + contrast * (x - x^2) * (0.5 - x) * 4
            //    在 x≈0.25 压低，x≈0.75 抬高，x=0/0.5/1 不变
            float sCurve = perceived + ContrastPreserve
                * (perceived - perceived * perceived)
                * (0.5f - perceived) * 4f;

            float grayLinear = Mathf.Clamp01(sCurve);

            // 5. 线性 → sRGB 编码
            float graySRgb = LinearToSRgb(grayLinear);

            // 6. lerp 原色 → 灰度
            float grayR = Mathf.Lerp(color.r, graySRgb, t);
            float grayG = Mathf.Lerp(color.g, graySRgb, t);
            float grayB = Mathf.Lerp(color.b, graySRgb, t);

            return new Color(grayR, grayG, grayB, color.a);
        }

        /// <summary>
        /// 批量处理纹理所有像素，生成新 Texture2D（Editor 离线预览用）。
        /// </summary>
        public static Texture2D BakeTexture(Texture2D source, float t)
        {
            if (source == null) return null;

            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            var srcPixels = source.GetPixels();
            var dstPixels = new Color[srcPixels.Length];

            for (int i = 0; i < srcPixels.Length; i++)
            {
                dstPixels[i] = Fade(srcPixels[i], t);
            }

            result.SetPixels(dstPixels);
            result.Apply();
            return result;
        }

        // ──────────────────────────────────────────────
        // 色彩空间转换
        // ──────────────────────────────────────────────

        /// <summary>
        /// sRGB → 线性（IEC 61966-2-1 标准公式）。
        /// 不直接用 pow(2.2) 因为暗部会偏色。
        /// </summary>
        public static float SRgbToLinear(float c)
        {
            if (c <= 0.04045f)
                return c / 12.92f;
            return Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        /// <summary>
        /// 线性 → sRGB 编码。
        /// </summary>
        public static float LinearToSRgb(float c)
        {
            if (c <= 0.0031308f)
                return c * 12.92f;
            return 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;
        }

        // ──────────────────────────────────────────────
        // 诊断 / 调试
        // ──────────────────────────────────────────────

        /// <summary>
        /// 返回给定 sRGB 颜色在算法下的灰度值（t=1 时的结果），
        /// 方便在 Inspector 上调参时对比。
        /// </summary>
        public static float GetGrayLevel(Color color)
        {
            float rLin = SRgbToLinear(color.r);
            float gLin = SRgbToLinear(color.g);
            float bLin = SRgbToLinear(color.b);

            float luminance = LumaR * rLin + LumaG * gLin + LumaB * bLin;

            float maxC = Mathf.Max(rLin, gLin, bLin);
            float minC = Mathf.Min(rLin, gLin, bLin);
            float saturation = maxC > 0.0001f ? (maxC - minC) / maxC : 0f;
            float perceived = luminance * (1f + saturation * HkCompensation);

            float sCurve = perceived + ContrastPreserve
                * (perceived - perceived * perceived)
                * (0.5f - perceived) * 4f;

            return Mathf.Clamp01(LinearToSRgb(Mathf.Clamp01(sCurve)));
        }
    }
}
