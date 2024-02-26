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
        [ReadOnly] public NativeArray<float3> positions;
        
        [WriteOnly] public NativeArray<uint> hashes;

        public SmallXXHash hash;

        public float3x4 domainTRS;
        
        public void Execute(int i)
        {
            float3 p = mul(domainTRS, float4(positions[i], 1f));
            
            int u = (int)floor(p.x);
            int v = (int)floor(p.y);
            int w = (int)floor(p.z);
            
            hashes[i] =  hash.Eat(u).Eat(v).Eat(w);
        }
    }

    private static int hashesId = Shader.PropertyToID("_Hashes");
    private static int positionsId = Shader.PropertyToID("_Positions");
    private static int configId = Shader.PropertyToID("_Config");

    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private Material material;

    [SerializeField, Range(1, 512)] private int resolution = 16;

    [SerializeField] private int seed = 0;

    [SerializeField, Range(-2f, 2f)] private float verticalOffset = 1f;

    [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 8f };

    private NativeArray<uint> _hashes;
    private NativeArray<float3> _positions;

    private ComputeBuffer _hashesBuffer;
    private ComputeBuffer _positionsBuffer;

    private MaterialPropertyBlock _propertyBlock;

    private void OnEnable()
    {
        int length = resolution * resolution;
        _hashes = new NativeArray<uint>(length, Allocator.Persistent);
        _positions = new NativeArray<float3>(length, Allocator.Persistent);
        _hashesBuffer = new ComputeBuffer(length, 4);
        _positionsBuffer = new ComputeBuffer(length, 12);
        
        JobHandle handle = Shapes.Job.ScheduleParallel(_positions, resolution, transform.localToWorldMatrix,default);

        new HashJob
        {
            positions = _positions,
            hashes = _hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(_hashes.Length, resolution, handle).Complete();

        _hashesBuffer.SetData(_hashes);
        _positionsBuffer.SetData(_positions);

        _propertyBlock ??= new MaterialPropertyBlock();
        _propertyBlock.SetBuffer(hashesId, _hashesBuffer);
        _propertyBlock.SetVector(configId, new Vector4(resolution, 1f / resolution, verticalOffset / resolution));
        _propertyBlock.SetBuffer(positionsId, _positionsBuffer);
    }

    private void OnDisable()
    {
        _hashes.Dispose();
        _positions.Dispose();
        _hashesBuffer.Release();
        _positionsBuffer.Release();
        _hashesBuffer = null;
        _positionsBuffer = null;
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
