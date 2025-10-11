using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;
using float4x3 = Unity.Mathematics.float4x3;
using static Noise;

public class NoiseVisualisation : Visualisation
{
    private static int noiseId = Shader.PropertyToID("_Noise");

    private static ScheduleDelegate[,] noiseJobs =
    {
        {
            Job<Lattice1D<Perlin, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice1D<Perlin, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice2D<Perlin, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice2D<Perlin, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice3D<Perlin, LatticeNormal>>.ScheduleParallel,
            Job<Lattice3D<Perlin, LatticeTilling>>.ScheduleParallel
            
        },
        {
            Job<Lattice1D<Turbulence<Perlin>, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice1D<Turbulence<Perlin>, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice2D<Turbulence<Perlin>, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice2D<Turbulence<Perlin>, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice3D<Turbulence<Perlin>, LatticeNormal>>.ScheduleParallel,
            Job<Lattice3D<Turbulence<Perlin>, LatticeTilling>>.ScheduleParallel
        },
        {
            Job<Lattice1D<Value, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice1D<Value, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice2D<Value, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice2D<Value, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice3D<Value, LatticeNormal>>.ScheduleParallel,
            Job<Lattice3D<Value, LatticeTilling>>.ScheduleParallel
        },
        {
            Job<Lattice1D<Turbulence<Value>, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice1D<Turbulence<Value>, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice2D<Turbulence<Value>, LatticeNormal>>.ScheduleParallel, 
            Job<Lattice2D<Turbulence<Value>, LatticeTilling>>.ScheduleParallel, 
            Job<Lattice3D<Turbulence<Value>, LatticeNormal>>.ScheduleParallel,
            Job<Lattice3D<Turbulence<Value>, LatticeTilling>>.ScheduleParallel
        }
    };
    
    [FormerlySerializedAs("noiseSettings")] [SerializeField] private NoiseSettings noiseNoiseSettings = NoiseSettings.Default;

    public enum NoiseType
    {
        Perlin,
        PerlinTurbulence,
        Value,
        ValueTurbulence
    };

    [SerializeField] private NoiseType type;
    
    [SerializeField, Range(1, 3)] private int dimensions = 1;

    [SerializeField] private bool tiling;

    [SerializeField] private SpaceTRS domain = new SpaceTRS { scale = 1f };

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
        noiseJobs[(int)type, 2 * dimensions - (tiling ? 1 : 2)](positions, _noise, noiseNoiseSettings, domain, resolution, handle).Complete();
        _noiseBuffer.SetData(_noise.Reinterpret<float>(4 * 4));
    }
}
