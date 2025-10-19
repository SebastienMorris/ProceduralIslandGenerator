using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Vector3 = UnityEngine.Vector3;

public class IslandGenerator : MonoBehaviour
{
	[SerializeField] private bool debug;
	
	[SerializeField] private ComputeShader marchingCubesShader;
	
	[SerializeField] private Material meshMaterial;
	
	[SerializeField]private Vector3Int dimensions = new (0, 0, 0);
	
	private int chunkSize = 10;
	
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;
    
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer triCountBuffer;
	private Chunk[] chunks;

	bool simulate = false;

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
		void SetupBuffers()
		{
			int numVoxels = chunkSize * chunkSize * chunkSize;
			int maxTriangleCount = numVoxels * 5;
	    
			triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
			triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

			Vector3Int numChunks = dimensions / chunkSize;
			int nbChunks = numChunks.x * numChunks.y * numChunks.z;
	    
			chunks = new Chunk[nbChunks];
		}
	}

	private void OnDisable()
	{
		
	}

	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
			simulate = !simulate;
	    }

		if(simulate)
		{
			ClearChunks();
			Calculate();
        }
    }

	private void LateUpdate()
	{
		if (chunks != null)
		{
			for (int i=0; i<chunks.Length; i++)
			{
				if (chunks[i].mesh != null)
				{
					Bounds bounds = new Bounds(chunks[i].position, new Vector3(chunkSize, chunkSize, chunkSize));
					Graphics.DrawMeshInstancedProcedural(chunks[i].mesh, 0, meshMaterial, bounds, 1);
				}
			}
		}
	}

    void Calculate()
    {
	    StartCoroutine(CalculateCoroutine(dimensions / chunkSize));
    }

    void SetupBuffers()
    {
	    int numVoxels = chunkSize * chunkSize * chunkSize;
	    int maxTriangleCount = numVoxels * 5;
	    
	    triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
	    triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

	    Vector3Int numChunks = dimensions / chunkSize;
	    int nbChunks = numChunks.x * numChunks.y * numChunks.z;
	    
	    chunks = new Chunk[nbChunks];
    }

    private IEnumerator CalculateCoroutine(Vector3Int numChunks)
    {
	    int chunkIndex = 0;
	    for (int x = 0; x < numChunks.x; x++)
	    {
		    for (int y = 0; y < numChunks.y; y++)
		    { 
			    for (int z = 0; z < numChunks.z; z++) 
			    { 
				    Vector3Int coord = new Vector3Int(x, y, z); 
				    
				    var chunk = CreateChunk(coord); 
				    chunks[chunkIndex] = chunk; 
				    CreateChunkMesh(chunk, CalculateChunkTriangles(chunk));
				    chunkIndex++; 
			    }
		    }
	    }
		yield return null;
    }
    
    Chunk CreateChunk(Vector3Int coord)
    {
	    Vector3 localPos = new Vector3Int(coord.x * chunkSize - (dimensions.x / 2 - chunkSize / 2), coord.y * chunkSize - (dimensions.y / 2 - chunkSize / 2), coord.z * chunkSize - (dimensions.z / 2 - chunkSize / 2));
	    Vector3 pos = transform.TransformPoint(localPos);
	    return new Chunk(pos, localPos);
    }

	private Triangle[] CalculateChunkTriangles(Chunk chunk)
	{
		triangleBuffer.SetCounterValue(0);
		
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
		
		marchingCubesShader.SetVector(Shader.PropertyToID("dimensions"), float4(chunkSize, chunkSize, chunkSize, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalPos"), float4(chunk.position, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("localPos"), float4(chunk.localPosition, 0f));
		
		marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
		
		marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
	}

	private void StopCalculation()
	{
		StopCoroutine(CalculateCoroutine(Vector3Int.zero));
	}

    private void ClearChunks()
    {
	    triangleBuffer.Release();
	    triCountBuffer.Release();
        chunks = null;
    }
}

[Serializable]
public struct Chunk
{
	public Vector3 position;
	public Vector3 localPosition;
	public Mesh mesh;

	public Chunk(Vector3 position, Vector3 localPosition)
	{
		this.position = position;
		this.localPosition = localPosition;
		this.mesh = new Mesh();
	}

	public Chunk(Vector3 position, Vector3 localPosition, Mesh mesh) {this.position = position; this.localPosition = localPosition; this.mesh = mesh; }
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

