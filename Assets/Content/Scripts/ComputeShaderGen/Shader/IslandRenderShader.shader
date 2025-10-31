Shader "Unlit/IslandRenderShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "darkgreen" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
            };

            struct Vertex
            {
                float3 position;
                float3 normal;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 shadowCoords : TEXCOORD3;
            };

            StructuredBuffer<Vertex> _VertexBuffer;
            float4 origin;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                
                float3 vertPos = _VertexBuffer[v.vertexID].position + float3(origin.x, origin.y, origin.z);

                VertexPositionInputs posInputs = GetVertexPositionInputs(vertPos);
                
                o.vertex = posInputs.positionCS;
                o.normal = TransformObjectToWorldNormal(_VertexBuffer[v.vertexID].normal);
                o.worldPos = posInputs.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.shadowCoords = GetShadowCoord(posInputs);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 albedo = tex2D(_MainTex, i.uv);
                
                float3 normalWS = normalize(i.normal);
                
                Light mainLight = GetMainLight(i.shadowCoords);
                float3 mainLightColor = mainLight.color * mainLight.shadowAttenuation * max(dot(mainLight.direction, normalWS), 0.0);
                float3 lighting = mainLightColor;
                
                return float4(albedo + lighting, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
            };

            struct Vertex
            {
                float3 position;
                float3 normal;
            };

            StructuredBuffer<Vertex> _VertexBuffer;
            float4 origin;

            float3 _LightDirection;

            float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS)
            {
                float3 lightDirectionWS = _LightDirection;
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Interpolators vert(Attributes input)
            {
                Interpolators output;

                float3 vertPos = _VertexBuffer[input.vertexID].position + float3(origin.x, origin.y, origin.z);
                float3 normal = _VertexBuffer[input.vertexID].normal;

                VertexPositionInputs posnInputs = GetVertexPositionInputs(vertPos);
                VertexNormalInputs normInputs = GetVertexNormalInputs(normal);

                output.positionCS = GetShadowCasterPositionCS(posnInputs.positionWS, normInputs.normalWS);
                return output;
            }

            float4 frag(Interpolators input) : SV_TARGET
            {
                return 0;
            }

            ENDHLSL
        }
    }
}