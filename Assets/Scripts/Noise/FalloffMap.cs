using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FalloffMap : MonoBehaviour
{
    public float[,] Generate2DFallOffMap(Vector2Int size, float steepness, float centerSize)
    {
        float[,] map = new float[size.x, size.y];
        for(int i=0; i<size.x; i++)
        {
            for(int j=0; j<size.y; j++)
            {
                float x = i / (float)size.x * 2 - 1;
                float y = j / (float)size.y * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value, steepness, centerSize);
            }
        }
        return map;
    }

    private float Evaluate(float value, float steepness, float centerSize)
    {
        return Mathf.Pow(value, steepness) / (Mathf.Pow(value, steepness) + Mathf.Pow(centerSize - centerSize * value, steepness));
    } 
    
}
