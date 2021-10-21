using System.IO;
using UnityEngine;
using UnityEditor;

public static class MTMatUtils
{
#if UNITY_EDITOR
    /// <summary>
    /// SplatMix In Shader
    /// </summary>
    /// <param name="tileName"></param>
    /// <param name="t"></param>
    /// <param name="matIdx"></param>
    /// <param name="layerStart"></param>
    /// <param name="shaderName"></param>
    private static void SaveMaterial(string tileName, Terrain t, int matIdx, int layerStart, string shaderName)
    {
        if (matIdx >= t.terrainData.alphamapTextureCount)
            return;
        string mathPath = MTWorldConfig.GetMaterialAssetPath(tileName, matIdx);
        //alpha map
        byte[] alphaMapData = t.terrainData.alphamapTextures[matIdx].EncodeToTGA();
        string alphaMapSavePath = MTWorldConfig.GetTerrainAlphaAssetPath(tileName, matIdx);
        FileStream stream = File.Open(alphaMapSavePath, FileMode.Create);
        stream.Write(alphaMapData, 0, alphaMapData.Length);
        stream.Close();
        AssetDatabase.Refresh();
        //the alpha map texture has to be set to best compression quality, otherwise the black spot may
        //show on the ground
        TextureImporter importer = AssetImporter.GetAtPath(alphaMapSavePath) as TextureImporter;
        if (importer == null)
        {
            MTLog.LogError("export terrain alpha map failed");
            return;
        }
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Texture2D alphaMap = MTEditorResourceLoader.LoadAssetAtPath<Texture2D>(alphaMapSavePath);
        //
        Material tMat = new Material(Shader.Find(shaderName));
        tMat.SetTexture("_Control", alphaMap);
        if (tMat == null)
        {
            MTLog.LogError("export terrain material failed");
            return;
        }
        for (int i = layerStart; i < layerStart + 4 && i < t.terrainData.terrainLayers.Length; ++i)
        {
            int idx = i - layerStart;
            TerrainLayer layer = t.terrainData.terrainLayers[i];
            Vector2 tiling = new Vector2(t.terrainData.size.x / layer.tileSize.x,
                t.terrainData.size.z / layer.tileSize.y);
            tMat.SetTexture(string.Format("_Splat{0}", idx), layer.diffuseTexture);
            tMat.SetTextureOffset(string.Format("_Splat{0}", idx), layer.tileOffset);
            tMat.SetTextureScale(string.Format("_Splat{0}", idx), tiling);
            tMat.SetTexture(string.Format("_Normal{0}", idx), layer.normalMapTexture);
            tMat.SetFloat(string.Format("_NormalScale{0}", idx), layer.normalScale);
            tMat.SetFloat(string.Format("_Metallic{0}", idx), layer.metallic);
            tMat.SetFloat(string.Format("_Smoothness{0}", idx), layer.smoothness);
        }
        AssetDatabase.CreateAsset(tMat, mathPath);
    }

    /// <summary>
    /// SplatMix In Texture
    /// </summary>
    /// <param name="tileName"></param>
    /// <param name="t"></param>
    /// <param name="shaderName"></param>
    static void SaveMaterial(string tileName, Terrain t, string shaderName)
    {
        //Get & Save MixerSplatTex
        MTTexMapUtilities.GenerateTerrainMap(t, tileName, 1024, 1024);
        string diffusePath = MTWorldConfig.GetSplitMixDiffuseTexturePath(tileName);
        Texture2D diffuseTex = MTEditorResourceLoader.LoadAssetAtPath<Texture2D>(diffusePath);
        string normalPath = MTWorldConfig.GetSplitMixNormalTexturePath(tileName);
        Texture2D normalTex = MTEditorResourceLoader.LoadAssetAtPath<Texture2D>(normalPath);

        //SaveMaterial 
        Material tMat = new Material(Shader.Find(shaderName));
        if (diffuseTex)
#if SRP_ON
            tMat.SetTexture("_BaseMap", diffuseTex);
#else
            tMat.SetTexture("_MainTex", diffuseTex);
#endif
        if (normalTex)
            tMat.SetTexture("_BumpMap", normalTex);
        string mathPath = MTWorldConfig.GetMaterialAssetPath(tileName);
        AssetDatabase.CreateAsset(tMat, mathPath);
    }

    public static void SaveMaterials(string dataName, Terrain t)
    {
#if UNITY_EDITOR
        if (t.terrainData == null)
        {
            MTLog.LogError("terrain data doesn't exist");
            return;
        }

        string matFlodPath = MTWorldConfig.GetMaterialFlodPath(dataName);
        if (Directory.Exists(matFlodPath))
            Directory.Delete(matFlodPath, true);
        Directory.CreateDirectory(matFlodPath);

#if SPLATMIX_IN_SHADER
        int matCount = t.terrainData.alphamapTextureCount;
        if (matCount <= 0)
            return;
        string basePassShaderName = "";
        string addPassShaderName = "";
#if SRP_ON
        basePassShaderName = "MT/Baked-BasePass";
        addPassShaderName = "MT/Baked-AddPass";
#else
        basePassShaderName = "MT/Standard-BasePass";
        addPassShaderName = "MT/Standard-AddPass";
#endif
        //base pass
        SaveMaterial(dataName, t, 0, 0, basePassShaderName);
        for (int i = 1; i < matCount; ++i)
        {
            SaveMaterial(dataName, t, i, i * 4, addPassShaderName);
        }
#elif SPLATMIX_IN_TEXTURE
        string shaderName = "";
#if SRP_ON
        shaderName = "MT/SimpleLit";
#else
        shaderName = "MT/Bumped";
#endif
        SaveMaterial(dataName, t, shaderName);
#endif
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }
    public static Color BakePixel(Terrain t, float u, float v)
    {
        Color c = Color.black;
        if (t.terrainData == null)
        {
            MTLog.LogError("terrain data doesn't exist");
            return c;
        }
        int matCount = t.terrainData.alphamapTextureCount;
        if (matCount <= 0)
            return c;
        //base pass
        for (int i = 0; i < matCount; ++i)
        {
            c += BakeLayerPixel(t, i, i * 4, u, v);
        }
        return c;
    }
    public static Color BakeLayerPixel(Terrain t, int matIdx, int layerStart, float u, float v)
    {
        Color c = Color.black;

        if (matIdx >= t.terrainData.alphamapTextureCount)
            return c;
        float ctrlRes = t.terrainData.alphamapResolution;
        float uvx = u / ctrlRes;
        float uvy = v / ctrlRes;
        Color ctrl = t.terrainData.alphamapTextures[matIdx].GetPixelBilinear(uvx, uvy);
        c += GetLayerDiffusePixel(t.terrainData, layerStart, uvx, uvy, ctrl.r);
        c += GetLayerDiffusePixel(t.terrainData, layerStart + 1, uvx, uvy, ctrl.g);
        c += GetLayerDiffusePixel(t.terrainData, layerStart + 2, uvx, uvy, ctrl.b);
        c += GetLayerDiffusePixel(t.terrainData, layerStart + 3, uvx, uvy, ctrl.a);
        return c;
    }
    private static Color GetLayerDiffusePixel(TerrainData tData, int l, float uvx, float uvy, float weight)
    {
        if (l < tData.terrainLayers.Length)
        {
            TerrainLayer layer = tData.terrainLayers[l];
            Vector2 tiling = new Vector2(tData.size.x / layer.tileSize.x,
               tData.size.z / layer.tileSize.y);
            float u = layer.tileOffset.x + tiling.x * uvx;
            float v = layer.tileOffset.y + tiling.y * uvy;
            return layer.diffuseTexture.GetPixelBilinear(u - Mathf.Floor(u), v - Mathf.Floor(v)) * weight;
        }
        return new Color(0, 0, 0, 0);
    }
    public static Color BakeNormal(Terrain t, float u, float v)
    {
        Color c = new Color(0, 0, 1, 1);
        if (t.terrainData == null)
        {
            MTLog.LogError("terrain data doesn't exist");
            return c;
        }
        int matCount = t.terrainData.alphamapTextureCount;
        if (matCount <= 0)
            return c;
        //base pass
        Vector3 normal = BakeLayerNormal(t, 0, 0, u, v);
        for (int i = 1; i < matCount; ++i)
        {
            normal += BakeLayerNormal(t, i, i * 4, u, v);
        }
        c.r = 0.5f * (normal.x + 1f);
        c.g = 0.5f * (normal.y + 1f);
        c.b = 0.5f * (normal.z + 1f);
        c.a = 1;
        return c;
    }
#endif

    public static Vector3 BakeLayerNormal(Terrain t, int matIdx, int layerStart, float u, float v)
    {
#if UNITY_EDITOR
        if (matIdx >= t.terrainData.alphamapTextureCount)
            return Vector3.zero;
        float ctrlRes = t.terrainData.alphamapResolution;
        float uvx = u / ctrlRes;
        float uvy = v / ctrlRes;
        Color ctrl = t.terrainData.alphamapTextures[matIdx].GetPixelBilinear(uvx, uvy);
        Vector3 normal = GetLayerNormal(t.terrainData, layerStart, uvx, uvy) * ctrl.r;
        normal += GetLayerNormal(t.terrainData, layerStart + 1, uvx, uvy) * ctrl.g;
        normal += GetLayerNormal(t.terrainData, layerStart + 2, uvx, uvy) * ctrl.b;
        normal += GetLayerNormal(t.terrainData, layerStart + 3, uvx, uvy) * ctrl.a;
        normal.z = 0.00001f;
        return normal;
#else
        return Vector3.one;
#endif
    }
    private static Vector3 GetLayerNormal(TerrainData tData, int l, float uvx, float uvy)
    {
#if UNITY_EDITOR
        if (l < tData.terrainLayers.Length)
        {
            TerrainLayer layer = tData.terrainLayers[l];
            if (layer.normalMapTexture == null)
                return Vector3.zero;
            Vector2 tiling = new Vector2(tData.size.x / layer.tileSize.x,
               tData.size.z / layer.tileSize.y);
            float u = layer.tileOffset.x + tiling.x * uvx;
            float v = layer.tileOffset.y + tiling.y * uvy;
            Color norm = layer.normalMapTexture.GetPixelBilinear(u - Mathf.Floor(u), v - Mathf.Floor(v));
            Vector3 normal = Vector3.up;
            //Unity is saving the normal map in the DXT5 file format, the red channel is stored in the alpha channel 
            normal.x = (norm.a * 2 - 1) * layer.normalScale;
            normal.y = (norm.g * 2 - 1) * layer.normalScale;
            normal.z = 0;
            float z = Mathf.Sqrt(1 - Mathf.Clamp01(Vector3.Dot(normal, normal)));
            normal.z = z;
            return normal;
        }
#endif
        return Vector3.zero;
    }
}