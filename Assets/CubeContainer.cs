using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class CubeContainer : MonoBehaviour
{
    public int dim = 512;
    private Material _mat;
    private Vector3[] _positions;
    private ComputeBuffer _posBuffer;
    private ComputeBuffer _colorBuffer;
    private static readonly int Posbuffer = Shader.PropertyToID("posbuffer");
    private static readonly int Camerapos = Shader.PropertyToID("camerapos");
    private int StrideVec3;
    private int StrideFloat;
    private float[] _modifiedPixels;
    
    [SerializeField] private Shader drawMeshShader;
    [SerializeField] private Mesh cubeMesh;


    private void Awake()
    {
        _mat = new Material(drawMeshShader);
        _mat.enableInstancing = true;
        StrideVec3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        StrideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
    }

    public void GenerateCubeInfo(NativeArray<float> modifiedPixels, byte[] pixels)
    {
        // Also clean up your code convetions with variable names...
        // There is a lot of copying around data - do we really need to copy it over to a float array before setting the data? 
        int totalCubes = dim * dim;
        var positionsNative = new NativeArray<Vector3>(totalCubes, Allocator.TempJob);
        _positions = new Vector3[dim * dim];
        _modifiedPixels = new float[dim * dim];
        _posBuffer?.Release();
        _colorBuffer?.Release();
        _posBuffer = new ComputeBuffer (dim * dim, StrideVec3, ComputeBufferType.Default);
        _colorBuffer = new ComputeBuffer(dim * dim, StrideFloat, ComputeBufferType.Default);
        modifiedPixels.CopyTo(_modifiedPixels);
        var job = new GenerateCubeInfoJob()
        {
            Positions = positionsNative,
            Dim = dim
        };
        
        JobHandle handle = job.Schedule(modifiedPixels.Length, 16 ); 
        handle.Complete();
        
        positionsNative.CopyTo(_positions);
        
        //Dispose - we don't appreciate memory leakers 'round these parts...
        positionsNative.Dispose();
        modifiedPixels.Dispose();
    }

    [BurstCompile]
    public struct GenerateCubeInfoJob : IJobParallelFor
    {
        public NativeArray<Vector3> Positions;
        public int Dim;

        public void Execute(int i)
        {
            int y = i / Dim;
            Vector3 position = new Vector3(i % Dim, y, 0);
            Positions[i] = position;
        }
    }

    private void Update()
    {
        Render();
    }
    public void Render()
    {
        _posBuffer.SetData(_positions);
        _colorBuffer.SetData(_modifiedPixels);
        _mat.SetBuffer("_InstancePosition", _posBuffer);
        _mat.SetBuffer("_InstanceColor", _colorBuffer);
        
        var bounds = new Bounds(Camera.main.transform.position, Vector3.one * 2000f);
        int cubesToDraw = _positions.Length;
        Graphics.DrawMeshInstancedProcedural(cubeMesh, 0, _mat, bounds, cubesToDraw, null, ShadowCastingMode.Off, false);
    }
    
    void OnDestroy()
    {
        _posBuffer?.Release();
        _colorBuffer?.Release();
    }
}
