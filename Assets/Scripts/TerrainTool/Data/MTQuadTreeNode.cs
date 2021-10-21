using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MTMeshHeader
{
    public int MeshID;
    public Bounds MeshBound;
    public MTMeshHeader(int id, Bounds bounds)
    {
        MeshID = id;
        MeshBound = bounds;
    }
}

public class MTQuadTreeNode
{
    public Bounds Bound { get; private set; }
    public int MeshID { get; private set; }
    protected MTQuadTreeNode[] mSubNode;
    public MTQuadTreeNode(int depth, Vector3 min, Vector3 max)
    {
        Vector3 center = 0.5f * (min + max);
        Vector3 size = max - min;
        Bound = new Bounds(center, size);
        if (depth > 0)
        {
            mSubNode = new MTQuadTreeNode[4];
            Vector3 subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z - 0.5f * size.z);
            Vector3 subMax = new Vector3(center.x, max.y, center.z);
            mSubNode[0] = new MTQuadTreeNode(depth - 1, subMin, subMax);
            subMin = new Vector3(center.x, min.y, center.z - 0.5f * size.z);
            subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z);
            mSubNode[1] = new MTQuadTreeNode(depth - 1, subMin, subMax);
            subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z);
            subMax = new Vector3(center.x, max.y, center.z + 0.5f * size.z);
            mSubNode[2] = new MTQuadTreeNode(depth - 1, subMin, subMax);
            subMin = new Vector3(center.x, min.y, center.z);
            subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z + 0.5f * size.z);
            mSubNode[3] = new MTQuadTreeNode(depth - 1, subMin, subMax);
        }
    }

    public void RetrieveVisibleMesh(Plane[] planes, Vector3 viewCenter, float[] lodPolicy, MTArray<uint> visible)
    {
        if (GeometryUtility.TestPlanesAABB(planes, Bound))
        {
            if (mSubNode == null)
            {
                float distance = Vector3.SqrMagnitude(viewCenter - Bound.center);
                uint lodLevel = (uint)lodPolicy.Length;
                for (uint lod = 0; lod < lodPolicy.Length; lod++)
                {
                    if (distance <= lodPolicy[lod])
                    {
                        lodLevel = lod;
                        break;
                    }
                }
                uint patchId = (uint)(MeshID + 1);
                patchId <<= 2;
                patchId |= (lodLevel);
                visible.Add(patchId);
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    mSubNode[i].RetrieveVisibleMesh(planes, viewCenter, lodPolicy, visible);
                }
            }
        }
    }

    public void RetrieveVisibleMesh(Plane[] planes, Vector3 viewCenter, float[] lodPolicy, Dictionary<int,int> visible)
    {
        if (GeometryUtility.TestPlanesAABB(planes, Bound))
        {
            if (mSubNode == null)
            {
                float distance = Vector3.SqrMagnitude(viewCenter - Bound.center);
                for (int lod = 0; lod < lodPolicy.Length; lod++)
                {
                    if (distance <= lodPolicy[lod])
                    {
                        if (visible.ContainsKey(MeshID))
                            visible[MeshID] = lod;
                        else
                            visible.Add(MeshID, lod);
                        break;
                    }
                }
                visible[MeshID] = -1;
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    mSubNode[i].RetrieveVisibleMesh(planes, viewCenter, lodPolicy, visible);
                }
            }
        }
    }

    public void AddMesh(MTMeshHeader mesh)
    {
        if (mSubNode == null && Bound.Contains(mesh.MeshBound.center))
        {
            MeshID = mesh.MeshID;
            Bound = mesh.MeshBound;
        }
        else if (mSubNode != null)
        {
            for (int i = 0; i < 4; ++i)
            {
                mSubNode[i].AddMesh(mesh);
            }
        }
    }
}