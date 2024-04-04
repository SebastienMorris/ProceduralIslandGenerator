using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Noise;

public struct IslandJob : IJob
{
    public GameObject islandObj;
    public Material chunkMat;

    public ComputeShader marchingCubesShader;
    public ComputeShader noiseShader;

    public int numThreadsPerAxis;
    
    public Vector3Int globalDimensions;
    public int chunkSize;
    public float surfaceLevel;
    
    public NoiseSettings noiseSettings;

    public bool applyFallOffMap;
    public float steepness;
    public float centerSize;


    private ComputeBuffer triangleBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer triCountBuffer;
    private ComputeBuffer noiseBuffer;

    private int numPointsPerChunk;
    
    private List<IslandChunk> chunks;
    
    public void Execute()
    {
        chunks = new List<IslandChunk>();
        numPointsPerChunk = (chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1);
        int numVoxelsPerAxis = chunkSize;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        Vector3Int numChunks = new(globalDimensions.x / chunkSize, globalDimensions.y / chunkSize, globalDimensions.z / chunkSize);

        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        noiseBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float) * 4, ComputeBufferType.Append);

        // Go through all coords and create a chunk there if one doesn't already exist
        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    var chunk = CreateChunk(coord);
                    chunk.Initialise(chunkMat);
                    UpdateChunk(chunk);
                    chunks.Add(chunk);
                }
            }
        }

        pointsBuffer.Release();
        triangleBuffer.Release();
        pointsBuffer = null;
        triangleBuffer = null;

        noiseBuffer.Release();
        noiseBuffer = null;
    }
    
    IslandChunk CreateChunk(Vector3Int coord)
    {
        GameObject obj = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        obj.transform.parent = islandObj.transform;
        obj.transform.localPosition = new Vector3Int(coord.x * chunkSize - (globalDimensions.x / 2 - chunkSize / 2),
            coord.y * chunkSize - (globalDimensions.y / 2 - chunkSize / 2),
            coord.z * chunkSize - (globalDimensions.z / 2 - chunkSize / 2));
        IslandChunk chunkScript = obj.AddComponent<IslandChunk>();
        chunkScript.coord = coord;
        return chunkScript;
    }

    private void UpdateChunk(IslandChunk chunk)
    {
        float4[] posAndNoise = new float4[numPointsPerChunk];

        CreateNoise(posAndNoise, float3(chunkSize, chunkSize, chunkSize), chunk);

        pointsBuffer.SetData(posAndNoise);

        triangleBuffer.SetCounterValue(0);
        marchingCubesShader.SetBuffer(0, Shader.PropertyToID("points"), pointsBuffer);
        marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
        marchingCubesShader.SetInt(Shader.PropertyToID("numPointsPerAxis"), chunkSize + 1);
        marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);

        marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }
    
    private void CreateNoise(float4[] posAndNoise, float3 dimensions, IslandChunk chunk)
    {
        noiseBuffer.SetCounterValue(0);
        noiseShader.SetBuffer(0, Shader.PropertyToID("posAndNoise"), noiseBuffer);

        noiseShader.SetInt(Shader.PropertyToID("numPointsPerAxis"), chunkSize + 1);

        noiseShader.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
        noiseShader.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
        noiseShader.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
        noiseShader.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
        noiseShader.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);

        noiseShader.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);

        noiseShader.SetFloat(Shader.PropertyToID("steepness"), steepness);
        noiseShader.SetFloat(Shader.PropertyToID("centerSize"), centerSize);
        noiseShader.SetBool(Shader.PropertyToID("applyFallOff"), applyFallOffMap);

        noiseShader.SetVector(Shader.PropertyToID("dimensions"), float4(dimensions, 0f));
        noiseShader.SetVector(Shader.PropertyToID("globalDimensions"),
            float4(globalDimensions.x, globalDimensions.y, globalDimensions.z, 0f));
        noiseShader.SetVector(Shader.PropertyToID("localPos"), float4(chunk.transform.localPosition, 0f));
        noiseShader.SetVector(Shader.PropertyToID("globalPos"), float4(chunk.transform.position, 0f));

        noiseShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        noiseBuffer.GetData(posAndNoise, 0, 0, numPointsPerChunk);
    }
    
    public void ClearChunks()
    {
        foreach (var chunk in chunks)
            chunk.Delete();

        chunks.Clear();
    }
}