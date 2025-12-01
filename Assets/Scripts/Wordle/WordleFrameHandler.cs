using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class WordleFrameHandler : MonoBehaviour
{
    private string m_pathToJpegs;
    private Texture2D m_jpeg;

    private int m_currFrame;
    public bool dynamicallyLoadFrames = true;
    private bool m_hasStartedPlayingVideo;
    private bool m_isFinished;
    public int framesToLoadAhead = 10;
    private int m_framesLoaded = 0;
    // this has to be a float and not a byte (even though a byte is totally enough) because gpus and shaders are wusses who are afraid of true speed and power
    private float[] m_modifiedPixels;

    public WordlePresenter m_presenter;

    private EnglishDictionary m_dict;
    public Dictionary<string, string> m_words;
    private AudioSource m_audio;

    private int m_totalFrames;
    private float m_platformVideoDelay;

    private Vector2Int m_textureSize;
    private long m_totalPixelsShown;
    

    
    private void Awake()
    {
        Application.targetFrameRate = 60;
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
        m_presenter = FindFirstObjectByType<WordlePresenter>();
        m_dict = FindFirstObjectByType<EnglishDictionary>();
        m_words = m_dict.GetWordDict();
        m_hasStartedPlayingVideo = false;
        m_isFinished = false;
        m_currFrame = 0;
        if (dynamicallyLoadFrames) LoadFrame(0);
        
        var fileAmount = TryFindFileAmount();
        m_totalFrames = fileAmount / 2;
        Debug.Log($"Total frames to render: {m_totalFrames}");
        Texture2D sampleTexture = Resources.Load<Texture2D>("frames/out-001");
        
        m_textureSize = new Vector2Int(sampleTexture.width, sampleTexture.height);
        m_presenter.Frames = m_totalFrames;
        
        // cc.SetupBuffers();
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

    void Update()
    {
        if (m_currFrame >= m_totalFrames)
        {
            m_audio.volume = 0;
            m_audio.Pause();
            if (m_isFinished == false && !m_isFinished) Finish();
            return;
        }
        if (!m_hasStartedPlayingVideo && CanStartPlayingVideo() == false) return;

        m_currFrame = m_audio.timeSamples / (m_audio.clip.frequency / 60); 
        
        LoadFrame(m_currFrame);
        
        PresentFrame();
    }

    void Finish()
    {
        Debug.Log($"Amount of times 'no words found' was displayed: {m_presenter.GetNoWordsFoundCount}");
        m_isFinished = true;
    }

    private void PresentFrame()
    {
        int dim = m_presenter.Dimension;
       
        Profiler.BeginSample("GetJpegTextureData");
        
        var pixels = m_jpeg.GetRawTextureData();
        Profiler.EndSample();
        
        Profiler.BeginSample("SampleImageJob");
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
        Profiler.EndSample();

        m_presenter.GenerateWordleFrame(modifiedPixelsNative, m_currFrame);
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


    private void LoadFrame(int frameToLoad)
    {
        Profiler.BeginSample("LoadFrame");
        // Unload previous assets
        Resources.UnloadAsset(m_jpeg);
        
        // Load future assets
        string basePath = "frames/out-";
        string nextPath = String.Concat(basePath, frameToLoad.ToString("D3"));
        m_jpeg = Resources.Load<Texture2D>(nextPath);
        m_framesLoaded++;
        
        Profiler.EndSample();
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
}
