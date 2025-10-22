using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using Vector3 = UnityEngine.Vector3;

public class IslandGenerator : MonoBehaviour
{
	[SerializeField] private bool debug;
	
	[SerializeField] private ComputeShader marchingCubesCompute;
	[SerializeField] private ComputeShader renderArgsCompute;
	
	[SerializeField] private Material renderMaterial;
	
	[SerializeField] private Vector3Int dimensions = new (0, 0, 0);
	
    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

    [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;
    
	private ComputeBuffer triangleBuffer;
	private ComputeBuffer renderArgsBuffer;

	private bool simulate = false;

	private Vector3Int lastFrameDimensions = Vector3Int.zero;
	
	#region CONSTANTS
		private const int TRIANGLE_STRIDE = sizeof(float) * 3 * 3;
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
	    }
    }

	private void LateUpdate()
	{
		if (simulate)
		{
			Generate();
		}
	}


	private void SetupBuffers()
	{
		int numVoxels = dimensions.x * dimensions.y * dimensions.z;
		int maxTriangles = numVoxels * 5;
		
		triangleBuffer = new ComputeBuffer(maxTriangles, TRIANGLE_STRIDE, ComputeBufferType.Append);
		renderArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
	    
		marchingCubesCompute.SetBuffer(0, Shader.PropertyToID("_Triangles"), triangleBuffer);
		renderArgsCompute.SetBuffer(0, Shader.PropertyToID("_RenderArgs"), renderArgsBuffer);
	}
	
	private void ClearBuffers()
	{
		triangleBuffer.Release();
		renderArgsBuffer.Release();
	}

	private void ResetBuffers()
	{
		triangleBuffer.SetCounterValue(0);
		renderArgsBuffer.SetCounterValue(0);
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
	    ResetBuffers();
	    SetComputeParams();
	    
	    marchingCubesCompute.Dispatch(0, 8, 8, 8);
	    
	    renderMaterial.SetBuffer(Shader.PropertyToID("_VertexBuffer"), triangleBuffer);
	    
	    ComputeBuffer.CopyCount(triangleBuffer, renderArgsBuffer, 0);
	    renderArgsCompute.Dispatch(0, 1, 1, 1);

	    Bounds bounds = new Bounds(transform.position, dimensions);
	    
		Graphics.DrawProceduralIndirect(renderMaterial, bounds, MeshTopology.Triangles, renderArgsBuffer);
    }
    
    
	private void SetComputeParams()
	{
		marchingCubesCompute.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		marchingCubesCompute.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		marchingCubesCompute.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		marchingCubesCompute.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		marchingCubesCompute.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);
	    
		marchingCubesCompute.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);
	    
		marchingCubesCompute.SetFloat(Shader.PropertyToID("steepness"), noiseSettings.steepness);
		marchingCubesCompute.SetFloat(Shader.PropertyToID("centerSize"), noiseSettings.centerSize);
		marchingCubesCompute.SetBool(Shader.PropertyToID("applyFallOff"), noiseSettings.applyFallOffMap);
		
		marchingCubesCompute.SetVector(Shader.PropertyToID("dimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		marchingCubesCompute.SetVector(Shader.PropertyToID("globalPos"), float4(transform.position, 0f));
		marchingCubesCompute.SetVector(Shader.PropertyToID("localPos"), float4(transform.localPosition, 0f));
		
		marchingCubesCompute.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
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

