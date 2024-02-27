using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x3 = Unity.Mathematics.float4x3;
using static Noise;

public class NoiseVisualisation : Visualisation
{
    private static int noiseId = Shader.PropertyToID("_Noise");

    private static ScheduleDelegate[] noiseJobs =
        {Job<Lattice1D>.ScheduleParallel, Job<Lattice2D>.ScheduleParallel, Job<Lattice3D>.ScheduleParallel};
    
    [SerializeField] private int seed = 0;

    [SerializeField, Range(1, 3)] private int dimensions = 1; 

    [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 8f };

    private NativeArray<float4> _noise;

    private ComputeBuffer _noiseBuffer;

    protected override void EnableVisualisation(int dataLength, MaterialPropertyBlock materialPropertyBlock)
    {
        _noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
        _noiseBuffer = new ComputeBuffer(dataLength * 4, 4);
        
        materialPropertyBlock.SetBuffer(noiseId, _noiseBuffer);
    }

    protected override void DisableVisualisation()
    {
        _noise.Dispose();
        _noiseBuffer.Release();
        _noiseBuffer = null;
    }

    protected override void UpdateVisualisation(NativeArray<float3x4> positions, int resolution, JobHandle handle)
    {
        noiseJobs[dimensions - 1](positions, _noise, seed, domain, resolution, handle).Complete();
        _noiseBuffer.SetData(_noise.Reinterpret<float4>(4 * 4));
    }
}
