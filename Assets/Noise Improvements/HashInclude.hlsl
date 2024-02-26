#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<uint> _Hashes;
    StructuredBuffer<float3> _Positions;
#endif

float4 _Config;

[numthreads(8, 8, 1)]
void ConfigureProcedural()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    unity_ObjectToWorld = 0.0;
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(_Positions[unity_InstanceID], 1.0);
    unity_ObjectToWorld._m13 += _Config.z * ((_Hashes[unity_InstanceID] >> 24) / 255.0 - 0.5);
    unity_ObjectToWorld._m00_m11_m22 = _Config.y;
		
    #endif
}

float3 GetHashColour()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        uint hash = _Hashes[unity_InstanceID];
        return float3(hash & 255, (hash >> 8) & 255, (hash >> 16) & 255) / 255.0;
    #else
        return 1.0;
    #endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 colour)
{
    Out = In;
    colour = GetHashColour();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 colour)
{
    Out = In;
    colour = GetHashColour();
}
