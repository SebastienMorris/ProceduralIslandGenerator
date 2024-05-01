using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandElementPlacement : MonoBehaviour
{
	List<Vector3> points = new();

	public struct SpawnableData
	{
		public float slopeThreshold;
		public float proximityRadius;
		public GameObject[] prefabs;
		public float[] scaleRange;
	}

	[SerializeField] float slopeThreshold = 0.6f;

	public SpawnableData spawnable;

	// TMP
	private Vector3 center;
	private float islandMaxHeight;
	private Vector3 islandSize;
	List<Vector2> points2D = new ();

	public GameObject testRb;

	public void InitPlacement(Vector3 center, float islandMaxHeight, Vector2 islandSpreadSize)
	{
		testRb.SetActive(true);

		print($"Center : {center}");
		print($"Height : {islandMaxHeight}");
		print($"Size : {islandSpreadSize.x} : {islandSpreadSize.y}");

		this.center = center;
		this.islandMaxHeight = islandMaxHeight;
		islandSize = new Vector3(islandSpreadSize.x, 0, islandSpreadSize.y);

		points2D = PoissonDiscSampling.GeneratePoints(5, islandSpreadSize);

		foreach(var p in points2D)
		{
			if (Physics.Raycast(center - islandSize / 2 + new Vector3(p.x, islandMaxHeight, p.y), Vector3.down, out RaycastHit hit, islandMaxHeight))
			{
				if (Vector3.Dot(hit.normal, Vector3.up) < slopeThreshold) continue;
				points.Add(hit.point);
			}
		}

		print(points.Count);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(center + Vector3.up * islandMaxHeight/2, islandSize);
		
		foreach(var point in points2D) Gizmos.DrawSphere(center - islandSize / 2 + new Vector3(point.x, islandMaxHeight/2, point.y), 1);

		Gizmos.color = Color.red;
		foreach (var point in points2D) Gizmos.DrawRay(center - islandSize / 2 + new Vector3(point.x, islandMaxHeight/2, point.y), Vector3.down * islandMaxHeight);

		Gizmos.color = Color.yellow;
		foreach (var point in points)
		{
			Gizmos.DrawSphere(point,1);
		}
	}
}
