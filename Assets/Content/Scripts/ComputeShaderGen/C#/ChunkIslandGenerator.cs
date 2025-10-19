
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Vector3 = UnityEngine.Vector3;

public class ChunkIslandGenerator : MonoBehaviour
{
	[SerializeField] private bool debug;

	[SerializeField] private ComputeShader chunkMarchCompute;

	[SerializeField] private Material meshMaterial;

	[SerializeField] private Vector3Int dimensions = new(0, 0, 0);

	private const int chunkSize = 10;
	private const int numVoxels = chunkSize * chunkSize * chunkSize;
	private const int maxTriangleCount = numVoxels * 5;
	private const int CHUNK_BYTE_SIZE = (sizeof(float) * 3 * 2) + (sizeof(float) * 3 * maxTriangleCount * 3) +
	                    (sizeof(int) * 3 * maxTriangleCount);

	[SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

	[SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;

	private ComputeBuffer chunkBuffer;
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
		SetupBuffers();
	}

	private void OnDisable()
	{
		StopCalculation();
		ClearBuffers();
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.G))
		{
			simulate = !simulate;

			if (simulate)
			{
				StartCalculation();
			}
			else
			{
				StopCalculation();
			}
		}
	}

	private void LateUpdate()
	{
		if (chunks != null)
		{
			for (int i = 0; i < chunks.Length; i++)
			{
				if (chunks[i].mesh != null)
				{
					Bounds bounds = new Bounds(chunks[i].position, new Vector3(chunkSize, chunkSize, chunkSize));
					Graphics.DrawMeshInstancedProcedural(chunks[i].mesh, 0, meshMaterial, bounds, 1);
				}
			}
		}
	}


	private void SetupBuffers()
	{
		Vector3Int numChunks = dimensions / chunkSize;
		int nbChunks = numChunks.x * numChunks.y * numChunks.z;
		
		chunkBuffer = new ComputeBuffer(numVoxels, CHUNK_BYTE_SIZE, ComputeBufferType.Append);
		chunks = new Chunk[nbChunks];

		chunkMarchCompute.SetBuffer(0, Shader.PropertyToID("DrawChunks"), chunkBuffer);
	}

	private void ClearBuffers()
	{
		chunkBuffer.Release();
		chunks = null;
	}

	private void ResetBuffers()
	{
		chunkBuffer.SetCounterValue(0);
	}

	private void ResetChunks()
	{
		Array.Clear(chunks, 0, chunks.Length);
	}

	private void StartCalculation()
	{
		StartCoroutine(CalculateCoroutine(dimensions / chunkSize));
	}

	private void StopCalculation()
	{
		StopCoroutine(CalculateCoroutine(Vector3Int.zero));
		ResetBuffers();
		ResetChunks();
	}

	private IEnumerator CalculateCoroutine(Vector3Int numChunks)
	{
		while (simulate)
		{
			ResetBuffers();
			SetComputeParams();
			
			chunkMarchCompute.Dispatch(0, numChunks.x, numChunks.y, numChunks.z);
			
			chunkBuffer.GetData(chunks, 0, 0, numChunks.x * numChunks.z * numChunks.z);

			yield return new WaitForEndOfFrame();
		}

		yield return null;
	}
	

	private void SetComputeParams()
	{
		chunkMarchCompute.SetInt(Shader.PropertyToID("seed"), noiseSettings.seed);
		chunkMarchCompute.SetInt(Shader.PropertyToID("frequency"), noiseSettings.frequency);
		chunkMarchCompute.SetInt(Shader.PropertyToID("octaves"), noiseSettings.octaves);
		chunkMarchCompute.SetInt(Shader.PropertyToID("lacunarity"), noiseSettings.lacunarity);
		chunkMarchCompute.SetFloat(Shader.PropertyToID("persistence"), noiseSettings.persistence);

		chunkMarchCompute.SetFloat(Shader.PropertyToID("scale"), noiseSettings.scale);

		chunkMarchCompute.SetFloat(Shader.PropertyToID("steepness"), noiseSettings.steepness);
		chunkMarchCompute.SetFloat(Shader.PropertyToID("centerSize"), noiseSettings.centerSize);
		chunkMarchCompute.SetBool(Shader.PropertyToID("applyFallOff"), noiseSettings.applyFallOffMap);

		chunkMarchCompute.SetVector(Shader.PropertyToID("globalDimensions"),
			float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
		chunkMarchCompute.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
		
		chunkMarchCompute.SetVector(Shader.PropertyToID("worldOrigin"), float4(transform.position, 0f));
		chunkMarchCompute.SetInt(Shader.PropertyToID("maxTriangles"), chunkSize * chunkSize * chunkSize * 5);
	}
}

