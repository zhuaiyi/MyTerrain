using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 资源加载管理器
/// </summary>
public class MTMapTileAssetManager : MonoSingleton<MTMapTileAssetManager>
{
    enum MapTileAssetStatus
    {
        //已经实例化的
        Instantiated,
        //资源已经加载的，PreLoad
        Loaded,
        //正在PreLoad中的
        Loading,
        //未加载的
        UnLoad
    }

    public delegate void MeshAssetsLoadCallback(string name, Object[] tileMeshes);

    class MeshAssetObject
    {
        public string tileName;
        public MeshAssetsLoadCallback callback;
        public AssetBundleRequest request;
        //Asset资源
        public Object[] meshes;
        public int2 tileID;
    }

    MTMapTileHeader mapTileHeader;
    public MTMapTileHeader MapTileHeader
    {
        get { return mapTileHeader; }
    }
    /// <summary>
    /// 所有Tile的资源集合
    /// </summary>
    Dictionary<int2, MTRuntimeTileAssetSet> tileAssetSetMap;

    float[] lodLevels;
    //不是正方形的xy方向扩展的格子数不同
    int2[] assetLodExtences;

    private Dictionary<int2, MapTileData> mapTileDic;
    public Dictionary<int2, MapTileData> MapTileDic
    {
        get { return mapTileDic; }
    }
    private Dictionary<int2, MapTileAssetStatus> tileAssetStatusDic;


    private Dictionary<string, GameObject> sceneObjectAssetMap;

    public void Init(float[] assetLodLevels)
    {
        lodLevels = assetLodLevels;
        tileAssetSetMap = new Dictionary<int2, MTRuntimeTileAssetSet>();
        int lodCount = lodLevels.Length;
#if UNITY_EDITOR
        string tileHeaderPath = MTWorldConfig.GetMapTileHeaderAssetPath(); ;
        mapTileHeader = MTEditorResourceLoader.LoadAssetAtPath<MTMapTileHeader>(tileHeaderPath);
        if(mapTileHeader)
        {
            foreach (var tileData in mapTileHeader.MapTileDatas)
            {
                var tileAssetSet = new MTRuntimeTileAssetSet(tileData.tileDataName, lodCount, 1 << mapTileHeader.MapTileQTDepth);
                string mapTileDataSetPath = MTWorldConfig.GetMapTileDataSetAssetPath(tileData.tileIdStr);
                tileAssetSet.MapTileDataSet = MTEditorResourceLoader.LoadAssetAtPath<MTMapTileDataSet>(mapTileDataSetPath);
                string quadTreeHeaderPath = MTWorldConfig.GetQuadTreeHeaderPath(tileData.tileIdStr);
                var tileQTHeader = MTEditorResourceLoader.LoadAssetAtPath<MTQuadTreeHeader>(quadTreeHeaderPath);
                tileAssetSet.QuadTreeHeader = tileQTHeader;
                tileAssetSetMap.Add(tileData.tileID, tileAssetSet);
            }
        }
#else
        //初始化资源集合 并 加载常驻资源
        //MTAssetBundleManager.Instance.Init();
        MTAssetBundleManager.Instance.LoadPersistentAsset(ref mapTileHeader, ref tileAssetSetMap, lodCount);
#endif

        InitAssetLodExtence(lodLevels);
        mapTileDic = new Dictionary<int2, MapTileData>();
        tileAssetStatusDic = new Dictionary<int2, MapTileAssetStatus>();
        foreach (var tileData in mapTileHeader.MapTileDatas)
        {
            mapTileDic.Add(tileData.tileID, tileData);
            tileAssetStatusDic.Add(tileData.tileID, MapTileAssetStatus.UnLoad);
        }
        sceneObjectAssetMap = new Dictionary<string, GameObject>();

        lastPreLoadList = new List<int2>();

        //加载LightMap并初始化LightmapSetting

    }

    private int chunkLenInTile;

    void InitAssetLodExtence(float[] lods)
    {
        var mapTileSize = new float2(mapTileHeader.MapTileSize.x, mapTileHeader.MapTileSize.z);
        chunkLenInTile = 1 << mapTileHeader.MapTileQTDepth;
        var chunkSize = new float2(mapTileSize.x / chunkLenInTile, mapTileSize.y / chunkLenInTile);
        assetLodExtences = new int2[lods.Length];
        for (int i = 0; i < lods.Length; i++)
        {
            assetLodExtences[i] = new int2((int)(lods[i] / chunkSize.x) + 2, (int)(lods[i] / chunkSize.y) + 2);
        }
        mVisiblePatches = new MTArray<uint>(chunkLenInTile * chunkLenInTile);
    }

    /// <summary>
    /// 同步更新MapTile
    /// </summary>
    /// <param name="loadList"></param>
    /// <param name="preLoadList"></param>
    /// <param name="playerPos"></param>
    public void UpdateMapTile(List<int2> loadList, List<int2> preLoadList, Vector3 playerPos)
    {
        lastPreLoadList.RemoveAll((item) => { return loadList.Contains(item) || preLoadList.Contains(item); });
        //加载3X3
        LoadMapTile(loadList, playerPos);
        //预加载5X5
        PreLoadMapTile(preLoadList, playerPos);

        //UnLoadList
        foreach (var id in lastPreLoadList)
        {
            //卸载资源
            UnLoadMapTile(id);
        }
        lastPreLoadList.Clear();
        preLoadList.ForEach(i => lastPreLoadList.Add(i));
    }

    /// <summary>
    /// 异步更新MapTile
    /// </summary>
    /// <param name="loadList"></param>
    /// <param name="preLoadList"></param>
    /// <param name="playerPos"></param>
    /// <returns></returns>
    public IEnumerator UpdateMapTileAsync(List<int2> loadList, List<int2> preLoadList, Vector3 playerPos)
    {
        lastPreLoadList.RemoveAll((item) => { return loadList.Contains(item) || preLoadList.Contains(item); });
        //加载3X3
        yield return StartCoroutine(LoadMapTileAsync(loadList, playerPos)); ;
        //预加载
        yield return StartCoroutine(PreLoadMapTileAsync(preLoadList, playerPos));

        //UnLoadList
        foreach (var id in lastPreLoadList)
        {
            //卸载资源
            UnLoadMapTile(id);
        }
        lastPreLoadList.Clear();
        preLoadList.ForEach(i => lastPreLoadList.Add(i));
    }

    private void LoadMapTile(List<int2> loadList, Vector3 playerPos)
    {
        SortByPlayerPos(ref loadList, playerPos);
        //string order = "LoadOrder";
        //foreach (var item in loadList)
        //{
        //    order += item.ToString();
        //    order += ":";
        //}
        //Debug.LogWarning(order);
        foreach (var id in loadList)
        {
            if (tileAssetStatusDic.ContainsKey(id))
            {
                //第一次初始化加载才会有（同步加载）
                if (tileAssetStatusDic[id] == MapTileAssetStatus.UnLoad)
                {
                    //加载地形快资源并实例化
                    LoadMapTileAsset(id, playerPos, true);
                    tileAssetStatusDic[id] = MapTileAssetStatus.Instantiated;
                }
                //资源已经加载，实例化资源
                else if (tileAssetStatusDic[id] == MapTileAssetStatus.Loaded)
                {
                    StartCoroutine(InstantiateMapTile(id, playerPos));
                    tileAssetStatusDic[id] = MapTileAssetStatus.Instantiated;
                }
            }
        }
    }

    /// <summary>
    /// 按距离从近到远排序Tile Load顺序
    /// </summary>
    /// <param name="loadList"></param>
    /// <param name="pos"></param>
    void SortByPlayerPos(ref List<int2> loadList, Vector3 pos)
    {
        loadList.Sort((i, j) => {
            if (!mapTileDic.ContainsKey(i) || !mapTileDic.ContainsKey(j))
                return -1;
            return Vector3.SqrMagnitude(mapTileDic[i].tileBound.center - pos).CompareTo(Vector3.SqrMagnitude(mapTileDic[j].tileBound.center - pos)); });
    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <param name="loadList"></param>
    /// <param name="playerPos"></param>
    private IEnumerator LoadMapTileAsync(List<int2> loadList, Vector3 playerPos)
    {
        SortByPlayerPos(ref loadList, playerPos);
        foreach (var id in loadList)
        {
            if (tileAssetStatusDic.ContainsKey(id))
            {
                //第一次初始化加载才会有（同步加载）
                if (tileAssetStatusDic[id] == MapTileAssetStatus.UnLoad)
                {
                    //加载地形快资源并实例化
                    yield return StartCoroutine(LoadMapTileAssetAsync(id, playerPos, true));
                }
                //资源已经加载，实例化资源
                else if (tileAssetStatusDic[id] == MapTileAssetStatus.Loaded)
                {
                    yield return StartCoroutine(InstantiateMapTile(id, playerPos));
                    tileAssetStatusDic[id] = MapTileAssetStatus.Instantiated;
                }
            }
        }
    }

    /// <summary>
    /// 记录上一帧预加载的MapTile
    /// </summary>
    private List<int2> lastPreLoadList;

    private void PreLoadMapTile(List<int2> preLoadList, Vector3 playerPos)
    {
        foreach (var id in preLoadList)
        {
            if (tileAssetStatusDic.ContainsKey(id))
            {
                //实例化的资源进入Preload，Destroy（）
                if (tileAssetStatusDic[id] == MapTileAssetStatus.Instantiated)
                {
                    DestoryMapTile(id);
                }
                //第一次预加载资源
                else if (tileAssetStatusDic[id] == MapTileAssetStatus.UnLoad)
                {
                    LoadMapTileAsset(id, playerPos);
                }
                tileAssetStatusDic[id] = MapTileAssetStatus.Loaded;
            }
        }
    }

    private IEnumerator PreLoadMapTileAsync(List<int2> preLoadList, Vector3 playerPos)
    {
        foreach (var id in preLoadList)
        {
            if (tileAssetStatusDic.ContainsKey(id))
            {
                //实例化的资源进入Preload，Destroy（）
                if (tileAssetStatusDic[id] == MapTileAssetStatus.Instantiated)
                {
                    DestoryMapTile(id);
                    tileAssetStatusDic[id] = MapTileAssetStatus.Loaded;
                }
                //第一次预加载资源
                else if (tileAssetStatusDic[id] == MapTileAssetStatus.UnLoad)
                {
                    yield return StartCoroutine(LoadMapTileAssetAsync(id, playerPos));
                }
            }
        }
    }


    List<string> sceneObjectList;

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <param name="mapTileData"></param>
    public void LoadMapTileAsset(int2 tileID, Vector3 playerPos, bool create = false)
    {
        var tileDataSet = tileAssetSetMap[tileID].MapTileDataSet;
        UWAEngine.PushSample("LoadMaterial");
        //Material
        Material[] materials = null;
#if UNITY_EDITOR
        string materialPath = MTWorldConfig.GetMaterialResourcePath(mapTileDic[tileID].tileDataName);
        materials = MTEditorResourceLoader.LoadAllAssetsAtPath<Material>(materialPath);
#else
        materials = MTAssetBundleManager.Instance.LoadMaterials(mapTileDic[tileID].tileDataName);
#endif
        tileAssetSetMap[tileID].MaterialAssets = materials;
        UWAEngine.PopSample();
        //SceneObject
        if (sceneObjectList == null)
            sceneObjectList = new List<string>();
        UWAEngine.PushSample("LoadSceneObject");
        tileDataSet.GetTileSceneObjectCollection(sceneObjectList);
        foreach (var prefabName in sceneObjectList)
        {
            if (!sceneObjectAssetMap.ContainsKey(prefabName))
            {
                GameObject prefabAsset = null;
#if UNITY_EDITOR
                string objectPath = MTWorldConfig.GetPrefabAssetPath(prefabName);
                prefabAsset = MTEditorResourceLoader.LoadAssetAtPath<GameObject>(objectPath);
#else
                prefabAsset = MTAssetBundleManager.Instance.LoadSceneObject(prefabName);
#endif
                if (prefabAsset != null)
                    sceneObjectAssetMap.Add(prefabName, prefabAsset);
            }
        }
        UWAEngine.PopSample();
        //Mesh
        foreach (var chunk in tileDataSet.chunkDataSetList)
        {
            //int lod = GetLodLevel(chunk, playerPos);
            //Load Mesh
            UWAEngine.PushSample("Load Mesh");
            for (int i = 0; i < lodLevels.Length; i++)
            {
                var tileName = mapTileDic[tileID].tileDataName;
                if (tileAssetSetMap[tileID].GetMeshAsset(chunk.meshID, i) == null)
                {
                    Mesh mesh = null;
#if UNITY_EDITOR
                    string meshAssetPath = MTWorldConfig.GetMeshAssetPath(tileName, chunk.meshID, i);
                    mesh = MTEditorResourceLoader.LoadAssetAtPath<Mesh>(meshAssetPath);
#else
                    mesh = MTAssetBundleManager.Instance.LoadMeshAsset(tileName, chunk.meshID, i);
#endif
                    tileAssetSetMap[tileID].AddMeshAsset(chunk.meshID, mesh, i);
                }
            }
            UWAEngine.PopSample();

            UWAEngine.PushSample("InstantiateMapTile");
            if (create)
                StartCoroutine(tileAssetSetMap[tileID].InstantiateTileChunk(chunk));
            UWAEngine.PopSample();
        }
    }

    public IEnumerator LoadMapTileAssetAsync(int2 tileID, Vector3 playerPos, bool create = false)
    {
        tileAssetStatusDic[tileID] = MapTileAssetStatus.Loading;
        var tileDataSet = tileAssetSetMap[tileID].MapTileDataSet;
        //LoadMaterial
        yield return StartCoroutine(MTAssetBundleManager.Instance.LoadMaterialsAsync(mapTileDic[tileID].tileDataName, (matObjs) =>
        {
            tileAssetSetMap[tileID].MaterialAssets = new Material[matObjs.Length];
            for (int i = 0; i < matObjs.Length; i++)
            {
                tileAssetSetMap[tileID].MaterialAssets[i] = (Material)matObjs[i];
            }
        }));

        //LoadSceneGo
        if (sceneObjectList == null)
            sceneObjectList = new List<string>();
        tileDataSet.GetTileSceneObjectCollection(sceneObjectList);
        foreach (string prefabName in sceneObjectList)
        {
            if (!sceneObjectAssetMap.ContainsKey(prefabName))
            {
                yield return StartCoroutine(MTAssetBundleManager.Instance.LoadSceneObjectAsync(prefabName, (obj) =>
                {
                    if (obj != null)
                        sceneObjectAssetMap.Add(prefabName, obj);
                }));
            }
        }

        //LoadMesh Async
        yield return MTAssetBundleManager.Instance.LoadMeshedAsync(mapTileDic[tileID].tileDataName, (mesh) =>
        {
            var meshStr = mesh.name.Split('_');
            int meshID = int.Parse(meshStr[1]);
            int lod = int.Parse(meshStr[2]);
            if (tileAssetSetMap[tileID].GetMeshAsset(meshID, lod) == null)
            {
                tileAssetSetMap[tileID].AddMeshAsset(meshID, mesh, lod);
            }
        });
        tileAssetStatusDic[tileID] = MapTileAssetStatus.Loaded;

        //Create
        if (create)
        {
            foreach (var chunk in tileDataSet.chunkDataSetList)
                yield return StartCoroutine(tileAssetSetMap[tileID].InstantiateTileChunk(chunk));
            tileAssetStatusDic[tileID] = MapTileAssetStatus.Instantiated;
        }
    }


    private IEnumerator PreloadMapTileAssetAsync(int2 tileID, Vector3 playerPos)
    {
        tileAssetStatusDic[tileID] = MapTileAssetStatus.Loading;
        var tileDataSet = tileAssetSetMap[tileID].MapTileDataSet;
        //Material
        Material[] materials = null;
#if UNITY_EDITOR
        string materialsPath = MTWorldConfig.GetMaterialResourcePath(mapTileDic[tileID].tileDataName);
        materials = MTEditorResourceLoader.LoadAllAssetsAtPath<Material>(materialsPath);
#else
        materials = MTAssetBundleManager.Instance.LoadMaterials(mapTileDic[tileID].tileDataName);
#endif
        tileAssetSetMap[tileID].MaterialAssets = materials;

        if (sceneObjectList == null)
            sceneObjectList = new List<string>();

        //SceneGo
        tileDataSet.GetTileSceneObjectCollection(sceneObjectList);
        foreach (var prefabName in sceneObjectList)
        {
            if (!sceneObjectAssetMap.ContainsKey(prefabName))
            {
                yield return StartCoroutine(MTAssetBundleManager.Instance.LoadSceneObjectAsync(prefabName, (obj) =>
                {
                    if (obj != null)
                        sceneObjectAssetMap.Add(prefabName, obj);
                }));
            }
        }

        //LoadMesh Async
        yield return MTAssetBundleManager.Instance.LoadMeshedAsync(mapTileDic[tileID].tileDataName, (mesh) =>
        {
            var meshStr = mesh.name.Split('_');
            int meshID = int.Parse(meshStr[1]);
            int lod = int.Parse(meshStr[2]);
            if (tileAssetSetMap[tileID].GetMeshAsset(meshID, lod) == null)
            {
                tileAssetSetMap[tileID].AddMeshAsset(meshID, mesh, lod);
            }
        });
    }

    public GameObject GetSceneObjectAsset(string assetName)
    {
        if (sceneObjectAssetMap.ContainsKey(assetName))
        {
            return sceneObjectAssetMap[assetName];
        }
        else
        {
            return null;
        }
    }

    public int GetLodLevel(ChunkData chunkData, Vector3 playerPos)
    {
        var direction = Vector3.ProjectOnPlane(playerPos - chunkData.center, Vector3.up);
        float dis = Vector3.SqrMagnitude(direction);
        int lod = 0;
        while (dis > lodLevels[lod] && lod < lodLevels.Length - 1)
            lod++;
        return lod;
    }

    private void DestoryMapTile(int2 tileID)
    {
        tileAssetSetMap[tileID].DestroyTile();
    }

    private void UnLoadMapTile(int2 tileID)
    {
        if (tileAssetSetMap.ContainsKey(tileID))
        {
            tileAssetSetMap[tileID].UnloadAsset();
            tileAssetStatusDic[tileID] = MapTileAssetStatus.UnLoad;
        }
    }

    private IEnumerator InstantiateMapTile(int2 tileID, Vector3 playerPos)
    {
        var tileAssetSet = tileAssetSetMap[tileID];
        foreach (var chunk in tileAssetSet.MapTileDataSet.chunkDataSetList)
        {
            yield return StartCoroutine(tileAssetSetMap[tileID].InstantiateTileChunk(chunk));
        }
    }

    /// <summary>
    /// 目前地形Mesh内存压力并不大暂时可以不考虑
    /// </summary>
    /// <param name="centerTileID"></param>
    /// <param name="centerMeshID"></param>
    public void UpdateMeshAsset(int2 centerTileID, int centerMeshID)
    {
        foreach (var tileID in tileAssetSetMap.Keys)
        {
            if (tileAssetStatusDic[tileID] != MapTileAssetStatus.Loaded)
                continue;
            foreach (var chunk in tileAssetSetMap[tileID].MapTileDataSet.chunkDataSetList)
            {
                int lod = GetChunkLOD(centerTileID, centerMeshID, tileID, chunk.meshID);
                tileAssetSetMap[tileID].UpdateTileMeshAsset(chunk.meshID, lod);
            }
        }
    }

    //todo
    private int GetChunkLOD(int2 fromTileID, int fromMeshID, int2 toTileID, int toMeshID)
    {
        int2 fromChunkOffset = new int2(fromMeshID / chunkLenInTile, fromMeshID % chunkLenInTile);
        int2 toChunkOffset = new int2(toMeshID / chunkLenInTile, toMeshID % chunkLenInTile);
        int2 offset = math.abs((toTileID - fromTileID) * chunkLenInTile + (toChunkOffset - fromChunkOffset));
        int lod = 0;
        for (; lod < assetLodExtences.Length; lod++)
        {
            if (offset.x <= assetLodExtences[lod].x && offset.y <= assetLodExtences[lod].y)
            {
                break;
            }
        }
        return lod;
    }

    MTArray<uint> mVisiblePatches;

    KeyValuePair<int, int> tileVisiableArry;
    /// <summary>
    /// 相机视锥体，顺序依次为：左、右、下、上、近、远
    /// </summary>
    Plane[] camFrustumPlanes;

    public void UpdateTileVisiable(Camera camera, List<int2> loadList, float horizontalExtent = 0f, float verticalExtent = 0f)
    {
        UWAEngine.PushSample("UpdateTileVisiable");
        var camPos = camera.transform.position;
        UWAEngine.PushSample("CalculateFrustumPlanes");
        camFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        camFrustumPlanes[0].Translate(horizontalExtent * camFrustumPlanes[0].normal);
        camFrustumPlanes[1].Translate(horizontalExtent * camFrustumPlanes[0].normal);
        camFrustumPlanes[2].Translate(verticalExtent * camFrustumPlanes[2].normal);
        camFrustumPlanes[3].Translate(verticalExtent * camFrustumPlanes[3].normal);
        UWAEngine.PopSample();
        foreach (var tileID in loadList)
        {
            if (tileAssetSetMap.ContainsKey(tileID) && tileAssetStatusDic[tileID] == MapTileAssetStatus.Instantiated)
            {
                mVisiblePatches.Reset();
                UWAEngine.PushSample("RetrieveVisibleMesh");
                tileAssetSetMap[tileID].TileQuadTreeRoot.RetrieveVisibleMesh(camFrustumPlanes, camPos, lodLevels, mVisiblePatches);
                UWAEngine.PopSample();

                UWAEngine.PushSample("UpdateVisiableChunk");
                tileAssetSetMap[tileID].UpdateVisiableChunk(mVisiblePatches);
                UWAEngine.PopSample();
            }
        }
        UWAEngine.PopSample();
    }

    protected override void Initialize()
    {

    }
}