using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandChunk : MonoBehaviour
{
	public Vector3Int coord;

	[HideInInspector]
	public Mesh mesh;

	MeshFilter meshFilter;
	MeshRenderer meshRenderer;
	MeshCollider meshCollider;

	/*public void DestroyOrDisable()
	{
		if (Application.isPlaying)
		{
			mesh.Clear();
			gameObject.SetActive(false);
		}
		else
		{
			DestroyImmediate(gameObject, false);
		}
	}*/

	public void Initialise(Material mat)
	{
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		meshCollider = GetComponent<MeshCollider>();

		if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
		if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
		if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

		mesh = meshFilter.sharedMesh;
		if (mesh == null)
		{
			mesh = new Mesh();
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			meshFilter.sharedMesh = mesh;
		}

		if (meshCollider.sharedMesh == null) meshCollider.sharedMesh = mesh;

		// force update
		meshCollider.enabled = false;
		meshCollider.enabled = true;

		meshRenderer.material = mat;
	}
}