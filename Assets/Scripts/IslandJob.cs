using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public struct ChunkJob : IJobFor
{
    public NativeArray<float3x4> islandTriangles;
    
    public Vector3Int numChunks;
    
    public void Execute(int i)
    {
        int z = i / (numChunks.x * numChunks.y);
        int y = (i - z * numChunks.x * numChunks.y) / numChunks.x;
        int x = i - numChunks.x * (y + numChunks.y * z);
        
        var chunk = CreateChunk(new Vector3Int(x,y,z));
        //chunk.Initialise(ChunkMarchingCube.meshMaterial);
        CreateChunkMesh(chunk, GetChunkTriangles(i));
    }

    private IslandChunk CreateChunk(Vector3Int coord)
    {
        GameObject obj = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        obj.transform.localPosition = new Vector3(0f, 0f, 0f);
        //obj.transform.localPosition = new Vector3Int(coord.x * chunkSize - (dimensions.x / 2 - chunkSize / 2), coord.y * chunkSize - (dimensions.y / 2 - chunkSize / 2), coord.z * chunkSize - (dimensions.z / 2 - chunkSize / 2));
        IslandChunk chunkScript = obj.AddComponent<IslandChunk>();
        return chunkScript;
    }

    private float3x4[] GetChunkTriangles(int chunkIndex)
    {
        List<float3x4> chunkTriangles = new List<float3x4>();
        foreach (float3x4 tri in islandTriangles)
        {
            if ((int)tri.c3.x == chunkIndex)
            {
                chunkTriangles.Add(tri);
            }
        }

        return chunkTriangles.ToArray();
    }

    private void CreateChunkMesh(IslandChunk chunk, float3x4[] chunkTriangles)
    {
        int numTris = chunkTriangles.Length;
		
        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            /*for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = chunkTriangles[i][j];
            }*/
            meshTriangles[i * 3 + 0] = i * 3 + 0;
            vertices[i * 3 + 0] = chunkTriangles[i].c0;
            
            meshTriangles[i * 3 + 1] = i * 3 + 1;
            vertices[i * 3 + 1] = chunkTriangles[i].c1;
            
            meshTriangles[i * 3 + 2] = i * 3 + 2;
            vertices[i * 3 + 2] = chunkTriangles[i].c2;
            
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }
}
