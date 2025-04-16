Shader "Custom/SimpleURPGlass"
{
    Properties
    {
        _Tint("Tint", Color) = (0.0, 0.7, 1.0, 1.0)
        _SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularPower("Specular Power", Range(0, 128)) = 64
        _Transparency("Transparency", Range(0, 1)) = 0.5
        _EdgeThickness("Edge Thickness", Range(0, 5)) = 1.0
        _EdgeColor("Edge Color", Color) = (0.0, 0.7, 1.0, 1.0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
                float3 viewDirWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fresnel : TEXCOORD2;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _SpecularColor;
                float _SpecularPower;
                float _Transparency;
                float _EdgeThickness;
                float4 _EdgeColor;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Transform position
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                
                // Transform normal
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Calculate view direction
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                
                // Calculate fresnel
                output.fresnel = 1.0 - saturate(dot(output.viewDirWS, output.normalWS));
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors for lighting calculations
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Base color with tint
                float4 color = _Tint;
                
                // Edge highlighting
                float edgeEffect = pow(input.fresnel, _EdgeThickness);
                color.rgb = lerp(color.rgb, _EdgeColor.rgb, edgeEffect);
                
                // Get main light
                Light mainLight = GetMainLight();
                
                // Calculate specular reflection
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                float specularIntensity = pow(saturate(dot(normalWS, halfVector)), _SpecularPower);
                color.rgb += _SpecularColor.rgb * specularIntensity;
                
                // Apply transparency
                color.a = lerp(_Transparency, 1.0, edgeEffect * 0.5);
                
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
} 