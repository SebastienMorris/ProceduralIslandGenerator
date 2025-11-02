using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Content.Scripts.ComputeShaderGen.C_
{
    public class ChunkGenerator : MonoBehaviour
    {
        [SerializeField] private bool Guizmo;

        
        [SerializeField] private ComputeShader marchingCubesCompute;
        [SerializeField] private Shader renderShader;
        
        
        [Header("Lighting")]
        [SerializeField] private Light mainLight;
        [SerializeField] private Vector3 mainLightRotation = Vector3.zero;
        [SerializeField, Range(0.0f, 1.0f)] private float shadowStrength = 0.2f; 
        

        [Header("Texturing")]
        [SerializeField] private IslandTexture textureData = IslandTexture.Default;
        
	
        [Header("Generation")]
        [SerializeField] private Vector3Int dimensions = new (0, 0, 0);
        [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;

        
        [Header("Noise Properties")]
        [SerializeField] private NoiseSettings noiseSettings = NoiseSettings.Default;

        public Action<IslandTexture, float, Vector3Int, Vector3Int, NoiseSettings, float> OnValueUpdate;

        private Vector3Int numCurrentChunks = Vector3Int.zero;

        private bool initilised = false;
        private bool update = false;
        private bool isUpdating = false;
        
        #region constants
        
            private int MAX_CHUNK_SIZE = 50;

        #endregion
        
        private void OnDrawGizmos()
        {
            if (Guizmo)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(transform.position, dimensions);
            }
        }

        private void Start()
        {
            mainLight.transform.rotation = Quaternion.Euler(mainLightRotation);
            InitialiseChunks();
        }

        private void OnValidate()
        {
            if (!initilised || isUpdating) return;
            
            OnValueUpdate?.Invoke(textureData, shadowStrength, dimensions, GetNumChunks(), noiseSettings, surfaceLevel);
            update = true;
        }

        private void Update()
        {
            if (update)
            {
                mainLight.transform.rotation = Quaternion.Euler(mainLightRotation);
                
                isUpdating = true;
                CheckNewChunks();
                update = false;
                StartCoroutine(EndUpdating());
            }
        }

        private IEnumerator EndUpdating()
        {
            yield return new WaitForEndOfFrame();
            isUpdating = false;
            yield return null;
        }

        private void InitialiseChunks()
        {
            Vector3Int numChunks = GetNumChunks();

            for (int x = 0; x < numChunks.x; x++)
            {
                int sizeX = 0;
                if (x >= numChunks.x - 1) sizeX = dimensions.x % MAX_CHUNK_SIZE;
                sizeX = sizeX == 0 ? MAX_CHUNK_SIZE : sizeX;
                
                for (int y = 0; y < numChunks.y; y++)
                {
                    int sizeY = MAX_CHUNK_SIZE;
                    if (y >= numChunks.y - 1) sizeY = dimensions.y % MAX_CHUNK_SIZE;
                    sizeY = sizeY == 0 ? MAX_CHUNK_SIZE : sizeY;
                    
                    for (int z = 0; z < numChunks.z; z++)
                    {  
                        Vector3Int coord = new Vector3Int(x, y, z);
                        
                        int sizeZ = MAX_CHUNK_SIZE;
                        if (z >= numChunks.z - 1) sizeZ = dimensions.z % MAX_CHUNK_SIZE;
                        sizeZ = sizeZ == 0 ? MAX_CHUNK_SIZE : sizeZ;

                        Vector3Int size = new Vector3Int(sizeX, sizeY, sizeZ);
                        
                        AddChunk(coord, size);

                        numCurrentChunks.z++;
                    }

                    numCurrentChunks.y++;
                }

                numCurrentChunks.x++;
            }

            initilised = true;
        }

        private void AddChunk(Vector3Int coord, Vector3Int size)
        {
            GameObject obj = new GameObject("Chunk " + coord.ToString());
            obj.transform.parent = gameObject.transform;

            var chunk = obj.AddComponent<Chunk>();
            chunk.Initialize(marchingCubesCompute, renderShader, textureData, shadowStrength, dimensions, coord, size, MAX_CHUNK_SIZE, noiseSettings, surfaceLevel);
            chunk.OnChunkDestroyed += OnChunkDestroyed;
            OnValueUpdate += chunk.OnParamUpdate;
        }

        private void CheckNewChunks()
        {
            var newNumChunks = GetNumChunks();
            Vector3Int numChunksAdd = newNumChunks - numCurrentChunks;

            for (int x = 0; x < newNumChunks.x; x++)
            {
                int sizeX = 0;
                if (x >= newNumChunks.x - 1) sizeX = dimensions.x % MAX_CHUNK_SIZE;
                sizeX = sizeX == 0 ? MAX_CHUNK_SIZE : sizeX;
                
                for (int y = 0; y < newNumChunks.y; y++)
                {
                    int sizeY = MAX_CHUNK_SIZE;
                    if (y >= newNumChunks.y - 1) sizeY = dimensions.y % MAX_CHUNK_SIZE;
                    sizeY = sizeY == 0 ? MAX_CHUNK_SIZE : sizeY;
                    
                    for (int z = 0; z < newNumChunks.z; z++)
                    {
                        if (x < numCurrentChunks.x && y < numCurrentChunks.y && z < numCurrentChunks.z) continue;
                        
                        Vector3Int coord = new Vector3Int(x, y, z);
                        
                        int sizeZ = MAX_CHUNK_SIZE;
                        if (z >= newNumChunks.z - 1) sizeZ = dimensions.z % MAX_CHUNK_SIZE;
                        sizeZ = sizeZ == 0 ? MAX_CHUNK_SIZE : sizeZ;
                        
                        Vector3Int size = new Vector3Int(sizeX, sizeY, sizeZ);
                        
                        AddChunk(coord, size);
                    }
                }
            }
            
            numCurrentChunks = newNumChunks;
        }

        private void OnChunkDestroyed(Chunk chunk)
        {
            OnValueUpdate -= chunk.OnParamUpdate;
            
            chunk.ConfirmDestroy();
        }

        private Vector3Int GetNumChunks()
        {
            Vector3Int numChunks = dimensions / MAX_CHUNK_SIZE;

            numChunks.x += dimensions.x % MAX_CHUNK_SIZE == 0 ? 0 : 1;
            numChunks.y += dimensions.y % MAX_CHUNK_SIZE == 0 ? 0 : 1;
            numChunks.z += dimensions.z % MAX_CHUNK_SIZE == 0 ? 0 : 1;

            return numChunks;
        }
    }

    [Serializable]
    public struct IslandTexture
    {
        public Texture2D grassTexture;
        public Texture2D groundTexture;

        [Range(0.0f, 1.0f)] public float grassBlend;
        
        public static IslandTexture Default => new IslandTexture(){grassBlend = 0.5f};
    }
}