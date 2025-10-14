#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<float3> Postions;
#endif

[numthreads(8, 8, 1)]
void GenerateVisuals(uint3 id : SV_DispatchThreadID)
{
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    unity_ObjectToWorld = 0.0;
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(_Positions[unity_InstanceID], 1.0);
    #endif
}

void ShaderGraphPositions_float(float3 In, out float3 Out)
{
    Out = In;
}