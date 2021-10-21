using Cysharp.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MTWorldConfig
{
    public const string MapTileHeaderResourcePath = "MapTileHeader";

    public const string MapTileName = "MapTile";

    public static string CurrentDataName;

    public static string MeshTerrainRoot = "MeshTerrain";

    public static string SceneObjectRootName = "SceneObjectRoot";

    //public static string GetMapTileHeaderRessourcePath()
    //{
    //    return ZString.Format("{0}/MapTileHeader", CurrentDataName);
    //}

    public static string GetSplitSceneFlodPath()
    {
        return ZString.Format("Assets/MTSplitScene/{0}", CurrentDataName);
    }

    public static string GetCurrentResourceFlodPath()
    {
        return ZString.Format("Assets/ArtResources/{0}", CurrentDataName);
    }

    public static string GetMapTileHeaderAssetPath()
    {
        return ZString.Format("Assets/ArtResources/{0}/MapTileHeader.asset", CurrentDataName);
    }

    public static string GetQuadTreeHeaderPath(string tileId)
    {
        return ZString.Format("Assets/ArtResources/{0}/QuadTreeHeader/QTHeader_{1}.asset", CurrentDataName, tileId);
    }

    public static string GetQuadTreeHeaderFlodPath()
    {
        return ZString.Format("Assets/ArtResources/{0}/QuadTreeHeader", CurrentDataName);
    }

    public static string GetQuadTreeHeaderResourcePath(string dataName = "")
    {
        if (dataName == "")
            return ZString.Format("Assets/ArtResources/{0}/QuadTreeHeader", CurrentDataName);
        else
            return ZString.Format("Assets/ArtResources/{0}/QuadTreeHeader/QTHeader_{1}", CurrentDataName, dataName);
    }

    public static string GetMapTileDataSetAssetPath(string tileName)
    {
        return ZString.Format("Assets/ArtResources/{0}/MapTileDataSet/TileDataSet_{1}.asset", CurrentDataName, tileName);
    }

    public static string GetMapTileDataSetFlodPath()
    {
        return ZString.Format("Assets/ArtResources/{0}/MapTileDataSet", CurrentDataName);
    }

    public static string GetMapTileDataSetResourcePath(string dataName = "")
    {
        if (dataName == "")
            return ZString.Format("Assets/ArtResources/{0}/MapTileDataSet", CurrentDataName);
        else
            return ZString.Format("Assets/ArtResources/{0}/MapTileDataSet/TileDataSet_{1}", CurrentDataName, dataName);
    }

    public static string GetMeshFlodPath(string tileName = "")
    {
        if (tileName == "")
            return ZString.Format("Assets/ArtResources/{0}/Mesh", CurrentDataName);
        else
            return ZString.Format("Assets/ArtResources/{0}/Mesh/{1}", CurrentDataName, tileName);
    }

    public static string GetMeshAssetPath(string tileName, int meshID, int lod)
    {
        return ZString.Format("Assets/ArtResources/{0}/Mesh/{1}/MeshID_{2}_{3}.asset", CurrentDataName, tileName, meshID, lod);
    }

    public static string GetMeshResourcePath(string tileName, int meshID, int lod)
    {
        return ZString.Format("Assets/ArtResources/{0}/Mesh/{1}/MeshID_{2}_{3}", CurrentDataName, tileName, meshID, lod);
    }

    public static string GetMeshAssetName(int meshID, int lod)
    {
        return ZString.Format("MeshID_{0}_{1}", meshID, lod);
    }

    public static string GetPrefabFlodPath()
    {
        return ZString.Format("Assets/ArtResources/{0}/Prefabs", CurrentDataName);
    }

    public static string GetPrefabResourcePath(string prefabName = "")
    {
        if (prefabName == "")
            return ZString.Format("Assets/ArtResources/{0}/Prefabs", CurrentDataName);
        else
            return ZString.Format("Assets/ArtResources/{0}/Prefabs/{1}", CurrentDataName, prefabName);
    }

    public static string GetPrefabAssetPath(string prefabName)
    {
        return ZString.Format("Assets/ArtResources/{0}/Prefabs/{1}.prefab", CurrentDataName, prefabName);
    }

    public static string GetMaterialFlodPath(string tileName = "")
    {
        if (tileName == "")
            return ZString.Format("Assets/ArtResources/{0}/Materials", CurrentDataName);
        else
            return ZString.Format("Assets/ArtResources/{0}/Materials/{1}", CurrentDataName, tileName);
    }

    public static string GetMaterialAssetPath(string tileName, int materialIndex)
    {
        return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Mat_{2}.mat", CurrentDataName, tileName, materialIndex);
    }

    public static string GetMaterialAssetPath(string tileName)
    {
        return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Mat.mat", CurrentDataName, tileName);
    }

    public static string GetMaterialResourcePath(string tileName = "", int materialIndex = 0)
    {
        if (tileName == "")
            return ZString.Format("Assets/ArtResources/{0}/Materials", CurrentDataName);
        else
            return ZString.Format("Assets/ArtResources/{0}/Materials/{1}", CurrentDataName, tileName);
    }

    public static string GetMaterialResourceName(int index)
    {
        return ZString.Format("Mat_{0}", index);
    }

    public static string GetTerrainAlphaAssetPath(string tileName, int idx)
    {
        return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Alpha_{2}.tga", CurrentDataName, tileName, idx);
    }


    public static string GetLightmapAssetPath(string tileName, int index)
    {
        return ZString.Format("Assets/ArtResources/{0}/Lightmaps/{1}/LMColor${2}${3}.exr", CurrentDataName, tileName, tileName, index);
    }

    public static string GetLightMapFlodPath(string tileName)
    {
        return ZString.Format("Assets/ArtResources/{0}/Lightmaps/{1}", CurrentDataName, tileName);
    }

    public static string GetLightMapRootFlod()
    {
        return ZString.Format("Assets/ArtResources/{0}/Lightmaps", CurrentDataName);
    }


    //AssetBundle
    public static string GetAssetBundleRoot()
    {
        return ZString.Format("{0}/{1}/Android", Application.streamingAssetsPath, CurrentDataName);
    }

    public static string MapTileHeaderAssetName = "maptileheader";
    public static string GetMapTileHeaderAssetBundlePath()
    {
        return ZString.Format("{0}/{1}/Android/{2}", Application.streamingAssetsPath, CurrentDataName, MapTileHeaderAssetName);
    }

    public static string MapTileDataSetAssetBundleName = "maptiledatasets";
    public static string GetMapTileDataSetAssetBundlePath()
    {
        return ZString.Format("{0}/{1}/Android/{2}", Application.streamingAssetsPath, CurrentDataName, MapTileDataSetAssetBundleName);
    }

    public static string GetMapTileSetAssetName(string tileStr)
    {
        return ZString.Format("TileDataSet_{0}", tileStr);
    }

    public static string QuadTreeHeaderAssetBundleName = "quadtreeheaders";
    public static string GetQuadTreeHeaderAssetBundlePath()
    {
        return ZString.Format("{0}/{1}/Android/{2}", Application.streamingAssetsPath, CurrentDataName, QuadTreeHeaderAssetBundleName);
    }

    public static string GetTileQuadTreeHeaderAssetName(string tileStr)
    {
        return ZString.Format("QTHeader_{0}", tileStr);
    }

    public static string GetMaterialAssetBundlePath(string tileName)
    {
        return ZString.Format("{0}/{1}/Android/material/{2}", Application.streamingAssetsPath, CurrentDataName, tileName.ToLower());
    }

    public static string GetSharedResAssetBundleName(string bundleName)
    {
        return ZString.Format("prefab/{0}", bundleName);
    }

    public static string SharedAssetHolderBundleName = "sharedAssetsholder";
    public static string GetSharedAssetHolderBundleName()
    {
        return ZString.Format("prefab/{0}", SharedAssetHolderBundleName);
    }

    public static string GetSharedAssetHolderAssetPath()
    {
        return ZString.Format("Assets/ArtResources/{0}/{1}.asset", CurrentDataName, SharedAssetHolderBundleName);
    }

    public static string GetMeshAssetBundleName(string tileName)
    {
        return ZString.Format("mesh/{0}", tileName);
    }

    public static string GetMeshAssetBundlePath(string tileName)
    {
        return ZString.Format("{0}/{1}/Android/mesh/{2}", Application.streamingAssetsPath, CurrentDataName, tileName.ToLower());
    }

    public static string GetMaterialAssetBundleName(string tileName)
    {
        return ZString.Format("material/{0}", tileName);
    }

    public static string GetPrefabAssetBundleName(string prefabName)
    {
        return ZString.Format("prefab/{0}", prefabName);
    }

    public static string GetPrefabAssetBundlePath(string prefabName)
    {
        return ZString.Format("{0}/{1}/Android/prefab/{2}", Application.streamingAssetsPath, CurrentDataName, prefabName.ToLower());
    }

    public static string GetManifestAssetBundlePath()
    {
        return ZString.Format("{0}/{1}/Android/Android", Application.streamingAssetsPath, CurrentDataName);
    }

    public static string GetAssetBundlePath(string assetName)
    {
        return ZString.Format("{0}/{1}/Android/{2}", Application.streamingAssetsPath, CurrentDataName, assetName);
    }

    public static string GetMeshABName(string tileName)
    {
        return ZString.Format("mesh/{0}", tileName);
    }

    public static string GetBaseMapTexturePath(string tileName)
    {
        return ZString.Format("Assets/ArtResources/{0}/Texture/{1}", CurrentDataName, tileName);
    }

#if UNITY_EDITOR
    public static string GetSplitMixDiffuseTexturePath(string tileName, TextureMapFormat format = TextureMapFormat.JPG)
    {
        switch (format)
        {
            case TextureMapFormat.JPG:
                return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Diffuse.jpg", CurrentDataName, tileName);
            case TextureMapFormat.PNG:
                return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Diffuse.png", CurrentDataName, tileName);
            case TextureMapFormat.TGA:
                return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Diffuse.tga", CurrentDataName, tileName);
        }
        return string.Empty;
    }

    public static string GetSplitMixNormalTexturePath(string tileName, TextureMapFormat format = TextureMapFormat.JPG)
    {
        switch (format)
        {
            case TextureMapFormat.JPG:
                return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Normal.jpg", CurrentDataName, tileName);
            case TextureMapFormat.PNG:
                return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Normal.png", CurrentDataName, tileName);
            case TextureMapFormat.TGA:
                return ZString.Format("Assets/ArtResources/{0}/Materials/{1}/Normal.tga", CurrentDataName, tileName);
        }
        return string.Empty;
    }
#endif

    public static string SharedMaterialPath = "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Materials";

    public static string SharedTexturePath = "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Textures";

    public static string SharedMeshPath = "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Meshes";


    public static string SharedAssetBundleName = "shared/assets";

    public static string ShaderAssetBundleName = "shader/mtshaders";

    public static string GetSharedAssetBundlePath()
    {
        return ZString.Format("{0}/{1}/Android/shared/assets", Application.streamingAssetsPath, CurrentDataName);
    }

    public static string ShaderVariantCollectionPath = "Assets/ArtResources/Shaders/SVC.shadervariants";

    public static string GetShaderAssetBundlePath()
    {
        return ZString.Format("{0}/{1}/Android/shader/mtshaders", Application.streamingAssetsPath, CurrentDataName);
    }
}
