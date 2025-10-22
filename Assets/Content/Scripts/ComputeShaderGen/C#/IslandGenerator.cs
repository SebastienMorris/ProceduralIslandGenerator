using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Unity.Mathematics.math;
using Vector3 = UnityEngine.Vector3;

public class IslandGenerator : MonoBehaviour
{
	[SerializeField] private bool debug;
	
	[SerializeField] private ComputeShader marchingCubesShader;
	
	[SerializeField] private Material meshMaterial;
	
	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
	
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;
    
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer renderArgsBuffer;

	private bool simulate = false;

	private Vector3Int lastFrameDimensions = Vector3Int.zero;
	
	#region CONSTANTS
		private const int CHUNK_STRIDE = sizeof(float) * 3 * 3;
	#endregion

	private void OnDrawGizmos()
	{
		if (debug)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(transform.position, dimensions);
		}
	}
	
	private void OnEnable()
	{
		SetupBuffers();
		lastFrameDimensions = dimensions;
	}

	private void OnDisable()
	{
		ClearBuffers();
	}
	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
			simulate = !simulate;
			
			if(simulate){ StartCalculation(); } 
			else { StopCalculation(); }
	    }
    }
	
	
	private void SetupBuffers()
	{
		int numVoxels = dimensions.x * dimensions.y * dimensions.z;
		int maxTriangles = numVoxels * 5;
		
		triangleBuffer = new ComputeBuffer(maxTriangles, CHUNK_STRIDE, ComputeBufferType.Append);
	    
		marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
	}
	
	private void ClearBuffers()
	{
		triangleBuffer.Release();
	}

	private void ResetBuffers()
	{
		triangleBuffer.SetCounterValue(0);
	}

    void StartCalculation()
    {
	    StartCoroutine(CalculateCoroutine());
    }
    
    private void StopCalculation()
    {
	    StopCoroutine(CalculateCoroutine());
	    ResetBuffers();
    }

    private void CheckResize()
    {
	    if (dimensions != lastFrameDimensions)
	    {
		    ClearBuffers();
		    SetupBuffers();
		    lastFrameDimensions = dimensions;
	    }
    }

    private void Generate()
    {
	    triangleBuffer.SetCounterValue(0);
	    
	    SetComputeParams();
    }

    private IEnumerator CalculateCoroutine()
    {
	    while (simulate)
	    {
		    CheckResize();
		    Vector3Int numChunks = dimensions / CHUNK_SIZE;
		    
		    int chunkIndex = 0;
		    int spawnedChunksThisFrame = 0;
		    
		    for (int x = 0; x < numChunks.x; x++)
		    {
			    for (int y = 0; y < numChunks.y; y++)
			    {
				    for (int z = 0; z < numChunks.z; z++)
				    {
					    if(resetCalculation)
					    {
						    ResetChunks();
						    ResetBuffers();
						    x = numChunks.x;
						    resetCalculation = false;
						    break;
					    }
					    
					    
					    Vector3Int coord = new Vector3Int(x, y, z);

					    var chunk = CreateChunk(coord);
					    chunks[chunkIndex] = chunk;
					    CreateChunkMesh(chunk, CalculateChunkTriangles(chunk));
					    chunkIndex++;
					    spawnedChunksThisFrame++;

					    if (spawnedChunksThisFrame >= CHUNKS_PER_FRAME)
					    {
						    spawnedChunksThisFrame = 0;
						    yield return new WaitForEndOfFrame();
					    }
				    }
			    }
		    }
		    yield return new WaitForEndOfFrame();
	    }

	    yield return null;
    }
    
    Chunk CreateChunk(Vector3Int coord)
    {
	    Vector3 localPos = new Vector3Int(coord.x * CHUNK_SIZE - (dimensions.x / 2 - CHUNK_SIZE / 2), coord.y * CHUNK_SIZE - (dimensions.y / 2 - CHUNK_SIZE / 2), coord.z * CHUNK_SIZE - (dimensions.z / 2 - CHUNK_SIZE / 2));
	    Vector3 pos = transform.TransformPoint(localPos);
	    return new Chunk(pos, localPos);
    }

	private Triangle[] CalculateChunkTriangles(Chunk chunk)
	{
		ResetBuffers();
		
		SetComputeParams(chunk);
		
		marchingCubesShader.Dispatch(0, 8, 8, 8);
		
		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];
		
		// Get triangle data from shader
		Triangle[] tris = new Triangle[numTris];
		triangleBuffer.GetData(tris, 0, 0, numTris);

		return tris;
	}

	private void CreateChunkMesh(Chunk chunk, Triangle[] chunkTriangles)
	{
		int numTris = chunkTriangles.Length;

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
		
		chunk.mesh.vertices = vertices;
		chunk.mesh.triangles = meshTriangles;
	}

	private void SetComputeParams(Chunk chunk)
	{
		marchingCubesShader.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		marchingCubesShader.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		marchingCubesShader.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		marchingCubesShader.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		marchingCubesShader.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
		marchingCubesShader.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);
	    
		marchingCubesShader.SetFloat(Shader.PropertyToID("steepness"), noiseSettings.steepness);
		marchingCubesShader.SetFloat(Shader.PropertyToID("centerSize"), noiseSettings.centerSize);
		marchingCubesShader.SetBool(Shader.PropertyToID("applyFallOff"), noiseSettings.applyFallOffMap);
		
		marchingCubesShader.SetVector(Shader.PropertyToID("dimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalPos"), float4(chunk.position, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("localPos"), float4(chunk.localPosition, 0f));
		
		marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
	}
}

public struct Chunk
{
	public Vector3 position;
	public Vector3 localPosition;
	public Mesh mesh;
	public bool meshSet;

	public Chunk(Vector3 position, Vector3 localPosition)
	{
		this.position = position;
		this.localPosition = localPosition;
		this.mesh = new Mesh();
		meshSet = true;
	}

	public Chunk(Vector3 position, Vector3 localPosition, Mesh mesh) {this.position = position; this.localPosition = localPosition; this.mesh = mesh; meshSet = false; }

	public void SetMesh(Vector3[] vertices, int[] trianglesIndex)
	{
		this.mesh.vertices = vertices;
		this.mesh.triangles = trianglesIndex;
		this.meshSet = true;
	}
}

public struct Triangle
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

	public bool applyFallOffMap;
	[Range(0.1f, 10f)] public float steepness;
	[Range(0.1f, 10f)] public float centerSize;
	
	public static NoiseSettings Default => new NoiseSettings{frequency = 2, octaves = 1, lacunarity = 2, persistence = 0.5f, scale = 0.5f, applyFallOffMap = true, steepness = 2f, centerSize = 10};

}

