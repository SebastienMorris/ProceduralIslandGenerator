using System;
using System.Collections;
using IslandGen;
using UnityEngine;

namespace Content.Scripts.ComputeShaderGen.C_
{
    public class ChunkGenerator : MonoBehaviour
    {
        [SerializeField] private bool gizmo;
        [SerializeField] CameraController cameraController;


        [SerializeField] private ComputeShader marchingCubesCompute;
        [SerializeField] private Shader renderShader;


        [Header("Lighting")] [SerializeField] private Light mainLight;
        [SerializeField] private Vector3 mainLightRotation = Vector3.zero;
        [SerializeField, Range(0.0f, 1.0f)] private float shadowStrength = 0.2f;


        [Header("Texturing")] [SerializeField] private IslandTexture textureData = IslandTexture.Default;


        [Header("Generation")] [SerializeField]
        private Vector3Int dimensions = new(0, 0, 0);

        [SerializeField] [Range(0, 1)] private float surfaceLevel = 0.5f;


        [Header("Noise Properties")] [SerializeField]
        private NoiseSettings noiseSettings = NoiseSettings.Default;

        
        public Action<IslandTexture, float, Vector3Int, Vector3Int, NoiseSettings, float> OnValueUpdate;

        
        private Vector3Int _numCurrentChunks = Vector3Int.zero;

        private bool _initialised = false;
        private bool _update = false;
        private bool _isUpdating = false;

        #region Constants

            private int MAX_CHUNK_SIZE = 50;

        #endregion
        
        #region Public Properties

        public Vector3Int Dimensions
        {
            get => dimensions;
            set
            {
                if (dimensions != value)
                {
                    dimensions = value;
                    UpdateValue();
                }
            }
        }

        public float SurfaceLevel
        {
            get => surfaceLevel;
            set
            {
                if (Mathf.Abs(surfaceLevel - value) > 0.001f)
                {
                    surfaceLevel = value;
                    UpdateValue();
                }
            }
        }

        public IslandTexture TextureData
        {
            get => textureData;
            set
            {
                textureData = value;
                UpdateValue();
            }
        }

        public NoiseSettings NoiseSettings
        {
            get => noiseSettings;
            set
            {
                noiseSettings = value;
                UpdateValue();
            }
        }

        public Vector3 MainLightRotation
        {
            get => mainLightRotation;
            set
            {
                if (mainLightRotation != value)
                {
                    mainLightRotation = value;
                    UpdateValue();
                }
            }
        }
        
        public float Intensity
        {
            get => mainLight.intensity;

            set
            {
                if (Mathf.Abs(mainLight.intensity - value) > 0.001f)
                {
                    mainLight.intensity = value;
                    UpdateValue();
                }
            }
            
        }

        public float ShadowStrength
        {
            get => shadowStrength;
            set
            {
                if (Mathf.Abs(shadowStrength - value) > 0.001f)
                {
                    shadowStrength = value;
                    UpdateValue();
                }
            }
        }
        #endregion


        private void OnDrawGizmos()
        {
            if (gizmo)
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
            UpdateValue();
        }

        private void UpdateValue()
        {
            if (!_initialised || _isUpdating) return;

            OnValueUpdate?.Invoke(textureData, shadowStrength, dimensions, GetNumChunks(), noiseSettings, surfaceLevel);
            _update = true;
        }

        private void Update()
        {
            if (_update)
            {
                mainLight.transform.rotation = Quaternion.Euler(mainLightRotation);

                _isUpdating = true;
                CheckNewChunks();
                _update = false;
                StartCoroutine(EndUpdating());
            }
        }

        private IEnumerator EndUpdating()
        {
            yield return new WaitForEndOfFrame();
            _isUpdating = false;
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

                        _numCurrentChunks.z++;
                    }

                    _numCurrentChunks.y++;
                }

                _numCurrentChunks.x++;
            }

            _initialised = true;
        }

        private void AddChunk(Vector3Int coord, Vector3Int size)
        {
            GameObject obj = new GameObject("Chunk " + coord.ToString());
            obj.transform.parent = gameObject.transform;

            var chunk = obj.AddComponent<Chunk>();
            chunk.Initialize(marchingCubesCompute, renderShader, textureData, shadowStrength, dimensions, coord, size,
                MAX_CHUNK_SIZE, noiseSettings, surfaceLevel);
            chunk.OnChunkDestroyed += OnChunkDestroyed;
            OnValueUpdate += chunk.OnParamUpdate;
        }

        private void CheckNewChunks()
        {
            var newNumChunks = GetNumChunks();
            Vector3Int numChunksAdd = newNumChunks - _numCurrentChunks;

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
                        if (x < _numCurrentChunks.x && y < _numCurrentChunks.y && z < _numCurrentChunks.z) continue;

                        Vector3Int coord = new Vector3Int(x, y, z);

                        int sizeZ = MAX_CHUNK_SIZE;
                        if (z >= newNumChunks.z - 1) sizeZ = dimensions.z % MAX_CHUNK_SIZE;
                        sizeZ = sizeZ == 0 ? MAX_CHUNK_SIZE : sizeZ;

                        Vector3Int size = new Vector3Int(sizeX, sizeY, sizeZ);

                        AddChunk(coord, size);
                    }
                }
            }

            _numCurrentChunks = newNumChunks;
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

        public static IslandTexture Default => new IslandTexture() { grassBlend = 0.5f };
    }
}

namespace IslandGen
{



    [Serializable]
    public struct NoiseSettings
    {
        public int seed;
        [Range(1, 200)] public int frequency;

        [Range(1, 6)] public int octaves;
        [Range(2, 4)] public int lacunarity;
        [Range(0f, 1f)] public float persistence;

        public bool applyFallOff;
        [Range(0.1f, 10f)] public float steepness;
        [Range(0.1f, 10f)] public float centerSize;

        public static NoiseSettings Default => new NoiseSettings
        {
            frequency = 50, octaves = 1, lacunarity = 2, persistence = 0.5f, applyFallOff = true, steepness = 2f,
            centerSize = 10
        };

        public bool Compare(NoiseSettings noiseSettings)
        {
            if (seed != noiseSettings.seed) return false;
            if (frequency != noiseSettings.frequency) return false;

            if (octaves != noiseSettings.octaves) return false;
            if (lacunarity != noiseSettings.lacunarity) return false;
            if (Mathf.Abs(persistence - noiseSettings.persistence) >= 0.001) return false;

            if (applyFallOff != noiseSettings.applyFallOff) return false;
            if (Mathf.Abs(steepness - noiseSettings.steepness) >= 0.001) return false;
            if (Mathf.Abs(centerSize - noiseSettings.centerSize) >= 0.001) return false;

            return true;
        }

        public bool Compare(int seed, int frequency, int octaves, int lacunarity, float persistence,
            bool applyFallOffMap, float steepness, float centerSize)
        {
            if (this.seed != seed) return false;
            if (this.frequency != frequency) return false;

            if (this.octaves != octaves) return false;
            if (this.lacunarity != lacunarity) return false;
            if (Mathf.Abs(this.persistence - persistence) >= 0.001) return false;

            if (this.applyFallOff != applyFallOffMap) return false;
            if (Mathf.Abs(this.steepness - steepness) >= 0.001) return false;
            if (Mathf.Abs(this.centerSize - centerSize) >= 0.001) return false;

            return true;
        }
    }
}