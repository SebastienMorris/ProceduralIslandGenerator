using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LargeMarchingCube : MonoBehaviour
{
    [SerializeField] private Vector3Int globalDimensions = new Vector3Int(0, 0, 0);

    [SerializeField] private Vector3Int dimensions = new Vector3Int(0, 0, 0);

    [SerializeField][Range(0,1)] private float surfaceLevel = 0f;

    //[SerializeField] private float noiseScale = 0f;
    [SerializeField] private Vector3 noiseOffset = new Vector3(0, 0, 0);

    [SerializeField] private bool activateDebug = true;
    [SerializeField] private Color debugColor = Color.white;

    [SerializeField] private bool interpolate = true;

    //[SerializeField] private int seed = 0;
    //[SerializeField] private int octaves = 0;

    private NoiseData noiseData;

    private float[ , , ] pointsNoise;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private MeshFilter meshFilter;

    [SerializeField] private Vector3Int testCoords = new Vector3Int();

    private CustomNoiseGen noiseGen;

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

    public void StartGeneration( CustomNoiseGen noiseGen, NoiseData noiseData, bool interpolate, Vector3Int globalDimensions,Vector3Int dimensions, float surfaceLevel, Vector3 noiseOffset)
    {
        this.noiseGen = noiseGen;

        this.noiseData = noiseData;

        this.interpolate = interpolate;
        this.globalDimensions = globalDimensions;
        this.dimensions = dimensions;
        this.surfaceLevel = surfaceLevel;
        this.noiseOffset = noiseOffset;

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
        for(int x=0; x< dimensions.x + 1; x++)
        {
            for(int y=0; y< dimensions.y + 1; y++)
            {
                for(int z=0; z< dimensions.z + 1; z++)
                {
                    pointsNoise[x, y, z] = CalculateCustomNoise(x, y , z);
                    //print(pointsNoise[x, y, z]);
                }
            }
        }
    }

    private float CalculateCustomNoise(float x, float y, float z)
    {
        float xNoise = (transform.position.x + x) / globalDimensions.x * noiseData.scale + noiseOffset.x;    // / globalDimensions.x * noiseScale
        float yNoise = (transform.position.y + y) / globalDimensions.y * noiseData.scale + noiseOffset.y;    // / globalDimensions.x * noiseScale
        float zNoise = (transform.position.z + z) / globalDimensions.z * noiseData.scale + noiseOffset.z;    // / globalDimensions.x * noiseScale

        return noiseGen.OctavePerlin(xNoise, yNoise, zNoise, noiseData.octaves, noiseData.persistance, noiseData.seed);
    }

    private float CalculateNoise(float x, float y, float z)
    {

        float xNoise = (transform.position.x + x) / globalDimensions.x + noiseOffset.x;    // / globalDimensions.x * noiseScale
        float yNoise = (transform.position.y + y) / globalDimensions.y + noiseOffset.y;    // / globalDimensions.x * noiseScale
        float zNoise = (transform.position.z + z) / globalDimensions.z + noiseOffset.z;    // / globalDimensions.x * noiseScale

        //return PerlinNoise3D(xNoise, yNoise, zNoise);
        //return Mathf.PerlinNoise(xNoise, yNoise);
        return LandMassNoise.Noise3D(xNoise, yNoise, zNoise, noiseData);
    }

    private float PerlinNoise3D(float x, float y, float z)
    {
        float AB = LandMassNoise.Noise( x, y, noiseData);
        float BC = LandMassNoise.Noise(y, z, noiseData);
        float AC = LandMassNoise.Noise(x, z, noiseData);

        float BA = LandMassNoise.Noise(y, x, noiseData);
        float CB = LandMassNoise.Noise(z, y, noiseData);
        float CA = LandMassNoise.Noise(z, x, noiseData);

        //Debug.Log($"<{x}> <{y}> <{z}>");
        //Debug.Log($"<{AB}> <{BC}> <{AC}> <{BA}> <{CB}> <{CA}>");

        return (AB + BC + AC + BA + CB + CA) / 6;
        //return AB * BC * AC * BA * CB * CA;
    }

    /*private float _perlin3DFixed(float a, float b)
    {
        //return Mathf.PerlinNoise(a, b);
        //return Mathf.Sin(Mathf.PI * Mathf.PerlinNoise(a, b));
    }*/

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
