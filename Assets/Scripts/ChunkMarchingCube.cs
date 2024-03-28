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
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using static Noise;
using Vector3 = UnityEngine.Vector3;
using static UnityEditor.PlayerSettings;

public class ChunkMarchingCube : MonoBehaviour
{


	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0f;
    [SerializeField] private Material mat;

    [SerializeField] private Settings noiseSettings = Settings.Default;
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
    [SerializeField] private ComputeShader noiseComputeShader;
	private ComputeBuffer pointsBuffer;
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer triCountBuffer;

	private ComputeBuffer noiseBuffer;
	private ComputeBuffer noisePositionsBuffer;

    [SerializeField, Range(1, 10)] private int smooth = 1;
    [SerializeField] private int numThreadsPerAxis = 8;
    int numPointsPerChunk;


	private List<IslandChunk> chunks = new List<IslandChunk>();

    private NativeArray<float3x4> positions;
    private NativeArray<float4x3> noisePositions;
    private NativeArray<float4> noise4;

    private float[,,] fallOffMapValues;
    
    private float[] finalNoise;
    private float3[] finalPositions;

    private float[] chunkNoise;

	private Vector3Int numChunks = Vector3Int.one;

	private int missingPoints;

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
        numPointsPerChunk = (smooth * chunkSize + 1) * (smooth * chunkSize + 1) * (smooth * chunkSize + 1);
		int numVoxelsPerAxis = chunkSize * smooth;
		int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
		int maxTriangleCount = numVoxels * 5;

		float coef = numPointsPerChunk / 4f - floor(numPointsPerChunk / 4f);
		missingPoints = (int)(4 * coef);
		
		//print("numChunks : " + numPointsPerChunk + "  maxTriangles : " + maxTriangleCount);

        numChunks = new(dimensions.x / chunkSize, dimensions.y / chunkSize, dimensions.z / chunkSize);

		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
		pointsBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float) * 4);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
		
		
		noisePositionsBuffer = new ComputeBuffer(numPointsPerChunk / 4 * 4, sizeof(float) * 4 * 3);
		noiseBuffer = new ComputeBuffer(numPointsPerChunk / 4 * 4, sizeof(float) * 4, ComputeBufferType.Append);
		
		fallOffMapValues = fallOffMap.GenerateFallOffMap(new Vector3Int(dimensions.x * smooth + 1, dimensions.y * smooth + 1, dimensions.z * smooth + 1), steepness, centerSize);

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
		
		noisePositionsBuffer.Release();
		noiseBuffer.Release();
		
		noisePositions.Dispose();
		noise4.Dispose();
	}

	IslandChunk CreateChunk(Vector3Int coord)
	{
		GameObject obj = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
		obj.transform.position = new Vector3Int(coord.x * chunkSize - (dimensions.x / 2 - chunkSize / 2), coord.y * chunkSize - (dimensions.y / 2 - chunkSize / 2), coord.z * chunkSize - (dimensions.z / 2 - chunkSize / 2));
		IslandChunk chunkScript = obj.AddComponent<IslandChunk>();
		chunkScript.coord = coord;
		return chunkScript;
	}

	private void UpdateChunk(IslandChunk chunk, int index)
	{
		int length = numPointsPerChunk / 4 + (numPointsPerChunk & 1);
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        noisePositions = new NativeArray<float4x3>(length, Allocator.Persistent);
        noise4 = new NativeArray<float4>(length, Allocator.Persistent);
        
        GetPositions(chunk.transform.position, new Vector3Int(chunkSize, chunkSize, chunkSize));
        CreateNoise(length);
        
        if (useFallOffMap)
        {
	        float[,,] chunkFalloff = new float[chunkSize * smooth + 1, chunkSize * smooth + 1, chunkSize * smooth + 1];
        
	        for (int i = 0; i < chunkSize * smooth + 1; i++)
	        {
		        for (int j = 0; j < chunkSize * smooth + 1; j++)
		        {
			        for (int h = 0; h < chunkSize * smooth + 1; h++)
			        {
				        chunkFalloff[i, j, h] = fallOffMapValues[i + chunk.coord.x * chunkSize * smooth, j + chunk.coord.y * chunkSize * smooth, h + chunk.coord.z * chunkSize * smooth];
			        }
		        }
	        }
	        
	        ApplyFalloffToNoise(noise4.Reinterpret<float>(4 * 4), chunkFalloff);
        }
        //else finalNoise = noise4.Reinterpret<float>(4 * 4).ToArray();
        finalPositions = positions.Reinterpret<float3>(3 * 4 * 4).ToArray();
        

        List<float4> posAndNoise = new();
        for (int i = 0; i < finalPositions.Length; i++)
        {
	        if(i + (4 - missingPoints) < finalPositions.Length)
				posAndNoise.Add(new(finalPositions[i], finalNoise[i]));
        }
		pointsBuffer.SetData(posAndNoise);

		triangleBuffer.SetCounterValue(0);
		marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
		marchingCubesShader.SetBuffer(0, "triangles", triangleBuffer);
		marchingCubesShader.SetInt("numPointsPerAxis", chunkSize * smooth + 1);
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

    private void GetPositions(Vector3 chunkPos, Vector3Int dimensions)
    {
        float3[] pos = new float3[numPointsPerChunk];
        
        int i = 0;
        for (int x = 0; x < dimensions.x * smooth + 1; x++)
        {
            for (int y = 0; y < dimensions.y * smooth + 1; y++)
            {
                for (int z = 0; z < dimensions.z * smooth + 1; z++)
                {
                    pos[i] = new float3(x, y, z);
                    i++;
                }
            }
        }
        VectorizePos(pos, dimensions, chunkPos);
    }

    private void VectorizePos(float3[] pos, Vector3 dimensions, Vector3 chunkPos)
    {
        int index = 0;
        for (int i = 0; i < pos.Length; i += 4)
        {
	        float3 zero = new float3(0f, 0f, 0f);

	        /*float3 pos1 = select(pos[i + 1], zero, i + 1 >= pos.Length);
	        float3 pos2 = select(pos[i + 2], zero, i + 2 >= pos.Length);
	        float3 pos3 = select(pos[i + 3], zero, i + 3 >= pos.Length);*/
	        
	        float3 pos1 = i + 1 >= pos.Length ? zero : pos[i + 1];
	        float3 pos2 = i + 2 >= pos.Length ? zero : pos[i + 2];
	        float3 pos3 = i + 3 >= pos.Length ? zero : pos[i + 3];
	        
	        
            float4 x = new float4(pos[i].x, pos1.x, pos2.x, pos3.x) / smooth;
            float4 y = new float4(pos[i].y, pos1.y, pos2.y, pos3.y) / smooth;
            float4 z = new float4(pos[i].z, pos1.z, pos2.z, pos3.z) / smooth;
            
            positions[index] = transpose(new float4x3(x - dimensions.x / 2, y - dimensions.y / 2, z - dimensions.z / 2));
            noisePositions[index] = domainTRS.Matrix.TransformVectors(new float4x3((chunkPos.x + x) / dimensions.x, (chunkPos.y + y) / dimensions.y, (chunkPos.z + z) / dimensions.z));
            index++;
        }
    }

    private void CreateNoise(int length)
    {
	    finalNoise = new float[length * 4];
	    
	    noisePositionsBuffer.SetData(noisePositions);
	    
	    noiseComputeShader.SetBuffer(0, "noiseValues", noiseBuffer);
	    noiseComputeShader.SetBuffer(0, "positions", noisePositionsBuffer);
	    noiseComputeShader.SetInt("numPointsPerAxis", chunkSize * smooth + 1);
	    noiseComputeShader.SetInt("seed", noiseSettings.seed);
	    noiseComputeShader.SetInt("frequency", noiseSettings.frequency);
	    noiseComputeShader.SetInt("octaves", noiseSettings.octaves);
	    noiseComputeShader.SetInt("lacunarity", noiseSettings.lacunarity);
	    noiseComputeShader.SetFloat("persistence", noiseSettings.persistence);

	    noiseComputeShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
	    
	    noiseBuffer.GetData(finalNoise, 0, 0, length * 4);
	    
	    print(finalNoise[0]);
	    print(finalNoise[1]);
	    print(finalNoise[2]);
	    print(finalNoise[3]);
	    
        /*for (int i = 0; i < length; i++)
        {
            float4 res = GenerateNoise(noisePositions[i]);
            //print(res);
            noise4[i] = res;
        }*/
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
