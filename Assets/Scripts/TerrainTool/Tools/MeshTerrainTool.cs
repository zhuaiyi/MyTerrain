using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using MightyTerrainMesh;
using System;

[Serializable]
public class MTMeshLODSetting : MeshLODCreate
{
    public bool bEditorUIFoldout = true;
}

#if UNITY_EDITOR
//
public class MeshTerrainTool : MonoBehaviour
{
    public Bounds VolumnBound;
    public int QuadTreeDepth;
    [HideInInspector]
    public MTMeshLODSetting[] LOD = new MTMeshLODSetting[0];
    public bool DrawGizmo = true;
    public string TileName = "";
    [HideInInspector]
    public int LightmapLodLevel;
    //intermediate data
    private CreateDataJob mCreateDataJob;

    [HideInInspector]
    public GameObject[] MTSceneObjectRootList;

    [HideInInspector]
    public MTMapTileDataSet MTMapTileDataSet;

    public float EditorCreateDataProgress
    {
        get
        {
            if (mCreateDataJob != null)
            {
                return mCreateDataJob.progress;
            }
            return 0;
        }
    }
    public bool IsEditorCreateDataDone
    {
        get
        {
            if (mCreateDataJob != null)
            {
                return mCreateDataJob.IsDone;
            }
            return true;
        }
    }
    //
    TessellationJob mTessellationJob;
    public float EditorTessProgress
    {
        get
        {
            if (mTessellationJob != null)
            {
                return mTessellationJob.progress;
            }
            return 0;
        }
    }
    public bool IsEditorTessDone
    {
        get
        {
            if (mTessellationJob != null)
            {
                return mTessellationJob.IsDone;
            }
            return true;
        }
    }
    public void EditorCreateDataBegin()
    {
        if (TileName == "")
        {
            MTLog.LogError("data should have a name");
            return;
        }
        if (LOD == null || LOD.Length == 0)
        {
            MTLog.LogError("no lod setting");
            return;
        }
        if (Terrain.activeTerrain == null)
        {
            MTLog.LogError(TileName + "no active terrain");
            return;
        }
        VolumnBound = Terrain.activeTerrain.terrainData.bounds;
        VolumnBound.center += Terrain.activeTerrain.transform.position;
        MTWorldConfig.CurrentDataName = MTMapTileDataSet.mapDataName;
        int gridMax = 1 << QuadTreeDepth;
        mCreateDataJob = new CreateDataJob(VolumnBound, gridMax, gridMax, LOD, Terrain.activeTerrain);
    }

    public bool EditorCreateDataUpdate()
    {
        if (mCreateDataJob == null)
            return true;
        mCreateDataJob.Update();
        return mCreateDataJob.IsDone;
    }
    public void EditorCreateDataEnd()
    {
        if (mCreateDataJob == null)
            return;
        //finaliz the tree data
        mCreateDataJob.EndProcess();        
    }
    public void EditorTessBegin()
    {
        if (mCreateDataJob == null || mCreateDataJob.LODs == null)
            return;
        mTessellationJob = new TessellationJob(mCreateDataJob.LODs);
    }
    public void EditorTessUpdate()
    {
        if (mTessellationJob == null)
            return;
        mTessellationJob.Update();
    }

    [HideInInspector]
    public MTQuadTreeHeader header;

    public void EditorTessEnd()
    {
        if (mTessellationJob == null || LOD == null)
            return;
        if(header == null)
        {
            MTLog.LogError("QuadTreeHeader is Empty");
            return;
        }     

        string meshFlodPath = MTWorldConfig.GetMeshFlodPath(TileName);
        if (Directory.Exists(meshFlodPath))
            Directory.Delete(meshFlodPath, true);
        Directory.CreateDirectory(meshFlodPath);

        //save data
        header.DataName = TileName;
        header.QuadTreeDepth = QuadTreeDepth;
        header.BoundMin = VolumnBound.min;
        header.BoundMax = VolumnBound.max;
        header.LOD = LOD.Length;
        header.Meshes = new MTMeshHeader[mTessellationJob.mesh.Length];
        int i = 0;
        foreach (var m in mTessellationJob.mesh)
        {
            MTMeshHeader mh = new MTMeshHeader(m.meshId, new Bounds());
            for (int lod = 0; lod < m.lods.Length; lod++)
            {
                string meshAssetPath = MTWorldConfig.GetMeshAssetPath(TileName, m.meshId, lod);
                Mesh mesh = new Mesh();
                mesh.vertices = m.lods[lod].vertices;
                mesh.normals = m.lods[lod].normals;
                mesh.uv = m.lods[lod].uvs;
                mesh.triangles = m.lods[lod].faces;
               
                if (lod == 0)
                {
                    mesh.RecalculateBounds();
                    mh.MeshBound = mesh.bounds;
                }
                // LOD 0当前需要拿来做Collider的Shared Mesh需要Readable
                if (lod != 0)
                    mesh.UploadMeshData(true);
                AssetDatabase.CreateAsset(mesh, meshAssetPath);
            }
            header.Meshes[i] = mh;
            i++;
        }
        MTLog.Log("mesh asset saved!");

        header.RefreshMeshData();
        EditorUtility.SetDirty(header);
        AssetDatabase.SaveAssets();
        MTLog.Log("MTQuadTreeHeader Asset Saved!");

        MTMatUtils.SaveMaterials(TileName, Terrain.activeTerrain);
        MTLog.Log("material saved!");
        Terrain.activeTerrain.gameObject.SetActive(false);
    }

    
    public void RefreshMaterialAssets()
    {
        MTMatUtils.SaveMaterials(TileName, Terrain.activeTerrain);
    }

    const string lodChunkRootName = "lod";
    const string lodChunkMeshName = "meshObj_";

    public void EditorCreateTerrainPreview()
    {
        if (TileName == "")
        {
            MTLog.LogError("data should have a name");
            return;
        }
        GameObject meshRoot = GameObject.Find(MTWorldConfig.MeshTerrainRoot);
        if (meshRoot == null)
            meshRoot = new GameObject(MTWorldConfig.MeshTerrainRoot);

        Transform[] lodParent = new Transform[LOD.Length];
        for (int i = 0; i < LOD.Length; ++i)
        {
            GameObject lodGo = new GameObject(lodChunkRootName + i);
            lodGo.transform.parent = meshRoot.transform;
            lodParent[i] = lodGo.transform;
        }
        if (header == null)
        {
            MTLog.LogError("No QuadTreeHeader, Check");
            return;
        }
        MTWorldConfig.CurrentDataName = MTMapTileDataSet.mapDataName;
        var flags = StaticEditorFlags.ContributeGI;
        var materials = MTEditorResourceLoader.LoadAllAssetsAtPath<Material>(MTWorldConfig.GetMaterialResourcePath(TileName));
        foreach (var m in header.Meshes)
        {
            Mesh[] lods = new Mesh[LOD.Length];
            for (int i = 0; i < LOD.Length; i++)
            {
                string meshAssetPath = MTWorldConfig.GetMeshAssetPath(TileName, m.MeshID, i);
                lods[i] = MTEditorResourceLoader.LoadAssetAtPath<Mesh>(meshAssetPath);
            }
            for (int i = 0; i < LOD.Length; ++i)
            {
                MeshFilter meshF;
                MeshRenderer meshR;
                GameObject meshGo = new GameObject(lodChunkMeshName + m.MeshID);
                meshGo.transform.parent = lodParent[i];
                meshF = meshGo.AddComponent<MeshFilter>();
                meshR = meshGo.AddComponent<MeshRenderer>();
                var collector = meshGo.AddComponent<MTLightmapChunkKeeper>();
                collector.MeshID = m.MeshID;
                meshR.materials = materials;
                meshF.sharedMesh = lods[i];
                GameObjectUtility.SetStaticEditorFlags(meshGo, flags);
            }
        }
        if (Terrain.activeTerrain)
            Terrain.activeTerrain.gameObject.SetActive(false);
        OnMeshLodLevelChange(0);
    }

    const string SceneObjectSubRootName = "MeshID";
    public void EditorCreateSceneObjectPreview()
    {
        CheckSceneObjectRoot();
        if (MTMapTileDataSet == null)
        {
            MTLog.LogError("No MapTileData, Check");
            return;
        }

        if (MTMapTileDataSet.chunkDataSetList == null || MTMapTileDataSet.chunkDataSetList.Count == 0)
        {
            MTLog.LogError("SceneObject didn't divided yet, Check \"Step2 : MTSceneObject Divide\" first");
            return;
        }
        var flags = StaticEditorFlags.ContributeGI;
        GameObject rootGo = GameObject.Find(MTWorldConfig.SceneObjectRootName);
        if (rootGo == null)
            rootGo = new GameObject(MTWorldConfig.SceneObjectRootName);
        foreach (var chunkData in MTMapTileDataSet.chunkDataSetList)
        {
            var subRoot = new GameObject(SceneObjectSubRootName + chunkData.meshID).transform;
            subRoot.SetParent(rootGo.transform);
            foreach (var sceneObject in chunkData.chunkSceneObjectList)
            {
                GameObject go = PrefabUtility.LoadPrefabContents(sceneObject.prefabPath);
                var sceneGo = Instantiate(go, subRoot);
                sceneGo.transform.position = sceneObject.position;
                sceneGo.transform.rotation = sceneObject.rotation;
                sceneGo.transform.localScale = sceneObject.scale;
                var collector = sceneGo.AddComponent<MTLightmapSceneObjectKeeper>();
                collector.IdentifyID = sceneObject.identifyID;
                collector.MeshID = chunkData.meshID;
                var mrArray = sceneGo.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < mrArray.Length; i++)
                {
                    GameObjectUtility.SetStaticEditorFlags(mrArray[i].gameObject, flags);
                }
                PrefabUtility.SaveAsPrefabAsset(go, sceneObject.prefabPath);
                PrefabUtility.UnloadPrefabContents(go);
            }
        }
        for (int i = 0; i < MTSceneObjectRootList.Length; i++)
        {
            MTSceneObjectRootList[i].SetActive(false);
        }
    }

    /// <summary>
    /// Refresh Other Preview Mesh Terrain Render Lightmap Properties & LightmapKeeper (All Preview)
    /// Collect All KeeperData
    /// </summary>
    /// <param name="lod">刷新依赖Lightmap依赖的Mesh的LOD </param>
    public void EditorCRLightmapInfo(int lod)
    {
        Transform targetRoot = transform.Find(lodChunkRootName + lod);
        if (targetRoot == null)
        {
            Debug.LogError(string.Format("Can't Find Lod {0} Preview Terrain, Create Preview First", lod));
            return;
        }
        //Collect Mesh Terrain Lightmap Data
        MTWorldConfig.CurrentDataName = MTMapTileDataSet.mapDataName;
        MTLightmapDataInfo mTLightmapData = new MTLightmapDataInfo();
        mTLightmapData.LightmapDataDic.Clear();
        int dataCount = 0;
        for (int i = 0; i < targetRoot.childCount; i++)
        {
            var childTF = targetRoot.GetChild(i);
            var mr = childTF.GetComponent<MeshRenderer>();
            if (mr == null || mr.lightmapIndex == -1)
                continue;
            int meshID = childTF.GetComponent<MTLightmapChunkKeeper>().MeshID; 
            mTLightmapData.LightmapDataDic.Add(meshID, new MTLightmapData(mr.lightmapIndex, mr.lightmapScaleOffset));
            dataCount++;
        }

        //Refresh OtherLOD
        for (int i = 0; i < transform.childCount; i++)
        {
            targetRoot = transform.GetChild(i);
            if (targetRoot.name.EndsWith(lod.ToString()))
                continue;
            dataCount = 0;
            for (int j = 0; j < targetRoot.childCount; j++)
            {
                var childTF = targetRoot.GetChild(j);
                var mr = childTF.GetComponent<MeshRenderer>();
                if (mr == null)
                    continue;
                int meshID = int.Parse(childTF.name.Split('_')[1]);
                if (mTLightmapData.LightmapDataDic.ContainsKey(meshID))
                {
                    var lmData = mTLightmapData.LightmapDataDic[meshID];
                    mr.lightmapIndex = lmData.lightmapIndex;
                    mr.lightmapScaleOffset = lmData.lightmapScaleOffset;
                    dataCount++;
                }
                else
                {
                    MTLog.LogError(string.Format("MeshID : {0} don't have lightmap data", j));
                }
            }
            MTLog.Log(string.Format("LOD {0} Refresh LightmapData {1}", i, dataCount));
        }
        MTLog.Log("Refresh All Preview Mesh Terrain Lightmap Data");

        if(MTMapTileDataSet == null)
        {
            MTLog.LogError("Set MTMapTileDataSet Fist");
            return;
        }

        var collectors = FindObjectsOfType<MTLightmapKeeper>();
        foreach (var collector in collectors)
        {
            List<MTLightmapData> mTLightmaps = collector.CollectLightmapData();
            if(mTLightmaps == null || mTLightmaps.Count == 0)
            {
                MTLog.LogError(string.Format("Collector With MeshID {0} Can't Collect Lightmapdata, Check Scene GameObject {1}", collector.MeshID, collector.transform.name));
                return;
            }
            var chunkData = MTMapTileDataSet.chunkDataSetList.Find((data) => { return data.meshID == collector.MeshID; });
            if(chunkData == null)
            {
                MTLog.LogError(string.Format("ChunkData With MeshID {0} isn't in MTChunkDataSet", collector.MeshID));
                return;
            }
            //Chunk Lightmap
            if (collector is MTLightmapChunkKeeper)
            {
                var chunkCollector = (MTLightmapChunkKeeper)collector;
                chunkData.lightmapIndex = mTLightmaps[0].lightmapIndex;
                chunkData.lightmapScaleOffset = mTLightmaps[0].lightmapScaleOffset;
            }
            //SceneObject Lightmap
            else
            {
                var sceneObjectCollector = (MTLightmapSceneObjectKeeper)collector;
                var sceneObjectData = chunkData.chunkSceneObjectList.Find((soData) => { return soData.identifyID == sceneObjectCollector.IdentifyID; });
                if(sceneObjectData == null)
                {
                    MTLog.LogError(string.Format("SceneObjectData With IdentifyID {0} isn't in MTChunkDataSet", sceneObjectCollector.IdentifyID));
                    return;
                }
                sceneObjectData.mTLightmapDatas = sceneObjectCollector.CollectLightmapData();
            }
        }
        EditorUtility.SetDirty(MTMapTileDataSet);
        AssetDatabase.SaveAssets();
        MTLog.Log("MTChunkDataSet Lightmap Data Saved!");

        //CoppyLightMap To Resource
        var lightmapFlodPath = MTWorldConfig.GetLightMapFlodPath(TileName);
        if (Directory.Exists(lightmapFlodPath))
            Directory.Delete(lightmapFlodPath, true);
        Directory.CreateDirectory(lightmapFlodPath);

        for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
        {
            var lightmapPath = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapColor);
            var targetLightmapPath = MTWorldConfig.GetLightmapAssetPath(TileName, i);
            AssetDatabase.CopyAsset(lightmapPath, targetLightmapPath);
            LightmapSettings.lightmaps[i].lightmapColor = MTEditorResourceLoader.LoadAssetAtPath<Texture2D>(targetLightmapPath);
        }
        MTLog.Log("Finish Copy Lightmap to Resources " + LightmapSettings.lightmaps.Length);
    }
    

    public void EditorClearPreview()
    {
        while (transform.childCount > 0)
        {
            Transform t = transform.GetChild(0);
            if (t == null || t.gameObject == null)
                break;
            DestroyImmediate(t.gameObject);
        }
        var meshRoot = GameObject.Find(MTWorldConfig.MeshTerrainRoot);
        if (meshRoot)
            DestroyImmediate(meshRoot);
        var SceneObjectRootGo = GameObject.Find(MTWorldConfig.SceneObjectRootName);
        if (SceneObjectRootGo)
            DestroyImmediate(SceneObjectRootGo);
    }

    public void OnMeshLodLevelChange(int lodLevel)
    {
        var MeshRoot = GameObject.Find(MTWorldConfig.MeshTerrainRoot);
        if (MeshRoot == null)
            return;
        for (int i = 0; i < LOD.Length; i++)
        {
            var patchRoot = MeshRoot.transform.Find(string.Format("{0}{1}", lodChunkRootName, i));
            if(patchRoot)
            {
                patchRoot.gameObject.SetActive(i == lodLevel);
            }
        }
    }

    bool containsTerrainTrees = true;

    public void DivideSceneObject()
    {
        if (MTMapTileDataSet == null)
        {
            MTLog.LogError("Set MTMapTileData Fist");
            return;
        }
        MTWorldConfig.CurrentDataName = MTMapTileDataSet.mapDataName;

        CheckSceneObjectRoot();

        MTMapTileDataSet.chunkDataSetList = new List<ChunkData>();

        string prefabRootPath = MTWorldConfig.GetPrefabFlodPath();
        if (!Directory.Exists(prefabRootPath))
            Directory.CreateDirectory(prefabRootPath);
        int gridLen = 1 << QuadTreeDepth;
        float gridSizeX = (float)VolumnBound.extents.x / gridLen;
        float gridSizeZ = (float)VolumnBound.extents.z / gridLen;
        Vector3 extends = new Vector3(gridSizeX, VolumnBound.extents.y, gridSizeZ);
        foreach (var md in header.Meshes)
        {
            MTMapTileDataSet.chunkDataSetList.Add(new ChunkData() { meshID = md.MeshID, center = md.MeshBound.center });
        }

        for (int i = 0; i < MTSceneObjectRootList.Length; i++)
        {
            if (MTSceneObjectRootList[i] != null)
            {
                var rootTF = MTSceneObjectRootList[i].transform;
                for (int j = 0; j < rootTF.childCount; j++)
                {
                    var curScennObject = rootTF.GetChild(j);
                    if (curScennObject == null && curScennObject.gameObject.activeSelf == false)
                        continue;
                    var prefabName = curScennObject.name;
                    prefabName = prefabName.Trim();
                    string prefabPath = MTWorldConfig.GetPrefabAssetPath(prefabName);
                    string resourcePath = MTWorldConfig.GetPrefabResourcePath(prefabName);
                    if (!File.Exists(prefabPath))
                        PrefabUtility.SaveAsPrefabAsset(curScennObject.gameObject, prefabPath);
                    bool isContain = false;
                    Bounds checkBox = new Bounds();
                    checkBox.extents = extends;
                    foreach (var mh in header.Meshes)
                    {
                        var center = mh.MeshBound.center;
                        center.y = curScennObject.position.y;
                        checkBox.center = center;
                        if (checkBox.Contains(curScennObject.position))
                        {
                            var sceneObjectData = new SceneObjectData()
                            {
                                name = prefabName,
                                position = curScennObject.position,
                                rotation = curScennObject.rotation,
                                scale = curScennObject.localScale,
                                resourcePath = resourcePath,
                                prefabPath = prefabPath
                            };
                            MTMapTileDataSet.AddSceneObject(mh.MeshID, sceneObjectData);
                            isContain = true;
                            break;
                        }
                    }
                    if (!isContain)
                    {
                        MTLog.LogError(curScennObject.name + " at " + curScennObject.position + " is not in Bounds " + checkBox.center + ":" + checkBox.extents);
                    }
                }
            }
            else
            {
                MTLog.LogError(string.Format("MTSceneObjectRootList Element {0} is Null", i));
                return;
            }
            MTLog.Log("MTSceneObject Divide Over & Prefabs Save Into " + prefabRootPath);
        }

        if (containsTerrainTrees)
        {
            if (Terrain.activeTerrain == null)
                MTLog.LogError(TileName + "no active terrain");
            else
            {
                var terrainData = Terrain.activeTerrain.terrainData;
                var treeCount = terrainData.treeInstanceCount;
                if (treeCount > 0)
                {
                    var treePrototypes = terrainData.treePrototypes;
                    for (int i = 0; i < treeCount; i++)
                    {
                        var treeInstance = terrainData.GetTreeInstance(i);
                        var prototype = treePrototypes[treeInstance.prototypeIndex];
                        var prefabName = prototype.prefab.name;
                        //临时处理，该Demo下的Prefab临时处理
                        if (prefabName.Contains("Prefab_"))
                            prefabName = prefabName.Remove(0, 7);
                        if (prefabName.Contains("_LOD"))
                            prefabName = prefabName.Substring(0, prefabName.Length - 5);
                        prefabName = prefabName.Trim();
                        string prefabPath = MTWorldConfig.GetPrefabAssetPath(prefabName);
                        string resourcePath = MTWorldConfig.GetPrefabResourcePath(prefabName);
                        if (!File.Exists(prefabPath))
                        {
                            var instanceGo = PrefabUtility.InstantiatePrefab(prototype.prefab) as GameObject;
                            PrefabUtility.UnpackPrefabInstance(instanceGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            instanceGo.name = prefabName;
                            bool saveSucceed = false;
                            PrefabUtility.SaveAsPrefabAsset(instanceGo, prefabPath, out saveSucceed);
                            if (saveSucceed && instanceGo)
                                DestroyImmediate(instanceGo);
                        }
                        Bounds checkBox = new Bounds();
                        checkBox.extents = extends;
                        var treePosRate = treeInstance.position;
                        var treeWorldPos = Terrain.activeTerrain.transform.position + new Vector3(terrainData.size.x * treePosRate.x, terrainData.size.y * treePosRate.y, terrainData.size.z * treePosRate.z);
                        foreach (var mh in header.Meshes)
                        {
                            var center = mh.MeshBound.center;
                            center.y = treeWorldPos.y;
                            checkBox.center = center;
                            if (checkBox.Contains(treeWorldPos))
                            {
                                var sceneObjectData = new SceneObjectData()
                                {
                                    name = prefabName,
                                    position = treeWorldPos,
                                    rotation = Quaternion.Euler(0, treeInstance.rotation, 0),
                                    scale = new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale),
                                    resourcePath = resourcePath,
                                    prefabPath = prefabPath
                                };
                                MTMapTileDataSet.AddSceneObject(mh.MeshID, sceneObjectData);
                                break;
                            }
                        }
                    }
                }
            }
        }

        EditorUtility.SetDirty(MTMapTileDataSet);
        AssetDatabase.SaveAssets();
        MTLog.Log("MTChunkDataSet SceneObject Division Info Saved!");

        for (int i = 0; i < MTSceneObjectRootList.Length; i++)
        {
            MTSceneObjectRootList[i].SetActive(false);
        }
    }

    //临时代码，只针对特定场景，去除特效
    string[] ObjectSceneRootNameList = new string[4] { "Rocks", "Water", "Foliage", "Props"};
    private void CheckSceneObjectRoot()
    {
        MTSceneObjectRootList = new GameObject[ObjectSceneRootNameList.Length];
        for (int i = 0; i < ObjectSceneRootNameList.Length; i++)
        {
            MTSceneObjectRootList[i] = GameObject.Find(ObjectSceneRootNameList[i]);
        }
    }

    public void SetSceneObjectRootStaticFlags(StaticEditorFlags flags)
    {
        CheckSceneObjectRoot();
        for (int i = 0; i < MTSceneObjectRootList.Length; i++)
        {
            if (MTSceneObjectRootList[i] != null && MTSceneObjectRootList[i].activeInHierarchy)
            {
                for (int j = 0; j < MTSceneObjectRootList[i].transform.childCount; j++)
                {
                    GameObjectUtility.SetStaticEditorFlags(MTSceneObjectRootList[i].transform.GetChild(j).gameObject, flags);
                }
            }
        }
    }


    private void OnDrawGizmos()
    {
        if (!DrawGizmo)
            return;
        int gridMax = 1 << QuadTreeDepth;
        Vector2 GridSize = new Vector2(VolumnBound.size.x / gridMax, VolumnBound.size.z / gridMax);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(VolumnBound.center, VolumnBound.size);
        if (GridSize.magnitude > 0)
        {
            int uCount = Mathf.CeilToInt(VolumnBound.size.x / GridSize.x);
            int vCount = Mathf.CeilToInt(VolumnBound.size.z / GridSize.y);
            Vector3 vStart = new Vector3(VolumnBound.center.x - VolumnBound.size.x / 2,
                 VolumnBound.center.y - VolumnBound.size.y / 2,
                VolumnBound.center.z - VolumnBound.size.z / 2);
            for (int u = 1; u < uCount; ++u)
            {
                for (int v = 1; v < vCount; ++v)
                {
                    Gizmos.DrawLine(vStart + v * GridSize.y * Vector3.forward,
                        vStart + v * GridSize.y * Vector3.forward + VolumnBound.size.x * Vector3.right);
                    Gizmos.DrawLine(vStart + u * GridSize.x * Vector3.right,
                        vStart + u * GridSize.x * Vector3.right + VolumnBound.size.x * Vector3.forward);
                }
            }
        }
    }
}
#endif