using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

public class IslandNoise : MonoBehaviour
{
    public int TextureSize;
    public float NoiseScale, IslandSize;
    [Range(1, 20)] public int NoiseOctaves;
    [Range(0, 9999999)] int Seed;

    private Color[] colours;
    private Texture2D texture;

    public Gradient colourGradient;

    private void Start()
    {
        texture = new Texture2D(TextureSize, TextureSize);
        colours = new Color[texture.height * texture.width];

        Renderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial.mainTexture = texture;

        Vector2 Org = new Vector2(Mathf.Sqrt(Seed), Mathf.Sqrt(Seed));

        for(int x=0, i=0; x<TextureSize; x++, i++)
        {
            for(int y=0; y<TextureSize; y++, i++)
            {
                float a = CreateNoise(x, y, Org);
                colours[i] = colourGradient.Evaluate(a);
            }
        }
        texture.SetPixels(colours);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    private float CreateNoise(int x, int y, Vector2 Origin)
    {
        float noiseValue = 0;

        float frequency = NoiseScale;
        float amplitude = 1f;

        for(int i=0; i < NoiseOctaves; i++)
        {
            float xVal = (x / (frequency * TextureSize)) + Origin.x;
            float yVal = (y / (frequency * TextureSize)) + Origin.y;

            float res = noise.snoise(new float2(xVal, yVal));

            noiseValue += Mathf.InverseLerp(0, 1, res) / amplitude;

            frequency /= 2f;
            amplitude *= 2f;
        }

        return noiseValue -= FallOffMap(x, y, TextureSize, IslandSize);
    }

    private float FallOffMap(float x, float y, float size, float IslandSize)
    {
        float gradient = 1f;


        return 0.1f;
    }
}
