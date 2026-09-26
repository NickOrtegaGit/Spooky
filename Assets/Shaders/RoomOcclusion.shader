Shader "Spooky/RoomOcclusion"
{
    // Blacks out everything outside the current room. Two passes:
    //
    //   0 — draw the room mesh, writing 1 into the stencil buffer. No color.
    //   1 — draw a full-screen quad wherever the stencil is NOT 1.
    //
    // The room polygon can be concave; the stencil does not care about shape,
    // only about which pixels the mesh covered. See Docs/House Layout.md.
    Properties
    {
        _Color ("Occlusion Color", Color) = (0, 0, 0, 1)
        _StencilRef ("Stencil Ref", Int) = 1
        _FadeAlpha ("Fade Alpha", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RoomStencilWrite"

            // Shape only — the color buffer is untouched.
            ColorMask 0
            ZWrite Off
            ZTest Always
            Cull Off

            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "RoomOcclusionFill"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            // Paint only where the room mesh did NOT write.
            Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                // Fullscreen triangle straight in clip space — no mesh needed.
                OUT.positionHCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return half4(_Color.rgb, _Color.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "RoomFadeOut"

            // Blacks out the room just left, ramping in over the transition so
            // the two rooms cross-fade instead of the screen blinking.
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;
            float _FadeAlpha;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                return half4(_Color.rgb, _Color.a * _FadeAlpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
