using UnityEngine;

/// <summary> Objet de stockage des information de placement d'un type d'élément. </summary>
[CreateAssetMenu(fileName = "PlacementData", menuName = "ScriptableObjects/Element Placement Data", order = 1)]
public class PlacementData : ScriptableObject
{
	#region Attributs

	#region Paramètres de placement

	[Header("Paramètres de placement"), Range(0,1), Tooltip("Inclinaison maximale de la face sur laquelle placer les éléments, "
	+"correspond à un produit scalaire entre un raycast vertical et la normal de la face :\n 0 = horizontal\n 1 = vertical")]
	public float slopeThreshold;
	[Tooltip("Distance minimum en mètres entre 2 éléments placés.")]
	public float proximityRadius;
	[SerializeField, Range(-100, 100), Tooltip("Pourcentage minimum de la hauteur de l'île par rapport au centre où l'élément peut être placé.")]
	private int minHeight;
	[SerializeField, Range(-100, 100), Tooltip("Pourcentage maximum de la hauteur de l'île par rapport au centre où l'élément peut être placé.")]
	private int maxHeight;
	#endregion

	#region Paramètres des éléments à placer

	[SerializeField, Header("Paramètres des éléments à placer"), Tooltip("Taille minimale de l'élément.")]
	private float minScale;
	[SerializeField, Tooltip("Taille maximale de l'élément.")]
	private float maxScale;
	[SerializeField, Tooltip("Liste des prefabs des éléments à placer.")]
	private GameObject[] prefabs;
	#endregion

	#endregion

	#region Accesseurs

	/// <summary> Accesseur sur un préfab aléatoire. </summary>
	public GameObject RandomPrefab { get { return prefabs[Random.Range(0,prefabs.Length)]; } }
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

	/// <summary> Accesseur sur une hauteur minimale par rapport à la hauteur de l'île. </summary>
	/// <param name="islandHeight"> La hauteur de l'île. </param>
	/// <returns> Une valeur de hauteur en mètres. </returns>
	public int MinHeight(int islandHeight) { return islandHeight * (minHeight / 100); }
	/// <summary> Accesseur sur une hauteur maximale par rapport à la hauteur de l'île. </summary>
	/// <param name="islandHeight"> La hauteur de l'île. </param>
	/// <returns> Une valeur de hauteur en mètres. </returns>
	public int MaxHeight(int islandHeight) { return islandHeight * (maxHeight / 100); }

	#endregion

	private void OnValidate()
	{
		// Vérifications d'input.
		minHeight = Mathf.Min(minHeight, maxHeight);
		maxHeight = Mathf.Max(minHeight, maxHeight);

		minScale = Mathf.Min(minScale, maxScale);
		maxScale = Mathf.Max(minScale, maxScale);
	}
}
