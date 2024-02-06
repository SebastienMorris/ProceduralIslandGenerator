using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] Renderer displayTexture;

    [SerializeField] Renderer[] display3DTextures;
    [SerializeField] Vector3Int layers;

    [Min(1)][SerializeField] private Vector3Int mapSize;
    [Min(0.01f)][SerializeField] private float noiseScale = 0.01f;
    [Range(1, 20)] [SerializeField] private int octaves;
    [Range(0,1)] [SerializeField] private float persistance;
    [Min(1)] [SerializeField] private float lacunarity;
    [Min(0)] [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;

    [SerializeField] private bool useSurfaceLevel = false;
    [SerializeField] private bool toggleNoiseMap = true;
    [Range(0, 1)] [SerializeField] private float surfaceLevel = 0.5f;

    [SerializeField] private FalloffMap falloffMap;
    [SerializeField] [Range(0, 10)] private float steepness = 3;
    [SerializeField] [Range(0, 10)] private float centerSize = 2.2f;
    [SerializeField] [Range(20, 2000)] private float resolution = 100;

    [SerializeField] private AnimationCurve falloffCurve;

    //[SerializeField] private Gradient colourGradient;

    private void OnValidate()
    {
        Generate3DFalloffMap();
    }

    private void GenerateMap()
    {
        NoiseData noiseData = LandMassNoise.CreateNoiseData(seed, octaves, noiseScale, persistance, lacunarity);

        if (toggleNoiseMap)
        {
            float[,] noiseMap = LandMassNoise.GenerateNoiseMap(new Vector2Int(mapSize.x, mapSize.y), seed, noiseScale, octaves, persistance, lacunarity, offset);
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

    private void Draw3DNoiseMap(float[,,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);
        int depth = noiseMap.GetLength(2);

        for(int i=0; i < display3DTextures.Length; i++)
        {
            switch (i)
            {
                case 0:
                    Texture2D texture = new Texture2D(width, height);

                    Color[] colourMap = new Color[width * height];

                    for (int x = 0; x < mapSize.x; x++)
                    {
                        for (int y = 0; y < mapSize.y; y++)
                        {
                            colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y, layers.x]);
                        }
                    }

                    texture.SetPixels(colourMap);
                    texture.Apply();

                    display3DTextures[i].sharedMaterial.mainTexture = texture;
                    display3DTextures[i].transform.localScale = new Vector3(width, 1 ,height);
                    break; 
                case 1:
                    texture = new Texture2D(width, depth);

                    colourMap = new Color[width * depth];

                    for (int x = 0; x < mapSize.x; x++)
                    {
                        for (int z = 0; z < mapSize.z; z++)
                        {
                            colourMap[z * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, layers.y, z]);
                        }
                    }

                    texture.SetPixels(colourMap);
                    texture.Apply();

                    display3DTextures[i].material.mainTexture = texture;
                    display3DTextures[i].transform.localScale = new Vector3(width, 1, depth);
                    break; 
                case 2:
                    texture = new Texture2D(depth, height);

                    colourMap = new Color[depth * height];

                    for (int z = 0; z < mapSize.z; z++)
                    {
                        for (int y = 0; y < mapSize.y; y++)
                        {
                            colourMap[y * depth + z] = Color.Lerp(Color.black, Color.white, noiseMap[layers.z, y, z]);
                        }
                    }

                    texture.SetPixels(colourMap);
                    texture.Apply();

                    display3DTextures[i].material.mainTexture = texture;
                    display3DTextures[i].transform.localScale = new Vector3(depth, 1, height);
                    break;

            }

            
        }
    }

    private void GenerateFalloffMap()
    {
        float[,] falloffMapValues = falloffMap.GenerateFallOffMap(new Vector2Int(mapSize.x, mapSize.y), steepness, centerSize);
        DrawNoiseMap(falloffMapValues);
    }

    private void Generate3DFalloffMap()
    {
        float[,,] fallOffMapValues = falloffMap.GenerateAltFallOffMap(mapSize, steepness, centerSize);
        //float[,,] fallOffMapValues = falloffMap.GenerateFallOffMap(mapSize, falloffCurve);
        Draw3DNoiseMap(fallOffMapValues);
    }
}