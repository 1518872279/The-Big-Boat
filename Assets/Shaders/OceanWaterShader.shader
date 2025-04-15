Shader "Custom/OceanWaterShader"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.3, 0.6, 0.8)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _WaveA ("Wave A (dir, steepness, wavelength)", Vector) = (1, 0, 0.5, 10)
        _WaveB ("Wave B (dir, steepness, wavelength)", Vector) = (0, 1, 0.25, 20)
        _WaveC ("Wave C (dir, steepness, wavelength)", Vector) = (1, 1, 0.15, 10)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaterDepth ("Water Depth", Float) = 1.0
        _DepthGradientShallow ("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)
        _DepthGradientDeep ("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
        _DepthMaxDistance ("Depth Maximum Distance", Float) = 1.0
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamMaxDistance ("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance ("Foam Minimum Distance", Float) = 0.04
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        // Depth pre-pass
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
            Name "OceanWater"
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
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                half4 _Color;
                half _Glossiness;
                half _Metallic;
                float4 _WaveA;
                float4 _WaveB;
                float4 _WaveC;
                float _WaveSpeed;
                float _WaterDepth;
                half4 _DepthGradientShallow;
                half4 _DepthGradientDeep;
                float _DepthMaxDistance;
                half4 _FoamColor;
                float _FoamMaxDistance;
                float _FoamMinDistance;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float4 tangentWS : TANGENT;
                float fogCoord : TEXCOORD2;
            };
            
            // Function to generate a Gerstner wave
            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
            {
                float steepness = wave.z;
                float wavelength = wave.w;
                float k = 2 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(wave.xy);
                float f = k * (dot(d, p.xz) - c * _Time.y * _WaveSpeed);
                float a = steepness / k;
                
                // Tangent
                tangent += float3(
                    -d.x * d.x * steepness * sin(f),
                    d.x * steepness * cos(f),
                    -d.x * d.y * steepness * sin(f)
                );
                
                // Binormal
                binormal += float3(
                    -d.x * d.y * steepness * sin(f),
                    d.y * steepness * cos(f),
                    -d.y * d.y * steepness * sin(f)
                );
                
                // Return the displaced position
                return float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 gridPoint = input.positionOS.xyz;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                
                // Apply multiple Gerstner waves
                p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
                
                // Apply the displacement to vertex position
                float4 positionOS = float4(p, 1.0);
                
                // Calculate normal from the tangent and binormal
                float3 normalOS = normalize(cross(binormal, tangent));
                
                // Transform to world space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS, input.tangentOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                
                float4 tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.tangentWS = tangentWS;
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample textures
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                // Normal mapping
                half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                half3 normalTS = UnpackNormal(normalSample);
                
                // Apply additional normal map details for animation
                float2 uv1 = input.uv + _Time.y * 0.05;
                float2 uv2 = input.uv - _Time.y * 0.05;
                half3 normalA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                half3 normalB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                
                normalTS = normalize(normalTS + normalA * 0.5 + normalB * 0.3);
                
                // Convert normal from tangent to world space
                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                
                normalWS = mul(normalTS, tangentToWorld);
                
                // Water depth coloring
                float depth = _WaterDepth;
                float depthDifference = saturate(depth / _DepthMaxDistance);
                half4 depthColor = lerp(_DepthGradientShallow, _DepthGradientDeep, depthDifference);
                
                // Foam around collision points
                float foamDistance = depth;
                float foamDifference = saturate((foamDistance - _FoamMinDistance) / (_FoamMaxDistance - _FoamMinDistance));
                half4 foam = _FoamColor * (1 - foamDifference);
                
                // Get main light
                Light mainLight = GetMainLight();
                
                // Calculate specular highlight
                float3 viewDirectionWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfVec = normalize(mainLight.direction + viewDirectionWS);
                float NdotH = saturate(dot(normalWS, halfVec));
                float specular = pow(NdotH, 32.0) * _Glossiness;
                
                // Final color
                float3 color = lerp(depthColor.rgb + foam.rgb, albedo.rgb, 0.65);
                
                // Add specular highlight
                color += specular * 0.5;
                
                // Apply lighting
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                color *= mainLight.color * (NdotL * 0.5 + 0.5); // Half lambert lighting
                
                // Apply fog
                color = MixFog(color, input.fogCoord);
                
                // Final alpha
                float alpha = lerp(depthColor.a, albedo.a, 0.5) + foam.a * 0.1;
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
} 