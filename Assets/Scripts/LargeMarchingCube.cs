using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LargeMarchingCube : MonoBehaviour
{
    [SerializeField] private int width = 0;
    [SerializeField] private int height = 0;
    [SerializeField] private int length = 0;

    [SerializeField][Range(0,1)] private float surfaceLevel = 0f;

    [SerializeField] private float noiseScale = 0f;
    [SerializeField] private float noiseOffsetX = 0f;
    [SerializeField] private float noiseOffsetY = 0f;
    [SerializeField] private float noiseOffsetZ = 0f;

    private float[ , , ] pointsNoise;

    private void Awake()
    {
        pointsNoise = new float[width,height,length];
    }

    private void GeneratePoints()
    {
        for(int x=0; x<width + 1; x++)
        {
            for(int y=0; y<height + 1; y++)
            {
                for(int z=0; z<length + 1; z++)
                {

                }
            }
        }
    }

    private float CalculatePerlinNoise(int x, int y, int z)
    {
        float xNoise = (x / width) * noiseScale + noiseOffsetX;
        float yNoise = (y / height) * noiseScale + noiseOffsetY;
        float zNoise = (z / length) * noiseScale + noiseOffsetZ;

        return PerlinNoise3D(xNoise, yNoise, zNoise);
    }

    private float PerlinNoise3D(float x, float y, float z)
    {
        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);

        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        return (AB + BC + AC + BA + CB + CA) / 6;
    }
}
