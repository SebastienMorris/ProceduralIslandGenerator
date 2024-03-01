using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;

using static Unity.Mathematics.math;
using static Noise;
using Vector3 = UnityEngine.Vector3;

public class ChunkMarchingCube : MonoBehaviour
{
    [SerializeField] private Vector3Int dimensions = new Vector3Int(0, 0, 0);
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0f;

    [SerializeField] private Noise.Settings noiseSettings;
    [SerializeField] private SpaceTRS domainTRS;

    
    [SerializeField] private GameObject largeMarchingCubePrefab;
    [SerializeField] private int chunkSize = 10;

    [SerializeField] private bool interpolate = true;

    [SerializeField][Range(0.1f, 10)] private float steepness = 3;
    [SerializeField][Range(0.1f, 10)] private float centerSize = 2.2f;

    [SerializeField] private CustomNoiseGen noiseGen;
    [SerializeField] private FalloffMap fallOffMap;

    [SerializeField] private bool useFallOffMap = false;
    [SerializeField] private AnimationCurve fallOffCurve;

    [SerializeField] private bool debug;

    private List<GameObject> listChunks = new List<GameObject>();

    private NativeArray<float3x4> positions;
    private NativeArray<float4> noise4;
    
    private float[] finalNoise;

    private float[] chunkNoise;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            CreateChunks();
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            ClearChunks();
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

    private void CreateChunks()
    {
        int length = dimensions.x * dimensions.y * dimensions.z;
        length = length / 4 + (length & 1);

        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        noise4 = new NativeArray<float4>(length, Allocator.Persistent);
        //finalNoise = new NativeArray<float>(length, Allocator.Persistent);
        
        GetPositions(length);
        CreateNoise(length);
        
        if (useFallOffMap)
        {
            float[,,] fallOffMapValues = fallOffMap.GenerateCircularFallOffMap(new Vector3Int(dimensions.x, dimensions.y, dimensions.z), steepness, centerSize);
            ApplyFalloffToNoise(noise4.Reinterpret<float>(4 * 4), fallOffMapValues);
            //fallOffMapValues = fallOffMap.GenerateFallOffMap(new Vector3Int(dimensions.x + 1, dimensions.y + 1, dimensions.z + 1), fallOffCurve);
        }
        else
        {
            finalNoise = noise4.Reinterpret<float>(4 * 4).ToArray();
        }

        int nbChunksX = dimensions.x / chunkSize;
        int nbChunksY = dimensions.y / chunkSize;
        int nbChunksZ = dimensions.z / chunkSize;

        int nbChunk = 0;

        for (int x=0; x < nbChunksX; x++)
        {
            for(int y=0; y < nbChunksY; y++)
            {
                for(int z=0; z < nbChunksZ; z++)
                {
                    Vector3 chunkPos = transform.position + new Vector3(x - nbChunksX / 2 + 0.5f, y - nbChunksY / 2 + 0.5f, z - nbChunksZ / 2 + 0.5f) * chunkSize;
                    GameObject spawnedChunk = Instantiate(largeMarchingCubePrefab, chunkPos, transform.rotation, transform);
                    listChunks.Add(spawnedChunk);
                    
                    chunkNoise = new float[chunkSize * chunkSize * chunkSize];
                    
                    GetNoisePortion(nbChunk, chunkSize);
                    
                    spawnedChunk.GetComponent<LargeMarchingCube>().StartGeneration(chunkNoise, interpolate, new Vector3Int(chunkSize, chunkSize, chunkSize), surfaceLevel);
                    nbChunk++;
                }
            }
        }
        positions.Dispose();
        noise4.Dispose();
    }

    private void GetNoisePortion(int nbChunk, int size)
    {
        int iterations = size * size * size;
        for (int i = 0; i < chunkNoise.Length; i++)
        {
            chunkNoise[i] =  finalNoise[(nbChunk * iterations) + i];
        }
    }

    private void ApplyFalloffToNoise(NativeArray<float> noise, float[,,] falloff)
    {
        finalNoise = new float[noise.Length];
        print(falloff.Length);
        int i = 0;
        foreach (float f in falloff)
        {
            finalNoise[i] = noise[i] * abs(f - 1f);
            i++;
        }
    }

    private void GetPositions(int length)
    {
        float3[] pos = new float3[length * 4];
        
        int i = 0;
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y ; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    pos[i] = new float3(x, y, z);
                    i++;
                }
            }
        }
        VectorizePos(pos, length, dimensions);
    }

    private void VectorizePos(float3[] pos, int totalLength, Vector3 dimensions)
    {
        int index = 0;
        float4x4 trs = transform.worldToLocalMatrix;
        for (int i = 0; i < pos.Length; i += 4)
        {
            float4 x = new float4(pos[i].x / dimensions.x, pos[i + 1].x  / dimensions.x, pos[i + 2].x  / dimensions.x, pos[i + 3].x  / dimensions.x);
            float4 y = new float4(pos[i].y  / dimensions.y, pos[i + 1].y / dimensions.y, pos[i + 2].y / dimensions.y, pos[i + 3].y / dimensions.y);
            float4 z = new float4(pos[i].z / dimensions.z, pos[i + 1].z / dimensions.z, pos[i + 2].z / dimensions.z, pos[i + 3].z / dimensions.z);
            
            positions[index] = transpose(trs.Get3x4().TransformVectors( new float4x3(x - 0.5f, y - 0.5f, z - 0.5f)));
            index++;
        }
    }

    private void CreateNoise(int length)
    {
        for (int i = 0; i < length; i++)
        {
            float4 res = GenerateNoise(positions[i]);
            //print(res);
            noise4[i] = res;
        }
    }

    private float4 GenerateNoise(float3x4 positions)
    {
        float4x3 position = domainTRS.Matrix.TransformVectors(transpose(positions));
        var hash = SmallXXHash4.Seed(noiseSettings.seed);
        int frequency = noiseSettings.frequency;
        float amplitude = 1f;
        float amplitudeSum = 0f;
        float4 sum = 0f;
        
        for (int j = 0; j < noiseSettings.octaves; j++)
        {
            sum += default(Lattice3D<Perlin, LatticeNormal>).GetNoise4(position, hash + j, frequency) * amplitude;
            amplitudeSum += amplitude;
            frequency *= noiseSettings.lacunarity;
            amplitude *= noiseSettings.persistence;
        }

        return (sum / amplitudeSum) / 2 + 0.5f;
    }

    private void ClearChunks()
    {
        foreach(GameObject chunk in listChunks)
        {
            Destroy(chunk);
        }
        listChunks.Clear();
    }
}
