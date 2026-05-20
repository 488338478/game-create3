Shader "Game/Test/Strobe2DTest"
{
    // 独立测试版：用 _MainTex + 标准 vert，挂在 Quad/Sprite 上即可，不依赖 URP Blit.hlsl。
    Properties
    {
        _MainTex            ("Source",          2D)     = "white" {}
        _Intensity          ("Intensity",       Range(0,1)) = 1
        _FlickerStrength    ("FlickerStrength", Range(0,1)) = 0.6
        _FlickerFrequency   ("FlickerFreq",     Float)  = 12
        _NoiseDensity       ("NoiseDensity",    Range(0,1)) = 0.3
        _RGBShiftAmount     ("RGBShiftPx",      Float)  = 8
        _ScanlineIntensity  ("ScanlineInt",     Range(0,1)) = 0.5
        _ScanlineFrequency  ("ScanlineFreq",    Float)  = 400
        _TearAmount         ("TearAmount",      Range(0,1)) = 0.2
        _TearFrequency      ("TearFreq",        Float)  = 5
        _ColorShift         ("ColorShift",      Color)  = (1,1,1,0)
        _TimeSeed           ("TimeSeed",        Float)  = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _Intensity, _FlickerStrength, _FlickerFrequency, _NoiseDensity;
            float _RGBShiftAmount, _ScanlineIntensity, _ScanlineFrequency;
            float _TearAmount, _TearFrequency, _TimeSeed;
            float4 _ColorShift;
            float4 _MainTex_ST;

            struct Attributes { float4 posOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 posCS:SV_POSITION; float2 uv:TEXCOORD0; };

            float Hash11(float n){ return frac(sin(n*12.9898)*43758.5453); }
            float Hash21(float2 p){ p=frac(p*float2(123.34,456.21)); p+=dot(p,p+45.32); return frac(p.x*p.y); }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.posCS = TransformObjectToHClip(IN.posOS.xyz);
                o.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t   = _Time.y + _TimeSeed;

                // tear
                float tearPhase = floor(t * max(_TearFrequency, 0.0001));
                float bandSeed  = Hash11(tearPhase + floor(uv.y * 30.0) + _TimeSeed);
                float tearMask  = step(0.85, bandSeed);
                float tearShift = (bandSeed - 0.5) * _TearAmount * tearMask * _Intensity;
                uv.x += tearShift;

                // rgb shift
                float shift = _RGBShiftAmount * _Intensity * 0.001;
                float3 col;
                col.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(shift,0)).r;
                col.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                col.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(shift,0)).b;
                float  a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                // flicker
                float sineFlicker   = sin(t * _FlickerFrequency * 6.2831853);
                float randomFlicker = Hash11(floor(t * _FlickerFrequency * 4.0)) * 2.0 - 1.0;
                float flicker = 1.0 + (sineFlicker*0.7 + randomFlicker*0.3) * _FlickerStrength * _Intensity;
                col *= flicker;

                // noise
                float n = Hash21(uv * 1024.0 + t * 137.0);
                col = lerp(col, float3(n,n,n), _NoiseDensity * _Intensity);

                // scanline
                float scan = sin(uv.y * _ScanlineFrequency * 6.2831853) * 0.5 + 0.5;
                col *= lerp(1.0, scan, _ScanlineIntensity * _Intensity);

                // color tint
                col = lerp(col, col * _ColorShift.rgb, _ColorShift.a * _Intensity);

                return float4(col, a);
            }
            ENDHLSL
        }
    }
}
