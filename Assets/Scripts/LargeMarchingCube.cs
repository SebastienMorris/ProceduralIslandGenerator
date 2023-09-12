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

    [SerializeField] [Min(1)] private int cubeSize = 1;

    [SerializeField] private bool activateDebug = true;

    private float[ , , ] pointsNoise;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private MeshFilter meshFilter;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        pointsNoise = new float[width + 1, height + 1, length + 1];
    }

    private void Start()
    {
        ClearMeshData();
        GeneratePoints();
      //  CreateCubeMeshData();
    }

    private void OnDrawGizmos()
    {
        if (activateDebug)
        {
            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < length + 1; z++)
                    {
                        if (pointsNoise != null)
                        {
                            if (pointsNoise[x, y, z] >= surfaceLevel)
                            {
                                Gizmos.color = Color.white;

                                Vector3 point = transform.position + new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);

                                Gizmos.DrawSphere(point, 0.05f);
                            }
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.G))
        {
            pointsNoise = new float[width + 1, height + 1, length + 1];
            ClearMeshData();
            GeneratePoints();
            CreateCubeMeshData();
        }
    }

    private void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
    }

    private void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void AssignCubeMeshData(Vector3 position, float[] cubePoints)
    {
        int[] configVertices = MarchingCubesTables.triangulationTable[CalculateTriangulationIndex(cubePoints)];

        foreach (int verticeIndex in configVertices)
        {
            if (verticeIndex == -1)
                break;
            int pointIndex1 = MarchingCubesTables.edgeConnections[verticeIndex][0];
            int pointIndex2 = MarchingCubesTables.edgeConnections[verticeIndex][1];

            Vector3 point1 = position + (MarchingCubesTables.cubeCorners[pointIndex1] * cubeSize);
            Vector3 point2 = position + (MarchingCubesTables.cubeCorners[pointIndex2] * cubeSize);

            Vector3 vertice = (point1 + point2) / 2;
             
            vertices.Add(vertice);
            triangles.Add(vertices.Count - 1);
        }

       // ClearMeshData();
        BuildMesh();
    }

    private void CreateCubeMeshData()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < length; z++)
                {
                    float[] cubePoints = new float[8];
                    for(int i=0; i<cubePoints.Length; i++)
                    {
                        Vector3Int cornerPos = new Vector3Int(x, y, z) + MarchingCubesTables.cubeCorners[i];
                        cubePoints[i] = pointsNoise[cornerPos.x, cornerPos.y, cornerPos.z];
                    }
                    AssignCubeMeshData(new Vector3(x * cubeSize, y * cubeSize, z * cubeSize), cubePoints);
                }
            }
        }
    }

    private void GeneratePoints()
    {
        for(int x=0; x<width + 1; x++)
        {
            for(int y=0; y<height + 1; y++)
            {
                for(int z=0; z<length + 1; z++)
                {
                    pointsNoise[x, y, z] = CalculatePerlinNoise(x, y, z);
                }
            }
        }
    }

    private float CalculatePerlinNoise(float x, float y, float z)
    {
        float xNoise = (x / width) * noiseScale + noiseOffsetX;
        float yNoise = (y / height) * noiseScale + noiseOffsetY;
        float zNoise = (z / length) * noiseScale + noiseOffsetZ;

        return PerlinNoise3D(xNoise, yNoise, zNoise);
        //return Mathf.PerlinNoise(xNoise, yNoise);
    }

    private float PerlinNoise3D(float x, float y, float z)
    {
        y++;
        z += 2;
        float AB = _perlin3DFixed(x, y);
        float BC = _perlin3DFixed(y, z);
        float AC = _perlin3DFixed(x, z);

        float BA = _perlin3DFixed(y, x);
        float CB = _perlin3DFixed(z, y);
        float CA = _perlin3DFixed(z, x);

        //Debug.Log($"<{x}> <{y}> <{z}>");
       // Debug.Log($"<{AB}> <{BC}> <{AC}> <{BA}> <{CB}> <{CA}>");

        return (AB + BC + AC + BA + CB + CA) / 6;
       // return AB * BC * AC * BA * CB * CA;
    }

    private float _perlin3DFixed(float a, float b)
    {
        return Mathf.PerlinNoise(a, b);
        //return Mathf.Sin(Mathf.PI * Mathf.PerlinNoise(a, b));
    }

    private int CalculateTriangulationIndex(float[] cubePoints)
    {
        int index = 0;
        for (int i = 0; i < cubePoints.Length; i++)
        {
            if (cubePoints[i] >= surfaceLevel)
            {
                index |= 1 << i;
            }
        }

        return index;
    }
}
