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

public class GPUIslandGenerator : MonoBehaviour
{
	[SerializeField] private ComputeShader marchingCompute;

	[SerializeField] private Material material;

	private bool initialized;


	private ComputeBuffer drawBuffer;

    private ComputeBuffer triCountBuffer;
    private DrawTriangle[] drawTriangles;
    private Mesh generatedMesh;


	private int idMarchingKernel;


	private const int DRAW_STRIDE = sizeof(float) * 3 * 3;


    [SerializeField] private Vector3Int dimensions;
    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;
	[SerializeField][Range(0, 1)] private float surfaceLevel = 0.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, dimensions);
    }

    private void OnEnable()
	{
		Initialize();

	}

	private void OnDisable()
	{
		if(initialized)
		{
			drawBuffer.Release();
            triCountBuffer.Release();
		}

		initialized = false;
	}

	private void LateUpdate()
	{
		if(Application.isPlaying == false)
		{
			OnDisable();
			OnEnable();
		}

        CalculateTriangles();

        if (generatedMesh != null)
        { 
            Bounds bounds = new Bounds(transform.position, dimensions);
            Graphics.DrawMeshInstancedProcedural(generatedMesh, 0, material, bounds, 1);
        }
	}

	private void Initialize()
	{
        if (initialized)
        {
			Debug.Log("Already Initialised", this.gameObject);
            OnDisable();
        }

        initialized = true;

        int nbCubes = dimensions.x * dimensions.y * dimensions.z;
        int nbTriangles = nbCubes * 5;


        drawBuffer = new ComputeBuffer(nbTriangles, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);


        idMarchingKernel = marchingCompute.FindKernel("March");

        marchingCompute.SetBuffer(idMarchingKernel, "DrawTriangles", drawBuffer);

		drawTriangles = new DrawTriangle[nbTriangles];

        generatedMesh = new Mesh();
    }

	private void CalculateTriangles()
	{
        drawBuffer.SetCounterValue(0);
        triCountBuffer.SetCounterValue(0);

        SetComputeSettings();

        marchingCompute.Dispatch(idMarchingKernel, 8, 8, 8);


        ComputeBuffer.CopyCount(drawBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        print(numTris + " / " + drawBuffer.count);


        drawBuffer.GetData(drawTriangles, 0, 0, numTris);

        //drawBuffer.GetData(drawTriangles);

        GenerateMesh();
    }

	private void GenerateMesh()
	{
        generatedMesh.Clear();

        int numTris = drawTriangles.Length;

        var vertices = new Vector3[numTris * 3];
        var triangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                triangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = drawTriangles[i][j];
            }
        }

        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
    }

	private void SetComputeSettings()
	{
        marchingCompute.SetBuffer(0, "DrawTriangles", drawBuffer);

        marchingCompute.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
        marchingCompute.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
        marchingCompute.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
        marchingCompute.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
        marchingCompute.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);

        marchingCompute.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);

        marchingCompute.SetFloat(Shader.PropertyToID("steepness"), noiseSettings.steepness);
        marchingCompute.SetFloat(Shader.PropertyToID("centerSize"), noiseSettings.centerSize);
        marchingCompute.SetBool(Shader.PropertyToID("applyFallOff"), noiseSettings.applyFallOffMap);

        marchingCompute.SetVector(Shader.PropertyToID("dimensions"), float4(dimensions.x, dimensions.y, dimensions.z, 0f));
        marchingCompute.SetVector(Shader.PropertyToID("globalDimensions"), float4(dimensions.x, dimensions.y, dimensions.z, 0f));
        marchingCompute.SetVector(Shader.PropertyToID("globalPos"), float4(transform.position, 0f));
        marchingCompute.SetVector(Shader.PropertyToID("localPos"), float4(transform.localPosition, 0f));

        marchingCompute.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
    }
}

public struct DrawTriangle
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

    [SerializeField] public bool applyFallOffMap;
    [SerializeField][Range(0.1f, 10)] public float steepness;
    [SerializeField][Range(0.1f, 10)] public float centerSize;

    public static NoiseSettings Default => new NoiseSettings{frequency = 2, octaves = 1, lacunarity = 2, persistence = 0.5f, scale = 0.5f, applyFallOffMap = true, steepness = 2f, centerSize = 10};
}
