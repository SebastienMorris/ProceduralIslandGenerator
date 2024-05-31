using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerationManager : MonoBehaviour
{
	#region Attributs

	Vector2[] NEIGHBOOR_CHUNKS_OFFSETS = new Vector2[]
	{
		new(0,1), new(1,1), new(1,0), new(1,-1),
		new(0,1), new(-1,-1), new(-1,0), new(-1,1)
	};

	public static Vector2 viewerPosition;
	
	private static readonly int CHUNK_SIZE = 500;
	private static readonly float DST_BETWEEN_ISLANDS = 300;
	public const float MAX_VIEW_DISTANCE = 750;

	bool startingChunk = true;

	public Transform viewer;
	private int chunkVisibleInViewDst;

	public readonly Dictionary<Vector2, TerrainChunk> terrainChunks = new();
	#endregion

	void Start()
	{
		chunkVisibleInViewDst = Mathf.RoundToInt(MAX_VIEW_DISTANCE / CHUNK_SIZE);
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

				if (chunk.points.Count != 0)
				{
					foreach (Vector3 point in chunk.points)
					{
						Gizmos.color = Color.yellow;
						Gizmos.DrawSphere(point, 5f);
					}
				}
			}
		}
	}

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
				else CreateChunk(viewedChunkCoord, startingChunk);

				startingChunk = false;
			}
		}
	}

	void CreateChunk(Vector2 chunkCoords, bool firstChunk)
	{
		terrainChunks.Add(chunkCoords, new(chunkCoords, CHUNK_SIZE, CalculateIslandSpawnChance(chunkCoords)));
	}

	private float CalculateIslandSpawnChance(Vector2 chunkCoords)
	{
		float islandSpawnCoef = 0;
		foreach(Vector2 offset in NEIGHBOOR_CHUNKS_OFFSETS)
		{
			Vector2 neighboorChunkCoords = chunkCoords + offset;
			if (terrainChunks.ContainsKey(neighboorChunkCoords)) islandSpawnCoef += terrainChunks[neighboorChunkCoords].islandSpawnChance;
		}

		return islandSpawnCoef;
	}

	public class TerrainChunk
	{
		//public List<Island> islands;
		public List<Vector3> points = new();

		public float islandSpawnChance;

		public bool IsEmpty { get { return points.Count == 0; } }
		//public bool IsEmpty { get { return islands.Count == 0; } }

		public Vector2 position;
		private Bounds bounds;

		public bool visible;

		public TerrainChunk(Vector2 coord, int size, float spawnChance)
		{
			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			islandSpawnChance = spawnChance;
			
			if(Random.Range(0f, 1f) > islandSpawnChance)
			{
				List<Vector2> rawPoints = PoissonDiscSampling.GeneratePoints(DST_BETWEEN_ISLANDS, new(CHUNK_SIZE, CHUNK_SIZE));
				int archipelagoSize = Random.Range(1, 4);
				foreach (Vector2 point in rawPoints)
				{
					points.Add(new Vector3(position.x, 0, position.y) - new Vector3(CHUNK_SIZE, 0, CHUNK_SIZE) / 2 + new Vector3(point.x, 0, point.y));
					if (points.Count > archipelagoSize) break;
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
			this.visible = visible;
			//if (IsEmpty) return;

			//foreach(Island island in islands) island.SetVisibility(visible);
		}
	}


}
