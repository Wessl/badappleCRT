using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.Video;
using UnityEngine.Windows;

public class TunnelHandler : MonoBehaviour
{
    private string m_pathToJpegs;
    public Texture2D[] _jpegs;

    private int m_currFrame;
    public bool dynamicallyLoadFrames = true;
    private bool m_hasStartedPlayingVideo;
    private bool m_isFinished;
    public int framesToLoadAhead = 10;
    private int m_framesLoaded = 0;
    // this has to be a float and not a byte (even though a byte is totally enough) because gpus and shaders are wusses who are afraid of true speed and power
    private float[] m_modifiedPixels;

    public CubeContainerMaintainer cc;
    private AudioSource m_audio;

    private int m_totalFrames;
    private float m_platformVideoDelay;

    private Vector2Int m_textureSize;
    private long m_totalPixelsShown;

    
    private void Awake()
    {
        # if UNITY_EDITOR
        m_platformVideoDelay = 0.125f;
        #elif UNITY_STANDALONE_WIN
        m_platformVideoDelay = 0.116f;
        #endif
    }

    void Start()
    {
        PrintBadAppleLog();
        m_audio = FindFirstObjectByType<AudioSource>();
        m_hasStartedPlayingVideo = false;
        m_isFinished = false;
        m_currFrame = 0;
        if (dynamicallyLoadFrames) DynamicFrameLoad();
        
        var fileAmount = TryFindFileAmount();
        m_totalFrames = fileAmount / 2;
        Debug.Log($"Total frames to render: {m_totalFrames}");
        Texture2D sampleTexture = Resources.Load<Texture2D>("frames/out-001");
        
        m_textureSize = new Vector2Int(sampleTexture.width, sampleTexture.height);
        cc.Frames = m_totalFrames;
        cc.SetupBuffers();
    }

    int TryFindFileAmount()
    {
#if UNITY_EDITOR
        string path = "Assets/Resources/frames";
        int fileAmount = System.IO.Directory.GetFiles(path).Length;
        string frameCountPath = "Assets/Resources/frameCount.txt";
        System.IO.File.WriteAllText(frameCountPath, fileAmount.ToString());
        AssetDatabase.Refresh();
        return fileAmount;
#else
        var frameCountAsset = Resources.Load<TextAsset>("frameCount");
        int.TryParse(frameCountAsset.text, out int fileAmount);
        return fileAmount;
#endif
    }

    void PrintBadAppleLog()
    {
        Debug.Log("Welcome to Bad Apple - programming by Dez/Wesslo. Original music video by nomico as 'Bad Apple!!'. Upscaled video courtesy of あにら on archive.org.");
        Debug.Log(Resources.Load<TextAsset>("ascii"));
    }

    void FixedUpdate()
    {
        if (m_currFrame >= m_totalFrames)
        {
            m_audio.volume = 0;
            m_audio.Pause();
            if (m_isFinished == false && !m_isFinished) Finish();
            return;
        }
        if (!m_hasStartedPlayingVideo && CanStartPlayingVideo() == false) return;
        
        PresentFrame();
    }

    void Finish()
    {
        m_isFinished = true;
        Debug.Log("Bad Apple finished rendering. Stats:");
        var totalVerticesRendered = m_totalPixelsShown * cc.cubeMesh.vertexCount;
        Debug.Log($"A total of {m_totalPixelsShown} pixels were rendered in real time.");
        Debug.Log($"Each pixel is actually a fully rasterized cube in 3d space. Thus, {totalVerticesRendered} vertices were rendered.");
    }

    private void PresentFrame()
    {
        Debug.Log("We are currently presenting frame number " + m_currFrame + " and it has been " + Time.time + " seconds.");
        if (dynamicallyLoadFrames && (m_currFrame >= (m_framesLoaded))) DynamicFrameLoad();
        int dim = cc.dim;
       
        var jpeg = _jpegs[m_currFrame + framesToLoadAhead - m_framesLoaded];
        var pixels = jpeg.GetRawTextureData();
        
        var pixelsNative = new NativeArray<byte>(pixels, Allocator.TempJob);
        var modifiedPixelsNative = new NativeArray<float>(dim*dim, Allocator.TempJob);

        var job = new SampleImageJob()
        {
            Pixels = pixelsNative,
            ModifiedPixels = modifiedPixelsNative,
            Dim = dim,
            TextureSize = m_textureSize
        };

        JobHandle jobHandle = job.Schedule(dim*dim, 512);
        jobHandle.Complete();
        
        m_currFrame++;
        cc.GenerateCubeInfo(modifiedPixelsNative, pixels, m_currFrame);
        pixelsNative.Dispose();
        
        // Stats
        m_totalPixelsShown += dim * dim;
    }

    bool CanStartPlayingVideo()
    {
        if (Time.time > m_platformVideoDelay)
        {
            m_hasStartedPlayingVideo = true;
            m_audio.time -= (Time.time - m_platformVideoDelay);
            return true;
        }

        return false;
    }
    
    public struct SampleImageJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte> Pixels;
        [ReadOnly] public int Dim;
        [ReadOnly] public Vector2Int TextureSize;
        public NativeArray<float> ModifiedPixels;
    
        public void Execute(int index)
        {
            int row = index / Dim; // Row in the downscaled image
            int col = index % Dim; // Column in the downscaled image
            
            float scaleWidth = (float)TextureSize.x / Dim;
            float scaleHeight = (float)TextureSize.y / Dim;

            int originalX = (int)(col * scaleWidth);
            int originalY = (int)(row * scaleHeight);

            // Correct indexing for accessing a pixel in a linear array
            ModifiedPixels[index] = Pixels[originalY * TextureSize.x + originalX];
        }
    }


    private void DynamicFrameLoad()
    {
        Profiler.BeginSample("DynamicFrameLoad");
        // Unload previous assets
        foreach (var oldJpeg in _jpegs)
        {
            Resources.UnloadAsset(oldJpeg);
        }
        
        // Load future assets
        string basePath = "frames/out-";
        _jpegs = new Texture2D[framesToLoadAhead];
        int frameToLoad = m_currFrame+1;
        for (int i = 0; i < framesToLoadAhead; i++)
        {
            string nextPath = String.Concat(basePath, frameToLoad.ToString("D3"));
            _jpegs[i] = Resources.Load<Texture2D>(nextPath);
            frameToLoad++;
            m_framesLoaded++;
        }
        // cc.dim++;
        Profiler.EndSample();
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    #if UNITY_EDITOR
    void PreHandle()
    {
        for (int i = 0; i < _jpegs.Length; i++)
        {
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath( AssetDatabase.GetAssetPath(_jpegs[i]) );
 
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Default;

            importer.maxTextureSize = 1024;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = 50;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
 
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
        
    }
    #endif
}
