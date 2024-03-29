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
    public static int dim = 512;
    [SerializeField] private Mesh cubeMesh;
    private Material matBlack;
    private Material matWhite;
    private Vector3[] blackPos = new Vector3[dim*dim];
    private Vector3[] whitePos = new Vector3[dim*dim];
    private RenderParams whiteRP;
    private RenderParams blackRP;
    private ComputeBuffer _posBufferWhite;
    private ComputeBuffer _posBufferBlack;
    private static readonly int Posbuffer = Shader.PropertyToID("posbuffer");
    private static readonly int Camerapos = Shader.PropertyToID("camerapos");
    private int numInstances;
    [SerializeField] private Shader drawMeshShader;

    private void Awake()
    {
        matBlack = new Material(drawMeshShader);
        matWhite = new Material(drawMeshShader);
        matWhite.enableInstancing = true;
        matBlack.enableInstancing = true;
        matWhite.color = Color.white;
        matWhite.color = Color.black;
        var strideVec3 = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        _posBufferWhite = new ComputeBuffer (dim*dim, strideVec3, ComputeBufferType.Default);
        _posBufferBlack = new ComputeBuffer (dim*dim, strideVec3, ComputeBufferType.Default);
        
    }

    public void GenerateCubeInfo(bool[] onOffArr, int onCount)
    {
        int totalCubes = dim * dim;
        var onOffArrNative = new NativeArray<bool>(onOffArr, Allocator.TempJob);
        var blackPosNative = new NativeArray<Vector3>(totalCubes, Allocator.TempJob);
        var whitePosNative = new NativeArray<Vector3>(totalCubes, Allocator.TempJob);

        var job = new GenerateCubeInfoJob()
        {
            OnOffArr = onOffArrNative,
            BlackPos = blackPosNative,
            WhitePos = whitePosNative,
            Dim = dim
        };
        
        // Schedule the job with one execution per element in onOffArr
        JobHandle handle = job.Schedule(onOffArrNative.Length, 64); 
        handle.Complete();
        
        whitePosNative.CopyTo(whitePos);
        blackPosNative.CopyTo(blackPos);
        
        //Dispose - we don't appreciate memory leakers 'round these parts...
        onOffArrNative.Dispose();
        blackPosNative.Dispose();
        whitePosNative.Dispose();

        /*
        int blackTotal = dim * dim - onCount;
        int whiteTotal = onCount;
        blackPos = new Vector3[dim * dim];
        whitePos = new Vector3[dim*dim];

        int arrIndexBlack = 0;
        int arrIndexWhite = 0;

        int y = 0;
        for (int i = 0; i < onOffArr.Length; i++)
        {
            if (i % dim == 0 && i != 0) // Increment y when a new row starts, but not at the first element
            {
                y++;
            }
            Vector3 position = new Vector3(i % dim, y, 0); // Calculate position once per iteration
            if (!onOffArr[i])
            {
                blackPos[arrIndexBlack++] = position;// + new Vector3(0,-y/10f,blackTotal/1000f + y/1.5f);
            }
            else
            {
                whitePos[arrIndexWhite++] = position;// + new Vector3(0, y/10f, -whiteTotal/1000f + y/1.4f);
            }
        }
        */
    }

    [BurstCompile]
    public struct GenerateCubeInfoJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<bool> OnOffArr;
        public NativeArray<Vector3> BlackPos;
        public NativeArray<Vector3> WhitePos;
        public int Dim;

        public void Execute(int i)
        {
            int y = i / Dim;
            Vector3 position = new Vector3(i % Dim, y, 0);
            if (OnOffArr[i])
            {
                WhitePos[i] = position;
            }
            else
            {
                BlackPos[i] = position;
            }
        }
    }

    private void Update()
    {
        Render();
    }
    public void Render()
    {
        _posBufferBlack.SetData(blackPos);
        _posBufferWhite.SetData(whitePos);
        matWhite.SetBuffer("_InstancePosition", _posBufferWhite);
        matBlack.SetBuffer("_InstancePosition", _posBufferBlack);
        matBlack.SetFloat("col", 0);
        matWhite.SetFloat("col", 1);
        var bounds = new Bounds(Camera.main.transform.position, Vector3.one * 2000f);
        int whitePixels = whitePos.Length;
        if (whitePixels > 0)
        {
            Graphics.DrawMeshInstancedProcedural(cubeMesh, 0, matWhite, bounds, whitePixels, null, ShadowCastingMode.Off, false);
        }
        int blackPixels = blackPos.Length;
        if (blackPixels > 0)
        {
            Graphics.DrawMeshInstancedProcedural(cubeMesh, 0, matBlack, bounds, blackPixels, null, ShadowCastingMode.Off, false);
        }
    }
    
    void OnDestroy()
    {
        if (_posBufferBlack != null)
            _posBufferBlack.Release();
        if (_posBufferWhite != null)
            _posBufferWhite.Release();
    }
}
