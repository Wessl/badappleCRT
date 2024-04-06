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
    [SerializeField] private Mesh cubeMesh;
    private Material matBlack;
    private Material matWhite;
    private Vector3[] whitePos;
    private RenderParams whiteRP;
    private RenderParams blackRP;
    private ComputeBuffer _posBuffer;
    private ComputeBuffer _colorBuffer;
    private static readonly int Posbuffer = Shader.PropertyToID("posbuffer");
    private static readonly int Camerapos = Shader.PropertyToID("camerapos");
    private int numInstances;
    private int StrideVec3;
    private int StrideFloat;
    private float[] _modifiedPixels;
    [SerializeField] private Shader drawMeshShader;

    private void Awake()
    {
        matBlack = new Material(drawMeshShader);
        matWhite = new Material(drawMeshShader);
        matWhite.enableInstancing = true;
        matBlack.enableInstancing = true;
        matWhite.color = Color.white;
        StrideVec3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        StrideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
    }

    public void GenerateCubeInfo(NativeArray<float> modifiedPixels, byte[] pixels)
    {
        // Also clean up your code convetions with variable names...
        // Figure out how to do this by just passing in colors straight from the pixels. For some reason the way I did it before did not work. 
        int totalCubes = dim * dim;
        //var onOffArrNative = new NativeArray<bool>(onOffArr, Allocator.TempJob);
        var whitePosNative = new NativeArray<Vector3>(totalCubes, Allocator.TempJob);
        whitePos = new Vector3[dim*dim];
        _modifiedPixels = new float[dim * dim];
        _posBuffer?.Release();
        _colorBuffer?.Release();
        _posBuffer = new ComputeBuffer (dim*dim, StrideVec3, ComputeBufferType.Default);
        _colorBuffer = new ComputeBuffer(dim * dim, StrideFloat, ComputeBufferType.Default);
        modifiedPixels.CopyTo(_modifiedPixels);
        var job = new GenerateCubeInfoJob()
        {
            WhitePos = whitePosNative,
            Dim = dim
        };
        
        // Schedule the job with one execution per element in onOffArr
        JobHandle handle = job.Schedule(modifiedPixels.Length, 64 ); 
        handle.Complete();
        
        whitePosNative.CopyTo(whitePos);
        
        //Dispose - we don't appreciate memory leakers 'round these parts...
        whitePosNative.Dispose();
    }

    [BurstCompile]
    public struct GenerateCubeInfoJob : IJobParallelFor
    {
        public NativeArray<Vector3> WhitePos;
        public int Dim;

        public void Execute(int i)
        {
            int y = i / Dim;
            Vector3 position = new Vector3(i % Dim, y, 0);
            WhitePos[i] = position;
        }
    }

    private void Update()
    {
        Render();
    }
    public void Render()
    {
        _posBuffer.SetData(whitePos);
        _colorBuffer.SetData(_modifiedPixels);
        matWhite.SetBuffer("_InstancePosition", _posBuffer);
        matWhite.SetBuffer("_InstanceColor", _colorBuffer);
        //matBlack.SetBuffer("_InstancePosition", _posBufferBlack);
        //matBlack.SetFloat("col", 0);
        matWhite.SetFloat("col", 1);
        
        var bounds = new Bounds(Camera.main.transform.position, Vector3.one * 2000f);
        int whitePixels = whitePos.Length;
        if (whitePixels > 0)
        {
            Graphics.DrawMeshInstancedProcedural(cubeMesh, 0, matWhite, bounds, whitePixels, null, ShadowCastingMode.Off, false);
        }
    }
    
    void OnDestroy()
    {
        _posBuffer?.Release();
        _colorBuffer?.Release();
    }
}
