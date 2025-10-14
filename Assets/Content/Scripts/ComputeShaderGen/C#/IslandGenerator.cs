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
using Matrix4x4 = System.Numerics.Matrix4x4;

public class IslandGenerator : MonoBehaviour
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
	

	private List<IslandChunk> chunks = new List<IslandChunk>();
	private List<Triangle[]> calculatedChunks = new List<Triangle[]>();

	private Mesh[] GeneratedMeshes;

	private bool calculateTriangles;
	
	private int generatedMeshIndex;

	bool simulate = false;

	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
			simulate = !simulate;
	    }

		if(simulate)
		{
            ClearChunks();
            InitChunks();
        }
    }

	private void LateUpdate()
	{
		if (GeneratedMeshes != null && GeneratedMeshes.Length > 0)
		{
			for (int i=0; i<generatedMeshIndex; i++)
			{
				Bounds bounds = new Bounds(chunks[i].transform.position, new Vector3(chunkSize, chunkSize, chunkSize));
				Graphics.DrawMeshInstancedProcedural(GeneratedMeshes[i], 0, meshMaterial, bounds, 1);
			}
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
	    int numVoxels = chunkSize * chunkSize * chunkSize;
	    int maxTriangleCount = numVoxels * 5;
	    triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3 + sizeof(int), ComputeBufferType.Append);
	    triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

	    Vector3Int numChunks = new(dimensions.x / chunkSize, dimensions.y / chunkSize, dimensions.z / chunkSize);
	    int nbChunks = numChunks.x * numChunks.y * numChunks.z;
	    
	    GeneratedMeshes = new Mesh[nbChunks];
	    generatedMeshIndex = 0;

	    //StartCoroutine(CreateChunksCoroutine(nbChunks));
	    StartCoroutine(ChunkTriangleCalcCoroutine(numChunks));
    }

    private IEnumerator ChunkTriangleCalcCoroutine(Vector3Int numChunks)
    {
	    int chunkIndex = 0;
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
				    chunks.Add(chunk); 
				    CalculateChunkTriangles(chunk, chunkIndex); 
				    chunkIndex++; 
				    /*spawnedChunksThisFrame++; 
				    if (spawnedChunksThisFrame >= numChunksSpawnedPerFrame)
				    {
					    spawnedChunksThisFrame = 0;
					    yield return new WaitForEndOfFrame();
					    
				    }*/
			    }
		    }
	    }

        int nbCalculatedChunks = calculatedChunks.Count;
        for (int i = 0; i < nbCalculatedChunks; i++)
        {
            Triangle[] chunkTriangles = calculatedChunks[0];
            CreateChunkMesh(chunks[chunkTriangles[0].chunkIndex], chunkTriangles);
            calculatedChunks.Remove(chunkTriangles);
            //spawnedChunks++;
        }

        triangleBuffer.Release();
	    triangleBuffer = null;
	    triCountBuffer.Release();
	    triCountBuffer = null;

		yield return null;
    }
    
    private IEnumerator CreateChunksCoroutine(int nbChunks)
    {
	   /* int spawnedChunks = 0;
	    while (spawnedChunks < nbChunks)
	    {
		    int nbCalculatedChunks = calculatedChunks.Count;
		    for (int i = 0; i < nbCalculatedChunks && i < numChunksSpawnedPerFrame; i++)
		    {
			    Triangle[] chunkTriangles = calculatedChunks[0];
			    CreateChunkMesh(chunks[chunkTriangles[0].chunkIndex], chunkTriangles);
			    calculatedChunks.Remove(chunkTriangles);
			    spawnedChunks++;
		    }
		    yield return new WaitForEndOfFrame();
	    }*/

        int nbCalculatedChunks = calculatedChunks.Count;
        for (int i = 0; i < nbCalculatedChunks; i++)
        {
            Triangle[] chunkTriangles = calculatedChunks[0];
            CreateChunkMesh(chunks[chunkTriangles[0].chunkIndex], chunkTriangles);
            calculatedChunks.Remove(chunkTriangles);
            //spawnedChunks++;
        }
		yield return null;
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

	private void CalculateChunkTriangles(IslandChunk chunk, int chunkIndex)
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
		
		marchingCubesShader.SetVector(Shader.PropertyToID("dimensions"), float4(chunkSize, chunkSize, chunkSize, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("globalPos"), float4(chunk.transform.position, 0f));
		marchingCubesShader.SetVector(Shader.PropertyToID("localPos"), float4(chunk.transform.localPosition, 0f));
		
		marchingCubesShader.SetBuffer(0, Shader.PropertyToID("triangles"), triangleBuffer);
		
		marchingCubesShader.SetInt(Shader.PropertyToID("chunkIndex"), chunkIndex);
		marchingCubesShader.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
		
		marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
		
		ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
		int[] triCountArray = { 0 };
		triCountBuffer.GetData(triCountArray);
		int numTris = triCountArray[0];
		
		// Get triangle data from shader
		Triangle[] tris = new Triangle[numTris];
		triangleBuffer.GetData(tris, 0, 0, numTris);

		calculatedChunks.Add(tris);
	}
	

	private void CreateChunkMesh(IslandChunk chunk, Triangle[] chunkTriangles)
	{
		int numTris = chunkTriangles.Length;
		
		//Mesh mesh = chunk.mesh;
		//mesh.Clear();
		
		Mesh mesh = new Mesh();

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

		if (GeneratedMeshes.Length > generatedMeshIndex)
		{
			GeneratedMeshes[generatedMeshIndex] = mesh;
			generatedMeshIndex++;
		}

		//mesh.RecalculateNormals();
		
		//UnityEngine.Matrix4x4[] worldMartixs = new UnityEngine.Matrix4x4[1];
		//worldMartixs[0] = chunk.transform.worldToLocalMatrix;
		//Graphics.DrawMeshInstanced(mesh, 0, meshMaterial,worldMartixs, 1);
	}

    private void ClearChunks()
    {
	    StopCoroutine(CreateChunksCoroutine(0));
	    StopCoroutine(ChunkTriangleCalcCoroutine(Vector3Int.zero));
        foreach(var chunk in chunks) 
	        Destroy(chunk.gameObject);
        
        chunks.Clear();
        calculatedChunks.Clear();
        GeneratedMeshes = null;
        generatedMeshIndex = 0;
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

