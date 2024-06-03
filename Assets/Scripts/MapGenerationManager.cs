using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

public class MapGenerationManager : MonoBehaviour
{
	#region Attributs

	[SerializeField] private GameObject islandGenPrefab;
	[SerializeField] private int globalSeed;
	[SerializeField] 
	private static GameObject staticIslandGenPrefab;

	Vector2[] NEIGHBOOR_CHUNKS_OFFSETS = new Vector2[]
	{
		new(0,1), new(1,1), new(1,0), new(1,-1),
		new(0,1), new(-1,-1), new(-1,0), new(-1,1)
	};

	public static Vector2 viewerPosition;
	private static readonly int CHUNK_SIZE = 500;
	private static readonly float DST_BETWEEN_ISLANDS = 300;
	public const float MAX_VIEW_DISTANCE = 750;

	public Transform viewer;
	private int chunkVisibleInViewDst;

	public readonly Dictionary<Vector2, TerrainChunk> terrainChunks = new();
	#endregion

	#region Méthodes Unity

	void Start()
	{
		chunkVisibleInViewDst = Mathf.RoundToInt(MAX_VIEW_DISTANCE / CHUNK_SIZE);
		staticIslandGenPrefab = islandGenPrefab;
		CreateChunk(Vector2.zero);
	}

	void Update()
	{
		viewerPosition = new(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks();
	}

	private void OnDrawGizmos()
	{
		foreach (TerrainChunk chunk in terrainChunks.Values)
		{
			Gizmos.color = chunk.IsEmpty ? Color.black : Color.blue;
			if (chunk.visible)
			{
				Gizmos.DrawWireCube(new(chunk.position.x, 0, chunk.position.y), new Vector3(CHUNK_SIZE, 1f, CHUNK_SIZE));

				/*if (chunk.points.Count != 0)
				{
					foreach (Vector3 point in chunk.points)
					{
						Gizmos.color = Color.yellow;
						Gizmos.DrawSphere(point, 5f);
					}
				}*/
			}
		}
	}
	#endregion

	void UpdateVisibleChunks()
	{
		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/ CHUNK_SIZE);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/ CHUNK_SIZE);

		for (int yOffset = -chunkVisibleInViewDst; yOffset <= chunkVisibleInViewDst; ++yOffset)
		{
			for (int xOffset = -chunkVisibleInViewDst; xOffset <= chunkVisibleInViewDst; ++xOffset)
			{
				Vector2 viewedChunkCoord = new(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if (terrainChunks.ContainsKey(viewedChunkCoord)) terrainChunks[viewedChunkCoord].UpdateChunk();
				else CreateChunk(viewedChunkCoord);
			}
		}
	}

	public static IslandGenerator SpawnIslandGenerator(Vector3 pos)
	{
		return Instantiate(staticIslandGenPrefab, pos, Quaternion.identity).GetComponent<IslandGenerator>();
	}

	void CreateChunk(Vector2 chunkCoords)
	{
		terrainChunks.Add(chunkCoords, new(chunkCoords, CHUNK_SIZE, CalculateIslandSpawnChance(chunkCoords), chunkCoords == Vector2.zero));
	}

	private float CalculateIslandSpawnChance(Vector2 chunkCoords)
	{
		print("Chunk à : " +chunkCoords);
		if (chunkCoords == Vector2.zero)
		{
			print("Spawn chance = 1");
			return 1;
		}
		float islandSpawnCoef = 0;
		foreach(Vector2 offset in NEIGHBOOR_CHUNKS_OFFSETS)
		{
			Vector2 neighboorChunkCoords = chunkCoords + offset;
			if (terrainChunks.ContainsKey(neighboorChunkCoords))
			{
				float neighboorSpawnChance = terrainChunks[neighboorChunkCoords].islandSpawnChance;
				if (neighboorSpawnChance == 1)
				{
					print("Spawn chance = 0");
					return 0;
				}
				if (neighboorSpawnChance > islandSpawnCoef) islandSpawnCoef = neighboorSpawnChance;
			}
		}

		print($"Spawn chance = {islandSpawnCoef + 0.15f}");
		return islandSpawnCoef + 0.15f;
	}

	public class TerrainChunk
	{
		public List<GameObject> islands = new();
		public List<Vector3> points = new();

		public float islandSpawnChance;
		public bool generating;

		public bool IsEmpty { get { return points.Count == 0; } }
		//public bool IsEmpty { get { return islands.Count == 0; } }

		public Vector2 position;
		private Bounds bounds;

		public bool visible;

		public TerrainChunk(Vector2 coord, int size, float spawnChance, bool start)
		{
			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			islandSpawnChance = spawnChance;
			
			if(Random.Range(0f, 1f) < islandSpawnChance)
			{
				List<Vector2> rawPoints;
				int archipelagoSize = Random.Range(1, 4);

				if (start) rawPoints = new() { new(CHUNK_SIZE/2,CHUNK_SIZE/2) };
				else rawPoints = PoissonDiscSampling.GeneratePoints(DST_BETWEEN_ISLANDS, new(CHUNK_SIZE, CHUNK_SIZE));

				foreach (Vector2 point in rawPoints)
				{
					float height = start ? 0 : Random.Range(-200f, 200f);
					points.Add(new Vector3(position.x, height, position.y) - new Vector3(CHUNK_SIZE, 0, CHUNK_SIZE) / 2 + new Vector3(point.x, 0, point.y));
					if (points.Count > archipelagoSize) break;
				}

				foreach (var point in points)
				{
					IslandGenerator gen = SpawnIslandGenerator(point);

					#region Paramètres de génération de l'île.

					if (start)
					{
						gen.dimensions = new(150, 40, 150);
						gen.noiseSettings = NoiseSettings.StartIsland;
						gen.steepness = 2.18f;
					}
					else
					{
						gen.noiseSettings.seed = Random.Range(2, int.MaxValue);
						gen.dimensions = new(Random.Range(5, 10)*10, Random.Range(2, 5)*10, Random.Range(5, 10) * 10);
						//gen.noiseSettings = NoiseSettings.Default;
					}

					#endregion
					gen.InitChunks();
					islands.Add(gen.gameObject);
					generating = true;
					gen.OnIslandGenerated += () =>
					{
						gen.transform.rotation = Quaternion.Euler(new(0, Random.Range(0f, 360f), 0));
						generating = false;
						UpdateChunk();
					};
				}
				islandSpawnChance = 0;
			}

			SetVisibility(false);
		}

		public void UpdateChunk()
		{
			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
			SetVisibility(viewerDstFromNearestEdge <= MAX_VIEW_DISTANCE);
		}

		public void SetVisibility(bool visible)
		{
			if (generating) return;
			this.visible = visible;
			if (IsEmpty) return;

			foreach(GameObject island in islands) island.SetActive(visible);
		}
	}
}
