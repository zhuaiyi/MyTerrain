using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MTLightmapKeeper : MonoBehaviour
{
    public int MeshID;
    public string DataName;

    public List<MTLightmapData> mTLightmapDatas;

    public abstract List<MTLightmapData> CollectLightmapData();

    public abstract void RefreshLightmap();
}