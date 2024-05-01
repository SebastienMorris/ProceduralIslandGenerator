using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandChunk : MonoBehaviour
{
	[HideInInspector]
	public Mesh mesh;

	[SerializeField] MeshFilter meshFilter;
	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshCollider meshCollider;

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

	public void Initialise(Mesh mesh)
	{
		this.mesh = mesh;
		meshFilter.sharedMesh = mesh;
		meshCollider.sharedMesh = mesh;

		// force update
		meshCollider.enabled = false;
		meshCollider.enabled = true;
	}
}