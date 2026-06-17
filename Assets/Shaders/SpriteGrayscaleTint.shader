Shader "Game/Sprites/GrayscaleTint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        _GrayscaleAmount ("Grayscale Amount", Range(0, 1)) = 1
        _Brightness ("Brightness", Range(0, 2)) = 0.72
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            fixed _GrayscaleAmount;
            fixed _Brightness;
            fixed4 _FlashColor;
            fixed _FlashAmount;

            // ── sRGB ↔ Linear ──────────────────────────────
            fixed3 SrgbToLinear(fixed3 c)
            {
                // IEC 61966-2-1: 分两段，避免暗部偏色
                return (c <= 0.04045) ? (c / 12.92) : pow((c + 0.055) / 1.055, 2.4);
            }

            fixed3 LinearToSrgb(fixed3 c)
            {
                return (c <= 0.0031308) ? (c * 12.92) : (1.055 * pow(c, 1.0 / 2.4) - 0.055);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(IN.texcoord) * IN.color;
                fixed fade = saturate(_GrayscaleAmount);

                // ── 1. sRGB → 线性空间 ──
                fixed3 linRgb = SrgbToLinear(color.rgb);

                // ── 2. CIE 1931 相对亮度 ──
                //     绿 71.52% + 红 21.26% + 蓝 7.22%
                //     比 BT.601(0.299,0.587,0.114) 更贴合人眼光谱
                fixed luminance = dot(linRgb, fixed3(0.2126, 0.7152, 0.0722));

                // ── 3. Helmholtz–Kohlrausch 补偿 ──
                //     高饱和色感知亮度 > 物理亮度，最多提亮 12%
                fixed maxC = max(linRgb.r, max(linRgb.g, linRgb.b));
                fixed minC = min(linRgb.r, min(linRgb.g, linRgb.b));
                fixed saturation = (maxC > 0.0001) ? ((maxC - minC) / maxC) : 0.0;
                fixed perceived = luminance * (1.0 + saturation * 0.12);

                // ── 4. 柔和 S 曲线（中间调对比保留）──
                //     y = x + contrast * (x - x²) * (0.5 - x) * 4
                //     中间调 (0.25/0.75) 处 ±contrast，黑/白/中灰点不变
                fixed sCurve = perceived + 0.06
                    * (perceived - perceived * perceived)
                    * (0.5 - perceived) * 4.0;

                fixed grayLinear = saturate(sCurve);

                // ── 5. 线性 → sRGB ──
                fixed graySRgb = LinearToSrgb(fixed3(grayLinear, grayLinear, grayLinear)).r;

                // ── 6. lerp 原色 → 灰度 (t = fade) ──
                fixed3 mutedRgb = lerp(color.rgb, graySRgb.xxx, fade);

                // ── 亮度微调（保留 _Brightness 供外部控制）──
                mutedRgb *= lerp(1.0, _Brightness, fade);

                // ── 7. 闪烁提示叠加（保留原逻辑）──
                fixed3 flashBaseRgb = lerp(mutedRgb, graySRgb.xxx, 0.35);
                fixed3 flashRgb = flashBaseRgb * _FlashColor.rgb;
                fixed3 finalRgb = lerp(mutedRgb, flashRgb, saturate(_FlashAmount));
                finalRgb *= color.a;

                return fixed4(finalRgb, color.a);
            }
            ENDCG
        }
    }
}
