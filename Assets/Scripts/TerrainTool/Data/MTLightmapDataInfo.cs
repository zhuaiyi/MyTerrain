using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MTLightmapDataInfo
{
    public Dictionary<int, MTLightmapData> LightmapDataDic;

    public MTLightmapDataInfo()
    {
        LightmapDataDic = new Dictionary<int, MTLightmapData>();
    }
}

[Serializable]
public struct MTLightmapData
{
    public int lightmapIndex;

    public Vector4 lightmapScaleOffset;

    public MTLightmapData(int lmIndex, Vector4 lmScaleOffset)
    {
        lightmapIndex = lmIndex;
        lightmapScaleOffset = lmScaleOffset;
    }
}
