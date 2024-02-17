using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint> hashes;

        public int resolution;
        public float invResolution;

        public void Execute(int i)
        {
            int v = (int)floor(invResolution * i + 0.00001f);
            int u = i - resolution * v - resolution / 2;
            v -= resolution / 2;

            var hash = new SmallXXHash(0);
            hash.Eat(u);
            hash.Eat(v);
            hashes[i] = hash;
        }
    }

    static int _hashesId = Shader.PropertyToID("_Hashes");
    static int _configId = Shader.PropertyToID("_Config");

    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private Material material;

    [SerializeField, Range(1, 512)] private int resolution = 16;

    private NativeArray<uint> _hashes;

    private ComputeBuffer _hashesBuffer;

    private MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        int length = resolution * resolution;
        _hashes = new NativeArray<uint>(length, Allocator.Persistent);
        _hashesBuffer = new ComputeBuffer(length, 4);

        new HashJob
        {
            hashes = _hashes,
            resolution = this.resolution,
            invResolution = 1f / this.resolution
        }.ScheduleParallel(_hashes.Length, resolution, default).Complete();

        _hashesBuffer.SetData(_hashes);

        _propertyBlock ??= new MaterialPropertyBlock();
        _propertyBlock.SetBuffer(_hashesId, _hashesBuffer);
        _propertyBlock.SetVector(_configId, new Vector4(resolution, 1f / resolution));
    }

    private void OnDisable()
    {
        _hashes.Dispose();
        _hashesBuffer.Release();
        _hashesBuffer = null;
    }

    private void OnValidate()
    {
        if(_hashesBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one), _hashes.Length, _propertyBlock);
    }
}
