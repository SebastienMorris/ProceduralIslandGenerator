using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomNoiseGen : MonoBehaviour
{
    [SerializeField]private int repeat;
    /*public CustomNoiseGen(int repeat = -1)
    {
        this.repeat = repeat;
    }*/

    private int[] p = { 151,160,137,91,90,15,                 
    131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,    
    190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
    88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
    77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
    102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
    135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
    5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
    223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
    129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
    251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
    49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
    138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
            151,160,137,91,90,15,
    131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
    190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
    88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
    77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
    102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
    135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
    5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
    223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
    129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
    251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
    49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
    138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    /*private int[] p;                                                    

    private void Awake()
    {
        p = new int[512];
        for (int x = 0; x < 512; x++)
        {
            p[x] = permutation[x % 256];
        }
    }*/

    private void Start()
    {
        
    }

    private void LerpTest(float a, float b, float x)
    {
        print("a : " + a + " // b: " + b + " // x: " + x);
        print("Mathf lerp " + Mathf.Lerp(a, b, x));
        print("Mathf Inverselerp " + Mathf.InverseLerp(a, b, x));
        print("Lerp function " + Lerp(a, b, x));
    }

    public float OctavePerlin(float x, float y, float z, int nbOctaves, float persistance, int seed)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        System.Random prng = new System.Random(seed);
        for (int i=0; i<nbOctaves; i++)
        {
            float offsetX = prng.Next(-1000000, 1000000);
            float offsetY = prng.Next(-1000000, 1000000);
            float offsetZ = prng.Next(-1000000, 1000000);
            total += Perlin(x * frequency + offsetX, y * frequency  + offsetY, z * frequency + offsetZ) * amplitude;
            maxValue += amplitude;

            amplitude *= persistance;
            frequency *= 2;
        }
        return total / maxValue;
    }

    public float Perlin(float x, float y, float z)
    {
        /*if(repeat > 0)
        {
            x = x % repeat;
            y = y % repeat;
            z = z % repeat;
        }*/

        int xCube = (int)x & 255;
        int yCube = (int)y & 255;
        int zCube = (int)z & 255;

        float xLoc = Mathf.Abs(x - (int)x);
        float yLoc = Mathf.Abs(y - (int)y);
        float zLoc = Mathf.Abs(z - (int)z);

        float u = Fade(xLoc);
        float v = Fade(yLoc);
        float w = Fade(zLoc);

        int aaa, aba, aab, abb, baa, bba, bab, bbb;
        aaa = p[p[p[xCube] + yCube] + zCube];
        aba = p[p[p[xCube] + Inc(yCube)] + zCube];
        aab = p[p[p[xCube] + yCube] + Inc(zCube)];
        abb = p[p[p[xCube] + Inc(yCube)] + Inc(zCube)];
        baa = p[p[p[Inc(xCube)] + yCube] + zCube];
        bba = p[p[p[Inc(xCube)] + Inc(yCube)] + zCube];
        bab = p[p[p[Inc(xCube)] + yCube] + Inc(zCube)];
        bbb = p[p[p[Inc(xCube)] + Inc(yCube)] + Inc(zCube)];

        float x1, x2, y1, y2;
        x1 = Mathf.Lerp(Gradient(aaa, xLoc, yLoc, zLoc), Gradient(baa, xLoc - 1, yLoc, zLoc), u);                                 
        x2 = Mathf.Lerp(Gradient(aba, xLoc, yLoc - 1, zLoc), Gradient(bba, xLoc - 1, yLoc - 1, zLoc), u);
        y1 = Mathf.Lerp(x1, x2, v);

        x1 = Mathf.Lerp(Gradient(aab, xLoc, yLoc, zLoc - 1), Gradient(bab, xLoc - 1, yLoc, zLoc - 1), u);
        x2 = Mathf.Lerp(Gradient(abb, xLoc, yLoc - 1, zLoc - 1), Gradient(bbb, xLoc - 1, yLoc - 1, zLoc - 1),
                      u);
        y2 = Mathf.Lerp(x1, x2, v);

        return (Mathf.Lerp(y1, y2, w) + 1) / 2;
    }

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private int Inc(int nb)
    {
        nb++;
        /*if(repeat > 0)
        {
            nb %= repeat;
        }*/
        return nb;
    }

    private float Gradient(int hash, float x, float y, float z)
    {
        switch (hash & 0xF)
        {
            case 0x0: return x + y;
            case 0x1: return -x + y;
            case 0x2: return x - y;
            case 0x3: return -x - y;
            case 0x4: return x + z;
            case 0x5: return -x + z;
            case 0x6: return x - z;
            case 0x7: return -x - z;
            case 0x8: return y + z;
            case 0x9: return -y + z;
            case 0xA: return y - z;
            case 0xB: return -y - z;
            case 0xC: return y + x;
            case 0xD: return -y + z;
            case 0xE: return y - x;
            case 0xF: return -y - z;
            default: return 0;
        }
    }

    public float grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;                                    // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
        float u = h < 8 /* 0b1000 */ ? x : y;                // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.

        float v;                                             // In Ken Perlin's original implementation this was another conditional operator (?:).  I
                                                              // expanded it for readability.

        if (h < 4 /* 0b0100 */)                                // If the first and second significant bits are 0 set v = y
            v = y;
        else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)  // If the first and second significant bits are 1 set v = x
            v = x;
        else                                                  // If the first and second significant bits are not equal (0/1, 1/0) set v = z
            v = z;

        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
    }

    private float Lerp(float a, float b, float x)
    {
        return a + x * (b - a);
    }
}
