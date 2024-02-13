#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<uint> _Hashes;
#endif

float4 _Config;

float3 GetHashColour()
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        uint hash = _Hashes[unity_InstanceID];
        return _Config.y * _Config.y * hash;
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