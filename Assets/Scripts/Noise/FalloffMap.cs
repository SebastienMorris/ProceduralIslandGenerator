using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using UnityEngine;

public class FalloffMap : MonoBehaviour
{

    public float[,] GenerateFallOffMap(Vector2Int size, float steepness, float centerSize)
    {
        float[,] map = new float[size.x, size.y];
        for(int i=0; i<size.x; i++)
        {
            for(int j=0; j<size.y; j++)
            {
                float x = (i / (float)size.x) * 2 - 1;
                float y = (j / (float)size.y) * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value, steepness, centerSize);
            }
        }
        return map;
    }

    public float[,,] GenerateFallOffMap(Vector3Int size, float steepness, float centerSize)
    {
        float[,,] map = new float[size.x, size.y, size.z];
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                for (int h = 0; h < size.z; h++)
                {
                    float x = (i / (float)size.x) * 2 - 1;
                    float y = (j / (float)size.y) * 2 - 1;
                    float z = (h / (float)size.z) * 2 - 1;

                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z));
                    map[i, j, h] = Evaluate(value, steepness, centerSize);
                }
            }
        }
        return map;
    }

    public float[,,] GenerateFallOffMap(Vector3Int size, AnimationCurve falloffCurve)
    {
        float[,,] map = new float[size.x, size.y, size.z];

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                for (int h = 0; h < size.z; h++)
                {
                    float x = (i / (float)size.x) * 2 - 1;
                    float y = (j / (float)size.y) * 2 - 1;
                    float z = (h / (float)size.z) * 2 - 1;

                    float xValue = falloffCurve.Evaluate(Mathf.Abs(x));
                    float yValue = falloffCurve.Evaluate(Mathf.Abs(y));
                    float zValue = falloffCurve.Evaluate(Mathf.Abs(z));

                    float value = Mathf.Max(falloffCurve.Evaluate(Mathf.Abs(x)), falloffCurve.Evaluate(Mathf.Abs(y)), falloffCurve.Evaluate(Mathf.Abs(z)));

                    map[i, j, h] = value;
                }
            }
        }

        return map;
    }

    private float Evaluate(float value, float steepness, float centerSize)
    {
        return Mathf.Pow(value, steepness) / (Mathf.Pow(value, steepness) + Mathf.Pow(centerSize - centerSize * value, steepness));
    } 
    
}
