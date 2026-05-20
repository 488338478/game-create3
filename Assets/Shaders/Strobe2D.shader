Shader "Game/PostProcess/Strobe2D"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float _Intensity;
    float _FlickerStrength;
    float _FlickerFrequency;
    float _NoiseDensity;
    float _RGBShiftAmount;
    float _ScanlineIntensity;
    float _ScanlineFrequency;
    float _TearAmount;
    float _TearFrequency;
    float4 _ColorShift;
    float _TimeSeed;

    float Hash11(float n)
    {
        return frac(sin(n * 12.9898) * 43758.5453);
    }

    float Hash21(float2 p)
    {
        p = frac(p * float2(123.34, 456.21));
        p += dot(p, p + 45.32);
        return frac(p.x * p.y);
    }

    float4 FragStrobe(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;
        float t  = _Time.y + _TimeSeed;

        // -------- Screen tear (occasional horizontal band shift) --------
        float tearPhase = floor(t * max(_TearFrequency, 0.0001));
        float bandSeed  = Hash11(tearPhase + floor(uv.y * 30.0) + _TimeSeed);
        float tearMask  = step(0.85, bandSeed);
        float tearShift = (bandSeed - 0.5) * _TearAmount * tearMask * _Intensity;
        uv.x += tearShift;

        // -------- RGB shift --------
        float shift = _RGBShiftAmount * _Intensity / max(_ScreenParams.x, 1.0);
        float3 col;
        col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(shift, 0)).r;
        col.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
        col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(shift, 0)).b;

        // -------- Brightness flicker (sin + random sub-flicker) --------
        float sineFlicker   = sin(t * _FlickerFrequency * 6.2831853);
        float randomFlicker = Hash11(floor(t * _FlickerFrequency * 4.0)) * 2.0 - 1.0;
        float flicker = 1.0 + (sineFlicker * 0.7 + randomFlicker * 0.3) * _FlickerStrength * _Intensity;
        col *= flicker;

        // -------- Snow noise --------
        float n = Hash21(uv * _ScreenParams.xy + t * 137.0);
        col = lerp(col, float3(n, n, n), _NoiseDensity * _Intensity);

        // -------- Scanlines --------
        float scan = sin(uv.y * _ScanlineFrequency * 6.2831853) * 0.5 + 0.5;
        col *= lerp(1.0, scan, _ScanlineIntensity * _Intensity);

        // -------- Color tint blend --------
        col = lerp(col, col * _ColorShift.rgb, _ColorShift.a * _Intensity);

        return float4(col, 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Strobe2D"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragStrobe
            ENDHLSL
        }
    }

    Fallback Off
}
