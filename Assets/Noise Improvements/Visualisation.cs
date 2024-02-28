using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x3 = Unity.Mathematics.float4x3;

public abstract class Visualisation : MonoBehaviour
{
    private static int positionsId = Shader.PropertyToID("_Positions");
    private static int normalsId = Shader.PropertyToID("_Normals");
    private static int configId = Shader.PropertyToID("_Config");

    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private Material material;

    [SerializeField, Range(1, 1024)] private int resolution = 16;

    [SerializeField, Range(-0.5f, 0.5f)] private float displacement = 0.1f;
    
    private NativeArray<float3x4> _positions;
    private NativeArray<float3x4> _normals;

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
        _positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        _normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        _positionsBuffer = new ComputeBuffer(length * 4, 12);
        _normalsBuffer = new ComputeBuffer(length * 4, 12);
        
        _propertyBlock ??= new MaterialPropertyBlock();
        EnableVisualisation(length, _propertyBlock);
        _propertyBlock.SetVector(configId, new Vector4(resolution, instanceScale / resolution, displacement));
        _propertyBlock.SetBuffer(positionsId, _positionsBuffer);
        _propertyBlock.SetBuffer(normalsId, _normalsBuffer);
    }

    private void OnDisable()
    {
        _positions.Dispose();
        _normals.Dispose();
        _positionsBuffer.Release();
        _normalsBuffer.Dispose();
        _positionsBuffer = null;
        _normalsBuffer = null;
        
        DisableVisualisation();
    }

    private void OnValidate()
    {
        if(_positionsBuffer != null && enabled)
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
            
            UpdateVisualisation(_positions, resolution, shapeJobs[(int)shape](_positions, _normals, resolution, transform.localToWorldMatrix,default));
            
            _positionsBuffer.SetData(_positions.Reinterpret<float3>(3 * 4 * 4));
            _normalsBuffer.SetData(_normals.Reinterpret<float3>(3 * 4 * 4));

            bounds = new Bounds(transform.position, float3(2f * cmax(abs(transform.lossyScale)) + displacement));
        }
        
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, resolution * resolution, _propertyBlock);
    }

    protected abstract void EnableVisualisation(int dataLength, MaterialPropertyBlock propertyBlock);
    protected abstract void DisableVisualisation();
    protected abstract void UpdateVisualisation(NativeArray<float3x4> positions, int resolution, JobHandle handle);
}
