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

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(IN.texcoord) * IN.color;
                fixed gray = dot(color.rgb, fixed3(0.299, 0.587, 0.114));
                fixed fade = saturate(_GrayscaleAmount);
                fixed3 grayscaleRgb = gray.xxx;

                // 目标是“褪色”而不是“黑白”：
                // 1. 明显降低饱和度
                // 2. 轻微降低亮度
                // 3. 始终保留原图颜色关系，不往纯白推
                fixed desaturateAmount = fade * 0.65;
                fixed brightnessScale = lerp(1.0, _Brightness, fade);
                fixed3 mutedRgb = lerp(color.rgb, grayscaleRgb, desaturateAmount) * brightnessScale;

                // 闪烁提示仍然按目标色覆盖，但保留一点原图明暗关系。
                fixed3 flashBaseRgb = lerp(mutedRgb, grayscaleRgb, 0.35);
                fixed3 flashRgb = flashBaseRgb * _FlashColor.rgb;
                fixed3 finalRgb = lerp(mutedRgb, flashRgb, saturate(_FlashAmount));
                finalRgb *= color.a;
                return fixed4(finalRgb, color.a);
            }
            ENDCG
        }
    }
}
