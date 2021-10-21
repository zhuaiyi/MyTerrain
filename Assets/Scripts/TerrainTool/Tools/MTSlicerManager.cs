#if UNITY_EDITOR

using Cysharp.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System;

public class MTSlicerManager : MonoBehaviour
{
    public int QuadTreeDepth;
    public string dataName;

    [HideInInspector]
    public MTSceneSlicer[] MapTiles;

    [HideInInspector]
    public Vector2 tileGridSize;
    [HideInInspector]
    public int tileSizeX;
    [HideInInspector]
    public int tileSizeY;

    private Vector3 startPos;

    [HideInInspector]
    public string splitScenePath = "Assets/SplitScene";
    [HideInInspector]
    public GameObject[] SharedGameObjects;
    [HideInInspector]
    public MTMapTileHeader MTMapTileHeader;
    [HideInInspector]
    public MTMeshLODSetting[] LOD = new MTMeshLODSetting[0];

    public void SetConfigDataName()
    {
        MTWorldConfig.CurrentDataName = dataName;
        Debug.Log("SetConfigDataName :" + dataName);
    }

    public void MapTileSplit()
    {
        MTWorldConfig.CurrentDataName = dataName;

        if (LOD == null || LOD.Length == 0)
        {
            MTLog.LogError("no lod setting");
            return;
        }

        if (MTMapTileHeader == null)
        {
            MTMapTileHeader = ScriptableObject.CreateInstance<MTMapTileHeader>();
            if (!Directory.Exists(MTWorldConfig.GetCurrentResourceFlodPath()))
                Directory.CreateDirectory(MTWorldConfig.GetCurrentResourceFlodPath());
            AssetDatabase.CreateAsset(MTMapTileHeader, MTWorldConfig.GetMapTileHeaderAssetPath());
        }
        else
        {
            MTMapTileHeader = MTEditorResourceLoader.LoadAssetAtPath<MTMapTileHeader>(MTWorldConfig.GetMapTileHeaderAssetPath());
        }

        MTMapTileHeader.WorldMapDataName = dataName;
        MTMapTileHeader.MapTileSizeX = tileSizeX;
        MTMapTileHeader.MapTileSzieY = tileSizeY;
        MTMapTileHeader.MapTileDatas = new List<MapTileData>();
        var tileSize = new Vector3(tileGridSize.x, 0, tileGridSize.y);
        startPos = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);

        if (Directory.Exists(splitScenePath))
            Directory.Delete(splitScenePath, true);
        Directory.CreateDirectory(splitScenePath);
        for (int i = 0; i < MapTiles.Length; i++)
        {
            int tileIDX = i % tileSizeX;
            int tileIDY = i / tileSizeX;
 
            string mapTileIDStr = ZString.Format("MapTile_{0}_{1}", tileIDX, tileIDY);

            if (MapTiles[i] == null || MapTiles[i].TileTerrain == null)
            {
                MTLog.LogError(ZString.Format("{0} is null，Check & Set Terrain Map", mapTileIDStr));
                continue;
            }
            else
            {
                var center = MapTiles[i].TileTerrain;
                Terrain left = tileIDX == 0 ? null : MapTiles[i - 1].TileTerrain;
                Terrain right = tileIDX == tileSizeX - 1 ? null : MapTiles[i + 1].TileTerrain;
                Terrain top = tileIDY == tileSizeY - 1 ? null : MapTiles[i + tileSizeX].TileTerrain;
                Terrain bottom = tileIDY == 0 ? null : MapTiles[i - tileSizeX].TileTerrain;
                center.SetNeighbors(left, top, right, bottom);
                center.Flush();
                
                center.gameObject.layer = LayerMask.NameToLayer("Terrain");
                var tpos = center.transform.position;
                if (tpos.x + tpos.z < startPos.x + startPos.z)
                    startPos = tpos;

                Bounds bounds = center.terrainData.bounds;
                bounds.center += center.transform.position;
                MapTileData tileData = new MapTileData(mapTileIDStr, bounds, tileIDX, tileIDY);
                MTMapTileHeader.AddMapTileData(tileData);

                GenerateMapTileScene(MapTiles[i], tileData);
            }
        }

        MTLog.Log("Scene Slice Over");

        MTMapTileHeader.MapTileSize = tileSize;
        MTMapTileHeader.OriginalPosition = startPos;
        MTMapTileHeader.MapTileQTDepth = QuadTreeDepth;
        EditorUtility.SetDirty(MTMapTileHeader);
        AssetDatabase.SaveAssets();
        MTLog.Log("Savae MapTileHeader Asset!");
    }

    private void GenerateMapTileScene(MTSceneSlicer slicer, MapTileData tileData)
    {
        string scenePath = ZString.Format("{0}/{1}.unity", MTWorldConfig.GetSplitSceneFlodPath(), tileData.tileDataName);
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject terrainCopy = Instantiate(slicer.TileTerrain.gameObject);
        terrainCopy.name = slicer.TileTerrain.gameObject.name;
        terrainCopy.transform.position = slicer.TileTerrain.transform.position;
        terrainCopy.transform.rotation = slicer.TileTerrain.transform.rotation;
        if (terrainCopy.transform.childCount > 0)
        {
            for (int i = terrainCopy.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(terrainCopy.transform.GetChild(i).gameObject);
            }
        }
        EditorSceneManager.MoveGameObjectToScene(terrainCopy, newScene);
        foreach (var rootGo in slicer.SplitSceneObjects)
        {
            GameObject copy = Instantiate(rootGo);
            copy.name = rootGo.name;
            copy.transform.position = rootGo.transform.position;
            copy.transform.rotation = rootGo.transform.rotation;
            EditorSceneManager.MoveGameObjectToScene(copy, newScene);
        }
        foreach (var sharedGo in SharedGameObjects)
        {
            GameObject copy = Instantiate(sharedGo);
            copy.name = sharedGo.name;
            copy.transform.position = sharedGo.transform.position;
            copy.transform.rotation = sharedGo.transform.rotation;
            EditorSceneManager.MoveGameObjectToScene(copy, newScene);
        }
        GameObject meshToolGo = new GameObject("MeshTool");
        var meshTool = meshToolGo.AddComponent<MeshTerrainTool>();
        InitMeshTerrainTool(meshTool, tileData);

        EditorSceneManager.MoveGameObjectToScene(meshToolGo, newScene);
        EditorSceneManager.SaveScene(newScene, scenePath, true);
        EditorSceneManager.CloseScene(newScene, true);
    }

    private void InitMeshTerrainTool(MeshTerrainTool meshTerrainTool, MapTileData tileData)
    {
        meshTerrainTool.TileName = tileData.tileDataName;
        //QuadTreeHeader
        if (!Directory.Exists(MTWorldConfig.GetQuadTreeHeaderFlodPath()))
            Directory.CreateDirectory(MTWorldConfig.GetQuadTreeHeaderFlodPath());
        string qtHeaderPath = MTWorldConfig.GetQuadTreeHeaderPath(tileData.tileIdStr);
        var qtHeader = MTEditorResourceLoader.LoadAssetAtPath<MTQuadTreeHeader>(qtHeaderPath);
        if (qtHeader == null)
        {
            qtHeader = ScriptableObject.CreateInstance<MTQuadTreeHeader>();
            AssetDatabase.CreateAsset(qtHeader, qtHeaderPath);
        }
        meshTerrainTool.header = qtHeader;
        EditorUtility.SetDirty(qtHeader);

        meshTerrainTool.VolumnBound = tileData.tileBound;

        //MapTileDataSet
        if (!Directory.Exists(MTWorldConfig.GetMapTileDataSetFlodPath()))
            Directory.CreateDirectory(MTWorldConfig.GetMapTileDataSetFlodPath());
        string tileDataSetPath = MTWorldConfig.GetMapTileDataSetAssetPath(tileData.tileIdStr);
        var tileDataSet = MTEditorResourceLoader.LoadAssetAtPath<MTMapTileDataSet>(tileDataSetPath);
        if (tileDataSet == null)
        {
            tileDataSet = ScriptableObject.CreateInstance<MTMapTileDataSet>();
            AssetDatabase.CreateAsset(tileDataSet, tileDataSetPath);
        }
        tileDataSet.mapDataName = dataName;
        meshTerrainTool.MTMapTileDataSet = tileDataSet;
        EditorUtility.SetDirty(tileDataSet);
        meshTerrainTool.LOD = LOD;
        meshTerrainTool.QuadTreeDepth = QuadTreeDepth;
    }

    public void RefreshSlicerArray()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        //先排X再排Y
        Array.Sort(terrains, (a, b) => { return a.name.Split('_')[0].CompareTo(b.name.Split('_')[0]); });
        Array.Sort(terrains, (a, b) => { return a.name.Split('_')[1].CompareTo(b.name.Split('_')[1]); });
        MapTiles = new MTSceneSlicer[terrains.Length];
        for (int i = 0; i < terrains.Length; i++)
        {
            MapTiles[i] = terrains[i].GetComponent<MTSceneSlicer>() == null ? terrains[i].gameObject.AddComponent<MTSceneSlicer>() : terrains[i].GetComponent<MTSceneSlicer>();
        }
    }

    /// <summary>
    /// 同步分割好的场景的MeshTool参数
    /// </summary>
    public void RefreshSliceData()
    {
        int sceneCount = MapTiles.Length;
        int index = 0;
        for (int i = 0; i < MapTiles.Length; i++)
        {
            int tileIDX = i % tileSizeX;
            int tileIDY = i / tileSizeX;
            string mapTileIDStr = ZString.Format("MapTile_{0}_{1}", tileIDX, tileIDY);
            if (MapTiles[i] == null || MapTiles[i].TileTerrain == null)
            {
                MTLog.LogError(ZString.Format("{0} is null，Check & Set Terrain Map", mapTileIDStr));
                continue;
            }
            else
            {
                if (RefreshSlicedSceneData(mapTileIDStr))
                    index++;
            }
        }
        MTLog.Log(ZString.Format("{0} Scene Refreshed: {1} Succeed —— {2} Faild", sceneCount, index, sceneCount - index));
    }

    private bool RefreshSlicedSceneData(string tileDataName)
    {
        if (MTMapTileHeader == null)
        {
            MTMapTileHeader = MTEditorResourceLoader.LoadAssetAtPath<MTMapTileHeader>(MTWorldConfig.GetMapTileHeaderAssetPath());
        }
        if (MTMapTileHeader == null)
            return false;
        MTMapTileHeader.WorldMapDataName = dataName;
        MTMapTileHeader.MapTileSizeX = tileSizeX;
        MTMapTileHeader.MapTileSzieY = tileSizeY;
        MTMapTileHeader.MapTileDatas = new List<MapTileData>();
        MTMapTileHeader.MapTileSize = new Vector3(tileGridSize.x, 0, tileGridSize.y); 
        MTMapTileHeader.MapTileQTDepth = QuadTreeDepth;

        string scenePath = ZString.Format("{0}/{1}.unity", MTWorldConfig.GetSplitSceneFlodPath(), tileDataName);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        if(scene == null)
        {
            MTLog.LogError("Not Find Scene " + tileDataName);
            return false;
        }
        var roots = scene.GetRootGameObjects();
        MeshTerrainTool mt = null;
        for (int i = 0; i < roots.Length; i++)
        {
            mt = roots[i].GetComponentInChildren<MeshTerrainTool>();
            if (mt != null)
                break;
        }
        if(mt)
        {
            mt.QuadTreeDepth = QuadTreeDepth;
            mt.TileName = tileDataName;
            mt.LOD = LOD;
        }
        else
        {
            return false;
        }
        EditorSceneManager.SaveScene(scene, scenePath, true);
        EditorSceneManager.CloseScene(scene, true);
        return true;
    }
}
#endif