using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using static Noise;
using Vector3 = UnityEngine.Vector3;
using static UnityEditor.PlayerSettings;
using float4 = Unity.Mathematics.float4;

public class ChunkMarchingCube : MonoBehaviour
{
	[SerializeField] private bool debug;
	
	[SerializeField] private ComputeShader marchingCubesShader;
	
	[SerializeField] private Material meshMaterial;

	[SerializeField, Min(1)] private int numChunksSpawnedPerFrame = 1;
	[SerializeField] private int numThreadsPerAxis = 8;
	
	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
	[SerializeField] private int chunkSize = 10;
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;

    [SerializeField] private bool applyFallOffMap;
    [SerializeField][Range(0.1f, 10)] private float steepness = 2f;
    [SerializeField][Range(0.1f, 10)] private float centerSize = 10f;
    
    
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer triCountBuffer;

	private Vector3Int numChunks;
	private int nbChunks;
	
	
    private int numPointsPerChunk;

	private List<IslandChunk> chunks = new List<IslandChunk>();

	private bool calculateTriangles;
	
	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
		    ClearChunks();
		    InitChunks();
	    }

	    if (calculateTriangles)
	    {
		    print("before");
		    int numVoxels = dimensions.x * dimensions.y * dimensions.z;
		    int maxTriangleCount = numVoxels * 5;
		    triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3 + sizeof(int), ComputeBufferType.Append);

		    triangleBuffer.SetCounterValue(0);
		
		    marchingCubesShader.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		    marchingCubesShader.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		    marchingCubesShader.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		    marchingCubesShader.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		    marchingCubesShader.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
		    marchingCubesShader.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);
	    
		    marchingCubesShader.SetFloat(Shader.PropertyToID("steepness"), steepness);
		    marchingCubesShader.SetFloat(Shader.PropertyToID("centerSize"), centerSize);
		    marchingCubesShader.SetBool(Shader.PropertyToID("applyFallOff"), applyFallOffMap);
		
		    marchingCubesShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		    marchingCubesShader.SetVector(Shader.PropertyToID("globalPos"), float4(transform.position, 0f));
		
		    marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
		
		    marchingCubesShader.SetInt(Shader.PropertyToID("numPointsPerChunk"), numPointsPerChunk);
		    marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
		
		    marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
		    AsyncGPUReadback.Request(triangleBuffer, OnCompleteMarchingCubesReadback);
		
		    triangleBuffer.Release();
		    triangleBuffer = null;

		    calculateTriangles = false;
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
	    //int numVoxels = dimensions.x * dimensions.y * dimensions.z;
	    //int maxTriangleCount = numVoxels * 5;

	    numChunks = new(dimensions.x / chunkSize, dimensions.y / chunkSize, dimensions.z / chunkSize);
	    nbChunks = numChunks.x * numChunks.y * numChunks.z;


	    RequestMarchingCubes();
	    /*triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3 + sizeof(int), ComputeBufferType.Append);
	    //triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

	    Triangle[] islandTriangles = CalculateTriangles();

	    int chunkIndex = 0;
	    for (int x = 0; x < numChunks.x; x++)
	    {
		    for (int y = 0; y < numChunks.y; y++)
		    {
			    for (int z = 0; z < numChunks.z; z++)
			    {
				    Vector3Int coord = new Vector3Int(x, y, z);
				    var chunk = CreateChunk(coord);
				    chunk.Initialise(meshMaterial);
				    CreateChunkMesh(chunk, GetChunkTriangles(chunkIndex, islandTriangles));
				    chunks.Add(chunk);
				    chunkIndex++;
			    }
		    }
	    }

	    triangleBuffer.Release();
	    triangleBuffer = null;*/
    }

	private void RequestMarchingCubes()
	{
		print("before");
		int numVoxels = dimensions.x * dimensions.y * dimensions.z;
		int maxTriangleCount = numVoxels * 5;
		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3 + sizeof(int), ComputeBufferType.Append);

		triangleBuffer.SetCounterValue(0);
		
		marchingCubesShader.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		marchingCubesShader.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		marchingCubesShader.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		marchingCubesShader.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		marchingCubesShader.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
		marchingCubesShader.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);
	    
		marchingCubesShader.SetFloat(Shader.PropertyToID("steepness"), steepness);
		marchingCubesShader.SetFloat(Shader.PropertyToID("centerSize"), centerSize);
		marchingCubesShader.SetBool(Shader.PropertyToID("applyFallOff"), applyFallOffMap);
		
		marchingCubesShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalPos"), float4(transform.position, 0f));
		
		marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
		
		marchingCubesShader.SetInt(Shader.PropertyToID("numPointsPerChunk"), numPointsPerChunk);
		marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
		
		marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
		//AsyncGPUReadback.Request(triangleBuffer, OnCompleteMarchingCubesReadback);
		
		triangleBuffer.Release();
		triangleBuffer = null;
	}
	
	private void OnCompleteMarchingCubesReadback(AsyncGPUReadbackRequest request){
		if(!request.done){
			Debug.Log("readback hasnt done yet");
			return;
		}


		if(request.hasError){
			Debug.Log("readback error");
		}else
		{
			StartCoroutine(CreateChunksCoroutine(request.GetData<Triangle>().ToArray()));
		}
	}

	private IEnumerator CreateChunksCoroutine(Triangle[] islandTriangles)
	{
		print("coroutine");
			int spawnedChunks = 0;
			int spawnedChunksThisFrame = 0;
			for (int x = 0; x < numChunks.x; x++)
			{
				for (int y = 0; y < numChunks.y; y++)
				{
					for (int z = 0; z < numChunks.z; z++)
					{
						Vector3Int coord = new Vector3Int(x, y, z);
						var chunk = CreateChunk(coord);
						chunk.Initialise(meshMaterial);
						CreateChunkMesh(chunk, GetChunkTriangles(spawnedChunks, islandTriangles));
						chunks.Add(chunk);
						spawnedChunksThisFrame++;
						spawnedChunks++;
						if (spawnedChunksThisFrame >= numChunksSpawnedPerFrame)
						{
							spawnedChunksThisFrame = 0;
							yield return new WaitForEndOfFrameUnit();
						}
					}
				}
			}
	}
	
	
	private Triangle[] GetChunkTriangles(int chunkIndex, Triangle[] islandTriangles)
	{
		List<Triangle> chunkTriangles = new List<Triangle>();
		foreach (Triangle tri in islandTriangles)
		{
			if(tri.chunkIndex == chunkIndex)
				chunkTriangles.Add(tri);
		}

		return chunkTriangles.ToArray();
	}

	IslandChunk CreateChunk(Vector3Int coord)
	{
		GameObject obj = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
		obj.transform.parent = transform;
		obj.transform.localPosition = new Vector3(0f, 0f, 0f);
		//obj.transform.localPosition = new Vector3Int(coord.x * chunkSize - (dimensions.x / 2 - chunkSize / 2), coord.y * chunkSize - (dimensions.y / 2 - chunkSize / 2), coord.z * chunkSize - (dimensions.z / 2 - chunkSize / 2));
		IslandChunk chunkScript = obj.AddComponent<IslandChunk>();
		chunkScript.coord = coord;
		return chunkScript;
	}

	private void CreateChunkMesh(IslandChunk chunk, Triangle[] chunkTriangles)
	{
		int numTris = chunkTriangles.Length;
		
		Mesh mesh = chunk.mesh;
		mesh.Clear();

		var vertices = new Vector3[numTris * 3];
		var meshTriangles = new int[numTris * 3];

		for (int i = 0; i < numTris; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				meshTriangles[i * 3 + j] = i * 3 + j;
				vertices[i * 3 + j] = chunkTriangles[i][j];
			}
		}
		mesh.vertices = vertices;
		mesh.triangles = meshTriangles;

		mesh.RecalculateNormals();
	}
	private Triangle[] CalculateTriangles()
	{
		triangleBuffer.SetCounterValue(0);
		
		marchingCubesShader.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		marchingCubesShader.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		marchingCubesShader.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		marchingCubesShader.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		marchingCubesShader.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
		marchingCubesShader.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);
	    
		marchingCubesShader.SetFloat(Shader.PropertyToID("steepness"), steepness);
		marchingCubesShader.SetFloat(Shader.PropertyToID("centerSize"), centerSize);
		marchingCubesShader.SetBool(Shader.PropertyToID("applyFallOff"), applyFallOffMap);
		
		marchingCubesShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalPos"), float4(transform.position, 0f));
		
		marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
		
		marchingCubesShader.SetInt(Shader.PropertyToID("numPointsPerChunk"), numPointsPerChunk);
		marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);

		marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
		
		
		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];
		
		Triangle[] triangles =  new Triangle[numTris];
		triangleBuffer.GetData(triangles, 0, 0, numTris);
		
		return triangles;
	}

    private void ClearChunks()
    {
        foreach(var chunk in chunks) 
	        Destroy(chunk.gameObject);
        
        chunks.Clear();
    }
}

public struct Triangle
{
#pragma warning disable 649 // disable unassigned variable warning
	public Vector3 a;
	public Vector3 b;
	public Vector3 c;
	public int chunkIndex;

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
