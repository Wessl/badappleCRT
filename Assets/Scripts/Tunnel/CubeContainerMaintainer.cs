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
    private Material m_mat;
    private NativeArray<Vector3> m_positions;
    private NativeArray<float> m_pixels;
    private ComputeBuffer m_posBuffer;
    private ComputeBuffer m_colorBuffer;
    private int StrideVec3;
    private int StrideFloat;
    private bool m_readyToRender;
    private int m_zDepth = 1024;

    private int m_cubeIndex;
    
    [SerializeField] private Shader drawMeshShader;
    [SerializeField] public Mesh cubeMesh;
    private static readonly int InstancePosition = Shader.PropertyToID("_InstancePosition");
    private static readonly int InstanceColor = Shader.PropertyToID("_InstanceColor");

    public int Frames { get; set; }

    private void Awake()
    {
        m_mat = new Material(drawMeshShader);
        m_mat.enableInstancing = true;
        StrideVec3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        StrideFloat = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
        m_readyToRender = false;
        m_cubeIndex = 0;
    }
    
    // TODO: 1. Double / Triple buffering to directly copy into GPU memory
    // TODO: 2. Implement circular buffer instead of 1 gigantic buffer holding everything in memory. 

    public void SetupBuffers()
    {
        
        int totalCubes = dim * dim * Frames;
        if (totalCubes <= 0)
        {
            Debug.LogError($"{nameof(totalCubes)} is negative [{totalCubes}], which probably means you tried to assign a number larger than {System.Int32.MaxValue}");
            return;
        }
        
        m_positions = new NativeArray<Vector3>(totalCubes, Allocator.Persistent);
        m_pixels = new NativeArray<float>(totalCubes, Allocator.Persistent);
        int bufferInstanceCount = dim * dim * m_zDepth;
        
        m_posBuffer = new ComputeBuffer (bufferInstanceCount, StrideVec3, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
        m_colorBuffer = new ComputeBuffer(bufferInstanceCount, StrideFloat, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
        
        
        // Initialize with a position far away - if stuff initializes at the origin, it creates huge "overdraw" at those pixels
        // since the camera renders quite a few pixels containing them at the beginning, put them far away :D 
        Vector3[] defaultPositions = new Vector3[bufferInstanceCount];
        for (int i = 0; i < bufferInstanceCount; i++)
        {
            defaultPositions[i] = new Vector3(10000f, 10000f, 10000f);
        }
        
        m_posBuffer.SetData(defaultPositions);
        m_colorBuffer.SetData(new float[bufferInstanceCount * StrideFloat / sizeof(float)]); 
        
        
        m_mat.SetBuffer(InstancePosition, m_posBuffer);
        m_mat.SetBuffer(InstanceColor, m_colorBuffer);
    }

    public void GenerateCubeInfo(NativeArray<float> modifiedPixels, byte[] pixels, int currentFrame)
    {
        Profiler.BeginSample("GenerateCubeInfo");
        int totalCubesThisFrame = dim * dim;
        var positionsNative = new NativeArray<Vector3>(totalCubesThisFrame, Allocator.TempJob);
        
        var job = new GenerateCubeInfoJob()
        {
            Positions = positionsNative,
            Dim = dim,
            Depth = currentFrame
        };
        
        JobHandle handle = job.Schedule(modifiedPixels.Length, 64); 
        handle.Complete();
        
        // Copying from NativeArray with new values into NativeArray with all previous values via Slice
        NativeSlice<Vector3> positionArraySlice = new NativeSlice<Vector3>(m_positions, m_cubeIndex, positionsNative.Length);
        positionArraySlice.CopyFrom(positionsNative);
        NativeSlice<float> pixelArraySlice = new NativeSlice<float>(m_pixels, m_cubeIndex, positionsNative.Length);
        pixelArraySlice.CopyFrom(modifiedPixels);
        
        m_readyToRender = true;
        positionsNative.Dispose();
        modifiedPixels.Dispose();
        
        // We need it for posterity. :)
        m_cubeIndex += dim * dim;
        Profiler.EndSample();
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
        int frameCount = Time.frameCount;
        
        // Write data from NativeArray into ComputeBuffer using Begin/EndWrite. Faster than using SetData(). 
        Profiler.BeginSample("WritePositionData");

        int modulatedIndex = m_cubeIndex % (m_zDepth * dim * dim);
        Debug.Log($"modulatedIndex: {modulatedIndex}, cubeIndex: {m_cubeIndex}");
        NativeArray<Vector3> posData = m_posBuffer.BeginWrite<Vector3>(modulatedIndex, dim*dim );
        for (int i = 0; i < dim*dim; i++)
        {
            posData[i] = m_positions[m_cubeIndex -dim*dim + i];
        }
        m_posBuffer.EndWrite<Vector3>(dim*dim);
        Profiler.EndSample();
        
        Profiler.BeginSample("WritePixelData");
        NativeArray<float> colorData = m_colorBuffer.BeginWrite<float>(modulatedIndex, dim*dim );
        for (int i = 0; i < dim*dim; i++)
        {
            colorData[i] = m_pixels[m_cubeIndex -dim*dim + i];
        }
        m_colorBuffer.EndWrite<float>(dim*dim);
        Profiler.EndSample();
        
        m_mat.SetBuffer(InstancePosition, m_posBuffer);
        m_mat.SetBuffer(InstanceColor, m_colorBuffer);
        
        var bounds = new Bounds(Vector3.zero, Vector3.one * 20000f);
        int cubesToDraw = m_positions.Length;
        Graphics.DrawMeshInstancedProcedural(cubeMesh, 0, m_mat, bounds, cubesToDraw, null, ShadowCastingMode.Off, false);
        Profiler.EndSample();
    }
    
    void OnDestroy()
    {
        m_posBuffer?.Release();
        m_posBuffer = null;
        m_colorBuffer?.Release();
        m_colorBuffer = null;
        

        m_posBuffer = null;
        m_colorBuffer = null;
        m_mat = null;
        
        m_positions.Dispose();
        m_pixels.Dispose();
    }
}
