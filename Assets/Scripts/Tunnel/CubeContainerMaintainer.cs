using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class CubeContainerMaintainer : MonoBehaviour
{
    public int dim = 512;
    private Material _mat;
    private Vector3[] m_positions;
    private ComputeBuffer m_posBuffer;
    private ComputeBuffer m_colorBuffer;
    private static readonly int Posbuffer = Shader.PropertyToID("posbuffer");
    private static readonly int Camerapos = Shader.PropertyToID("camerapos");
    private int StrideVec3;
    private int StrideFloat;
    private float[] m_pixels;
    private bool m_readyToRender;

    private int m_cubeIndex;
    
    [SerializeField] private Shader drawMeshShader;
    [SerializeField] public Mesh cubeMesh;

    public int Frames { get; set; }

    private void Awake()
    {
        _mat = new Material(drawMeshShader);
        _mat.enableInstancing = true;
        StrideVec3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        StrideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
        m_readyToRender = false;
        m_cubeIndex = 0;
    }

    public void SetupBuffers()
    {
        int totalCubes = dim * dim * Frames;
        if (totalCubes <= 0)
        {
            Debug.LogError($"{nameof(totalCubes)} is negative [{totalCubes}], which probably means you tried to assign a number larger than {System.Int32.MaxValue}");
            return;
        }
        m_positions = new Vector3[totalCubes];
        m_pixels = new float[totalCubes];
        m_posBuffer = new ComputeBuffer (totalCubes, StrideVec3, ComputeBufferType.Default);
        m_colorBuffer = new ComputeBuffer(totalCubes, StrideFloat, ComputeBufferType.Default);
    }

    public void GenerateCubeInfo(NativeArray<float> modifiedPixels, byte[] pixels, int currentFrame)
    {
        int totalCubesThisFrame = dim * dim;
        var positionsNative = new NativeArray<Vector3>(totalCubesThisFrame, Allocator.TempJob);
        
       // Todo: here, copy pixels from modifiedpixels into m_pixels at the correct position in the array. 
       // Then do the same thing for the positions. 
        var job = new GenerateCubeInfoJob()
        {
            Positions = positionsNative,
            Dim = dim,
            Depth = currentFrame
        };
        
        JobHandle handle = job.Schedule(modifiedPixels.Length, 64); 
        handle.Complete();
        
        // Copying - surely there are better ways than this? 
        Vector3[] tempPosArray = new Vector3[positionsNative.Length];
        float[] tempColorArray = new float[modifiedPixels.Length];
        positionsNative.CopyTo(tempPosArray);
        tempPosArray.CopyTo(m_positions, m_cubeIndex);
        modifiedPixels.CopyTo(tempColorArray);
        tempColorArray.CopyTo(m_pixels, m_cubeIndex);
        Debug.Log($"What is m_pixels of m_cubeIndex? {m_pixels[m_cubeIndex]}");
        
        m_readyToRender = true;
        
        //Dispose - we don't appreciate memory leakers 'round these parts...
        positionsNative.Dispose();
        modifiedPixels.Dispose();
        
        // We need it for posterity. :)
        m_cubeIndex += dim * dim;
    }

    [BurstCompile]
    public struct GenerateCubeInfoJob : IJobParallelFor
    {
        public NativeArray<Vector3> Positions;
        public int Dim;
        public int Depth;

        public void Execute(int i)
        {
            int y = i / Dim;
            Vector3 position = new Vector3(i % Dim, y, -Depth);
            Positions[i] = position;
        }
    }

    private void Update()
    {
        if(m_readyToRender)Render();
    }
    public void Render()
    {
        Profiler.BeginSample("Render()");
        m_posBuffer.SetData(m_positions);
        m_colorBuffer.SetData(m_pixels);
        _mat.SetBuffer("_InstancePosition", m_posBuffer);
        _mat.SetBuffer("_InstanceColor", m_colorBuffer);
        
        var bounds = new Bounds(Camera.main.transform.position, Vector3.one * 2000f);
        int cubesToDraw = m_positions.Length;
        Graphics.DrawMeshInstancedProcedural(cubeMesh, 0, _mat, bounds, cubesToDraw, null, ShadowCastingMode.Off, false);
        Profiler.EndSample();
    }
    
    void OnDestroy()
    {
        m_posBuffer?.Release();
        m_colorBuffer?.Release();
    }
}
