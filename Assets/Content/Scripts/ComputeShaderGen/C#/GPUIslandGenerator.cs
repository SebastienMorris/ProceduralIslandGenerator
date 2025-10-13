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
using System.Diagnostics;

public class GPUIslandGenerator : MonoBehaviour
{
	//[SerializeField] private Mesh sourceMesh;

	[SerializeField] private ComputeShader marchingCompute;

	[SerializeField] private Material material;

	/*[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
	private struct SourceVertex
	{
		public Vector3 position;
	}*/

	private bool initialized;

	//private ComputeBuffer sourceVertBuffer;
	//private ComputeBuffer sourceTriBuffer;

	private ComputeBuffer drawBuffer;
	private ComputeBuffer argsBuffer;

	private int idMarchingKernel;

	private Bounds localBounds;


	private const int DRAW_STRIDE = sizeof(float) * 3 * 3;
	private const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;

	private int[] argsBufferReset = new int[] { 0, 1, 0, 0 };



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
		UnityEngine.Debug.Assert(marchingCompute != null, "The marching compute shader is null", gameObject);
        UnityEngine.Debug.Assert(material != null, "The material is null", gameObject);

		if(initialized)
		{
			OnDisable();
		}

		initialized = true;


		//Vector3[] positions = sourceMesh.vertices;
		//int[] tris = sourceMesh.triangles;

		/*SourceVertex[] vertices = new SourceVertex[positions.Length];
		for(int i=0; i < vertices.Length; i++)
		{
			vertices[i] = new SourceVertex() { position = positions[i] };
		}*/

		//int numSourceTriangles = tris.Length / 3;

		//sourceVertBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
		//sourceVertBuffer.SetData(vertices);
		//sourceTriBuffer = new ComputeBuffer(tris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
		//sourceTriBuffer.SetData(tris);

		int nbCubes = dimensions.x * dimensions.y * dimensions.z;
		int nbTriangles = nbCubes * 5;


		drawBuffer = new ComputeBuffer(nbTriangles, DRAW_STRIDE, ComputeBufferType.Append);
		drawBuffer.SetCounterValue(0);

		argsBuffer = new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);

	
		idMarchingKernel = marchingCompute.FindKernel("March");


		//marchingCompute.SetBuffer(idMarchingKernel, "SourceVertices", sourceVertBuffer);
		//marchingCompute.SetBuffer(idMarchingKernel, "SourceTriangles", sourceTriBuffer);

		marchingCompute.SetBuffer(idMarchingKernel, "DrawTriangles", drawBuffer);

		marchingCompute.SetBuffer(idMarchingKernel, "IndirectArgsBuffer", argsBuffer);

		marchingCompute.SetInt("_NumSourceTriangles", nbCubes);


		material.SetBuffer("DrawTriangles", drawBuffer);


		marchingCompute.GetKernelThreadGroupSizes(idMarchingKernel, out uint threadGroupSize, out _, out _);

		localBounds = new Bounds(transform.position, dimensions);
		localBounds.Expand(1);
	}

	private void OnDisable()
	{
		if(initialized)
		{
			drawBuffer.Release();
			argsBuffer.Release();
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
		 
		drawBuffer.SetCounterValue(0);
		argsBuffer.SetData(argsBufferReset);

		//Transform bounds to world space
		//Bounds bounds = TransformBounds(localBounds);

		marchingCompute.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

		SetComputeSettings();

		marchingCompute.Dispatch(idMarchingKernel, dimensions.x, dimensions.y, dimensions.z);

		Graphics.DrawProceduralIndirect(material, localBounds, MeshTopology.Triangles, argsBuffer, 0, null, null, ShadowCastingMode.On, true, gameObject.layer); 
	}

	private void SetComputeSettings()
	{
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

    [SerializeField] public bool applyFallOffMap;
    [SerializeField][Range(0.1f, 10)] public float steepness;
    [SerializeField][Range(0.1f, 10)] public float centerSize;

    public static NoiseSettings Default => new NoiseSettings{frequency = 2, octaves = 1, lacunarity = 2, persistence = 0.5f, scale = 0.5f, applyFallOffMap = true, steepness = 2f, centerSize = 10};
}
