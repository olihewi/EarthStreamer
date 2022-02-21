Shader "Custom/Water"
{
    Properties
    {
        _Surface ("Water Surface Colour", Color) = (0,1,1,0)
        _Depth ("Water Depth Colour", Color) = (0,0,1,0)
        _DepthFactor ("Water Depth Density", float) = 100
        _FoamAmount ("Foam Amount", float) = 2
        _SpecularColour ("Specular Colour", Color) = (1,1,1,1)
        _SpecularAmount ("Specular Amount", float) = 0.5
    }
    SubShader 
    {
        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex VertexProg
            #pragma fragment FragProg

            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityPBSLighting.cginc"

            sampler2D _CameraDepthTexture;

            float4 _Surface;
            float4 _Depth;
            float _DepthFactor;
            float _FoamAmount;
            float4 _SpecularColour;
            float _SpecularAmount;

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            VertexOutput VertexProg( VertexInput v )
            {
                VertexOutput i;
                i.position = UnityObjectToClipPos(v.position);
                i.screenPos = ComputeScreenPos(i.position);
                i.worldPos = mul(unity_ObjectToWorld, v.position);
                i.normal = float3(0,1,0);
                return i;
            }
            
            float4 Lighting(VertexOutput i)
            {
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflectionDir = reflect(-lightDir, i.normal);
                float4 specular = pow(DotClamped(viewDir,reflectionDir),(1/_SpecularAmount) * 100) * 1/_SpecularColour;

                return specular;
            }
            
            float4 FragProg( VertexOutput i ) : SV_TARGET
            {
                float4 lighting = Lighting(i);
                
                float depthMap = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos)).r - i.screenPos.w;
                float4 depth = lerp(_Surface, _Depth, saturate(depthMap / _DepthFactor));
                float4 foam = saturate(1-depthMap / _FoamAmount);
                return clamp((depth + foam),0,1) + lighting;
            }

            
            
            ENDCG
        }

    }
}
