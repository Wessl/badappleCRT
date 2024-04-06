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
    public int framesToLoadAhead = 10;
    private int framesLoaded = 0;
    // this has to be a float and not a byte (even though a byte is totally enough) because gpus and shaders are wusses who are afraid of true speed and power
    private float[] modifiedPixels;

    public CubeContainer cc;
    // Start is called before the first frame update
    void Start()
    {
        currFrame = 0;
        
        if (dynamicallyLoadFrames) DynamicFrameLoad();
    }

    void FixedUpdate()
    {
        if (dynamicallyLoadFrames && (currFrame >= (framesLoaded))) DynamicFrameLoad();
        int dim = cc.dim;
        modifiedPixels = new float[dim * dim];
        // Debug.Log($"what is currFrame {currFrame}, framesToLookAhead {framesToLoadAhead}, and framesLoaded {framesLoaded}?");
        var jpeg = jpegs[currFrame + framesToLoadAhead - framesLoaded];
        var pixels = jpeg.GetRawTextureData();
        Vector2Int textureSize = new Vector2Int(2048, 1536);
        


        var modifiedPixelsNative = new NativeArray<float>(modifiedPixels, Allocator.TempJob);
        var pixelsNative = new NativeArray<byte>(pixels, Allocator.TempJob);

        var job = new SampleImageJob()
        {
            Pixels = pixelsNative,
            ModifiedArr = modifiedPixelsNative,
            Dim = dim,
            TextureSize = textureSize
        };

        JobHandle jobHandle = job.Schedule(modifiedPixels.Length, 64);
        jobHandle.Complete();
        
        currFrame++;
        // we used to save how many needed were on in total, which is useful - think about how to get it back
        cc.GenerateCubeInfo(modifiedPixelsNative, pixels);
        
        pixelsNative.Dispose();
    }
    
    public struct SampleImageJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte> Pixels;
        public NativeArray<float> ModifiedArr;
        public int Dim;
        public Vector2Int TextureSize;
    
        public void Execute(int index)
        {
            int row = index / Dim; // Row in the downscaled image
            int col = index % Dim; // Column in the downscaled image
            
            float scaleWidth = (float)TextureSize.x / Dim;
            float scaleHeight = (float)TextureSize.y / Dim;

            int originalX = (int)(col * scaleWidth);
            int originalY = (int)(row * scaleHeight);

            // Correct indexing for accessing a pixel in a linear array
            ModifiedArr[index] = Pixels[originalY * TextureSize.x + originalX];
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

        cc.dim++;
        // Is there some way to EFFICIENTLY unload the previously loaded assets?
    }

    private void OnDestroy()
    {
        Resources.UnloadUnusedAssets();
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
