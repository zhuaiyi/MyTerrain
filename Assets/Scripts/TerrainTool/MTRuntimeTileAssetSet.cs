using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MTRuntimeTileAssetSet
{
    private int lodCount;
    private int chunkCount;
    private string tileName;
    private Dictionary<int, Mesh[]> meshAssets;
    private List<GameObject> sceneObjectList;

    private Material[] materialAssets;
    public Material[] MaterialAssets { get => materialAssets; set => materialAssets = value; }

    private Texture2D[] lightmapAsssets;

    private MTMapTileDataSet mapTileDataSet;
    public MTMapTileDataSet MapTileDataSet { get => mapTileDataSet; set => mapTileDataSet = value; }

    private MTQuadTreeHeader quadTreeHeader;
    public MTQuadTreeHeader QuadTreeHeader 
    { 
        get => quadTreeHeader;
        set
        { 
            quadTreeHeader = value;
            if (quadTreeHeader)
            {
                tileQuadTreeRoot = new MTQuadTreeNode(quadTreeHeader.QuadTreeDepth, quadTreeHeader.BoundMin, quadTreeHeader.BoundMax);
                foreach (var mh in quadTreeHeader.Meshes)
                    tileQuadTreeRoot.AddMesh(mh);
            }
        }
    }

    private MTQuadTreeNode tileQuadTreeRoot;
    public MTQuadTreeNode TileQuadTreeRoot { get => tileQuadTreeRoot; }

    private GameObject tileRootGO;
    public GameObject TileRootGO { get => tileRootGO; set => tileRootGO = value; }

    private Dictionary<int, GameObject> chunkGoMap;
    private Dictionary<int, GameObject> chunkObjectRootMap;
    private Dictionary<int, MeshFilter> chunkMeshFilterMap;
    private Dictionary<int, MeshRenderer> chunkMeshRenderMap;
    private Dictionary<int, List<MeshRenderer>> sceneObjectMeshRenderMap;
    private Dictionary<int, ChunkStateInfo> chunkStateInfoMap;

    struct ChunkStateInfo
    {
        public bool ChunkActive;
        public bool ObjectActive;
        public int Lod;
    }

    private WaitForEndOfFrame instantiateWait;
    private int instantiateCount;

    public MTRuntimeTileAssetSet(string tileName, int lod, int chunkCount)
    {
        this.tileName = tileName;
        lodCount = lod;
        this.chunkCount = chunkCount;
        meshAssets = new Dictionary<int, Mesh[]>();
        chunkGoMap = new Dictionary<int, GameObject>();
        chunkObjectRootMap = new Dictionary<int, GameObject>();
        chunkMeshFilterMap = new Dictionary<int, MeshFilter>();
        chunkMeshRenderMap = new Dictionary<int, MeshRenderer>();
        sceneObjectMeshRenderMap = new Dictionary<int, List<MeshRenderer>>();
        chunkStateInfoMap = new Dictionary<int, ChunkStateInfo>();
        sceneObjectList = new List<GameObject>();
        tileRootGO = new GameObject(tileName);
        instantiateWait = new WaitForEndOfFrame();
        instantiateCount = 0;
    }

    public void AddMeshAsset(int meshID, Mesh mesh, int lod)
    {
        if (!meshAssets.ContainsKey(meshID))
            meshAssets.Add(meshID, new Mesh[lodCount]);
        //释放CPU Mesh内存，放在生成Mesh处理
        //mesh.UploadMeshData(true);
        meshAssets[meshID][lod] = mesh;
    }

    public Mesh GetMeshAsset(int meshID, int lod)
    {
        if (!meshAssets.ContainsKey(meshID))
            return null;
        return meshAssets[meshID][lod];
    }

    public void UnloadAsset()
    {
        for (int i = 0; i < materialAssets.Length; i++)
        {
            Resources.UnloadAsset(materialAssets[i]);
            materialAssets[i] = null;
        }
        foreach (var key in meshAssets.Keys)
        {
            for (int i = 0; i < meshAssets[key].Length; i++)
            {
                Resources.UnloadAsset(meshAssets[key][i]);
                meshAssets[key][i] = null;
            }
        }
    }

    public void DestroyTile()
    {
        foreach (var key in chunkGoMap.Keys)
        {
            GameObject.Destroy(chunkGoMap[key]);
        }
        for (int i = sceneObjectList.Count - 1; i >= 0; i--)
        {
            GameObject.Destroy(sceneObjectList[i]);
            sceneObjectList.RemoveAt(i);
        }
        sceneObjectList.Clear();
        chunkGoMap.Clear();
        chunkObjectRootMap.Clear();
        chunkMeshFilterMap.Clear();
        chunkMeshRenderMap.Clear();
        chunkStateInfoMap.Clear();
        foreach (var sbmrList in sceneObjectMeshRenderMap.Values)
        {
            sbmrList.Clear();
        }
        sceneObjectMeshRenderMap.Clear();
        instantiateCount = 0;
    }

    public IEnumerator InstantiateTileChunk(ChunkData chunk, int instantiateStep = 35, int lod = -1)
    {
        instantiateCount++;
        int meshID = chunk.meshID;
        if (lod == -1)
            lod = lodCount - 1;
        GameObject chunkGo;
        GameObject objectRoot;
        if (chunkGoMap.ContainsKey(meshID))
        {
            chunkGo = chunkGoMap[meshID];
            objectRoot = chunkObjectRootMap[meshID];
        }
        else
        {
            chunkGo = new GameObject(meshID.ToString());
            var mr = chunkGo.AddComponent<MeshRenderer>();
            var mf = chunkGo.AddComponent<MeshFilter>();
            var col = chunkGo.AddComponent<MeshCollider>();
            mr.materials = MaterialAssets;
            mr.receiveShadows = false;
            mf.mesh = GetMeshAsset(meshID, lod);
            
            col.sharedMesh = GetMeshAsset(meshID, 0);
            chunkGo.transform.SetParent(tileRootGO.transform);
            chunkGoMap.Add(meshID, chunkGo);
            chunkMeshFilterMap.Add(meshID, mf);
            chunkMeshRenderMap.Add(meshID, mr);

            objectRoot = new GameObject("ChunkObjectRoot");
            objectRoot.transform.parent = chunkGo.transform;
            objectRoot.transform.localPosition = Vector3.zero;
            chunkObjectRootMap.Add(meshID, objectRoot);
        }

        if (!sceneObjectMeshRenderMap.ContainsKey(chunk.meshID))
            sceneObjectMeshRenderMap.Add(chunk.meshID, new List<MeshRenderer>());
        int curIndex = 0;
        int totalNum = chunk.chunkSceneObjectList.Count;
        while (curIndex < totalNum)
        {
            int targetIndex = curIndex + instantiateStep > totalNum ? totalNum : curIndex + instantiateStep;
            for (int i = curIndex; i < targetIndex; i++, curIndex++)
            {
                var sbData = chunk.chunkSceneObjectList[i];
                var asset = MTMapTileAssetManager.Instance.GetSceneObjectAsset(sbData.name);
                if (asset != null)
                {
                    var sbGO = GameObject.Instantiate(asset, objectRoot.transform);
                    sbGO.transform.position = sbData.position;
                    sbGO.transform.rotation = sbData.rotation;
                    sbGO.transform.localScale = sbData.scale;
                    sceneObjectList.Add(sbGO);
                    sceneObjectMeshRenderMap[chunk.meshID].AddRange(sbGO.GetComponentsInChildren<MeshRenderer>());
                }
                else
                {
                    MTLog.LogError(sbData.name + " Asset is null");
                }
            }
            yield return instantiateWait;
        }
        
        objectRoot.SetActive(false);
        chunkStateInfoMap.Add(meshID, new ChunkStateInfo() { Lod = lod, ChunkActive = true, ObjectActive = false });
        instantiateCount--;
  
    }

    public bool IsInstantiateFinished()
    {
        return chunkGoMap.Count == chunkCount;
    }


    public void UpdateTileMeshAsset(int meshID, int lod)
    {
        for (int i = 0; i < meshAssets[meshID].Length; i++)
        {
            //需要加载
            if (i >= lod && meshAssets[meshID][i] == null)
            {
#if UNITY_EDITOR
                string meshAssetPath = MTWorldConfig.GetMeshResourcePath(tileName, meshID, lod);
                meshAssets[meshID][i] = MTEditorResourceLoader.LoadAssetAtPath<Mesh>(meshAssetPath);
#else
                meshAssets[meshID][i] = MTAssetBundleManager.Instance.LoadMeshAsset(tileName, meshID, lod);
#endif
            }
            else
            {
                if (meshAssets[meshID][i] != null)
                {
                    Resources.UnloadAsset(meshAssets[meshID][i]);
                    meshAssets[meshID][i] = null;
                }
            }
        }
    }

    public void UpdateVisiableChunk(MTArray<uint> visiblePatches)
    {
        //异步实例化，如果存在未实例化完的情况先不更新Visiable和LOD
        if (instantiateCount > 0)
            return;

        for (int j = 0; j < mapTileDataSet.chunkDataSetList.Count; j++)
        {
            int meshID = j;
            uint patchId = 0;
            ChunkStateInfo chunkStateInfo = chunkStateInfoMap[meshID];
            //todo：visiblePatches如果以MeshID为索引的话可以优化一下，每个Chunk会少很多遍历，需要改MTArray数据结构
            UWAEngine.PushSample("GetPatchID");
            for (int i = 0; i < visiblePatches.Data.Length; i++)
            {
                if ((visiblePatches.Data[i] >> 2) - 1 == meshID)
                {
                    patchId = visiblePatches.Data[i];
                    break;
                }
            }
            UWAEngine.PopSample();

            if (patchId > 0)
            {
                int lod = (int)(patchId & 0x00000003);
                int lodFixed = 0;
                bool showSceneObject = true;
                if (lod == lodCount)//默认LOD Setting外的需要显示MeshTerrain，但SceneObject可以不显示
                {
                    lodFixed = -1;
                    showSceneObject = false;
                }

                if (chunkStateInfo.Lod != lod + lodFixed)
                {
                    UWAEngine.PushSample("SetMeshLOD1");
                    chunkMeshFilterMap[meshID].mesh = GetMeshAsset(meshID, lod + lodFixed);
                    UWAEngine.PopSample();
                }
                if (!chunkStateInfo.ChunkActive)
                {
                    chunkGoMap[meshID].SetActive(true);
                    chunkStateInfo.ChunkActive = true;
                }
                    
                if (chunkStateInfo.ObjectActive != showSceneObject)
                {
                    chunkObjectRootMap[meshID].SetActive(showSceneObject);
                    chunkStateInfo.ObjectActive = showSceneObject;
                } 
            }
            else
            {
                if (chunkStateInfo.Lod != lodCount - 1)
                {
                    UWAEngine.PushSample("SetMeshLOD2");
                    chunkMeshFilterMap[meshID].mesh = GetMeshAsset(meshID, lodCount - 1);
                    UWAEngine.PopSample();
                }
                if (chunkStateInfo.ChunkActive)
                {
                    chunkGoMap[meshID].SetActive(false);
                    chunkStateInfo.ChunkActive = false;
                }
                if (chunkStateInfo.ObjectActive)
                {
                    chunkObjectRootMap[meshID].SetActive(false);
                    chunkStateInfo.ObjectActive = false;
                }  
            }
            chunkStateInfoMap[meshID] = chunkStateInfo;
        }
    }
}