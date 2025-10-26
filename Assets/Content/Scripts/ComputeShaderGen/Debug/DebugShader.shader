Shader "Unlit/DebugShader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {

		Tags { "RenderType"="Opaque" }
        LOD 100

		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			
			StructuredBuffer<float4> Positions;
			float debugZoom;
			//float scale;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 colour : COLOR;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				float3 centreWorld = float3(Positions[instanceID].xyz) * debugZoom;
				float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex);
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));
				
				v2f o;
				o.pos = UnityObjectToClipPos(objectVertPos);
				float col = Positions[instanceID].w < 0 ? 0 : Positions[instanceID].w;
				o.colour = float4(col, col, col, 1);

				return o;
			}


			float4 frag (v2f i) : SV_Target
			{
				return i.colour;
			}
			
			ENDCG
		}
	}
}