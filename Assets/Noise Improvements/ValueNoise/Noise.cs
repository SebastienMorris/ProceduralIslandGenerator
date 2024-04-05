using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static Unity.Mathematics.math;

public static partial class Noise
{
    public interface INoise
    {
        float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency);
    }

    [Serializable]
    public struct Settings
    {
        public int seed;
        [Min(1)] public int frequency;
        [Range(1, 6)] public int octaves;

        [Range(2, 4)] public int lacunarity;

        [Range(0f, 1f)] public float persistence;

        public static Settings Default => new Settings{frequency = 4, octaves = 1, lacunarity = 2, persistence = 0.5f};
    }
    
    public delegate JobHandle ScheduleDelegate(NativeArray<float3x4> positions, NativeArray<float4> noise, Settings settings,
        SpaceTRS domainTRS, int resolution, JobHandle dependency);

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] public NativeArray<float3x4> positions;

        [WriteOnly] public NativeArray<float4> noise;

        public Settings settings;

        public float3x4 domainTRS;

        public void Execute(int i)
        {
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
            var hash = SmallXXHash4.Seed(settings.seed);
            int frequency = settings.frequency;
            float amplitude = 1f;
            float amplitudeSum = 0f;
            float4 sum = 0f;

            for (int j = 0; j < settings.octaves; j++)
            {
                sum += default(N).GetNoise4(position, hash + j, frequency) * amplitude;
                amplitudeSum += amplitude;
                frequency *= settings.lacunarity;
                amplitude *= settings.persistence;
            }

            noise[i] = sum / amplitudeSum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> positions, NativeArray<float4> noise, Settings settings,
            SpaceTRS domainTRS, int resolution, JobHandle dependency)
        {
            return new Job<N>
            {
                positions = positions,
                noise = noise,
                settings = settings,
                domainTRS = domainTRS.Matrix
            }.ScheduleParallel(positions.Length, resolution, dependency);
        }
    }
}
