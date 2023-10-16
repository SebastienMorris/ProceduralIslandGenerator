using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using UnityEngine.UI;

public class IslandNoise : MonoBehaviour
{
    [SerializeField] private int resolution = 100;
    [SerializeField] private float noiseScale = 0.25f, islandSize = 40;

    [Range(1, 20)] [SerializeField] private int nbOctaves = 1;
    [Range(0, 9999999)] [SerializeField] private int seed;

    private Color[] colours;
    private Texture2D texture;

    [SerializeField] private Gradient colourGradient;

    [SerializeField] private Vector2 offset;
    [SerializeField] private bool applyFallOffMap = true;

    [Min(1f)] [SerializeField] private float persistance = 2f;

    private void OnValidate()
    {
        CreateNoiseMap();
    }

    private void CreateNoiseMap()
    {

       // _noiseMap = new float[resolution, resolution];

        texture = new Texture2D(resolution, resolution);
        colours = new Color[texture.height * texture.width];

        GetComponent<RawImage>().texture = texture;

        for (int x = 0, i = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++, i++)
            {
                float noiseVal = Noise(x, y);
                colours[i] = colourGradient.Evaluate(noiseVal);
                //_noiseMap[x, y] = noiseVal;
            }
        }
        texture.SetPixels(colours);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    private float Noise(float x, float y)
    {
        float noiseValue = 0f;

        float amplitude = noiseScale;
        float frequency = 1f;

        Vector2 origin = new Vector2(Mathf.Sqrt(seed), Mathf.Sqrt(seed));

        for (int i=0; i < nbOctaves; i++)
        {
            float xVal = (x / (amplitude * resolution)) + origin.x + offset.x;
            float yVal = (y / (amplitude * resolution)) + origin.y + offset.y;

            float res = noise.snoise(new float2(xVal, yVal));

            noiseValue += Mathf.Clamp01(res) / frequency;

            amplitude /= persistance;
            frequency *= persistance;
        }
        float finalNoise = noiseValue;

        if(applyFallOffMap)
            finalNoise = Mathf.Clamp01(noiseValue -= FallOffMap(x, y, resolution, islandSize));

        return finalNoise;
    }

    private float FallOffMap(float x, float y, float resolution, float IslandSize)
    {
        float gradient = 1f;

        gradient /= (x * y) / (resolution * resolution) * (1 - (x / resolution)) * (1 - (y / resolution));
        gradient -= 16;
        gradient /= IslandSize;

        return gradient;
    }

    public void GenerateNoiseMap()
    {
        CreateNoiseMap();
    }

    public float GetIslandNoise(float x, float y)
    {
        float noise = Noise(x, y);
        return noise;
    }
}
