using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
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

        public SmallXXHash hash;

        public float3x4 domainTRS;
        
        public void Execute(int i)
        {
            float vf = floor(invResolution * i + 0.00001f);
            float uf = invResolution * (i - resolution * vf + 0.5f) - 0.5f;
            vf = invResolution * (vf + 0.5f) - 0.5f;

            float3 p = mul(domainTRS, float4(uf, 0f, vf, 1f));
            
            int u = (int)floor(p.x);
            int v = (int)floor(p.z);
            
            hashes[i] =  hash.Eat(u).Eat(v);
        }
    }

    static int _hashesId = Shader.PropertyToID("_Hashes");
    static int _configId = Shader.PropertyToID("_Config");

    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private Material material;

    [SerializeField, Range(1, 512)] private int resolution = 16;

    [SerializeField] private int seed = 0;

    [SerializeField, Range(-2f, 2f)] private float verticalOffset = 1f;

    [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 8f };

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
            invResolution = 1f / this.resolution,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(_hashes.Length, resolution, default).Complete();

        _hashesBuffer.SetData(_hashes);

        _propertyBlock ??= new MaterialPropertyBlock();
        _propertyBlock.SetBuffer(_hashesId, _hashesBuffer);
        _propertyBlock.SetVector(_configId, new Vector4(resolution, 1f / resolution, verticalOffset / resolution));
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
