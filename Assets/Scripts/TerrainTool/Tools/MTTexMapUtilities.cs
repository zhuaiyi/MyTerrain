#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public enum TextureMapFormat
{
    PNG,
    JPG,
    TGA
}

/// <summary>
/// 此类用于生成地形的混合Splat纹理的方案
/// </summary>
public static class MTTexMapUtilities
{
    public static void GenerateTerrainMap(Terrain terrain, string tileName, int baseMapWidth, int baseMapHeight, TextureMapFormat format = TextureMapFormat.JPG)
    {
        Texture2D[] alphamapTextures = terrain.terrainData.alphamapTextures;
        if (alphamapTextures == null || alphamapTextures.Length == 0)
        {
            MTLog.LogError("Terrain has no splatmaps");
            return;
        }
        Texture2D texDiffuse = null;
        Texture2D texNormal = null;
        baseMapWidth = Mathf.Max(4, baseMapWidth);
        baseMapHeight = Mathf.Max(4, baseMapHeight);
        Shader convertShader = Shader.Find("MT/BasemapConvert");
        if (convertShader == null)
        {
            MTLog.LogError("'MT/BasemapConvert' shader not found");
            return;
        }
        bool sRGB = PlayerSettings.colorSpace == ColorSpace.Linear;
        Vector4 basemapSplitOffsetScale = new Vector4(1f, 1f, 0f, 0f);
        Material mat = new Material(convertShader);

        #region GetTerrainData
        int splatCount = terrain.terrainData.terrainLayers.Length;
        if (splatCount == 0)
        {
            MTLog.LogError("Terrain has no enough data for Basemap generating");
            return;
        }
        Texture2D[] diffuseTexs = new Texture2D[splatCount];
        Texture2D[] normalTexs = new Texture2D[splatCount];
        Vector2[] uvScales = new Vector2[splatCount];
        Vector2[] uvOffsets = new Vector2[splatCount];
        float[] metalics = new float[splatCount];
        float[] smoothnesses = new float[splatCount];
        bool hasDiffuse = false;
        bool hasNormal = false;
        for (int i = 0; i < splatCount; i++)
        {
            TerrainLayer terrainLayer = terrain.terrainData.terrainLayers[i];
            if (terrainLayer != null)
            {
                diffuseTexs[i] = terrainLayer.diffuseTexture;
                hasDiffuse = !hasNormal && diffuseTexs[i] != null ? true : hasDiffuse;
                normalTexs[i] = terrainLayer.normalMapTexture;
                hasNormal = !hasNormal && normalTexs[i] != null ? true : hasNormal;
                 float uvScalex = (terrainLayer.tileSize.x == 0f) ? 0f : (terrain.terrainData.size.x / terrainLayer.tileSize.x);
                float uvScaley = (terrainLayer.tileSize.y == 0f) ? 0f : (terrain.terrainData.size.z / terrainLayer.tileSize.y);
                float uvOffsetx = (terrainLayer.tileSize.x == 0f) ? 0f : (terrainLayer.tileOffset.x / terrainLayer.tileSize.x);
                float uvOffsety = (terrainLayer.tileSize.y == 0f) ? 0f : (terrainLayer.tileOffset.y / terrainLayer.tileSize.y);
                uvScales[i] = new Vector2(uvScalex, uvScaley);
                uvOffsets[i] = new Vector2(uvOffsetx, uvOffsety);
                metalics[i] = terrainLayer.metallic;
                smoothnesses[i] = terrainLayer.smoothness;
            }
        }
        #endregion

        //BaseMap
        Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        texture2D.SetPixels(new Color[] { Color.clear, Color.clear, Color.clear, Color.clear });
        texture2D.Apply();
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = null;
        RenderTexture rt1 = RenderTexture.GetTemporary(baseMapWidth, baseMapHeight, 24);
        RenderTexture rt2 = RenderTexture.GetTemporary(baseMapWidth, baseMapHeight, 24);
        mat.SetVector("_Splat_uvOffset", basemapSplitOffsetScale);
        if (hasDiffuse)
        {
            rt1.DiscardContents();
            GL.sRGBWrite = false;
            Graphics.Blit(texture2D, rt1);
            for (int i = 0; i < splatCount; i++)
            {
                if (i % 4 == 0)
                {
                    mat.SetTexture("_Control", alphamapTextures[i / 4]);
                }
                mat.SetFloat("_ChannelIndex", 0.5f + (float)(i % 4));
                mat.SetTexture("_Splat_D", diffuseTexs[i]);
                mat.SetVector("_Splat_uvScale", new Vector4(uvScales[i].x, uvScales[i].y, uvOffsets[i].x, uvOffsets[i].y));
                rt2.DiscardContents();
                GL.sRGBWrite = sRGB;
                Graphics.Blit(rt1, rt2, mat, 0);
                rt1.DiscardContents();
                GL.sRGBWrite = sRGB;
                Graphics.Blit(rt2, rt1);
            }
            RenderTexture.active = rt1;
            texDiffuse = new Texture2D(baseMapWidth, baseMapHeight, TextureFormat.ARGB32, true);
            texDiffuse.ReadPixels(new Rect(0f, 0f, (float)baseMapWidth, (float)baseMapHeight), 0, 0);
            texDiffuse.Apply();
            texDiffuse.wrapMode = TextureWrapMode.Clamp;
        }

        //NormalMap
        Texture2D t2d2 = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        t2d2.SetPixels(new Color[] { color, color, color, color });
        t2d2.Apply();
        if (hasNormal)
        {
            rt1.DiscardContents();
            GL.sRGBWrite = false;
            Graphics.Blit(t2d2, rt1);
            for (int i = 0; i < splatCount; i++)
            {
                if (i % 4 == 0)
                {
                    mat.SetTexture("_Control", alphamapTextures[i / 4]);
                }
                mat.SetFloat("_ChannelIndex", 0.5f + (float)(i % 4));
                mat.SetTexture("_Splat_N", normalTexs[i]);
                mat.SetVector("_Splat_uvScale", new Vector4(uvScales[i].x, uvScales[i].y, uvOffsets[i].x, uvOffsets[i].y));
                rt2.DiscardContents();
                GL.sRGBWrite = sRGB;
                Graphics.Blit(rt1, rt2, mat, 1);
                rt1.DiscardContents();
                GL.sRGBWrite = sRGB;
                Graphics.Blit(rt2, rt1);
            }
            RenderTexture.active = rt1;
            texNormal = new Texture2D(baseMapWidth, baseMapHeight, TextureFormat.ARGB32, true);
            texNormal.ReadPixels(new Rect(0f, 0f, (float)baseMapWidth, (float)baseMapHeight), 0, 0);
            texNormal.Apply();
            texNormal.wrapMode = TextureWrapMode.Clamp;
        }

        RenderTexture.active = null;
        if (Application.isPlaying)
        {
            RenderTexture.active = active;
        }
        Object.DestroyImmediate(mat);
        Object.DestroyImmediate(texture2D);
        Object.DestroyImmediate(t2d2);
        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);

        //SaveTexture，保存混合Splat纹理
        if (texDiffuse != null)
        {
            byte[] diffuseArray = null;
            if (format == TextureMapFormat.PNG)
            {
                diffuseArray = texDiffuse.EncodeToPNG();
            }
            else if (format == TextureMapFormat.TGA)
            {
                diffuseArray = texDiffuse.EncodeToTGA();
            }
            else if (format == TextureMapFormat.JPG)
            {
                diffuseArray = texDiffuse.EncodeToJPG();
            }
            string diffPath = MTWorldConfig.GetSplitMixDiffuseTexturePath(tileName, format);
            FileStream stream = File.Open(diffPath, FileMode.Create);
            stream.Write(diffuseArray, 0, diffuseArray.Length);
            stream.Close();
        }

        if(texNormal != null)
        {
            byte[] normalArray = null;
            if (format == TextureMapFormat.PNG)
            {
                normalArray = texNormal.EncodeToPNG();
            }
            else if (format == TextureMapFormat.TGA)
            {
                normalArray = texNormal.EncodeToTGA();
            }
            else if (format == TextureMapFormat.JPG)
            {
                normalArray = texNormal.EncodeToJPG();
            }
            string normalPath = MTWorldConfig.GetSplitMixNormalTexturePath(tileName, format);
            FileStream stream = File.Open(normalPath, FileMode.Create);
            stream.Write(normalArray, 0, normalArray.Length);
            stream.Close();
        }
        AssetDatabase.Refresh();
    }
}
#endif