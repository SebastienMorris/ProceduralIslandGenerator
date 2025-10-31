using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using Vector3 = UnityEngine.Vector3;

public class IslandGenerator : MonoBehaviour
{
	[SerializeField] private bool Guizmo;
	
	[SerializeField] private ComputeShader marchingCubesCompute;
	
	[SerializeField] private Shader renderShader;
	[SerializeField] private Shader debugShader;
	[SerializeField] private Mesh debugMesh;
	
	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
	
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;

    [SerializeField] private bool debug = false;
    [SerializeField] private float debugZoom = 2;
    [SerializeField] private float debugScale = 1;

    private ComputeShader compute;

    private Material renderMaterial;
    private Material debugMaterial;
    
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer renderArgsBuffer;
	private ComputeBuffer debugBuffer;
	private ComputeBuffer debugArg;

	private bool simulate = false;
	private bool update = false;
	
	#region CONSTANTS
		private const int TRIANGLE_STRIDE = sizeof(float) * 3 * 3;

		private const int SAMPLE_MODIFIER = 10;
	#endregion

	private void OnDrawGizmos()
	{
		if (Guizmo)
		{
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(transform.position, (Vector3)dimensions * (debug ? debugZoom : 1));
		}
	}
	
	private void OnEnable()
	{
		compute = (ComputeShader)Instantiate(marchingCubesCompute);
		
		if (renderMaterial == null) { renderMaterial = new Material(renderShader); }
		if (debugMaterial == null) { debugMaterial = new Material(debugShader); }
		
		SetupBuffers();
		update = true;
	}

	private void OnDisable()
	{
		ClearBuffers();
		if (compute != null)
			DestroyImmediate(compute);
	}
	private void Update()
    {
	    if (Input.GetKeyUp(KeyCode.G))
	    {
			simulate = !simulate;
	    }
    }

	private void LateUpdate()
	{
		if (simulate)
		{
			if(debug)
				GenerateDebug();
			else
				Generate();
		}
	}

	private void OnValidate()
	{
		update = true;
	}


	private void SetupBuffers()
	{
		int numVoxels = dimensions.x * dimensions.y * dimensions.z;
		int maxTriangles = numVoxels * 5;
		
		triangleBuffer = new ComputeBuffer(maxTriangles, TRIANGLE_STRIDE, ComputeBufferType.Append);
		renderArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
		debugBuffer = new ComputeBuffer(numVoxels, sizeof(float) * 4, ComputeBufferType.Append);
		debugArg = CreateDebugArgsBuffer(debugMesh, numVoxels);
	    
		compute.SetBuffer(0, Shader.PropertyToID("_Triangles"), triangleBuffer);
		compute.SetBuffer(0, Shader.PropertyToID("_RenderArgs"), renderArgsBuffer);
		compute.SetBuffer(0, Shader.PropertyToID("_Debug"), debugBuffer);
	}
	
	private void ClearBuffers()
	{
		triangleBuffer.Release();
		renderArgsBuffer.Release();
		debugBuffer.Release();
		debugArg.Release();
	}

	private void ResetBuffers()
	{
		triangleBuffer.SetCounterValue(0);
		renderArgsBuffer.SetCounterValue(0);
		debugBuffer.SetCounterValue(0);
		debugArg.SetCounterValue(0);
	}
	

    private void Generate()
    {
	    if (update)
	    {
		    ClearBuffers();
		    SetupBuffers();

		    ResetBuffers();
		    SetComputeParams();

		    compute.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
		    var a = new Vector3Int((int)x, (int)y, (int)z);

		    compute.Dispatch(0, Mathf.CeilToInt(dimensions.x / (float)a.x),
			    Mathf.CeilToInt(dimensions.y / (float)a.y), Mathf.CeilToInt(dimensions.z / (float)a.z));

		    update = false;
	    }
	    
	    renderMaterial.SetBuffer(Shader.PropertyToID("_VertexBuffer"), triangleBuffer);
	    renderMaterial.SetVector(Shader.PropertyToID("origin"), float4(transform.position, 0.0f));

	    Bounds bounds = new Bounds(transform.position, dimensions);
	    
		Graphics.DrawProceduralIndirect(renderMaterial, bounds, MeshTopology.Triangles, renderArgsBuffer);
    }
    
    
	private void SetComputeParams()
	{
		compute.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		compute.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		compute.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		compute.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		compute.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
		compute.SetFloat(Shader.PropertyToID("steepness"), noiseSettings.steepness);
		compute.SetFloat(Shader.PropertyToID("centerSize"), noiseSettings.centerSize);
		compute.SetBool(Shader.PropertyToID("applyFallOff"), noiseSettings.applyFallOffMap);
		
		compute.SetVector(Shader.PropertyToID("dimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		compute.SetVector(Shader.PropertyToID("globalPos"), float4(transform.position, 0f));
		compute.SetVector(Shader.PropertyToID("localPos"), float4(transform.localPosition, 0f));
		
		compute.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
	}

	private void GenerateDebug()
	{
		if (update)
		{
			ClearBuffers();
			SetupBuffers();

			ResetBuffers();
			SetComputeParams();

			compute.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
			var a = new Vector3Int((int)x, (int)y, (int)z);

			compute.Dispatch(0, Mathf.CeilToInt(dimensions.x / (float)a.x),
				Mathf.CeilToInt(dimensions.y / (float)a.y), Mathf.CeilToInt(dimensions.z / (float)a.z));

			//float4[] temp = new float4[dimensions.x * dimensions.y * dimensions.z];
			//debugBuffer.GetData(temp);

			//for(int i=0; i<temp.Length; i++) print(temp[i]);

			update = false;
		}

		debugMaterial.SetBuffer(Shader.PropertyToID("Positions"), debugBuffer);
		debugMaterial.SetFloat(Shader.PropertyToID("scale"), debugScale);
		debugMaterial.SetFloat(Shader.PropertyToID("debugZoom"), debugZoom);
		
		Bounds bounds = new Bounds(transform.position, dimensions);
		
		Graphics.DrawMeshInstancedIndirect(debugMesh, 0, debugMaterial, bounds, debugArg);
	}
	
	public ComputeBuffer CreateDebugArgsBuffer(Mesh mesh, int numInstances)
	{
		const int stride = sizeof(uint);
		const int numArgs = 5;

		const int subMeshIndex = 0;
		uint[] args = new uint[numArgs];
		args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
		args[1] = (uint)numInstances;
		args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
		args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);
		args[4] = 0; // offset

		ComputeBuffer argsBuffer = new ComputeBuffer(numArgs, stride, ComputeBufferType.IndirectArguments);
		argsBuffer.SetData(args);
		return argsBuffer;
	}
}


[Serializable]
public struct NoiseSettings
{
	public int seed;
	
	[Range(1, 200)] public int frequency;
	
	[Range(1, 6)] public int octaves;
	[Range(2, 4)] public int lacunarity;
	[Range(0f, 1f)] public float persistence;

	public bool applyFallOffMap;
	[Range(0.1f, 10f)] public float steepness;
	[Range(0.1f, 10f)] public float centerSize;
	
	public static NoiseSettings Default => new NoiseSettings{frequency = 50, octaves = 1, lacunarity = 2, persistence = 0.5f, applyFallOffMap = true, steepness = 2f, centerSize = 10};

}


