using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Windows;

public class Handler : MonoBehaviour
{
    private string pathToJpegs;
    public Texture2D[] jpegs;

    private int currFrame;

    public CubeContainer cc;
    // Start is called before the first frame update
    void Start()
    {
        currFrame = 0;
    }

    void FixedUpdate()
    {
        bool[] onOffArr = new bool[CubeContainer.dim * CubeContainer.dim];
        int onCount = 0;
        var jpeg = jpegs[currFrame];
        Debug.Log(currFrame);
        var pixels = jpeg.GetPixels32();
        int textureSize = 1024;
        int samplingInterval = textureSize / CubeContainer.dim;
        for (int j = 0; j < CubeContainer.dim; j++)
        {
            for (int k = 0; k < CubeContainer.dim; k++)
            {
                int originalX = k * samplingInterval;
                int originalY = j * samplingInterval;
                
                if (pixels[originalY * textureSize + originalX].r < 0.5)
                {
                    onOffArr[j * CubeContainer.dim + k] = false;
                }
                else
                {
                    onCount++;
                    onOffArr[j * CubeContainer.dim + k] = true;
                }
            }
        }
        currFrame++;
        cc.GenerateCubeInfo(onOffArr,onCount);
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
