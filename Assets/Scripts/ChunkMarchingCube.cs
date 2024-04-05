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
using float4 = Unity.Mathematics.float4;

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
	//private ComputeBuffer noisePositionsBuffer;

    [SerializeField] private int numThreadsPerAxis = 8;
    int numPointsPerChunk;


	private List<IslandChunk> chunks = new List<IslandChunk>();

    private float[,,] fallOffMapValues;

	private Vector3Int numChunks = Vector3Int.one;

	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
		    ClearChunks();
		    InitChunks();
		    //CreateChunks();
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

	void InitChunks()
	{
        numPointsPerChunk = (chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1);
		int numVoxelsPerAxis = chunkSize;
		int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
		int maxTriangleCount = numVoxels * 5;
		
        numChunks = new(dimensions.x / chunkSize, dimensions.y / chunkSize, dimensions.z / chunkSize);

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
                    chunk.Initialise(mat);
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
		obj.transform.parent = transform;
		obj.transform.localPosition = new Vector3Int(coord.x * chunkSize - (dimensions.x / 2 - chunkSize / 2), coord.y * chunkSize - (dimensions.y / 2 - chunkSize / 2), coord.z * chunkSize - (dimensions.z / 2 - chunkSize / 2));
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
	    noiseComputeShader.SetBuffer(0, Shader.PropertyToID("posAndNoise"), noiseBuffer);
	    
	    noiseComputeShader.SetInt(Shader.PropertyToID("numPointsPerAxis"), chunkSize + 1);
	    
	    noiseComputeShader.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
	    noiseComputeShader.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
	    noiseComputeShader.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
	    noiseComputeShader.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
	    noiseComputeShader.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
	    noiseComputeShader.SetFloat(Shader.PropertyToID("scale"), domainTRS.scale.x);
	    
	    noiseComputeShader.SetFloat(Shader.PropertyToID("steepness"), steepness);
	    noiseComputeShader.SetFloat(Shader.PropertyToID("centerSize"), centerSize);
	    noiseComputeShader.SetBool(Shader.PropertyToID("applyFallOff"), useFallOffMap);
	    
	    noiseComputeShader.SetVector(Shader.PropertyToID("dimensions"), float4(dimensions, 0f));
	    noiseComputeShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
	    noiseComputeShader.SetVector(Shader.PropertyToID("localPos"), float4(chunk.transform.localPosition, 0f));
	    noiseComputeShader.SetVector(Shader.PropertyToID("globalPos"), float4(chunk.transform.position, 0f));
	    
	    noiseComputeShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
	    
	    noiseBuffer.GetData(posAndNoise, 0, 0, numPointsPerChunk);
    }

    private void ClearChunks()
    {
        foreach(var chunk in chunks) 
	        Destroy(chunk.gameObject);
        
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
