using System;
using static Unity.Mathematics.math;
using UnityEngine;

namespace Content.Scripts.ComputeShaderGen.C_
{
    public class Chunk : MonoBehaviour
    {
        public Action<Chunk> OnChunkDestroyed;

        private ComputeShader compute;

        private Material renderMaterial;
        private float grassBlend;

        private float shadowStrength;

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer renderArgsBuffer;

        private Vector3Int dimensions;
        private Vector3Int coord;
        private Vector3 indexOffset;
        [SerializeField] private Vector3Int size;
        private Vector3Int maxSize;

        private NoiseSettings settings;

        private float surfaceLevel;

        private bool initialized = false;
        private bool update = false;
        private bool isDestroying = false;

        #region Constants

        private const int TRIANGLE_STRIDE = sizeof(float) * 3 * 4;

        #endregion


        public void Initialize(ComputeShader marchingCompute, Shader renderShader, IslandTexture textureData, float shadowStrength, Vector3Int dimensions, Vector3Int coord,
            Vector3Int size, int maxSize, NoiseSettings settings, float surfaceLevel)
        {
            compute = (ComputeShader)Instantiate(marchingCompute);
            renderMaterial = new Material(renderShader);
            renderMaterial.SetTexture(Shader.PropertyToID("_GrassTex"), textureData.grassTexture);
            renderMaterial.SetTexture(Shader.PropertyToID("_GroundTex"), textureData.groundTexture);
            this.grassBlend = textureData.grassBlend;

            this.shadowStrength = shadowStrength;
                
            this.dimensions = dimensions;
            this.coord = coord;
            this.size = size;
            this.maxSize = new Vector3Int(maxSize, maxSize, maxSize);
            this.settings = settings;
            this.surfaceLevel = surfaceLevel;

            this.indexOffset = coord * maxSize;
            SetPosition();
            SetupBuffers();

            initialized = true;
            update = true;
        }

        private void OnDisable()
        {
            ClearBuffers();
        }

        private void LateUpdate()
        {
            if (!initialized || isDestroying) return;
            
            Generate();
        }

        public void OnParamUpdate(IslandTexture textureData, float shadowStrength, Vector3Int dimensions, Vector3Int numChunks, NoiseSettings settings,
            float surfaceLevel)
        {
            if (!initialized || isDestroying) return;

            this.grassBlend = textureData.grassBlend;

            this.shadowStrength = shadowStrength;
            
            this.dimensions = dimensions;
            this.settings = settings;
            this.surfaceLevel = surfaceLevel;

            CheckResize(numChunks);
            SetPosition();

            update = true;
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
            Vector3 position = -dimensions / 2 + maxSize / 2 + coord * size;

            if (size.x != maxSize.x)
            {
                position.x = (float)maxSize.x / 2 * coord.x;
            }
            
            if (size.y != maxSize.y)
            {
                position.y = (float)maxSize.y / 2 * coord.y;
            }
            
            if (size.z != maxSize.z)
            {
                position.z = (float)maxSize.z / 2 * coord.z;
            }

            transform.position = position;
        }

        private void SetupBuffers()
        {
            int numVoxels = maxSize.x * maxSize.y * maxSize.z;

            triangleBuffer = new ComputeBuffer(numVoxels, TRIANGLE_STRIDE, ComputeBufferType.Append);
            renderArgsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

            compute.SetBuffer(0, Shader.PropertyToID("_Triangles"), triangleBuffer);
            compute.SetBuffer(0, Shader.PropertyToID("_RenderArgs"), renderArgsBuffer);
        }

        private void ClearBuffers()
        {
            triangleBuffer.Release();
            renderArgsBuffer.Release();
        }

        private void ResetBuffers()
        {
            triangleBuffer.SetCounterValue(0);
            
            uint[] args = new uint[5] { 0, 1, 0, 0, 0 };
            renderArgsBuffer.SetData(args);
        }

        private void CheckResize(Vector3Int numChunks)
        {
            size = maxSize;
            bool destroy = false;

            if (coord.x == numChunks.x - 1)
            {
                var rest = dimensions.x % maxSize.x;
                size.x = rest == 0 ? maxSize.x : rest;
            }
            else if (coord.x >= numChunks.x)
            {
                destroy = true;
            }

            if (coord.y == numChunks.y - 1)
            {
                var rest = dimensions.y % maxSize.y;
                size.y = rest == 0 ? maxSize.y : rest;
            }
            else if (coord.y >= numChunks.y)
            {
                destroy = true;
            }

            if (coord.z == numChunks.z - 1)
            {
                var rest = dimensions.z % maxSize.z;
                size.z = rest == 0 ? maxSize.z : rest;
            }
            else if (coord.z >= numChunks.z)
            {
                destroy = true;
            }

            if (destroy)
            {
                update = false;
                isDestroying = true;
                ClearBuffers();
                CallDestroy();
            }
        }

        private void Generate()
        {
            if (update)
            {
                ResetBuffers();
                SetParams();

                compute.GetKernelThreadGroupSizes(0, out uint x, out uint y, out uint z);
                var a = new Vector3Int((int)x, (int)y, (int)z);

                compute.Dispatch(0, Mathf.CeilToInt(size.x / (float)a.x),
                    Mathf.CeilToInt(size.y / (float)a.y), Mathf.CeilToInt(size.z / (float)a.z));

                update = false;
            }

            Vector3 position = transform.position;

            renderMaterial.SetBuffer(Shader.PropertyToID("_VertexBuffer"), triangleBuffer);
            renderMaterial.SetVector(Shader.PropertyToID("_Origin"), float4(position, 0.0f));
            renderMaterial.SetFloat(Shader.PropertyToID("_ShadowStrength"), shadowStrength);
            renderMaterial.SetFloat(Shader.PropertyToID("_GrassBlend"), grassBlend);

            Bounds bounds = new Bounds(position, size);

            Graphics.DrawProceduralIndirect(renderMaterial, bounds, MeshTopology.Triangles, renderArgsBuffer);
        }

        private void SetParams()
        {
            Debug.Log($"Chunk {coord}: WorldPos = {transform.TransformPoint(Vector3.zero)}, LocalPos = {transform.localPosition}, IndexOffset = {indexOffset}");
            
            compute.SetInt(Shader.PropertyToID("seed"), settings.seed);
            compute.SetInt(Shader.PropertyToID("frequency"), settings.frequency);
            compute.SetInt(Shader.PropertyToID("octaves"), settings.octaves);
            compute.SetInt(Shader.PropertyToID("lacunarity"), settings.lacunarity);
            compute.SetFloat(Shader.PropertyToID("persistence"), settings.persistence);

            compute.SetFloat(Shader.PropertyToID("steepness"), settings.steepness);
            compute.SetFloat(Shader.PropertyToID("centerSize"), settings.centerSize);
            compute.SetBool(Shader.PropertyToID("applyFallOff"), settings.applyFallOffMap);

            compute.SetVector(Shader.PropertyToID("dimensions"),  float4(size.x, size.y, size.z, 0f));
            compute.SetVector(Shader.PropertyToID("globalDimensions"), float4(this.dimensions.x, this.dimensions.y, this.dimensions.z, 0f));
            compute.SetVector(Shader.PropertyToID("indexOffset"), float4(transform.TransformPoint(Vector3.zero), 0f));
            compute.SetVector(Shader.PropertyToID("localPos"), float4(transform.localPosition, 0f));

            compute.SetFloat(Shader.PropertyToID("isoLevel"), surfaceLevel);
        }
    }
}