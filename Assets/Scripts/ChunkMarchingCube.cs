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
using static UnityEditor.PlayerSettings;

public class ChunkMarchingCube : MonoBehaviour
{


	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0f;
    [SerializeField] private Material mat;

    [SerializeField] private Settings noiseSettings;
    [SerializeField] private SpaceTRS domainTRS;

    [SerializeField] private GameObject largeMarchingCubePrefab;
    [SerializeField] private int chunkSize = 10;

    [SerializeField][Range(0.1f, 10)] private float steepness = 3;
    [SerializeField][Range(0.1f, 10)] private float centerSize = 2.2f;

    [SerializeField] private CustomNoiseGen noiseGen;
    [SerializeField] private FalloffMap fallOffMap;

    [SerializeField] private bool useFallOffMap = false;
    [SerializeField] private AnimationCurve fallOffCurve;

    [SerializeField] private bool debug;
    [SerializeField] private ComputeShader marchingCubesShader;
    [SerializeField] private ComputeBuffer pointsBuffer;
    [SerializeField] private ComputeBuffer triangleBuffer;
    [SerializeField] private ComputeBuffer triCountBuffer;

    [SerializeField] private int numPointsPerAxis = 30;
    [SerializeField] private int numThreadsPerAxis = 8;
    int numPointsPerChunk;


	private List<IslandChunk> chunks = new List<IslandChunk>();

    private NativeArray<float3x4> positions;
    private NativeArray<float4> noise4;
    
    private float[] finalNoise;
    private float3[] finalPositions;

    private float[] chunkNoise;

	private Vector3Int numChunks = Vector3Int.one;

	public float boundsSize = 1;

	private void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
			ClearChunks();
			InitChunks();
			//CreateChunks();
        }

        /*if (Input.GetKeyUp(KeyCode.C))
        {
            ClearChunks();
        }*/
    }

    private void OnDrawGizmos()
    {
        if (debug)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, dimensions);
        }
    }

   /* private void CreateChunks()
    {
        foreach(var chunk in chunks)
        {
            UpdateChunk(chunk);
        }
    }*/

	void InitChunks()
	{
        numPointsPerChunk = numPointsPerAxis * chunkSize * chunkSize * chunkSize;
		int numVoxelsPerAxis = numPointsPerAxis - 1;
		int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
		int maxTriangleCount = numVoxels * 5;

		int length = dimensions.x * dimensions.y * dimensions.z * numPointsPerAxis;
		length = length / 4 + (length & 1);

        numChunks = new(dimensions.x / chunkSize, dimensions.y / chunkSize, dimensions.z / chunkSize);

		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
		pointsBuffer = new ComputeBuffer(length*4, sizeof(float) * 4);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

		positions = new NativeArray<float3x4>(length, Allocator.Persistent);
		noise4 = new NativeArray<float4>(length, Allocator.Persistent);

		GetPositions(length);
		CreateNoise(length);

		if (useFallOffMap)
		{
			float[,,] fallOffMapValues = fallOffMap.GenerateCircularFallOffMap(new Vector3Int(dimensions.x, dimensions.y, dimensions.z), steepness, centerSize);
			ApplyFalloffToNoise(noise4.Reinterpret<float>(4 * 4), fallOffMapValues);
			//fallOffMapValues = fallOffMap.GenerateFallOffMap(new Vector3Int(dimensions.x + 1, dimensions.y + 1, dimensions.z + 1), fallOffCurve);
		}
		else finalNoise = noise4.Reinterpret<float>(4 * 4).ToArray();
		finalPositions = positions.Reinterpret<float3>(3 * 4 * 4).ToArray();

		// Go through all coords and create a chunk there if one doesn't already exist
		int i = 0;
		for (int x = 0; x < numChunks.x; x++)
		{
			for (int y = 0; y < numChunks.y; y++)
			{
				for (int z = 0; z < numChunks.z; z++)
				{
					Vector3Int coord = new Vector3Int(x, y, z);
                    var chunk = CreateChunk(coord);
                    chunk.Initialise(mat);
					UpdateChunk(chunk, i++);
					chunks.Add(chunk);
				}
			}
		}

		positions.Dispose();
		pointsBuffer.Release();
		triangleBuffer.Release();
		noise4.Dispose();
	}

	IslandChunk CreateChunk(Vector3Int coord)
	{
		GameObject obj = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
		IslandChunk chunkScript = obj.AddComponent<IslandChunk>();
		chunkScript.coord = coord;
		return chunkScript;
	}

	private void UpdateChunk(IslandChunk chunk, int index)
    {
        /*Vector3Int coord = chunk.coord;
		Vector3 centre = CentreFromCoord(coord);

		Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * boundsSize;*/

        List<float4> posAndNoise = new();
		for (int i = numPointsPerChunk * index; i < numPointsPerChunk * index + numPointsPerChunk; ++i) posAndNoise.Add(new(finalPositions[i], finalNoise[i]));
		pointsBuffer.SetData(posAndNoise);

		triangleBuffer.SetCounterValue(0);
		marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
		marchingCubesShader.SetBuffer(0, "triangles", triangleBuffer);
		marchingCubesShader.SetInt("numPointsPerAxis", numPointsPerAxis);
		marchingCubesShader.SetFloat("isoLevel", surfaceLevel);

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

		/*int nbChunksX = dimensions.x / chunkSize;
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
        }*/
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
        foreach(var chunk in chunks) Destroy(chunk.gameObject);
        chunks.Clear();
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
}
