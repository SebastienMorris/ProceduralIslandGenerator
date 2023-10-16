using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] Renderer displayTexture;

    [Min(1)][SerializeField] private Vector2Int mapSize;
    [Min(0.01f)][SerializeField] private float noiseScale = 0.01f;
    [Range(1, 20)] [SerializeField] private int octaves;
    [Range(0,1)] [SerializeField] private float persistance;
    [Min(1)] [SerializeField] private float lacunarity;
    [Min(0)] [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;

    [SerializeField] private bool useSurfaceLevel = false;
    [Range(0, 1)] [SerializeField] private float surfaceLevel = 0.5f;

    //[SerializeField] private Gradient colourGradient;

    private void OnValidate()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        float[,] noiseMap = LandMassNoise.GenerateNoiseMap(mapSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        DrawNoiseMap(noiseMap);
    }

    private void DrawNoiseMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colourMap = new Color[width * height];

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (useSurfaceLevel)
                {
                    if (noiseMap[x, y] >= surfaceLevel)
                    {
                        colourMap[y * width + x] = Color.white;
                    }
                    else
                    {
                        colourMap[y * width + x] = Color.black;
                    }
                }
                else
                    colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x,y]);
            }
        }

        texture.SetPixels(colourMap);
        texture.Apply();

        displayTexture.sharedMaterial.mainTexture = texture;
        displayTexture.transform.localScale = new Vector3(width, 1, height);
    }
}
