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
        GenerateNoiseMap();
    }

    private void GenerateNoiseMap()
    {
        print("Starting");

        texture = new Texture2D(resolution, resolution);
        colours = new Color[texture.height * texture.width];

        GetComponent<RawImage>().texture = texture;

        Vector2 origin = new Vector2(Mathf.Sqrt(seed), Mathf.Sqrt(seed));

        for (int x = 0, i = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++, i++)
            {
                colours[i] = colourGradient.Evaluate(CreateNoise(x, y, origin));
            }
        }
        texture.SetPixels(colours);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        print("Ending");
    }

    private float CreateNoise(float x, float y, Vector2 origin)
    {
        float noiseValue = 0;

        float amplitude = noiseScale;
        float frequency = 1f;

        for(int i=0; i < nbOctaves; i++)
        {
            float xVal = (x / (amplitude * resolution)) + origin.x + offset.x;
            float yVal = (y / (amplitude * resolution)) + origin.y + offset.y;

            float res = noise.snoise(new float2(xVal, yVal));

            noiseValue += Mathf.InverseLerp(0, 1, res) / frequency;

            amplitude /= persistance;
            frequency *= persistance;
        }
        if (applyFallOffMap)
            return noiseValue -= FallOffMap(x, y, resolution, islandSize);
        else
            return noiseValue;
    }

    private float FallOffMap(float x, float y, float size, float IslandSize)
    {
        float gradient = 1f;

        gradient /= (x * y) / (size * size) * (1 - (x / size)) * (1 - (y / size));
        gradient -= 16;
        gradient /= IslandSize;

        return gradient;
    }
}
