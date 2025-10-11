using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x3 = Unity.Mathematics.float4x3;

public class HashVisualisation : Visualisation
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [ReadOnly] public NativeArray<float3x4> positions;
        
        [WriteOnly] public NativeArray<uint4> hashes;

        public SmallXXHash4 hash;

        public float3x4 domainTRS;

        public void Execute(int i)
        {
            float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));
            
            int4 u = (int4)floor(p.c0);
            int4 v = (int4)floor(p.c1);
            int4 w = (int4)floor(p.c2);
            
            hashes[i] =  hash.Eat(u).Eat(v).Eat(w);
        }
    }

    private static int hashesId = Shader.PropertyToID("_Hashes");

    [SerializeField] private int seed = 0;

    [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 8f };

    private NativeArray<uint4> _hashes;

    private ComputeBuffer _hashesBuffer;

    protected override void EnableVisualisation(int dataLength, MaterialPropertyBlock materialPropertyBlock)
    {
        _hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);
        _hashesBuffer = new ComputeBuffer(dataLength * 4, 4);
        
        materialPropertyBlock.SetBuffer(hashesId, _hashesBuffer);
    }

    protected override void DisableVisualisation()
    {
        _hashes.Dispose();
        _hashesBuffer.Release();
        _hashesBuffer = null;
    }

    protected override void UpdateVisualisation(NativeArray<float3x4> positions, int resolution, JobHandle handle)
    { 
        new HashJob
        {
            positions = positions,
            hashes = _hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(_hashes.Length, resolution, handle).Complete();

        _hashesBuffer.SetData(_hashes.Reinterpret<uint>(4 * 4));
    }
}
