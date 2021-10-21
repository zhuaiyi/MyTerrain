using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于地形块的Lightamp
/// </summary>
public class MTLightmapChunkKeeper : MTLightmapKeeper
{
    private void Awake()
    {
        RefreshLightmap();
    }

    public override List<MTLightmapData> CollectLightmapData()
    {
        MeshRenderer chunkMr = GetComponent<MeshRenderer>();
        MTLightmapData lightmapData = new MTLightmapData()
        {
            lightmapIndex = chunkMr.lightmapIndex,
            lightmapScaleOffset = chunkMr.lightmapScaleOffset
        };
        return new List<MTLightmapData>() { lightmapData };
    }

    public override void RefreshLightmap()
    {
        var meshrender = GetComponent<MeshRenderer>();
        if (mTLightmapDatas != null && mTLightmapDatas.Count > 0)
        {
            var lightmapData = mTLightmapDatas[0];
            meshrender.lightmapIndex = lightmapData.lightmapIndex;
            meshrender.lightmapScaleOffset = lightmapData.lightmapScaleOffset;
        }
    }
}