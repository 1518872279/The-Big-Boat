Shader "Custom/SimpleURPGlass"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (0.0, 0.5, 1.0, 0.1)
        _SpecularColor ("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularPower ("Specular Power", Range(0, 1)) = 0.8
        _Transparency ("Transparency", Range(0, 1)) = 0.5
        _EdgeThickness ("Edge Thickness", Range(0, 5)) = 1.5
        _EdgeColor ("Edge Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 300
        
        // Write to depth buffer
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
        
        // Main glass pass
        Pass
        {
            Name "Glass"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _SpecularColor;
                float _SpecularPower;
                float _Transparency;
                float _EdgeThickness;
                float4 _EdgeColor;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 viewDirWS : TEXCOORD0;
                float fogCoord : TEXCOORD1;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // Calculate specular highlight
                float3 halfVec = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVec));
                float specular = pow(NdotH, 32.0) * _SpecularPower;
                
                // Calculate fresnel for edge effect
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _EdgeThickness);
                
                // Combine colors
                float3 color = _Tint.rgb;
                
                // Add specular highlights
                color += _SpecularColor.rgb * specular;
                
                // Add edge highlighting
                color += _EdgeColor.rgb * fresnel;
                
                // Apply fog
                color = MixFog(color, input.fogCoord);
                
                // Calculate alpha - more transparent at center, more opaque at edges
                float alpha = _Transparency + fresnel * 0.5;
                alpha = saturate(alpha); // Clamp between 0 and 1
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
} 