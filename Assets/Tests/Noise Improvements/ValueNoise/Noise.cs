using System;
using IslandGen;
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
    
    public delegate JobHandle ScheduleDelegate(NativeArray<float3x4> positions, NativeArray<float4> noise, NoiseSettings noiseSettings,
        SpaceTRS domainTRS, int resolution, JobHandle dependency);

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] public NativeArray<float3x4> positions;

        [WriteOnly] public NativeArray<float4> noise;

        public NoiseSettings noiseSettings;

        public float3x4 domainTRS;

        public void Execute(int i)
        {
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
            var hash = SmallXXHash4.Seed(noiseSettings.seed);
            float frequency = noiseSettings.frequency;
            float amplitude = 1f;
            float amplitudeSum = 0f;
            float4 sum = 0f;

            for (int j = 0; j < noiseSettings.octaves; j++)
            {
                sum += default(N).GetNoise4(position, hash + j, (int)frequency) * amplitude;
                amplitudeSum += amplitude;
                frequency *= noiseSettings.lacunarity;
                amplitude *= noiseSettings.persistence;
            }

            noise[i] = sum / amplitudeSum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> positions, NativeArray<float4> noise, NoiseSettings noiseSettings,
            SpaceTRS domainTRS, int resolution, JobHandle dependency)
        {
            return new Job<N>
            {
                positions = positions,
                noise = noise,
                noiseSettings = noiseSettings,
                domainTRS = domainTRS.Matrix
            }.ScheduleParallel(positions.Length, resolution, dependency);
        }
    }
}
