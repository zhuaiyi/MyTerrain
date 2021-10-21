using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "MTMapTileDataSet", menuName = "MTTerrain/MTMapTileDataSet")]
public class MTMapTileDataSet : ScriptableObject
{
    [SerializeField]
    public List<ChunkData> chunkDataSetList;

    public string mapDataName;

    public void AddSceneObject(int meshID, SceneObjectData sceneObjectData)
    {
        if (chunkDataSetList == null)
            chunkDataSetList = new List<ChunkData>();
        var chunkData = chunkDataSetList.Find((data) => { return data.meshID == meshID; });
        if (chunkData == null)
        {
            chunkData = new ChunkData() { meshID = meshID };
            chunkData.AddSceneObject(sceneObjectData);
            chunkDataSetList.Add(chunkData);
        }
        else
        {
            chunkData.AddSceneObject(sceneObjectData);
        }
    }

    /// <summary>
    /// 收集该Tile中所有SceneObject资源种类
    /// 可以改成静态的
    /// </summary>
    /// <param name="sceneObjCollection"></param>
    public void GetTileSceneObjectCollection(List<string> sceneObjCollection)
    {
        sceneObjCollection.Clear();
        for (int i = 0; i < chunkDataSetList.Count; i++)
        {
            chunkDataSetList[i].GetSceneObjectCollection(sceneObjCollection);
        }
    }
}

[Serializable]
public class ChunkData
{
    public int meshID;

    public int lightmapIndex;

    public Vector4 lightmapScaleOffset;

    public Vector3 center;

    public List<SceneObjectData> chunkSceneObjectList;

    public ChunkData()
    {
        chunkSceneObjectList = new List<SceneObjectData>();
    }

    public void AddSceneObject(SceneObjectData sceneObjectData)
    {
        if (chunkSceneObjectList == null)
            chunkSceneObjectList = new List<SceneObjectData>();
        sceneObjectData.identifyID = chunkSceneObjectList.Count;
        chunkSceneObjectList.Add(sceneObjectData);
    }

    /// <summary>
    /// 收集Chunk中所有的SceneObject资源种类
    /// </summary>
    /// <param name="sceneObjCollection"></param>
    public void GetSceneObjectCollection(List<string> sceneObjCollection)
    {
        for (int i = 0; i < chunkSceneObjectList.Count; i++)
        {
            
            if (!sceneObjCollection.Contains(chunkSceneObjectList[i].name))
            {
                sceneObjCollection.Add(chunkSceneObjectList[i].name);
            }
        }
    }
}

[Serializable]
public class SceneObjectData
{
    public string name;

    public int identifyID;

    public List<MTLightmapData> mTLightmapDatas;

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public string resourcePath;
    public string prefabPath;
}