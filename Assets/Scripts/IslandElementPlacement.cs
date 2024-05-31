using System.Collections.Generic;
using UnityEngine;

public class IslandElementPlacement : MonoBehaviour
{
	public enum Influence
	{
		None,
		Forest,
		Deposit,
		NoPlacement,
	}
	public struct InfluenceZone
	{
		public Vector3 center;
		public float radius;
		public Influence influenceType;

		public override string ToString()
		{
			return $"Center : {center}, Radius : {radius}, Type : {influenceType}";
		}
	}

	private int spawnPassStep = 5;

	private List<Vector3> points = new();
	private List<InfluenceZone> influenceZones = new();
	private List<Vector2> rawRayOrigins = new();
	private List<GameObject> instantiatedPrefabs = new();

	private Vector3 center;
	private Vector3 islandSpreadSize3D;
	private Vector2 islandSpreadSize;
	private Transform islandTransform;
	private int islandMaxHeight;

	private int minHeight;
	private int maxHeight;

	public bool debugDisplay;

	private List<Vector3> displayOrigins = new();

	public void InitPlacement(Vector3 center, Vector3Int dimensions, Transform islandTransform,
							  ItemPlacementData[] itemsPlacementDatas, ZonePlacementData[] zonesPlacementDatas = null)
	{
		#region Enregistrement des infos de l'île sur laquelle placer les éléments.

		this.center = center;
		this.islandTransform = islandTransform;
		islandSpreadSize = new Vector2(dimensions.x, dimensions.z);
		islandSpreadSize3D = new Vector3(dimensions.x, 0, dimensions.z);
		islandMaxHeight = dimensions.y/ 2;
		#endregion

		if (zonesPlacementDatas != null)
		{
			foreach (var zoneData in zonesPlacementDatas)
			{
				minHeight = zoneData.MinHeight(islandMaxHeight);
				maxHeight = zoneData.MaxHeight(islandMaxHeight);

				InfluenceZonePass(zoneData);
			}
		}

		foreach(var itemData in itemsPlacementDatas)
		{
			minHeight = itemData.MinHeight(islandMaxHeight);
			maxHeight = itemData.MaxHeight(islandMaxHeight);

			// Passes d'instanciations d'élements sur l'île.
			for (int i = islandMaxHeight; i >= -islandMaxHeight; i -= spawnPassStep)
			{
				if (i < minHeight) break;
				if (i > maxHeight) continue;
				PointPlacementPass(itemData, i);
			}

			print("Placement des objets");
			ElementSpawn(itemData, zonesPlacementDatas != null);
		}
		print(instantiatedPrefabs.Count);
	}

	private void InfluenceZonePass(ZonePlacementData zoneData)
	{
		int nbZones = Random.Range(1, 4);
		int rayDistance = maxHeight - minHeight;

		rawRayOrigins = PoissonDiscSampling.GeneratePoints(zoneData.proximityRadius, islandSpreadSize/2);
		
		foreach (var origin in rawRayOrigins)
		{
			displayOrigins.Add(ProportionalPosition(new Vector3(origin.x, maxHeight, origin.y), islandSpreadSize3D / 2));
			if (Physics.Raycast(ProportionalPosition(new Vector3(origin.x, maxHeight, origin.y), islandSpreadSize3D / 2), Vector3.down, out RaycastHit hit, rayDistance))
			{
				//if (Vector3.Dot(hit.normal, Vector3.up) < zoneData.slopeThreshold) continue;
				influenceZones.Add(new InfluenceZone() { center = hit.point, influenceType = zoneData.influence, radius = Random.Range(10, 16) });
				
				if (influenceZones.Count == nbZones) return;
			}
		}
	}

	private void PointPlacementPass(ItemPlacementData itemData, float height)
	{
		rawRayOrigins = PoissonDiscSampling.GeneratePoints(itemData.proximityRadius, islandSpreadSize);

		foreach (var origin in rawRayOrigins)
		{
			if (Physics.Raycast(ProportionalPosition(new Vector3(origin.x, height, origin.y)), Vector3.down, out RaycastHit hit, spawnPassStep-1))
			{
				if (Vector3.Dot(hit.normal, Vector3.up) < itemData.slopeThreshold) continue;
				points.Add(hit.point);
			}
		}
	}

	private void ElementSpawn(ItemPlacementData itemData, bool influenced = false)
	{
		foreach (var pos in points)
		{
			if(influenced)
			{
				bool pointOK = true;
				foreach(var zone in influenceZones)
				{
					if (zone.influenceType != itemData.influence) continue;
					if (Vector3.Distance(zone.center, pos) > zone.radius)
					{
						pointOK = false;
						break;
					}
				}
				if (!pointOK) continue;
			}
			print("spawn d'un truc");
			GameObject el = Instantiate(itemData.RandomPrefab, pos, Quaternion.Euler(0, Random.Range(0, 361), 0), islandTransform);
			el.transform.localScale = itemData.RandomScale;
			instantiatedPrefabs.Add(el);
		}

		points.Clear();
	}

	private Vector3 ProportionalPosition(Vector3 rawPos, Vector3 customSize = default)
	{
		Vector3 size = customSize == default ? islandSpreadSize3D : customSize;
		return center - size / 2 + rawPos;
	}

	private void OnDrawGizmos()
	{
		if (!debugDisplay) return;

		Gizmos.color = Color.yellow;
		foreach (var point in points) Gizmos.DrawSphere(point, 0.2f);

		foreach(var zone in influenceZones)
		{
			Gizmos.color = zone.influenceType switch { Influence.Forest => Color.green, Influence.Deposit => Color.gray, Influence.NoPlacement => Color.black, _=> Color.clear };
			Gizmos.DrawCube(zone.center, Vector3.one);
			Gizmos.DrawWireSphere(zone.center, zone.radius);
		}

		#region Sélection des points

		/*Gizmos.color = Color.blue;

		foreach(var d in displayOrigins)
		{
			Gizmos.DrawSphere(d, 0.2f);
			Gizmos.DrawRay(d, Vector3.down * 20f);
		}

		int j = 0;
		for (int i = islandMaxHeight; i >= -islandMaxHeight; i -= spawnPassStep)
		{
			if (i < minHeight) return;
			if (i > maxHeight) continue;
			Gizmos.DrawWireCube(center + Vector3.up * i, islandSpreadSize3D);
		}*/
		#endregion
	}
}
