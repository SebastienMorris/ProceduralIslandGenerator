Shader "Unlit/IslandRenderShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = " darkgreen" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            // make fog work
            //#pragma multi_compile_fog

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

            float3 lightPos;
            float3 lightColour;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                float3 vertPos = _VertexBuffer[v.vertexID].position + float3(origin.x, origin.y, origin.z);
                o.vertex = TransformObjectToHClip(float4(vertPos, 1));

                o.normal =  TransformObjectToWorldNormal(_VertexBuffer[v.vertexID].normal);
                o.worldPos = TransformObjectToWorld(_VertexBuffer[v.vertexID].position);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.shadowCoords = GetShadowCoord(GetVertexPositionInputs(_VertexBuffer[v.vertexID].position));
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                //sample the texture
                //ixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);

                float3 lightDir = normalize(lightPos - i.worldPos);
                float3 lightCol = max(dot(lightDir, i.normal), 0.0) * lightColour;

                half shadowAmount = MainLightRealtimeShadow(i.shadowCoords);
                
                return float4(lightCol * shadowAmount, 1.0);
            }
            ENDHLSL
        }
    }
}
