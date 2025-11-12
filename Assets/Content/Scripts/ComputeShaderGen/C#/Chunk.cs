using System;
using IslandGen;
using static Unity.Mathematics.math;
using UnityEngine;

namespace Content.Scripts.ComputeShaderGen.C_
{
    public class Chunk : MonoBehaviour
    {
        public Action<Chunk> OnChunkDestroyed;

        private ComputeShader _compute;

        private Material _renderMaterial;
        private float _grassBlend;

        private float _shadowStrength;

        private ComputeBuffer _triangleBuffer;
        private ComputeBuffer _renderArgsBuffer;

        private Vector3Int _dimensions;
        private Vector3Int _coord;
        private Vector3 _indexOffset;
        private Vector3Int _size;
        private Vector3Int _maxSize;

        private NoiseSettings _settings;

        private float _surfaceLevel;

        private bool _initialized = false;
        private bool _update = false;
        private bool _isDestroying = false;

        #region Constants

        private const int TRIANGLE_STRIDE = sizeof(float) * 3 * 4;

        #endregion


        public void Initialize(ComputeShader marchingCompute, Shader renderShader, IslandTexture textureData, float shadowStrength, Vector3Int dimensions, Vector3Int coord,
            Vector3Int size, int maxSize, NoiseSettings settings, float surfaceLevel)
        {
            _compute = (ComputeShader)Instantiate(marchingCompute);
            _renderMaterial = new Material(renderShader);
            _renderMaterial.SetTexture(Shader.PropertyToID("_GrassTex"), textureData.grassTexture);
            _renderMaterial.SetTexture(Shader.PropertyToID("_GroundTex"), textureData.groundTexture);
            this._grassBlend = textureData.grassBlend;

            this._shadowStrength = shadowStrength;
                
            this._dimensions = dimensions;
            this._coord = coord;
            this._size = size;
            this._maxSize = new Vector3Int(maxSize, maxSize, maxSize);
            this._settings = settings;
            this._surfaceLevel = surfaceLevel;
            
            SetPosition();
            SetupBuffers();

            _initialized = true;
            _update = true;
        }

        private void OnDisable()
        {
            ClearBuffers();
        }

        private void LateUpdate()
        {
            if (!_initialized || _isDestroying) return;
            
            Generate();
        }

        public void OnParamUpdate(IslandTexture textureData, float shadowStrength, Vector3Int dimensions, Vector3Int numChunks, NoiseSettings settings,
            float surfaceLevel)
        {
            if (!_initialized || _isDestroying) return;

            this._grassBlend = textureData.grassBlend;

            this._shadowStrength = shadowStrength;
            
            this._dimensions = dimensions;
            this._settings = settings;
            this._surfaceLevel = surfaceLevel;

            CheckResize(numChunks);
            SetPosition();

            _update = true;
        }

        public void ConfirmDestroy()
        {
            Destroy(gameObject);
        }

        private void CallDestroy()
        {
            OnChunkDestroyed?.Invoke(this);
        }

        private void SetPosition()
        {
            Vector3 position = -_dimensions / 2 + _maxSize / 2 + _coord * _size;

            if (_size.x != _maxSize.x)
            {
                position.x = (float)_maxSize.x / 2 * _coord.x;
            }
            
            if (_size.y != _maxSize.y)
            {
                position.y = (float)_maxSize.y / 2 * _coord.y;
            }
            
            if (_size.z != _maxSize.z)
            {
                position.z = (float)_maxSize.z / 2 * _coord.z;
            }

            transform.localPosition = position;
            
            _indexOffset = new Vector3(
                transform.position.x - _maxSize.x / 2.0f,
                transform.position.y - _maxSize.y / 2.0f,
                transform.position.z - _maxSize.z / 2.0f
            );
        }

        private void SetupBuffers()
        {
            int numVoxels = _maxSize.x * _maxSize.y * _maxSize.z;

            _triangleBuffer = new ComputeBuffer(numVoxels, TRIANGLE_STRIDE, ComputeBufferType.Append);
            _renderArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

            _compute.SetBuffer(0, Shader.PropertyToID("_DrawTriangles"), _triangleBuffer);
            _compute.SetBuffer(0, Shader.PropertyToID("_RenderArgs"), _renderArgsBuffer);
        }

        private void ClearBuffers()
        {
            _triangleBuffer.Release();
            _renderArgsBuffer.Release();
        }

        private void ResetBuffers()
        {
            _triangleBuffer.SetCounterValue(0);
            
            uint[] args = new uint[5] { 0, 1, 0, 0, 0 };
            _renderArgsBuffer.SetData(args);
        }

        private void CheckResize(Vector3Int numChunks)
        {
            _size = _maxSize;
            bool destroy = false;

            if (_coord.x == numChunks.x - 1)
            {
                var rest = _dimensions.x % _maxSize.x;
                _size.x = rest == 0 ? _maxSize.x : rest;
            }
            else if (_coord.x >= numChunks.x)
            {
                destroy = true;
            }

            if (_coord.y == numChunks.y - 1)
            {
                var rest = _dimensions.y % _maxSize.y;
                _size.y = rest == 0 ? _maxSize.y : rest;
            }
            else if (_coord.y >= numChunks.y)
            {
                destroy = true;
            }

            if (_coord.z == numChunks.z - 1)
            {
                var rest = _dimensions.z % _maxSize.z;
                _size.z = rest == 0 ? _maxSize.z : rest;
            }
            else if (_coord.z >= numChunks.z)
            {
                destroy = true;
            }

            if (destroy)
            {
                _update = false;
                _isDestroying = true;
                ClearBuffers();
                CallDestroy();
            }
        }

        private void Generate()
        {
            if (_update)
            {
                ResetBuffers();
                SetParams();

                _compute.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
                var a = new Vector3Int((int)x, (int)y, (int)z);

                _compute.Dispatch(0, Mathf.CeilToInt(_size.x / (float)a.x),
                    Mathf.CeilToInt(_size.y / (float)a.y), Mathf.CeilToInt(_size.z / (float)a.z));

                _update = false;
            }

            Vector3 position = transform.position;

            _renderMaterial.SetBuffer(Shader.PropertyToID("_VertexBuffer"), _triangleBuffer);
            _renderMaterial.SetVector(Shader.PropertyToID("_Origin"), float4(position, 0.0f));
            _renderMaterial.SetFloat(Shader.PropertyToID("_ShadowStrength"), _shadowStrength);
            _renderMaterial.SetFloat(Shader.PropertyToID("_GrassBlend"), _grassBlend);

            Bounds bounds = new Bounds(position, _size);

            Graphics.DrawProceduralIndirect(_renderMaterial, bounds, MeshTopology.Triangles, _renderArgsBuffer);
        }

        private void SetParams()
        {
            _compute.SetInt(Shader.PropertyToID("_Seed"), _settings.seed);
            _compute.SetInt(Shader.PropertyToID("_Frequency"), _settings.frequency);
            _compute.SetInt(Shader.PropertyToID("_Octaves"), _settings.octaves);
            _compute.SetInt(Shader.PropertyToID("_Lacunarity"), _settings.lacunarity);
            _compute.SetFloat(Shader.PropertyToID("_Persistence"), _settings.persistence);

            _compute.SetBool(Shader.PropertyToID("_ApplyFallOff"), _settings.applyFallOff);
            _compute.SetFloat(Shader.PropertyToID("_Steepness"), _settings.steepness);
            _compute.SetFloat(Shader.PropertyToID("_CenterSize"), _settings.centerSize);

            _compute.SetVector(Shader.PropertyToID("_ChunkSize"),  float4(_size.x, _size.y, _size.z, 0f));
            _compute.SetVector(Shader.PropertyToID("_Dimensions"), float4(this._dimensions.x, this._dimensions.y, this._dimensions.z, 0f));
            _compute.SetVector(Shader.PropertyToID("_IndexOffset"), float4(_indexOffset, 0f));
            _compute.SetVector(Shader.PropertyToID("_LocalChunkPos"), float4(transform.localPosition, 0f));

            _compute.SetFloat(Shader.PropertyToID("_IsoLevel"), _surfaceLevel);
        }
    }
}