Shader "Custom/URPGlassShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.9, 0.9, 0.9, 0.2)
        _Smoothness("Smoothness", Range(0, 1)) = 0.95
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _RimColor("Rim Color", Color) = (0.8, 0.9, 1.0, 0.6)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.0
        _DistortionStrength("Distortion Strength", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        // First pass to write to depth buffer
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Main pass
        Pass
        {
            Name "GlassForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 viewDirWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float fogCoord : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
                half4 _RimColor;
                half _RimPower;
                half _DistortionStrength;
            CBUFFER_END

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Transform positions and normals
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                
                // Calculate view direction in world space
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                // Pass screen position for refraction
                output.screenPos = vertexInput.positionNDC;
                
                // Add fog
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Calculate rim effect (fresnel)
                half rimFactor = 1.0 - saturate(dot(normalWS, viewDirWS));
                half3 rimColor = _RimColor.rgb * pow(rimFactor, _RimPower);

                // Screen UV with distortion based on view angle
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 distortion = normalWS.xy * _DistortionStrength * rimFactor;
                float2 refractedUV = screenUV + distortion;

                // Sample the background texture (what's behind the glass)
                half4 background = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV);

                // Mix base color with background and rim effect
                half3 color = lerp(background.rgb, _BaseColor.rgb, _BaseColor.a * 0.5);
                color += rimColor * _RimColor.a;

                // Apply fog
                color = MixFog(color, input.fogCoord);

                // Final alpha based on base color and enhanced at edges
                half alpha = _BaseColor.a + pow(rimFactor, _RimPower * 0.5) * 0.3;
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
} 
