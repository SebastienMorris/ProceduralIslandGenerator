using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static Noise;
using Vector3 = UnityEngine.Vector3;
using System;
using UnityEngine.Serialization;
using float4 = Unity.Mathematics.float4;
using Unity.Jobs;
using UnityEditor;
using UnityEngine.Serialization;

public class ChunkMarchingCube : MonoBehaviour
{
    [SerializeField] private ComputeShader marchingCubesShader;
    [SerializeField] private ComputeShader noiseShader;

    [FormerlySerializedAs("mat")] [SerializeField] private Material chunkMat;
    
    [SerializeField] private bool debug;
    
    [SerializeField] private Vector3Int dimensions = new(0, 0, 0);
    [SerializeField] private int chunkSize = 10;
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;

    [SerializeField] private bool applyFallOffMap = false;
    [SerializeField] [Range(0.1f, 10)] private float steepness = 3;
    [SerializeField] [Range(0.1f, 10)] private float centerSize = 2.2f;
    
    [SerializeField] private int numThreadsPerAxis = 8;

    private IslandJob island;
    
    private void Update() {
        if (Input.GetKeyUp(KeyCode.G))
        {
            if(!island.Equals(default(IslandJob)))
                island.ClearChunks();
            
            CreateIsland();
        }
    }

    private void OnDrawGizmos()
    {
        if (debug)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, dimensions);
        }
    }

    private void CreateIsland()
    {
        island = new IslandJob();
        
        
        island.islandObj = gameObject;
        island.chunkMat = chunkMat;

        island.marchingCubesShader = marchingCubesShader;
        island.noiseShader = noiseShader;

        island.numThreadsPerAxis = numThreadsPerAxis;

        island.globalDimensions = dimensions;
        island.chunkSize = chunkSize;
        island.surfaceLevel = surfaceLevel;

        island.noiseSettings = noiseSettings;

        island.applyFallOffMap = applyFallOffMap;
        island.steepness = steepness;
        island.centerSize = centerSize;
        
        
        island.Execute();
    }
}

struct Triangle
{
#pragma warning disable 649 // disable unassigned variable warning
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }
}

[Serializable]
public struct NoiseSettings
{
    public int seed;
    [Min(1)] public int frequency;
    [Range(1, 6)] public int octaves;

    [Range(2, 4)] public int lacunarity;

    [Range(0f, 1f)] public float persistence;

    [Range(0.1f, 2f)] public float scale;

    public static NoiseSettings Default => new NoiseSettings{frequency = 4, octaves = 1, lacunarity = 2, persistence = 0.5f, scale = 1f};
}