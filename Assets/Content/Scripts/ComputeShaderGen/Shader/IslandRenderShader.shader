Shader "Unlit/IslandRenderShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            };

            struct Vertex
            {
                float3 position;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            StructuredBuffer<Vertex> _VertexBuffer;
            float4 origin;

            /*sampler2D _MainTex;
            float4 _MainTex_ST;*/

            v2f vert (appdata v)
            {
                v2f o;
                float3 vertPos = _VertexBuffer[v.vertexID].position + float3(origin.x, origin.y, origin.z);
                o.vertex = UnityObjectToClipPos(float4(vertPos, 1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(0, 0, 0, 1);
            }
            ENDCG
        }
    }
}
