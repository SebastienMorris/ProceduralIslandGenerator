using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class LargeMarchingCube : MonoBehaviour
{
    [SerializeField] private Vector3Int dimensions = new Vector3Int(0, 0, 0);

    [SerializeField][Range(0,1)] private float surfaceLevel = 0f;
    [SerializeField] private bool activateDebug = true;
    [SerializeField] private Color debugColor = Color.white;

    [SerializeField] private bool interpolate = true;

    private float[,,] pointsNoise;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private MeshFilter meshFilter;

    [SerializeField] private Vector3Int testCoords = new Vector3Int();

    private float[] noiseValues;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        pointsNoise = new float[dimensions.x + 1, dimensions.y + 1, dimensions.z + 1];
    }

    /*
    private void Start()
    {
        ClearMeshData();
        GeneratePoints();
      //  CreateCubeMeshData();
    }*/

    private void OnDrawGizmos()
    {
        if (activateDebug)
        {
            /*
            for (int x = 0; x < dimensions.x + 1; x++)
            {
                for (int y = 0; y < dimensions.y + 1; y++)
                {
                    for (int z = 0; z < dimensions.z + 1; z++)
                    {
                        if (pointsNoise != null)
                        {
                            if (pointsNoise[x, y, z] >= surfaceLevel)
                            {
                                Gizmos.color = debugColor;

                                Vector3 point = transform.position + new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);

                                Gizmos.DrawSphere(point, 0.05f);
                            }
                        }
                    }
                }
            }*/

            Gizmos.color = debugColor;
            Gizmos.DrawWireCube(transform.position, dimensions);
        }
    }

    private void Update()
    {
        
        if(Input.GetKeyUp(KeyCode.H))
        {
            pointsNoise = new float[dimensions.x + 1, dimensions.y + 1, dimensions.z + 1];
            ClearMeshData();
            GeneratePoints();
            CreateCubeMeshData();
        }
    }

    public void StartGeneration(float[] noiseValues,  bool interpolate, Vector3Int dimensions, float surfaceLevel)
    {
        this.noiseValues = noiseValues;
        this.interpolate = interpolate;
        this.dimensions = dimensions;
        this.surfaceLevel = surfaceLevel;

        pointsNoise = new float[dimensions.x + 1, dimensions.y + 1, dimensions.z + 1];

        ClearMeshData();
        GeneratePoints();
        CreateCubeMeshData();
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

            Vector3 point1 = position + (MarchingCubesTables.cubeCorners[pointIndex1] - new Vector3(dimensions.x / 2, dimensions.y / 2, dimensions.z / 2));
            Vector3 point2 = position + (MarchingCubesTables.cubeCorners[pointIndex2] - new Vector3(dimensions.x / 2, dimensions.y / 2, dimensions.z / 2));

            Vector3 vertice = (point1 + point2) / 2;
            if (interpolate)
                vertice = (point1 + point2) / ((cubePoints[pointIndex1] + cubePoints[pointIndex2]) / surfaceLevel);

            vertices.Add(vertice);
            triangles.Add(vertices.Count - 1);
            
        } 

       // ClearMeshData();
        BuildMesh();
    }

    private void CreateCubeMeshData()
    {
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    float[] cubePoints = new float[8];
                    for(int i=0; i<cubePoints.Length; i++)
                    {
                        Vector3Int cornerPos = new Vector3Int(x, y, z) + MarchingCubesTables.cubeCorners[i];
                        cubePoints[i] = pointsNoise[cornerPos.x, cornerPos.y, cornerPos.z];
                    }
                    AssignCubeMeshData(new Vector3(x, y, z), cubePoints);
                }
            } 
        }
    }

    private void GeneratePoints()
    {
        int i = 0;
        for(int x=0; x< dimensions.x; x++)
        {
            for(int y=0; y< dimensions.y ; y++)
            {
                for(int z=0; z< dimensions.z; z++)
                {
                    pointsNoise[x, y, z] = noiseValues[i];
                    i++;
                }
            }
        }
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
