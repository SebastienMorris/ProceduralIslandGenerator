using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static IslandElementPlacement;

[CreateAssetMenu(fileName = "ZonePlacementData", menuName = "ScriptableObjects/Zone Placement Data", order = 1)]
public class ZonePlacementData : PlacementData
{
	[Range(1,50), Header("Paramètres de la zone"), Tooltip("Rayon minimal en mètres de la zone.")]
	public float minRadius;
	[Range(1,50), Tooltip("Rayon minimal en mètres de la zone.")]
	public float maxRadius;

	protected override void OnValidate()
	{
		base.OnValidate();
		minRadius = Mathf.Min(minRadius, maxRadius);
		maxRadius = Mathf.Max(minRadius, maxRadius);
	}
}
