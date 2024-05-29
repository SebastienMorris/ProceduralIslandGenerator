using UnityEngine;
using static IslandElementPlacement;


/// <summary> Objet de stockage des information de placement d'un type d'élément. </summary>

public abstract class PlacementData : ScriptableObject
{
	#region Attributs

	#region Paramètres de placement

	[Header("Paramètres de placement"), Range(0,1), Tooltip("Inclinaison maximale de la face sur laquelle placer les éléments, "
	+"correspond à un produit scalaire entre un raycast vertical et la normal de la face :\n 0 = horizontal\n 1 = vertical")]
	public float slopeThreshold;

	[Tooltip("Distance minimum en mètres entre 2 éléments placés.")]
	public float proximityRadius;

	[SerializeField, Range(-100, 100), Tooltip("Pourcentage minimum de la hauteur de l'île par rapport au centre où l'élément peut être placé.")]
	protected int minHeight;
	[SerializeField, Range(-100, 100), Tooltip("Pourcentage maximum de la hauteur de l'île par rapport au centre où l'élément peut être placé.")]
	protected int maxHeight;

	[Tooltip("Type d'influence.")]
	public Influence influence;
	#endregion

	#endregion

	#region Accesseurs

	/// <summary> Accesseur sur une hauteur minimale par rapport à la hauteur de l'île. </summary>
	/// <param name="islandHeight"> La hauteur de l'île. </param>
	/// <returns> Une valeur de hauteur en mètres. </returns>
	public int MinHeight(int islandHeight) { return islandHeight * (minHeight / 100); }
	/// <summary> Accesseur sur une hauteur maximale par rapport à la hauteur de l'île. </summary>
	/// <param name="islandHeight"> La hauteur de l'île. </param>
	/// <returns> Une valeur de hauteur en mètres. </returns>
	public int MaxHeight(int islandHeight) { return islandHeight * (maxHeight / 100); }

	#endregion

	protected virtual void OnValidate()
	{
		// Vérifications d'input.
		minHeight = Mathf.Min(minHeight, maxHeight);
		maxHeight = Mathf.Max(minHeight, maxHeight);
	}
}
