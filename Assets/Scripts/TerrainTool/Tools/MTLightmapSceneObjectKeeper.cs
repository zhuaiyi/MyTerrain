using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MTLightmapSceneObjectKeeper : MTLightmapKeeper
{
    public int IdentifyID;

    private void Awake()
    {
        RefreshLightmap();
    }

    public override List<MTLightmapData> CollectLightmapData()
    {
        List<MTLightmapData> re = new List<MTLightmapData>();
        var mrs = GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in mrs)
        {
            re.Add(new MTLightmapData() { lightmapIndex = mr.lightmapIndex, lightmapScaleOffset = mr.lightmapScaleOffset });
        }
        mTLightmapDatas = re;
        return re;
    }

    public override void RefreshLightmap()
    {
        var mrArray = GetComponentsInChildren<MeshRenderer>();
        if(mTLightmapDatas != null && mTLightmapDatas.Count > 0 && mrArray != null)
        {
            for (int i = 0; i < mrArray.Length; i++)
            {
                var lightmapData = i < mTLightmapDatas.Count ? mTLightmapDatas[i] : mTLightmapDatas[mTLightmapDatas.Count - 1];
                mrArray[i].lightmapIndex = lightmapData.lightmapIndex;
                mrArray[i].lightmapScaleOffset = lightmapData.lightmapScaleOffset;
            }
        }
    }
}