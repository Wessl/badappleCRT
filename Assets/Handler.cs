using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Windows;

public class Handler : MonoBehaviour
{
    private string pathToJpegs;
    public Texture2D[] jpegs;

    private int currFrame;
    public bool dynamicallyLoadFrames = true;
    private int framesToLoadAhead = 10;
    private int framesLoaded = 0;
    private bool[] onOffArr;

    public CubeContainer cc;
    // Start is called before the first frame update
    void Start()
    {
        currFrame = 0;
        
        if (dynamicallyLoadFrames) DynamicFrameLoad();
    }

    void FixedUpdate()
    {
        int dim = cc.dim;
        if (dynamicallyLoadFrames && (currFrame >= (framesLoaded))) DynamicFrameLoad();
        onOffArr = new bool[dim * dim];
        int onCount = 0;
        Debug.Log($"what is currFrame {currFrame}, framesToLookAhead {framesToLoadAhead}, and framesLoaded {framesLoaded}?");
        var jpeg = jpegs[currFrame + framesToLoadAhead - framesLoaded];
        var pixels = jpeg.GetPixels32();
        int textureSize = 1024;
        int samplingInterval = textureSize / dim;


        var onOffArrNative = new NativeArray<bool>(onOffArr, Allocator.TempJob);
        var pixelsNative = new NativeArray<Color32>(pixels, Allocator.TempJob);

        var job = new PixelReadJob()
        {
            Pixels = pixelsNative,
            OnOffArr = onOffArrNative,
            SamplingInterval = samplingInterval,
            Dim = dim,
            TextureSize = textureSize
        };

        JobHandle jobHandle = job.Schedule(onOffArr.Length, 32);
        jobHandle.Complete();
        
        currFrame++;
        // we used to save how many needed were on in total, which is useful - think about how to get it back
        cc.GenerateCubeInfo(onOffArrNative,onCount);
        
        pixelsNative.Dispose();
    }

    public struct PixelReadJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Color32> Pixels;
        public NativeArray<bool> OnOffArr;
        public int SamplingInterval;
        public int Dim;
        public int TextureSize;
    
        public void Execute(int index)
        {
            int j = index / Dim; // row
            int k = index % Dim; // column
        
            int originalX = k * SamplingInterval;
            int originalY = j * SamplingInterval;
        
            if (Pixels[originalY * TextureSize + originalX].r < 128)
                OnOffArr[index] = false;
            else
                OnOffArr[index] = true;
        }
    }


    private void DynamicFrameLoad()
    {
        string basePath = "frames/out-";
        jpegs = new Texture2D[framesToLoadAhead];
        int frameToLoad = currFrame+1;
        for (int i = 0; i < framesToLoadAhead; i++)
        {
            string nextPath = String.Concat(basePath, frameToLoad.ToString("D3"));
            jpegs[i] = Resources.Load<Texture2D>(nextPath);
            frameToLoad++;
            framesLoaded++;
        }
        
    }

    void PreHandle()
    {
        for (int i = 0; i < jpegs.Length; i++)
        {
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath( AssetDatabase.GetAssetPath(jpegs[i]) );
 
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
}
