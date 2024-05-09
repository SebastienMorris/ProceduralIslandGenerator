using System.Collections.Generic;
using UnityEngine;

public class IslandElementPlacement : MonoBehaviour
{
	private int spawnPassStep = 5;

	private List<Vector3> points = new();
	private List<Vector2> rawRayOrigins = new();
	private List<List<Vector2>> displayAllOrigins = new();
	private List<GameObject> instantiatedPrefabs = new();

	public PlacementData elementDataToSpawn;

	private Vector3 center;
	private Vector3 islandSpreadSize3D;
	private Vector2 islandSpreadSize;
	private Transform islandTransform;
	private int islandMaxHeight;

	private void OnDrawGizmos()
	{
		/*Gizmos.color = Color.red;
		foreach (var point in displayAllOrigins) Gizmos.DrawRay(ProportionalPosition(new Vector3(point.x, islandMaxHeight / 2, point.y)), Vector3.down * islandMaxHeight);*/

		Gizmos.color = Color.yellow;
		foreach (var point in points) Gizmos.DrawSphere(point, 0.2f);

		Gizmos.color = Color.blue;

		int j = 0;
		for (int i = islandMaxHeight; i >= -islandMaxHeight; i -= spawnPassStep)
		{
			if (i < elementDataToSpawn.MinHeight(islandMaxHeight)) return;
			if (i > elementDataToSpawn.MaxHeight(islandMaxHeight)) continue;
			//foreach (var point in displayAllOrigins[j++]) Gizmos.DrawSphere(ProportionalPosition(new Vector3(point.x, i, point.y)), 0.2f);
			Gizmos.DrawWireCube(center + Vector3.up * i, islandSpreadSize3D);
		}
	}

	public void InitPlacement(Vector3 center, Vector3Int islandDimensions, Transform islandTransform)
	{
		// Enregistrement des infos de l'île sur laquelle placer les éléments.
		this.center = center;
		this.islandTransform = islandTransform;
		islandSpreadSize = new Vector2(islandDimensions.x, islandDimensions.z);
		islandSpreadSize3D = new Vector3(islandDimensions.x, 0, islandDimensions.z);
		islandMaxHeight = islandDimensions.y/ 2;

		print(elementDataToSpawn.MinHeight(islandMaxHeight));
		print(elementDataToSpawn.MaxHeight(islandMaxHeight));

		// Passes d'instanciations d'élements sur l'île.
		for (int i = islandMaxHeight; i >= -islandMaxHeight; i -= spawnPassStep)
		{
			if (i < elementDataToSpawn.MinHeight(islandMaxHeight)) return;
			if (i > elementDataToSpawn.MaxHeight(islandMaxHeight)) continue;
			PointPlacementPass(i);
		}

		ElementSpawn();
	}

	private void PointPlacementPass(float height)
	{
		print($"pass : {height}");
		rawRayOrigins = PoissonDiscSampling.GeneratePoints(elementDataToSpawn.proximityRadius, islandSpreadSize);
		displayAllOrigins.Add(new List<Vector2>(rawRayOrigins));

		foreach (var origin in rawRayOrigins)
		{
			if (Physics.Raycast(ProportionalPosition(new Vector3(origin.x, height, origin.y)), Vector3.down, out RaycastHit hit, spawnPassStep-1))
			{
				if (Vector3.Dot(hit.normal, Vector3.up) < elementDataToSpawn.slopeThreshold) continue;
				points.Add(hit.point);
			}
		}
	}

	private void ElementSpawn()
	{
		foreach (var pos in points)
		{
			GameObject el = Instantiate(elementDataToSpawn.RandomPrefab, pos, Quaternion.Euler(0, Random.Range(0, 361), 0), islandTransform);
			el.transform.localScale = elementDataToSpawn.RandomScale;
			instantiatedPrefabs.Add(el);
		}
	}


	private Vector3 ProportionalPosition(Vector3 rawPos){ return center - islandSpreadSize3D/2 + rawPos; }

	
}
