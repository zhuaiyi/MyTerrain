using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MTStreamingLoader : MonoBehaviour
{
    private struct AgentLocation
    {
        public int2 mapTileID;
        public int meshID;
    }

    public string DataName;

    public Transform Player;

    public Camera mainCamera;

    private MTMapTileHeader mapTileHeader;

    public bool loadAsync = false;

    public float[] viewLodSetting;
    //Tile改变缓冲
    public float2 tileReserveSize = new float2(40f, 40f);
    //Chunk改变缓冲（小于Tile判定）
    public float2 chunkReserveSzie = new float2(10f, 10f);

    /// <summary>
    /// 视锥剔除水平扩展
    /// </summary>
    public float horizontalCullExtent = 0f;
    /// <summary>
    /// 视锥剔除垂直扩展
    /// </summary>
    public float verticalCullExtent = 0f;

    Vector3 mapOriginal;
    float2 mapTileSize;
    float2 chunkSize;
    int chunkLenInTile;

    AgentLocation currentLocation;
    AgentLocation lastLocation;

    private Bounds loadBounds;
    private Bounds preLoadBounds;
    public bool drawLoadBounds;

    //记录3X3（Load）及4X4（PreLoad）MapTile Index
    private List<int2> preLoadIndexList;
    private List<int2> loadIndexList;

    void InitStreamingLoader()
    {
        mapTileHeader = MTMapTileAssetManager.Instance.MapTileHeader;
        mapOriginal = mapTileHeader.OriginalPosition;
        mapTileSize = new float2(mapTileHeader.MapTileSize.x, mapTileHeader.MapTileSize.z);
        chunkLenInTile = 1 << mapTileHeader.MapTileQTDepth;
        chunkSize = new float2(mapTileSize.x / chunkLenInTile, mapTileSize.y / chunkLenInTile);
        currentLocation = new AgentLocation() { mapTileID = new int2(-1, -1), meshID = -1 };
        lastLocation = currentLocation;
    }

    private void Awake()
    {
        if (string.IsNullOrEmpty(DataName))
        {
            Debug.LogError("No DemoName");
            return;
        }

#if UNITY_EDITOR
        loadAsync = false;
#endif

        MTWorldConfig.CurrentDataName = DataName;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        Physics.autoSimulation = false;

        var assetLodSettings = new float[viewLodSetting.Length];
        for (int i = 0; i < viewLodSetting.Length; i++)
        {
            assetLodSettings[i] = viewLodSetting[i] * viewLodSetting[i];
        }
        MTMapTileAssetManager.Instance.Init(assetLodSettings);
        InitStreamingLoader();
        InitStartMapTile();
    }

    /// <summary>
    /// 初始化初始MapTile
    /// </summary>
    private void InitStartMapTile()
    {
        currentTileLimit = new float4(float.MinValue, float.MinValue, float.MaxValue, float.MinValue);
        currentChunkLimit = new float4(float.MinValue, float.MinValue, float.MaxValue, float.MinValue);
        UpdateAgentLocation();
        lastLocation = currentLocation;
        if (!MTMapTileAssetManager.Instance.MapTileDic.ContainsKey(currentLocation.mapTileID))
        {
            MTLog.LogError("Player is Not in Map");
            return;
        }
        loadIndexList = GetCheckTileIndex(1);
        loadIndexList.Add(currentLocation.mapTileID);
        preLoadIndexList = GetCheckTileIndex(2);
        if (loadAsync)
            StartCoroutine(MTMapTileAssetManager.Instance.UpdateMapTileAsync(loadIndexList, preLoadIndexList, Player.position));
        else
            MTMapTileAssetManager.Instance.UpdateMapTile(loadIndexList, preLoadIndexList, Player.position);
    }

    /// <summary>
    /// 方形边扩展的MapTile索引(仅在初始化调用)
    /// </summary>
    /// <param name="extentStep"></param>
    /// <returns></returns>
    public List<int2> GetCheckTileIndex(int extentStep)
    {
        List<int2> re = new List<int2>();
        int2 offset = int2.zero;
        offset.x = -extentStep;
        for (int i = -extentStep; i < extentStep; i++)
        {
            offset.y = i;
            re.Add(currentLocation.mapTileID + offset);
        }
        offset.x = extentStep;
        for (int i = extentStep; i > -extentStep; i--)
        {
            offset.y = i;
            re.Add(currentLocation.mapTileID + offset);
        }
        offset.y = -extentStep;

        for (int i = extentStep; i > -extentStep; i--)
        {
            offset.x = i;
            re.Add(currentLocation.mapTileID + offset);
        }
        offset.y = extentStep;
        for (int i = -extentStep; i < extentStep; i++)
        {
            offset.x = i;
            re.Add(currentLocation.mapTileID + offset);
        }
        return re;
    }

    private void Update()
    {
        CheckLocationChanged();
        UpdateFrustumCulling();
    }

    private void CheckLocationChanged()
    {
        UpdateAgentLocation();

        //TileID变化
        if (!currentLocation.mapTileID.Equals(lastLocation.mapTileID))
        {
            var offset = currentLocation.mapTileID - lastLocation.mapTileID;
            TileIndexApplyOffset(offset);
            if (loadAsync)
                StartCoroutine(MTMapTileAssetManager.Instance.UpdateMapTileAsync(loadIndexList, preLoadIndexList, Player.position));
            else
                MTMapTileAssetManager.Instance.UpdateMapTile(loadIndexList, preLoadIndexList, Player.position);
            lastLocation = currentLocation;
            Debug.LogWarning("Update Location-->" + currentLocation.mapTileID);
        }

        //MeshID变化
        //if (currentLocation.meshID != lastLocation.meshID)
        //{
        //    UpdateMeshAsset
        //    MTMapTileAssetManager.Instance.UpdateMeshAsset(currentLocation.mapTileID, currentLocation.meshID);
        //    lastLocation = currentLocation;
        //}
    }

    private void TileIndexApplyOffset(int2 offset)
    {
        for (int i = 0; i < loadIndexList.Count; i++)
        {
            loadIndexList[i] += offset;
        }
        for (int i = 0; i < preLoadIndexList.Count; i++)
        {
            preLoadIndexList[i] += offset;
        }
    }

    /// <summary>
    /// 当前Tile判定的缓冲限制范围
    /// x:左边界、y:下边界、z:右边界、w:上边界
    /// </summary>
    private float4 currentTileLimit;
    /// <summary>
    /// 当前Chunk判定的缓冲限制范围
    /// x:左边界、y:下边界、z:右边界、w:上边界
    /// </summary>
    private float4 currentChunkLimit;

    /// <summary>
    /// 更新Agent的Localtion坐标
    /// </summary>
    void UpdateAgentLocation()
    {
        UWAEngine.PushSample("UpdateAgentLocation");
        float2 position = new float2(Player.position.x - mapOriginal.x, Player.position.z - mapOriginal.z);
        int chunkLenX = (int)(position.x / chunkSize.x);
        int chunkLenY = (int)(position.y / chunkSize.y);
        if (position.x < currentTileLimit.x || position.y < currentTileLimit.y || position.x > currentTileLimit.z || position.y > currentTileLimit.w)
        {
            currentLocation.mapTileID.x = chunkLenX / chunkLenInTile;
            currentLocation.mapTileID.y = chunkLenY / chunkLenInTile;
            currentLocation.meshID = chunkLenX % chunkLenInTile * chunkLenInTile + chunkLenY % chunkLenInTile;
            currentTileLimit = GetTileLimitArea(currentLocation.mapTileID);
            currentChunkLimit = GetChunkLimitArea(currentLocation.mapTileID, currentLocation.meshID);
            //Debug.LogWarning("MapTileChanged-->" + currentLocation.mapTileID + "--->" + currentLocation.meshID);
        }
        else
        {
            //Chunk改变两种情况：
            //1.Chunk改变，Tile因为缓冲的问题判定为未改变（一般设置Tile缓冲大于Chunk缓冲判定）
            //2.Tile内的Chunk发生变化（此时没有资源状态的变化）
            if (position.x < currentChunkLimit.x || position.y < currentChunkLimit.y || position.x > currentChunkLimit.z || position.y > currentChunkLimit.w)
            {
                int2 tileID = new int2(chunkLenX / chunkLenInTile, chunkLenY / chunkLenInTile);
                currentLocation.meshID = chunkLenX % chunkLenInTile * chunkLenInTile + chunkLenY % chunkLenInTile;
                currentChunkLimit = GetChunkLimitArea(tileID, currentLocation.meshID);
                //MTMapTileAssetManager.Instance.UpdateMeshAsset(tileID, currentLocation.meshID);
                //Debug.LogWarning("ChunkChanged-->" + currentLocation.mapTileID + "--->" + currentLocation.meshID);
            }
        }
        UWAEngine.PopSample();
    }

    float4 GetTileLimitArea(int2 tileID)
    {
        float2 min = new float2(tileID.x * mapTileSize.x, tileID.y * mapTileSize.y) - tileReserveSize;
        float2 max = min + mapTileSize + 2 * tileReserveSize;
        return new float4(min, max);
    }

    float4 GetChunkLimitArea(int2 tileID, int meshID)
    {
        float2 min = new float2(tileID.x * mapTileSize.x, tileID.y * mapTileSize.y) 
            + new float2((meshID / chunkLenInTile) * chunkSize.x, (meshID % chunkLenInTile) * chunkSize.y)
            - chunkReserveSzie;
        float2 max = min + chunkSize + 2 * chunkReserveSzie;
        return new float4(min, max);
    }

    void UpdateFrustumCulling()
    {
        MTMapTileAssetManager.Instance.UpdateTileVisiable(mainCamera, loadIndexList, horizontalCullExtent, verticalCullExtent);
    }

    private void OnDrawGizmos()
    {
        if (!drawLoadBounds)
            return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(preLoadBounds.center, preLoadBounds.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(loadBounds.center, loadBounds.size);
    }
}