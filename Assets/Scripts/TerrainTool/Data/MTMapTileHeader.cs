using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

/// <summary>
/// 所有地形Maptile数据头
/// WorldMap下一层数据MapTile
/// </summary>
[CreateAssetMenu(fileName = "MTMapTileHeader", menuName = "MTTerrain/CreateMTMapTileHeader")]
public class MTMapTileHeader : ScriptableObject
{
    public Vector3 MapTileSize;

    public Vector3 OriginalPosition;

    public int MapTileSizeX;

    public int MapTileSzieY;

    public int MapTileQTDepth;

    public string WorldMapDataName;

    [SerializeField]
    public List<MapTileData> MapTileDatas;

    public void AddMapTileData(MapTileData mapTileData)
    {
        if (MapTileDatas == null)
            MapTileDatas = new List<MapTileData>();
        MapTileDatas.Add(mapTileData);
    }
}

[Serializable]
public struct MapTileData
{
    public string tileDataName;

    public Bounds tileBound;

    public int2 tileID;

    public string tileIdStr;

    
    public MapTileData(string dataName, Bounds bounds, int tilex, int tiley)
    {
        tileDataName = dataName;
        tileBound = bounds;
        tileID = new int2(tilex, tiley);
        tileIdStr = tilex + "_" + tiley;
    }
}