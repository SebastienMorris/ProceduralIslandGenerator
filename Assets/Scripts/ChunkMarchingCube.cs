using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMarchingCube : MonoBehaviour
{
    [SerializeField] private Vector3Int dimensions = new Vector3Int(0, 0, 0);

    [SerializeField] [Range(0, 1)] private float surfaceLevel = 0f;

    [SerializeField] private float noiseScale = 0f;
    [SerializeField] private Vector3 noiseOffset = new Vector3(0f, 0f, 0f);

    [SerializeField] [Min(1)] private int cubeSize = 1;

    [SerializeField] private GameObject largeMarchingCubePrefab;

    [SerializeField] private int chunkSize = 10;

    [SerializeField] private bool interpolate = true;

    [SerializeField] private IslandNoise islandNoise;

    private List<GameObject> listChunks = new List<GameObject>();

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            GenerateNoiseMap();
            CreateChunks();
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            ClearChunks();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, dimensions);
    }

    private void GenerateNoiseMap()
    {
        islandNoise.GenerateNoiseMap();
    }

    private void CreateChunks()
    {
        int nbChunksX = dimensions.x / chunkSize;
        int nbChunksY = dimensions.y / chunkSize;
        int nbChunksZ = dimensions.z / chunkSize;

        int nbChunks = nbChunksX * nbChunksY * nbChunksZ;

        for (int x=0; x < nbChunksX; x++)
        {
            for(int y=0; y < nbChunksY; y++)
            {
                for(int z=0; z < nbChunksZ; z++)
                {
                    Vector3 chunkPos = transform.position + new Vector3((x - nbChunksX / 2) + 0.5f, (y - nbChunksY / 2) + 0.5f, (z - nbChunksZ / 2) + 0.5f) * chunkSize;
                    GameObject spawnedChunk = Instantiate(largeMarchingCubePrefab, chunkPos, transform.rotation, transform);
                    listChunks.Add(spawnedChunk);
                    spawnedChunk.GetComponent<LargeMarchingCube>().StartGeneration(interpolate, dimensions, new Vector3Int(chunkSize, chunkSize, chunkSize), surfaceLevel, noiseScale, noiseOffset, cubeSize, islandNoise);
                }
            }
        }
    }

    private void ClearChunks()
    {
        foreach(GameObject chunk in listChunks)
        {
            Destroy(chunk);
        }
        listChunks.Clear();
    }
}
