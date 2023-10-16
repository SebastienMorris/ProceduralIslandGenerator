using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LandMassNoise
{
    public static float[,] GenerateNoiseMap(Vector2Int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapSize.x, mapSize.y];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i<octaves; i++)
        {
            float offsetX = prng.Next(-1000000, 1000000);
            float offsetY = prng.Next(-1000000, 1000000);

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float minNoiseValue = float.MaxValue;
        float maxNoiseValue = float.MinValue;

        float halfWidth = mapSize.x / 2;
        float halfHeight = mapSize.y / 2;

        for(int x = 0; x < mapSize.x; x++)
        {
            for(int y = 0; y < mapSize.y; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseValue = 0;

                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x + offset.x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y + offset.y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    noiseValue += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if(noiseValue > maxNoiseValue)
                {
                    maxNoiseValue = noiseValue;
                }
                else if(noiseValue < minNoiseValue)
                { 
                    minNoiseValue = noiseValue;
                }

                noiseMap[x, y] = noiseValue;
            }
        }

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x, y]);
            }
        }

                return noiseMap;
    }
}
