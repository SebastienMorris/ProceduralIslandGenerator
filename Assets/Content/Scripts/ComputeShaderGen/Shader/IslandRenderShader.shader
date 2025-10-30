Shader "Unlit/IslandRenderShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = " darkgreen" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

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
            };

            StructuredBuffer<Vertex> _VertexBuffer;
            uniform float4 origin;

            uniform float3 lightPos;
            uniform float3 lightColour;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                float3 vertPos = _VertexBuffer[v.vertexID].position + float3(origin.x, origin.y, origin.z);
                o.vertex = UnityObjectToClipPos(float4(vertPos, 1));
                
                
                //float3 lightDir = normalize(lightPos - _VertexBuffer[v.vertexID].position);
                //loat3 lightCol = max(dot(lightDir, _VertexBuffer[v.vertexID].normal), 0.0) * lightColour;

                o.normal =  UnityObjectToWorldNormal(_VertexBuffer[v.vertexID].normal);
                o.worldPos = mul(unity_ObjectToWorld, _VertexBuffer[v.vertexID].position);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);

                float3 lightDir = normalize(lightPos - i.worldPos);
                float3 lightCol = max(dot(lightDir, i.normal), 0.0) * lightColour;
                
                return float4(lightCol * col.xyz, 1.0);
            }
            ENDCG
        }
    }
}
