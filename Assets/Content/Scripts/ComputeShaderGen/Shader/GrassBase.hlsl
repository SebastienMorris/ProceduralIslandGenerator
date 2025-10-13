#ifndef ISLAND_INCLUDED
#define ISLAND_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "NMGIslandGraphicsHelpers.hlsl"

struct DrawVertex {
    float3 worldPosition;
    //float3 normal;
};

struct DrawTriangle {
    DrawVertex vertices[3];
};

StructuredBuffer<DrawTriangle> DrawTriangles;

struct VertexOutput
{
    float uv : TEXCOORD0;
    float3 position : TEXCOORD1;
    //float3 normal : TEXCOORD2;

    float4 positionClipSpace : SV_POSITION;
};

float4 _BaseColor;
float4 _TipColor;

VertexOutput Vertex(uint vertexID: SV_VertexID)
{
    VertexOutput output = (VertexOutput)0;

    DrawTriangle tri = DrawTriangles[vertexID / 3];
    DrawVertex input = tri.vertices[vertexID % 3];

    output.position = input.worldPosition;
    //output.normal = input.normal;
    //output.uv = input.height;
    output.positionClipSpace = TransformWorldToHClip(input.worldPosition);

    return output;
}

half4 Fragment(VertexOutput input) : SV_Target
{
    InputData lightingInput = (InputData)0;
    lightingInput.positionWS = input.position;
    //lightingInput.normalWS = input.normal;
    lightingInput.viewDirectionWS = GetViewDirectionFromPosition(input.position);
    lightingInput.shadowCoord = CalculateShadowCoord(input.position, input.positionClipSpace);

    float colorLerp = input.uv;
    float3 albedo = lerp(_BaseColor, _TipColor.rgb, input.uv);

    return UniversalFragmentBlinnPhong(lightingInput, albedo, 1, 0, 0, 1, 0);
    
}

#endif