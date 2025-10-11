using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCube : MonoBehaviour
{
    [SerializeField] private bool[] pointsValue = new bool[8];
    private bool[] oldPointsValue = new bool[8];

    [SerializeField] [Range(0, 255)] private int triangulationIndex = 0;
    private int oldIndex = 0;

    [SerializeField][Min(1)] private int cubeSize = 1;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private MeshFilter meshFilter;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        /*
        if(triangulationIndex != oldIndex)
        {
            ClearMeshData();
            AssignMeshData();
            oldIndex = triangulationIndex;
        }*/
        for(int i=0; i<pointsValue.Length; i++)
        {
            if(pointsValue[i] != oldPointsValue[i])
            {
                ClearMeshData();
                CalculateTriangulationIndex();
                for(int j=0; j<oldPointsValue.Length; j++)
                {
                    oldPointsValue[j] = pointsValue[j];
                }
                break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        for(int i=0; i<pointsValue.Length; i++)
        {
            if (pointsValue[i])
                Gizmos.color = Color.white;
            else
                Gizmos.color = Color.black;

            Vector3 point = transform.position + ((MarchingCubesTables.cubeCorners[i] - new Vector3(0.5f, 0.5f, 0.5f)) * cubeSize);

            Gizmos.DrawSphere(point, 0.2f);
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

    private void AssignMeshData(int triangulationIndex)
    {
        int[] configVertices = MarchingCubesTables.triangulationTable[triangulationIndex];

        foreach(int verticeIndex in configVertices)
        {
            if (verticeIndex == -1)
                break;
            int pointIndex1 = MarchingCubesTables.edgeConnections[verticeIndex][0];
            int pointIndex2 = MarchingCubesTables.edgeConnections[verticeIndex][1];

            Vector3 point1 = transform.position + ((MarchingCubesTables.cubeCorners[pointIndex1] - new Vector3(0.5f, 0.5f, 0.5f)) * cubeSize);
            Vector3 point2 = transform.position + ((MarchingCubesTables.cubeCorners[pointIndex2] - new Vector3(0.5f, 0.5f, 0.5f)) * cubeSize);

            Vector3 vertice = (point1 + point2) / 2;

            vertices.Add(vertice);
            triangles.Add(vertices.Count - 1);
        }

        BuildMesh();
    }

    private void CalculateTriangulationIndex()
    {
        int index = 0;
        for(int i=0; i<pointsValue.Length; i++)
        {
            if(pointsValue[i])
            {
                index |= 1 << i;
            }
        }

        AssignMeshData(index);
    }
}
