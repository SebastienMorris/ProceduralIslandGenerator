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
    [SerializeField] private bool toggleNoiseMap = true;
    [Range(0, 1)] [SerializeField] private float surfaceLevel = 0.5f;

    //[SerializeField] private Gradient colourGradient;

    private void OnValidate()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        NoiseData noiseData = LandMassNoise.CreateNoiseData(seed, octaves, noiseScale, persistance, lacunarity);

        if (toggleNoiseMap)
        {
            float[,] noiseMap = LandMassNoise.GenerateNoiseMap(mapSize, seed, noiseScale, octaves, persistance, lacunarity, offset);
            DrawNoiseMap(noiseMap);
        }
        else
        {
            /*float[,,] noiseMap = LandMassNoise.Generate3DNoiseMap(new Vector3Int(mapSize.x, mapSize.y, mapSize.x), seed, noiseScale, octaves, persistance, lacunarity, new Vector3(offset.x, offset.y, offset.x));
            float[,] noiseMap2D = new float[noiseMap.GetLength(0), noiseMap.GetLength(1)];
            for(int x=0; x<noiseMap2D.GetLength(0); x++)
            {
                for(int y = 0; y<noiseMap2D.GetLength(1); y++)
                {
                    noiseMap2D[x, y] = noiseMap[x, y, 0];
                }
            }
            DrawNoiseMap(noiseMap2D);*/
            float[,] noiseMap2D = new float[mapSize.x, mapSize.y];
            for(int x = 0; x<mapSize.x; x++)
            {
                for(int y = 0; y<mapSize.y; y++)
                {
                    for(int z=0; z<mapSize.x; z++)
                    {
                        float sampleX = x + offset.x;
                        float sampleY = y + offset.y;
                        float samplez = 0 + offset.x;

                        noiseMap2D[x, y] = LandMassNoise.Noise3D(sampleX, sampleY, samplez,noiseData);
                    }
                }
            }
            DrawNoiseMap(noiseMap2D);

        }

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
