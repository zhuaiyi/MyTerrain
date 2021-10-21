using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Bundle加载管理器
/// </summary>
public class MTAssetBundleManager
{
    private static MTAssetBundleManager instance;
    private static readonly object locker = new object();
    private MTAssetBundleManager()
    {
    }

    public static MTAssetBundleManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new MTAssetBundleManager();
                    }
                }
            }
            return instance;
        }
    }

    public void LoadPersistentAsset(ref MTMapTileHeader mapTileHeader, ref Dictionary<int2, MTRuntimeTileAssetSet> tileAssetSetMap, int lodCount)
    {
        //Load Shader
        var shaderAB = MTWorldConfig.GetShaderAssetBundlePath();
        shaderAssetBundle = AssetBundle.LoadFromFile(shaderAB);
        var shaderAssets = shaderAssetBundle.LoadAllAssets<Shader>();
        var svc = shaderAssetBundle.LoadAsset<ShaderVariantCollection>("SVC");
        if (svc)
            svc.WarmUp();
        else
            MTLog.LogError("No SVC Load");

        //Load Shared Assets
        var sharedABPath = MTWorldConfig.GetSharedAssetBundlePath();
        sharedAssetsAssetBundle = AssetBundle.LoadFromFile(sharedABPath);
        sharedAssetsAssetBundle.LoadAllAssets();

        materialAssetBundleMap = new Dictionary<string, AssetBundle>();
        meshAssetBundleMap = new Dictionary<string, AssetBundle>();
        sceneObjectAssetBudleMap = new Dictionary<string, AssetBundle>();

        //Load MapTileHeader
        var mapTileHeaderABPath = MTWorldConfig.GetMapTileHeaderAssetBundlePath();
        var mapTileHeaderAB = AssetBundle.LoadFromFile(mapTileHeaderABPath);
        if (mapTileHeaderAB)
        {
            mapTileHeader = mapTileHeaderAB.LoadAsset<MTMapTileHeader>(MTWorldConfig.MapTileHeaderAssetName);
            mapTileHeaderAB.Unload(false);
        }

        //Load TileDataSet & TileQuadTreeHeader
        if (mapTileHeader)
        {
            var tileDataSetABPath = MTWorldConfig.GetMapTileDataSetAssetBundlePath();
            var tileDataSetAB = AssetBundle.LoadFromFile(tileDataSetABPath);

            var tileQTTreeHeaderABPath = MTWorldConfig.GetQuadTreeHeaderAssetBundlePath();
            var tileQTTreeHeaderAB = AssetBundle.LoadFromFile(tileQTTreeHeaderABPath);
            if (tileDataSetAB == null || tileQTTreeHeaderAB == null)
            {
                Debug.LogError("Tile Data Load Faild");
                return;
            }
            else
            {
                foreach (var tileData in mapTileHeader.MapTileDatas)
                {
                    var tileAssetSet = new MTRuntimeTileAssetSet(tileData.tileDataName, lodCount, 1 << mapTileHeader.MapTileQTDepth);
                    var tileDataSetAssetName = MTWorldConfig.GetMapTileSetAssetName(tileData.tileIdStr);
                    var tileDataSet = tileDataSetAB.LoadAsset<MTMapTileDataSet>(tileDataSetAssetName);
                    if (tileDataSet)
                        tileAssetSet.MapTileDataSet = tileDataSet;
                    var tileQTHeaderAssetName = MTWorldConfig.GetTileQuadTreeHeaderAssetName(tileData.tileIdStr);
                    tileAssetSet.QuadTreeHeader = tileQTTreeHeaderAB.LoadAsset<MTQuadTreeHeader>(tileQTHeaderAssetName);
                    tileAssetSetMap.Add(tileData.tileID, tileAssetSet);
                }
                tileDataSetAB.Unload(false);
                tileQTTreeHeaderAB.Unload(false);
            }
        }
    }


    AssetBundle sharedAssetsAssetBundle;
    AssetBundle shaderAssetBundle;

    Dictionary<string, AssetBundle> meshAssetBundleMap;
    Dictionary<string, AssetBundle> materialAssetBundleMap; 

    public Mesh LoadMeshAsset(string tileName, int meshID, int lod)
    {
        var meshAB = LoadMeshAssetBundle(tileName);
        var meshAssetName = MTWorldConfig.GetMeshAssetName(meshID, lod);
        return meshAB.LoadAsset<Mesh>(meshAssetName);
    }

    private AssetBundle LoadMeshAssetBundle(string tileName)
    {
        if (!meshAssetBundleMap.ContainsKey(tileName))
        {
            var tileMeshABPath = MTWorldConfig.GetMeshAssetBundlePath(tileName);
            var tileMeshAB = AssetBundle.LoadFromFile(tileMeshABPath);
            if (tileMeshAB == null)
            {
                Debug.LogError("Load Mesh AssetBundle Faild " + tileName);
                return null;
            }
            meshAssetBundleMap.Add(tileName, tileMeshAB);
        }
        return meshAssetBundleMap[tileName];
    }

    public IEnumerator LoadMeshedAsync(string tileName, Action<Mesh> onMeshLoaded)
    {
        if (!meshAssetBundleMap.ContainsKey(tileName))
        {
            var tileMeshABPath = MTWorldConfig.GetMeshAssetBundlePath(tileName);
            var loadMeshABRequst = AssetBundle.LoadFromFileAsync(tileMeshABPath);
            yield return loadMeshABRequst;

            meshAssetBundleMap.Add(tileName, loadMeshABRequst.assetBundle);
        }
        var meshAB = meshAssetBundleMap[tileName];
        var meshAssetRequest = meshAB.LoadAllAssetsAsync<Mesh>();
        yield return meshAssetRequest;
        var meshAssets = meshAssetRequest.allAssets;
        if(onMeshLoaded != null)
        {
            for (int i = 0; i < meshAssets.Length; i++)
            {
                onMeshLoaded((Mesh)meshAssets[i]);
            }
        }
    }

    public Material[] LoadMaterials(string tileName)
    {
        if (!materialAssetBundleMap.ContainsKey(tileName))
        {
            var tileMatABPath = MTWorldConfig.GetMaterialAssetBundlePath(tileName);
            var tileMatAB = AssetBundle.LoadFromFile(tileMatABPath);
            if(tileMatAB == null)
            {
                Debug.LogError("Load Material AssetBundle Faild " + tileName);
                return null;
            }
            materialAssetBundleMap.Add(tileName, tileMatAB);
        }
        var matAB = materialAssetBundleMap[tileName];
        return matAB.LoadAllAssets<Material>();
    }

    public IEnumerator LoadMaterialsAsync(string tileName, Action<Object[]> onMaterialLoaded)
    {
        if (!materialAssetBundleMap.ContainsKey(tileName))
        {
            var tileMatABPath = MTWorldConfig.GetMaterialAssetBundlePath(tileName);
            var loadMatABRequest = AssetBundle.LoadFromFileAsync(tileMatABPath);
            yield return loadMatABRequest;
            materialAssetBundleMap.Add(tileName, loadMatABRequest.assetBundle);
        }
        var matAB = materialAssetBundleMap[tileName];
        var loadMatAssetRequest = matAB.LoadAllAssetsAsync<Material>();
        yield return loadMatAssetRequest;
        if (onMaterialLoaded != null)
            onMaterialLoaded(loadMatAssetRequest.allAssets);
    }

    private Dictionary<string, AssetBundle> sceneObjectAssetBudleMap;

    public GameObject LoadSceneObject(string prefabName)
    {
        GameObject sceneObjectAsset = null;
        AssetBundle bundle = sceneObjectAssetBudleMap.ContainsKey(prefabName) ? sceneObjectAssetBudleMap[prefabName] : AssetBundle.LoadFromFile(MTWorldConfig.GetPrefabAssetBundlePath(prefabName));
        if (bundle)
        {
            sceneObjectAssetBudleMap.Add(prefabName, bundle);
            sceneObjectAsset = bundle.LoadAsset<GameObject>(prefabName);
        }
        return sceneObjectAsset;
    }

    public IEnumerator LoadSceneObjectAsync(string prefabName, Action<GameObject> onPrefabLoaded)
    {
        AssetBundle prefabAB = null;
        if (sceneObjectAssetBudleMap.ContainsKey(prefabName))
            prefabAB = sceneObjectAssetBudleMap[prefabName];
        else
        {
            var loadPrefabAbRequest = AssetBundle.LoadFromFileAsync(MTWorldConfig.GetPrefabAssetBundlePath(prefabName));
            yield return loadPrefabAbRequest;
            prefabAB = loadPrefabAbRequest.assetBundle;
            sceneObjectAssetBudleMap.Add(prefabName, prefabAB);
        }
        var loadPrefabAssetRequest = prefabAB.LoadAssetAsync<GameObject>(prefabName);
        yield return loadPrefabAssetRequest;
        if (onPrefabLoaded != null)
            onPrefabLoaded((GameObject)loadPrefabAssetRequest.asset);
    }
} 