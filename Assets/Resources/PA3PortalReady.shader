Shader "PA3/PortalReady"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float edge = saturate(1.0 - length(centered * float2(0.9, 0.8)));
                float wave = sin(input.uv.y * 12.0 + _Time.y * 2.4) * 0.5 + 0.5;
                float shimmer = sin(input.uv.x * 17.0 - input.uv.y * 9.0 + _Time.y * 1.7) * 0.5 + 0.5;
                float3 cyan = float3(0.12, 2.2, 2.8);
                float3 violet = float3(1.3, 0.35, 2.3);
                float3 color = lerp(cyan, violet, saturate(wave * 0.7 + shimmer * 0.3));
                return half4(color, edge * (0.42 + shimmer * 0.18));
            }
            ENDHLSL
        }
    }
}
