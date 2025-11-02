Shader "Unlit/IslandRenderShader"
{
    Properties
    {
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
            };

            struct Vertex
            {
                float3 position;
                float3 normal;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 normalOS : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float4 shadowCoords : TEXCOORD4;
                float3 posOS : TEXCOORD5;
            };

            StructuredBuffer<Vertex> _VertexBuffer;
            float4 _Origin;

            float _ShadowStrength;

            sampler2D _GrassTex;
            sampler2D _GroundTex;
            float _GrassBlend;

            float3 GetTriplanarWeights(float3 normal)
            {
                float3 triW = abs(normal);

                return triW / (triW.x + triW.y + triW.z);
            }

            float3 TriplanarMapping(sampler2D Tex, float3 posOS, float3 normal)
            {
                float2 uvA = posOS.yz;
                float2 uvB = posOS.xz;
                float2 uvC = posOS.xy;

                if(normal.x < 0)
                {
                    uvA.x = -uvA.x;
                }
                if(normal.y < 0)
                {
                    uvB.x = -uvB.x;
                }
                if(normal.z >= 0)
                {
                    uvC.x = -uvC.x;
                }

                uvA.y += 0.5;
                uvC.x += 0.5;
                
                float3 texColourA = tex2D(Tex, uvA);
                float3 texColourB = tex2D(Tex, uvB);
                float3 texColourC = tex2D(Tex, uvC);

                float3 triW = GetTriplanarWeights(normal);

                return texColourA * triW.x + texColourB * triW.y + texColourC * triW.z;
            }

            float3 BlendTextures(float3 normal, float3 colourA, float3 colourB)
            {
                
                float3 coef = normal / 2 + 0.5f * (_GrassBlend * 2);
                float3 inverseCoef = 1 - coef;
                
                return colourA * coef + colourB * inverseCoef;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                float3 vertPos = _VertexBuffer[v.vertexID].position + float3(_Origin.x, _Origin.y, _Origin.z);

                VertexPositionInputs posInputs = GetVertexPositionInputs(vertPos);
                
                o.vertex = posInputs.positionCS;
                o.normalWS = TransformObjectToWorldNormal(_VertexBuffer[v.vertexID].normal);
                o.normalOS = _VertexBuffer[v.vertexID].normal;
                o.worldPos = posInputs.positionWS;
                o.shadowCoords = GetShadowCoord(posInputs);

                o.posOS = _VertexBuffer[v.vertexID].position;
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 normalWS = normalize(i.normalWS);
                float3 normalOS = normalize(i.normalOS);
                
                float3 texColourGrass = TriplanarMapping(_GrassTex, i.posOS, normalOS);
                float3 texColourGround = TriplanarMapping(_GroundTex, i.posOS, normalOS);

                float3 texColour = BlendTextures(normalOS, texColourGrass, texColourGround);
                
                Light mainLight = GetMainLight(i.shadowCoords);
                float3 mainLightColor = mainLight.color * mainLight.shadowAttenuation * max(dot(mainLight.direction, normalWS), 0.0);
                float3 lighting = mainLightColor;
                
               return float4(texColour * (lighting + (1 - _ShadowStrength)), 1.0);
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
            float4 _Origin;

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

                float3 vertPos = _VertexBuffer[input.vertexID].position + float3(_Origin.x, _Origin.y, _Origin.z);
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