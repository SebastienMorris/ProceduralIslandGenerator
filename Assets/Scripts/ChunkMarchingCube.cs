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
	[SerializeField] private bool debug;
	
	[SerializeField] private ComputeShader marchingCubesShader;
	[SerializeField] private ComputeShader noiseShader;
	
	[SerializeField] private Material meshMaterial;
	
	[SerializeField] private int numThreadsPerAxis = 8;
	
	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
	[SerializeField] private int chunkSize = 10;
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;

    [SerializeField] private bool applyFallOffMap;
    [SerializeField][Range(0.1f, 10)] private float steepness = 2f;
    [SerializeField][Range(0.1f, 10)] private float centerSize = 10f;
    
    
	private ComputeBuffer pointsBuffer;
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer triCountBuffer;
	private ComputeBuffer noiseBuffer;

    private int numPointsPerChunk;

	private List<IslandChunk> chunks = new List<IslandChunk>();

	private Action onMeshGenerated;

	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
		    ClearChunks();
		    InitChunks();
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

    private IEnumerator GenerateMeshCoroutine(Vector3Int numChunks)
    {
	    for (int x = 0; x < numChunks.x; x++)
	    {
		    for (int y = 0; y < numChunks.y; y++)
		    {
			    for (int z = 0; z < numChunks.z; z++)
			    {
				    Vector3Int coord = new Vector3Int(x, y, z);
				    var chunk = CreateChunk(coord);
				    chunk.Initialise(meshMaterial);
				    UpdateChunk(chunk);
				    chunks.Add(chunk);
				    yield return new WaitForEndOfFrame();
			    }
		    }
	    }
	    onMeshGenerated.Invoke();
	    
    }

    private void OnEndMeshGeneration()
    {
	    pointsBuffer.Release();
	    triangleBuffer.Release();
	    pointsBuffer = null;
	    triangleBuffer = null;
		
	    noiseBuffer.Release();
	    noiseBuffer = null;

	    onMeshGenerated -= OnEndMeshGeneration;
    }

	void InitChunks()
	{
        numPointsPerChunk = (chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1);
		int numVoxelsPerAxis = chunkSize;
		int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
		int maxTriangleCount = numVoxels * 5;
		
        Vector3Int numChunks = new(dimensions.x / chunkSize, dimensions.y / chunkSize, dimensions.z / chunkSize);

		triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
		pointsBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float) * 4);
		triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
		
		noiseBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float) * 4, ComputeBufferType.Append);

		onMeshGenerated += OnEndMeshGeneration;

		StartCoroutine(GenerateMeshCoroutine(numChunks));
		/*for (int x = 0; x < numChunks.x; x++)
		{
			for (int y = 0; y < numChunks.y; y++)
			{
				for (int z = 0; z < numChunks.z; z++)
				{
					Vector3Int coord = new Vector3Int(x, y, z);
                    var chunk = CreateChunk(coord);
                    chunk.Initialise(meshMaterial);
					UpdateChunk(chunk);
					chunks.Add(chunk);
				}
			}
		}*/
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
	    noiseShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
	    noiseShader.SetVector(Shader.PropertyToID("localPos"), float4(chunk.transform.localPosition, 0f));
	    noiseShader.SetVector(Shader.PropertyToID("globalPos"), float4(chunk.transform.position, 0f));
	    
	    noiseShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
	    
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
