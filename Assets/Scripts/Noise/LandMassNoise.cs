using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

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

    public static float[,,] Generate3DNoiseMap(Vector3Int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector3 offset)
    {
        float[,,] noiseMap = new float[mapSize.x, mapSize.y,mapSize.z];

        System.Random prng = new System.Random(seed);
        Vector3[] octaveOffsets = new Vector3[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-1000000, 1000000);
            float offsetY = prng.Next(-1000000, 1000000);
            float offsetZ = prng.Next(-1000000, 1000000);

            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
        }

        float minNoiseValue = float.MaxValue;
        float maxNoiseValue = float.MinValue;

        float halfWidth = mapSize.x / 2;
        float halfHeight = mapSize.y / 2;
        float halfLength = mapSize.z / 2;

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int z = 0; z < mapSize.z; z++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseValue = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x + offset.x;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y + offset.y;
                        float sampleZ = (z - halfLength) / scale * frequency + octaveOffsets[i].z + offset.z;

                        float AB = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        float BC = Mathf.PerlinNoise(sampleY, sampleZ) * 2 - 1;
                        float AC = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;

                        float BA = Mathf.PerlinNoise(sampleY, sampleX) * 2 - 1;
                        float CB = Mathf.PerlinNoise(sampleZ, sampleY) * 2 - 1;
                        float CA = Mathf.PerlinNoise(sampleZ, sampleX) * 2 - 1;

                        float perlinValue = (AB + BC + AC + BA + CB + CA) / 6;

                        noiseValue += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }
                    if (noiseValue > maxNoiseValue)
                    {
                        maxNoiseValue = noiseValue;
                    }
                    else if (noiseValue < minNoiseValue)
                    {
                        minNoiseValue = noiseValue;
                    }

                    noiseMap[x, y, z] = noiseValue;
                }
            }
        }

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int z = 0; z < mapSize.z; z++)
                {
                    noiseMap[x, y, z] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x, y, z]);
                }
            }
        }
        return noiseMap;
    }

    public static float Noise( float x, float y, NoiseData noiseData)
    {
        System.Random prng = new System.Random(noiseData.seed);
        Vector2[] octaveOffsets = new Vector2[noiseData.octaves];
        for (int i = 0; i < noiseData.octaves; i++)
        {
            float offsetX = prng.Next(-1000000, 1000000);
            float offsetY = prng.Next(-1000000, 1000000);

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float amplitude = 1;
        float frequency = 1;
        float noiseValue = 0;
        float normalization = 0;

        for (int i = 0; i < noiseData.octaves; i++)
        {
            float sampleX = x / noiseData.scale * frequency + octaveOffsets[i].x;
            float sampleY = y / noiseData.scale * frequency + octaveOffsets[i].y;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

            noiseValue += perlinValue * amplitude;

            normalization += amplitude;
            amplitude *= noiseData.persistance;
            frequency *= noiseData.lacunarity;
        }

        return noiseValue / normalization;
    }

    public static float Noise3D(float x, float y, float z, NoiseData noiseData)
    {
        System.Random prng = new System.Random(noiseData.seed);
        Vector3[] octaveOffsets = new Vector3[noiseData.octaves];
        for (int i = 0; i < noiseData.octaves; i++)
        {
            float offsetX = prng.Next(-1000000, 1000000);
            float offsetY = prng.Next(-1000000, 1000000);
            float offsetZ = prng.Next(-1000000, 1000000);

            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
        }

        float amplitude = 1;
        float frequency = 1;
        float noiseValue = 0;
        float normalization = 0;

        for (int i = 0; i < noiseData.octaves; i++)
        {
            float sampleX = x / noiseData.scale * frequency + octaveOffsets[i].x;
            float sampleY = y / noiseData.scale * frequency + octaveOffsets[i].y;
            float sampleZ = z / noiseData.scale * frequency + octaveOffsets[i].z;

            float AB = Mathf.PerlinNoise(sampleX, sampleY);
            float BC = Mathf.PerlinNoise(sampleY, sampleZ);
            float AC = Mathf.PerlinNoise(sampleX, sampleZ);

            float BA = Mathf.PerlinNoise(sampleY, sampleX);
            float CB = Mathf.PerlinNoise(sampleZ, sampleY);
            float CA = Mathf.PerlinNoise(sampleZ, sampleX);

            float perlinValue = (AB + BC + AC + BA + CB + CA) / 6;

            noiseValue += perlinValue * amplitude;

            normalization += amplitude;
            amplitude *= noiseData.persistance;
            frequency *= noiseData.lacunarity;
        }

        return noiseValue / normalization;
    }

    public static NoiseData CreateNoiseData(int seed, int octaves, float scale, float persistance, float lacunarity)
    {
        NoiseData noiseData = new NoiseData();

        noiseData.seed = seed;
        noiseData.octaves = octaves;

        noiseData.scale = scale;
        noiseData.persistance = persistance;
        noiseData.lacunarity = lacunarity;

        return noiseData;
    }
}

public struct NoiseData
{
    public int seed;
    public int octaves;

    public float scale;
    public float persistance;
    public float lacunarity;
}
