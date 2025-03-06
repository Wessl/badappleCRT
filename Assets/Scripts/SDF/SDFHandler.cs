using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Windows;

public class SDFHandler : MonoBehaviour
{
    private string pathToJpegs;
    public Texture2D[] _jpegs;
    public Image imageToRenderTo;
    private Texture2D targetTexture;
    private int currFrame;
    public bool dynamicallyLoadFrames = true;
    private bool hasStartedPlayingVideo;
    private bool isFinished;
    public int framesToLoadAhead = 10;
    private int framesLoaded = 0;
    // this has to be a float and not a byte (even though a byte is totally enough) because gpus and shaders are wusses who are afraid of true speed and power
    private float[] modifiedPixels;

    private AudioSource _audio;

    private int _totalFrames;
    private float platformVideoDelay;

    private Vector2Int textureSize;
    private long totalPixelsShown;
    
    
    private void Awake()
    {
        # if UNITY_EDITOR
        platformVideoDelay = 0.125f;
        #elif UNITY_STANDALONE_WIN
        platformVideoDelay = 0.3f;
        #endif
    }

    void Start()
    {
        PrintBadAppleLog();
        _audio = GameObject.FindObjectOfType<AudioSource>();
        hasStartedPlayingVideo = false;
        isFinished = false;
        currFrame = 0;
        if (dynamicallyLoadFrames) DynamicFrameLoad();
        var fileAmount = TryFindFileAmount();
        _totalFrames = fileAmount / 2;
        Debug.Log($"Total frames to render: {_totalFrames}");
        Texture2D sampleTexture = Resources.Load<Texture2D>("frames/out-001");
        textureSize = new Vector2Int(sampleTexture.width, sampleTexture.height);
        targetTexture = new Texture2D(1024, 768, TextureFormat.R8, false);
        imageToRenderTo.sprite = Sprite.Create(targetTexture, new Rect(0.0f, 0.0f, targetTexture.width, targetTexture.height), new Vector2(0.5f, 0.5f));
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
        if (currFrame >= _totalFrames)
        {
            _audio.volume = 0;
            _audio.Pause();
            if (isFinished == false && !isFinished) Finish();
            return;
        }
        if (!hasStartedPlayingVideo && CanStartPlayingVideo() == false) return;
        
        PresentFrame();
    }

    void Finish()
    {
        isFinished = true;
        Debug.Log("Bad Apple finished rendering. Stats:");
    }

    private void PresentFrame()
    {
        Debug.Log("We are currently presenting frame number " + currFrame + " and it has been " + Time.time + " seconds.");
        if (dynamicallyLoadFrames && (currFrame >= (framesLoaded))) DynamicFrameLoad();
        var jpeg = _jpegs[currFrame + framesToLoadAhead - framesLoaded];
        // Technically using bytes here instead of bits is not necessary... Maybe we can do something about that?
        NativeArray<byte> pixels = jpeg.GetRawTextureData<byte>();
        // And now - actually do something with this :D
        CalculateSDF(pixels, jpeg.width, jpeg.height);
        targetTexture.LoadRawTextureData(pixels);
        targetTexture.Apply();
        currFrame++;
        pixels.Dispose();

    }

    void CalculateSDF(NativeArray<byte> pixels, int width, int height)
    {
        // Naive first approach: Do a sweep around each pixel that is not black, until we find one that is black. 
        Debug.Log($"Byte pixel 0: {pixels[0]}");
        for (int i = 0; i < pixels.Length; i++)
        {
            byte pixel = pixels[i];
            CheckSurroundingPixels(i % width, (int)Math.Floor((double)i / width));
        }
    }
    bool CheckSurroundingPixels(int x, int y)
    {
        throw new NotImplementedException();
        //if (x < 0) // blah blah
        //if (y >= height) // blasch blacsh
    }

    bool CanStartPlayingVideo()
    {
        if (Time.time > platformVideoDelay)
        {
            hasStartedPlayingVideo = true;
            _audio.time -= (Time.time - platformVideoDelay);
            return true;
        }

        return false;
    }

    private void DynamicFrameLoad()
    {
        // Unload previous assets
        foreach (var oldJpeg in _jpegs)
        {
            Resources.UnloadAsset(oldJpeg);
        }
        
        // Load future assets
        string basePath = "frames/out-";
        _jpegs = new Texture2D[framesToLoadAhead];
        int frameToLoad = currFrame+1;
        for (int i = 0; i < framesToLoadAhead; i++)
        {
            string nextPath = String.Concat(basePath, frameToLoad.ToString("D3"));
            _jpegs[i] = Resources.Load<Texture2D>(nextPath);
            frameToLoad++;
            framesLoaded++;
        }
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
    }
}
