using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemPlacementData", menuName = "ScriptableObjects/Item Placement Data", order = 1)]
public class ItemPlacementData : PlacementData
{
	#region Paramètres des éléments à placer

	[SerializeField, Header("Paramètres des éléments à placer"), Tooltip("Taille minimale de l'élément.")]
	private float minScale;
	[SerializeField, Tooltip("Taille maximale de l'élément.")]
	private float maxScale;
	[SerializeField, Tooltip("Liste des prefabs des éléments à placer.")]
	private GameObject[] prefabs;
	#endregion

	#region Accesseurs

	/// <summary> Accesseur sur un préfab aléatoire. </summary>
	public GameObject RandomPrefab { get { return prefabs[Random.Range(0, prefabs.Length)]; } }
	/// <summary> Accesseur direct sur un préfab dans la liste. </summary>
	/// <param name="index"> L'index du préfab. </param>
	public GameObject GetPrefab(int index) { return prefabs[index]; }

	/// <summary> Accesseur sur une taille aléatoire comprise dans l </summary>
	public Vector3 RandomScale
	{
		get
		{
			float scale = Random.Range(minScale, maxScale + 1);
			return new Vector3(scale, scale, scale);
		}
	}
	#endregion

	protected override void OnValidate()
	{
		base.OnValidate();
		minScale = Mathf.Min(minScale, maxScale);
		maxScale = Mathf.Max(minScale, maxScale);
	}
}
