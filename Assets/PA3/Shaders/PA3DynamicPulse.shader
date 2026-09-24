Shader "PA3/DynamicPulse"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.1,0.75,1,1)
        _PulseSpeed ("Pulse Speed", Float) = 2
        _EmissionStrength ("Emission Strength", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _PulseSpeed;
                float _EmissionStrength;
            CBUFFER_END
            Varyings vert(Attributes input) { Varyings o; o.positionHCS = TransformObjectToHClip(input.positionOS.xyz); return o; }
            half4 frag(Varyings input) : SV_Target
            {
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float3 color = _BaseColor.rgb * (0.65 + pulse * 0.75);
                return half4(color + _BaseColor.rgb * pulse * _EmissionStrength * 0.25, 1);
            }
            ENDHLSL
        }
    }
}
