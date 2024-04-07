using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;
using UnityEngine.Windows;

public class Handler : MonoBehaviour
{
    private string pathToJpegs;
    public Texture2D[] _jpegs;

    private int currFrame;
    public bool dynamicallyLoadFrames = true;
    public int framesToLoadAhead = 10;
    private int framesLoaded = 0;
    // this has to be a float and not a byte (even though a byte is totally enough) because gpus and shaders are wusses who are afraid of true speed and power
    private float[] modifiedPixels;

    public CubeContainer cc;
    private AudioSource _audio;

    private int _totalFrames;

    private Vector2Int textureSize;
    // Start is called before the first frame update
    void Start()
    {
        _audio = GameObject.FindObjectOfType<AudioSource>();
        currFrame = 0;
        if (dynamicallyLoadFrames) DynamicFrameLoad();
        #if UNITY_EDITOR
        string path = "Assets/Resources/frames";
        int fileAmount = System.IO.Directory.GetFiles(path).Length;
        string frameCountPath = "Assets/Resources/frameCount.txt";
        System.IO.File.WriteAllText(frameCountPath, fileAmount.ToString());
        AssetDatabase.Refresh();
        #else
        var frameCountAsset = Resources.Load<TextAsset>("frameCount");
        int.TryParse(frameCountAsset.text, out int fileAmount);
        #endif
        _totalFrames = fileAmount / 2;
        Debug.Log($"Total frames to render: {_totalFrames}");
        Texture2D sampleTexture = Resources.Load<Texture2D>("frames/out-001");
        textureSize = new Vector2Int(sampleTexture.width, sampleTexture.height);
    }

    void FixedUpdate()
    {
        if (currFrame >= _totalFrames)
        {
            _audio.volume = 0;
            return;
        }
        if (!_audio.isPlaying) _audio.Play();
        if (dynamicallyLoadFrames && (currFrame >= (framesLoaded))) DynamicFrameLoad();
        int dim = cc.dim;
       
        var jpeg = _jpegs[currFrame + framesToLoadAhead - framesLoaded];
        var pixels = jpeg.GetRawTextureData();
        
        
        var modifiedPixelsNative = new NativeArray<float>(dim*dim, Allocator.TempJob);
        var pixelsNative = new NativeArray<byte>(pixels, Allocator.TempJob);

        var job = new SampleImageJob()
        {
            Pixels = pixelsNative,
            ModifiedPixels = modifiedPixelsNative,
            Dim = dim,
            TextureSize = textureSize
        };

        JobHandle jobHandle = job.Schedule(dim*dim, 512);
        jobHandle.Complete();
        
        currFrame++;
        cc.GenerateCubeInfo(modifiedPixelsNative, pixels);
        pixelsNative.Dispose();
    }
    
    public struct SampleImageJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte> Pixels;
        [ReadOnly]public int Dim;
        [ReadOnly]public Vector2Int TextureSize;
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
        cc.dim++;
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
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
