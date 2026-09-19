Shader "Spooky/VhsDistortion"
{
    // Fullscreen VHS / cassette-tape pass. Driven by VhsController, which
    // rolls every artifact off a single _Strength so the whole look can be
    // dialed from one number. See Docs/Shaders.md.
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Strength ("Effect Strength", Range(0, 1)) = 0.5

        _WobbleAmount ("Wobble Amount", Range(0, 0.05)) = 0.006
        _WobbleSpeed ("Wobble Speed", Range(0, 20)) = 4
        _WobbleFrequency ("Wobble Frequency", Range(0, 200)) = 60

        _BandSize ("Tracking Band Size", Range(0, 0.5)) = 0.08
        _BandOffset ("Tracking Band Offset", Range(0, 0.1)) = 0.02
        _BandSpeed ("Tracking Band Speed", Range(-2, 2)) = 0.35

        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.003
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.25
        _ScanlineCount ("Scanline Count", Range(0, 1200)) = 320
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.12
        _Desaturation ("Desaturation", Range(0, 1)) = 0.3
        _Vignette ("Vignette", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "VhsDistortion"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Blit.hlsl lives in core, not universal — it supplies Vert,
            // Varyings and _BlitTexture for fullscreen passes.
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;
            float _WobbleAmount, _WobbleSpeed, _WobbleFrequency;
            float _BandSize, _BandOffset, _BandSpeed;
            float _Aberration, _ScanlineStrength, _ScanlineCount;
            float _NoiseStrength, _Desaturation, _Vignette;

            // Cheap hash noise. A texture lookup would be steadier, but this
            // keeps the effect self-contained with no asset to wire up.
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float3 SampleSource(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // Nothing on: return the frame untouched so the pass is free
                // to leave enabled at zero strength.
                if (_Strength <= 0.001)
                {
                    return half4(SampleSource(uv), 1);
                }

                float s = saturate(_Strength);
                float time = _Time.y;

                // --- Horizontal wobble: the tape not tracking straight ---
                float wobble = sin(uv.y * _WobbleFrequency + time * _WobbleSpeed)
                             * _WobbleAmount * s;
                uv.x += wobble;

                // --- Tracking band: a slab of misaligned tape drifting up ---
                float bandPosition = frac(time * _BandSpeed);
                float distanceToBand = abs(uv.y - bandPosition);
                float band = 1.0 - smoothstep(0.0, max(_BandSize, 1e-5), distanceToBand);
                uv.x += band * _BandOffset * s * (Hash(float2(bandPosition, time)) - 0.5) * 2.0;

                // --- Chromatic aberration: split R and B off center ---
                float aberration = _Aberration * s * (1.0 + band);
                float3 color;
                color.r = SampleSource(float2(uv.x + aberration, uv.y)).r;
                color.g = SampleSource(uv).g;
                color.b = SampleSource(float2(uv.x - aberration, uv.y)).b;

                // --- Desaturate toward tape washout ---
                float luma = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, luma.xxx, _Desaturation * s);

                // --- Scanlines ---
                float scanline = sin(uv.y * _ScanlineCount * 3.14159);
                color *= 1.0 - (scanline * 0.5 + 0.5) * _ScanlineStrength * s;

                // --- Grain ---
                float noise = Hash(uv * 512.0 + time * 60.0) - 0.5;
                color += noise * _NoiseStrength * s;

                // --- Vignette ---
                float2 centered = uv - 0.5;
                float vignette = 1.0 - dot(centered, centered) * _Vignette * s * 2.0;
                color *= saturate(vignette);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
