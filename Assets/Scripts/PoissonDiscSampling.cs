using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiscSampling
{
	/// <summary> Permet de générer des points aléatoirement distribués dans une zone. </summary>
	/// <param name="radius"> L'écart entre chaque points placés. </param>
	/// <param name="sampleRegionSize"> La taille de la zone dans laquelle poser les points. </param>
	/// <param name="nbSamplesBeforeRejection"> Le nombre d'essais de placements de points avant abandon. </param>
	/// <param name="nbPoints"> Le nombre de points à placer au total. </param>
	/// <returns> Une liste de points dans l'espace 2D. </returns>
	public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int nbPoints = -1, int nbSamplesBeforeRejection = 30)
	{
		// Initialisation de la taille des cellules en fonction de l'écart entre chaque points.
		float cellSize = radius / Mathf.Sqrt(2);

		// Initialisation des listes de cellules, de points et des points temporaires.
		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
		List<Vector2> points = new();
		List<Vector2> spawnPoints = new() { sampleRegionSize / 2 };

		// Le premier point.
		//bool firstPoint = true;

		// On tente de générer des points tant que l'on peut en placer dans la zone ou jusqu'à ce qu'on ait généré assez de points.
		while (spawnPoints.Count > 0 || points.Count == nbPoints)
		{
			int spawnIndex = Random.Range(0, spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];

			bool candidateAccepted = false;

			for (int i = 0; i < nbSamplesBeforeRejection; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				Vector2 dir = new(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2 candidate;
				/*if (firstPoint)
				{
					candidate = sampleRegionSize / 2;
					firstPoint = false;
				}
				else*/
				candidate = spawnCentre + dir * Random.Range(radius, radius * 2);

				if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
				{
					points.Add(candidate);
					spawnPoints.Add(candidate);
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
					candidateAccepted = true;
					break;
				}
			}

			if (!candidateAccepted) spawnPoints.RemoveAt(spawnIndex);
		}

		return points;
	}

	private static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
	{
		if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
		{
			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);
			int searchStartX = Mathf.Max(0, cellX - 2);
			int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
			int searchStartY = Mathf.Max(0, cellY - 2);
			int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

			for (int x = searchStartX; x <= searchEndX; x++)
			{
				for (int y = searchStartY; y <= searchEndY; y++)
				{
					int pointIndex = grid[x, y] - 1;
					if (pointIndex != -1)
					{
						float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
						if (sqrDst < radius * radius) return false;
					}
				}
			}
			return true;
		}

		return false;
	}
}
