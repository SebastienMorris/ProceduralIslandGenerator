using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x3 = Unity.Mathematics.float4x3;

public class HashVisualization : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [ReadOnly] public NativeArray<float3x4> positions;
        
        [WriteOnly] public NativeArray<uint4> hashes;

        public SmallXXHash4 hash;

        public float3x4 domainTRS;

        float4x3 TransformPositions(float3x4 trs, float4x3 p)
        {
            return float4x3(
                trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x,
                trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y,
                trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z);
        }

        public void Execute(int i)
        {
            float4x3 p = TransformPositions(domainTRS, transpose(positions[i]));
            
            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);
            
            hashes[i] =  hash.Eat(u).Eat(v).Eat(w);
        }
    }

    private static int hashesId = Shader.PropertyToID("_Hashes");
    private static int positionsId = Shader.PropertyToID("_Positions");
    private static int normalsId = Shader.PropertyToID("_Normals");
    private static int configId = Shader.PropertyToID("_Config");

    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private Material material;

    [SerializeField, Range(1, 512)] private int resolution = 16;

    [SerializeField] private int seed = 0;

    [SerializeField, Range(-0.5f, 0.5f)] private float displacement = 0.1f;

    [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 8f };

    private NativeArray<uint4> _hashes;
    private NativeArray<float3x4> _positions;
    private NativeArray<float3x4> _normals;

    private ComputeBuffer _hashesBuffer;
    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _normalsBuffer;

    private MaterialPropertyBlock _propertyBlock;

    private bool isDirty = false;

    private Bounds bounds;
    
    public enum Shape{Plane, Sphere, Torus}

    private static Shapes.ScheduleDelegate[] shapeJobs =
    {
        Shapes.Job<Shapes.Plane>.ScheduleParallel,
        Shapes.Job<Shapes.Sphere>.ScheduleParallel,
        Shapes.Job<Shapes.Torus>.ScheduleParallel
    };

    [SerializeField] private Shape shape;
    
    [SerializeField, Range(0.1f, 10f)] private float instanceScale = 2f;

    private void OnEnable()
    {
        isDirty = true;
        int length = resolution * resolution;
        length = length / 4 + (length & 1);
        _hashes = new NativeArray<uint4>(length, Allocator.Persistent);
        _positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        _normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        _hashesBuffer = new ComputeBuffer(length * 4, 4);
        _positionsBuffer = new ComputeBuffer(length * 4, 12);
        _normalsBuffer = new ComputeBuffer(length * 4, 12);
        
        _propertyBlock ??= new MaterialPropertyBlock();
        _propertyBlock.SetBuffer(hashesId, _hashesBuffer);
        _propertyBlock.SetVector(configId, new Vector4(resolution, instanceScale / resolution, displacement));
        _propertyBlock.SetBuffer(positionsId, _positionsBuffer);
        _propertyBlock.SetBuffer(normalsId, _normalsBuffer);
    }

    private void OnDisable()
    {
        _hashes.Dispose();
        _positions.Dispose();
        _normals.Dispose();
        _hashesBuffer.Release();
        _positionsBuffer.Release();
        _normalsBuffer.Dispose();
        _hashesBuffer = null;
        _positionsBuffer = null;
        _normalsBuffer = null;
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
        if (isDirty || transform.hasChanged)
        {
            isDirty = false;
            transform.hasChanged = false;
            
            JobHandle handle = shapeJobs[(int)shape](_positions, _normals, resolution, transform.localToWorldMatrix,default);

            new HashJob
            {
                positions = _positions,
                hashes = _hashes,
                hash = SmallXXHash.Seed(seed),
                domainTRS = domain.Matrix
            }.ScheduleParallel(_hashes.Length, resolution, handle).Complete();

            _hashesBuffer.SetData(_hashes.Reinterpret<uint>(4 * 4));
            _positionsBuffer.SetData(_positions.Reinterpret<float3>(3 * 4 * 4));
            _normalsBuffer.SetData(_normals.Reinterpret<float3>(3 * 4 * 4));

            bounds = new Bounds(transform.position, float3(2f * cmax(abs(transform.lossyScale)) + displacement));
        }
        
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, resolution * resolution, _propertyBlock);
    }
}
