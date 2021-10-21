using System.Collections.Generic;
using UnityEngine;

public class MeshLODCreate
{
    public int Subdivision = 3;
    public float SlopeAngleError = 5f;
}

public class MTMeshData
{
    public class LOD
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uvs;
        public int[] faces;
    }
    public int meshId { get; private set; }
    public Vector3 center { get; private set; }
    public LOD[] lods;
    public MTMeshData(int id, Vector3 c)
    {
        meshId = id;
        center = c;
    }
}

/// <summary>
/// One job for one lod
/// </summary>
public class CreateDataJob
{
    public MTTerrainScanner[] LODs;
    private int curLodIdx = 0;
    public bool IsDone
    {
        get
        {
            return curLodIdx >= LODs.Length;
        }
    }
    public float progress
    {
        get
        {
            if (curLodIdx < LODs.Length)
            {
                return (curLodIdx + LODs[curLodIdx].progress) / LODs.Length;
            }
            return 1;
        }
    }
    public CreateDataJob(Bounds VolumnBound, int mx, int mz, MeshLODCreate[] setting, Terrain terrain)
    {
        LODs = new MTTerrainScanner[setting.Length];
        for (int i = 0; i < setting.Length; ++i)
        {
            MeshLODCreate s = setting[i];
            //仅LOD 0的边缘顶点会做接缝处理，其他LOD边缘会用LOD 0的边缘替代，避免chunk间的接缝问题
            LODs[i] = new MTTerrainScanner(VolumnBound, s.Subdivision, s.SlopeAngleError, mx, mz, i == 0, terrain);
        }
    }
    public void Update()
    {
        if (LODs == null || IsDone)
            return;
        LODs[curLodIdx].Update();
        if (LODs[curLodIdx].IsDone)
            ++curLodIdx;
    }
    public void EndProcess()
    {
        //copy borders
        MTTerrainScanner detail = LODs[0];
        detail.FillData();
        for (int i = 1; i < LODs.Length; ++i)
        {
            MTTerrainScanner scaner = LODs[i];
            for (int t = 0; t < detail.Trees.Length; ++t)
            {
                SamplerTree dt = detail.Trees[t];
                SamplerTree lt = scaner.Trees[t];
                //其他LOD添加LOD 0的边缘数据
                foreach (var b in dt.Boundaries)
                {
                    lt.Boundaries.Add(b.Key, b.Value);
                }
            }
            scaner.FillData();
        }
    }
}

/// <summary>
/// one scaner for one lod
/// </summary>
public class MTTerrainScanner : ITerrainTreeScaner
{
    public int maxX { get; private set; }
    public int maxZ { get; private set; }
    public int subdivision { get; private set; }
    public float slopeAngleErr { get; private set; }
    /// <summary>
    /// Chunk Size
    /// </summary>
    public Vector2 gridSize { get; private set; }
    public SamplerTree[] Trees { get; private set; }
    public Vector3 center { get; private set; }
    private Vector3 vCheckTop = Vector3.one;
    private float CheckRayLen = 0;
    private int curXIdx = 0;
    private int curZIdx = 0;
    private int detailedSize = 1;
    private bool stitchBorder = true;
    private Terrain terrain;
    private TerrainData terrainData;
    public bool IsDone
    {
        get
        {
            return curXIdx >= maxX && curZIdx >= maxZ;
        }
    }
    public float progress
    {
        get
        {
            return (float)(curXIdx + curZIdx * maxX) / (float)(maxX * maxZ);
        }
    }
    public MTTerrainScanner(Bounds VolumnBound, int sub, float angleErr, int mx, int mz, bool sbrd, Terrain t)
    {
        maxX = mx;
        maxZ = mz;
        subdivision = Mathf.Max(1, sub);
        slopeAngleErr = Mathf.Max(0.1f, angleErr);
        stitchBorder = sbrd;
        center = VolumnBound.center;
        gridSize = new Vector2(VolumnBound.size.x / mx, VolumnBound.size.z / mz);
        vCheckTop = new Vector3(VolumnBound.center.x - VolumnBound.size.x / 2,
             VolumnBound.center.y + VolumnBound.size.y,
            VolumnBound.center.z - VolumnBound.size.z / 2);
        CheckRayLen = VolumnBound.size.y * 2f;
        //
        detailedSize = 1 << subdivision;
        //
        Trees = new SamplerTree[maxX * maxZ];
        terrain = t;
        terrainData = t.terrainData;
    }
    public SamplerTree GetSubTree(int x, int z)
    {
        if (x < 0 || x >= maxX || z < 0 || z >= maxZ)
            return null;
        return Trees[x * maxZ + z];
    }

    //遍历Chunk中每个LeafNode通过射线拿到MT Vertex数据
    void ITerrainTreeScaner.Run(Vector3 center, out Vector3 hitpos, out Vector3 hitnormal)
    {
        hitpos = center;
        hitnormal = Vector3.up;
        float height = terrain.SampleHeight(center);
        hitpos = new Vector3(center.x, height, center.z);
        var offset = center - terrain.transform.position;
        var x = Mathf.Clamp(offset.x / terrainData.size.x, 0, 1);
        var y = Mathf.Clamp(offset.z / terrainData.size.z, 0, 1);
        hitnormal = terrain.terrainData.GetInterpolatedNormal(x, y);
        //RaycastHit hit = new RaycastHit();
        //if (Physics.Raycast(center, Vector3.down, out hit, CheckRayLen, 1 << LayerMask.NameToLayer("Terrain")))
        //{
        //    hitpos = hit.point;
        //    hitnormal = hit.normal;
        //}
        //else
        //{
        //    MTLog.LogError("scan didn't hit terrain");
        //}
    }
    //ScanTree的单位是Chunk，通过射线
    private void ScanTreeByRaycast(SamplerTree sampler)
    {
        sampler.RunSampler(this);
        if (!stitchBorder)
            return;
        int detailedX = curXIdx * detailedSize;
        int detailedZ = curZIdx * detailedSize;
        //todo:边界处理会导致Tile接缝问题
        //解决方案1:保证射线获得数据后，在将点沿Normal平面平移Offset（dx）得到新的点
        float bfx = curXIdx * gridSize[0];
        float bfz = curZIdx * gridSize[1];
        float borderOffset = 0;
        var offset = Vector3.zero;
        if (curXIdx == 0 || curZIdx == 0 || curXIdx == maxX - 1 || curZIdx == maxZ - 1)
            borderOffset = 0.0005f;
        offset = new Vector3(borderOffset, 0, borderOffset);
        RayCastBoundary(bfx + borderOffset, bfz + borderOffset, detailedX, detailedZ, SamplerTree.LBCorner, sampler, offset);
        offset = new Vector3(borderOffset, 0, -borderOffset);
        RayCastBoundary(bfx + borderOffset, bfz + gridSize[1] - borderOffset, detailedX, detailedZ + detailedSize - 1, SamplerTree.LTCorner, sampler, offset);
        offset = new Vector3(-borderOffset, 0, -borderOffset);
        RayCastBoundary(bfx + gridSize[0] - borderOffset, bfz + gridSize[1] - borderOffset, detailedX + detailedSize - 1, detailedZ + detailedSize - 1, SamplerTree.RTCorner, sampler, offset);
        offset = new Vector3(-borderOffset, 0, borderOffset);
        RayCastBoundary(bfx + gridSize[0] - borderOffset, bfz + borderOffset, detailedX + detailedSize - 1, detailedZ, SamplerTree.RBCorner, sampler, offset);
        for (int u = 1; u < detailedSize; ++u)
        {
            float fx = (curXIdx + (float)u / detailedSize) * gridSize[0];
            offset = new Vector3(0, 0, borderOffset);
            RayCastBoundary(fx, bfz + borderOffset, u + detailedX, detailedZ, SamplerTree.BBorder, sampler, offset);
            offset = new Vector3(0, 0, -borderOffset);
            RayCastBoundary(fx, bfz + gridSize[1] - borderOffset, u + detailedX, detailedZ + detailedSize - 1, SamplerTree.TBorder, sampler, offset);
        }
        for (int v = 1; v < detailedSize; ++v)
        {
            float fz = (curZIdx + (float)v / detailedSize) * gridSize[1];
            offset = new Vector3(borderOffset, 0, 0);
            RayCastBoundary(bfx + borderOffset, fz, detailedX, v + detailedZ, SamplerTree.LBorder, sampler, offset);
            offset = new Vector3(-borderOffset, 0, 0);
            RayCastBoundary(bfx + gridSize[0] - borderOffset, fz, detailedX + detailedSize - 1, v + detailedZ, SamplerTree.RBorder, sampler, offset);
        }
    }

    private void RayCastBoundary(float fx, float fz, int x, int z, byte bk, SamplerTree sampler, Vector3 offset)
    {
        Vector3 top = vCheckTop + fx * Vector3.right + fz * Vector3.forward;
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(top, Vector3.down, out hit, CheckRayLen, 1 << LayerMask.NameToLayer("Terrain")))
        {
            SampleVertexData vert = new SampleVertexData();
            if (offset != Vector3.zero)
            {
                vert.Position = GetCorrectionPosition(hit.point, hit.normal, offset);
            }
            else
            {
                vert.Position = hit.point;
            }
            vert.Normal = hit.normal;
            vert.UV = new Vector2(fx / maxX / gridSize[0], fz / maxZ / gridSize[1]);
            sampler.AddBoundary(subdivision, x, z, bk, vert);
        }
        else
        {
            MTLog.LogError("RayCastBoundary didn't hit terrain");
        }
    }

    private Vector3 GetCorrectionPosition(Vector3 point, Vector3 normal, Vector3 offset)
    {
        float A = normal.x;
        float B = normal.y;
        float C = normal.z;
        float D = Vector3.Dot(normal, point);
        Vector3 correctionPoint = point - offset;
        correctionPoint.y = (D - A * correctionPoint.x - C * correctionPoint.z) / B;
        return correctionPoint;
    }

    /// <summary>
    /// 通过地形数据采样
    /// </summary>
    /// <param name="sampler"></param>
    private void ScanTreeBySampler(SamplerTree sampler)
    {
        //采样非边缘数据
        sampler.RunSampler(this);
        //其他LOD与LOD公用边缘顶点，保证不同LOD的接缝问题
        if (!stitchBorder)
            return;
        int detailedX = curXIdx * detailedSize;
        int detailedZ = curZIdx * detailedSize;
        float bfx = curXIdx * gridSize[0];
        float bfz = curZIdx * gridSize[1];
        //采样边缘数据
        //4个角
        SamplerBoundary(bfx, bfz, detailedX, detailedZ, SamplerTree.LBCorner, sampler);
        SamplerBoundary(bfx, bfz + gridSize[1], detailedX, detailedZ + detailedSize - 1, SamplerTree.LTCorner, sampler);
        SamplerBoundary(bfx + gridSize[0], bfz + gridSize[1], detailedX + detailedSize - 1, detailedZ + detailedSize - 1, SamplerTree.RTCorner, sampler);
        SamplerBoundary(bfx + gridSize[0], bfz, detailedX + detailedSize - 1, detailedZ, SamplerTree.RBCorner, sampler);
        //上下边
        for (int u = 1; u < detailedSize; ++u)
        {
            float fx = (curXIdx + (float)u / detailedSize) * gridSize[0];
            SamplerBoundary(fx, bfz, u + detailedX, detailedZ, SamplerTree.BBorder, sampler);
            SamplerBoundary(fx, bfz + gridSize[1], u + detailedX, detailedZ + detailedSize - 1, SamplerTree.TBorder, sampler);
        }
        //左右边
        for (int v = 1; v < detailedSize; ++v)
        {
            float fz = (curZIdx + (float)v / detailedSize) * gridSize[1];
            SamplerBoundary(bfx, fz, detailedX, v + detailedZ, SamplerTree.LBorder, sampler);
            SamplerBoundary(bfx + gridSize[0], fz, detailedX + detailedSize - 1, v + detailedZ, SamplerTree.RBorder, sampler);
        }
    }

    private void SamplerBoundary(float fx, float fz, int x, int z, byte bk, SamplerTree sampler)
    {
        if (terrain == null)
        {
            MTLog.LogError("Terrain is null");
            return;
        }
        Vector3 top = vCheckTop + fx * Vector3.right + fz * Vector3.forward;
        float height = terrain.SampleHeight(top);
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(fx / terrainData.size.x, fz / terrainData.size.z);
        top.y = height;
        SampleVertexData vert = new SampleVertexData();
        vert.Position = top;
        vert.Normal = normal;
        vert.UV = new Vector2(fx / maxX / gridSize[0], fz / maxZ / gridSize[1]);
        sampler.AddBoundary(subdivision, x, z, bk, vert);
    }

    public void Update()
    {
        if (IsDone)
            return;
        float fx = (curXIdx + 0.5f) * gridSize[0];
        float fz = (curZIdx + 0.5f) * gridSize[1];
        Vector3 center = vCheckTop + fx * Vector3.right + fz * Vector3.forward;
        Vector2 uv = new Vector2((curXIdx + 0.5f) / maxX, (curZIdx + 0.5f) / maxZ);
        Vector2 uvstep = new Vector2(1f / maxX, 1f / maxZ);
        int currentTreeIdx = curXIdx * maxZ + curZIdx;
        if (Trees[currentTreeIdx] == null)
            Trees[currentTreeIdx] = new SamplerTree(subdivision, center, gridSize, uv, uvstep);

        //ScanTreeByRaycast(Trees[currentTreeIdx]);
        ScanTreeBySampler(Trees[currentTreeIdx]);
        //update idx
        ++curXIdx;
        if (curXIdx >= maxX)
        {
            if (curZIdx < maxZ - 1)
                curXIdx = 0;
            ++curZIdx;
        }
    }
    private Vector3 AverageNormal(List<SampleVertexData> lvers)
    {
        Vector3 normal = Vector3.up;
        for (int i = 0; i < lvers.Count; ++i)
        {
            normal += lvers[i].Normal;
        }
        return normal.normalized;
    }
    private void MergeCorners(List<SampleVertexData> l0, List<SampleVertexData> l1, List<SampleVertexData> l2,
        List<SampleVertexData> l3)
    {
        List<SampleVertexData> lvers = new List<SampleVertexData>();
        //lb
        lvers.Add(l0[0]);
        if (l1 != null)
            lvers.Add(l1[0]);
        if (l2 != null)
            lvers.Add(l2[0]);
        if (l3 != null)
            lvers.Add(l3[0]);
        Vector3 normal = AverageNormal(lvers);
        l0[0].Normal = normal;
        if (l1 != null)
            l1[0].Normal = normal;
        if (l2 != null)
            l2[0].Normal = normal;
        if (l3 != null)
            l3[0].Normal = normal;
    }
    private void StitchCorner(int x, int z)
    {
        SamplerTree center = GetSubTree(x, z);
        if (!center.Boundaries.ContainsKey(SamplerTree.LBCorner))
        {
            MTLog.LogError("boundary data missing");
            return;
        }
        SamplerTree right = GetSubTree(x + 1, z);
        SamplerTree left = GetSubTree(x - 1, z);
        SamplerTree right_top = GetSubTree(x + 1, z + 1);
        SamplerTree top = GetSubTree(x, z + 1);
        SamplerTree left_top = GetSubTree(x - 1, z + 1);
        SamplerTree left_down = GetSubTree(x - 1, z - 1);
        SamplerTree down = GetSubTree(x, z - 1);
        SamplerTree right_down = GetSubTree(x + 1, z - 1);
        if (!center.StitchedBorders.Contains(SamplerTree.LBCorner))
        {
            MergeCorners(center.Boundaries[SamplerTree.LBCorner],
                left != null ? left.Boundaries[SamplerTree.RBCorner] : null,
                left_down != null ? left_down.Boundaries[SamplerTree.RTCorner] : null,
                down != null ? down.Boundaries[SamplerTree.LTCorner] : null);
            center.StitchedBorders.Add(SamplerTree.LBCorner);
            if (left != null) left.StitchedBorders.Add(SamplerTree.RBCorner);
            if (left_down != null) left_down.StitchedBorders.Add(SamplerTree.RTCorner);
            if (down != null) left.StitchedBorders.Add(SamplerTree.LTCorner);
        }
        if (!center.StitchedBorders.Contains(SamplerTree.RBCorner))
        {
            MergeCorners(center.Boundaries[SamplerTree.RBCorner],
                right != null ? right.Boundaries[SamplerTree.LBCorner] : null,
                right_down != null ? right_down.Boundaries[SamplerTree.LTCorner] : null,
                down != null ? down.Boundaries[SamplerTree.RTCorner] : null);
            center.StitchedBorders.Add(SamplerTree.RBCorner);
            if (right != null) right.StitchedBorders.Add(SamplerTree.LBCorner);
            if (right_down != null) right_down.StitchedBorders.Add(SamplerTree.LTCorner);
            if (down != null) down.StitchedBorders.Add(SamplerTree.RTCorner);
        }
        if (!center.StitchedBorders.Contains(SamplerTree.LTCorner))
        {
            MergeCorners(center.Boundaries[SamplerTree.LTCorner],
                left != null ? left.Boundaries[SamplerTree.RTCorner] : null,
                left_top != null ? left_top.Boundaries[SamplerTree.RBCorner] : null,
                top != null ? top.Boundaries[SamplerTree.LBCorner] : null);
            center.StitchedBorders.Add(SamplerTree.LTCorner);
            if (left != null) left.StitchedBorders.Add(SamplerTree.RTCorner);
            if (left_top != null) left_top.StitchedBorders.Add(SamplerTree.RBCorner);
            if (top != null) top.StitchedBorders.Add(SamplerTree.LBCorner);
        }

        if (!center.StitchedBorders.Contains(SamplerTree.RTCorner))
        {
            MergeCorners(center.Boundaries[SamplerTree.RTCorner],
                right != null ? right.Boundaries[SamplerTree.LTCorner] : null,
                right_top != null ? right_top.Boundaries[SamplerTree.LBCorner] : null,
                top != null ? top.Boundaries[SamplerTree.RBCorner] : null);
            center.StitchedBorders.Add(SamplerTree.RTCorner);
            if (right != null) right.StitchedBorders.Add(SamplerTree.LTCorner);
            if (right_top != null) right_top.StitchedBorders.Add(SamplerTree.LBCorner);
            if (top != null) top.StitchedBorders.Add(SamplerTree.RBCorner);
        }
    }
    public void FillData()
    {
        for (int i = 0; i < Trees.Length; ++i)
        {
            Trees[i].FillData(slopeAngleErr);
        }
        //stitch the border
        float minDis = Mathf.Min(gridSize.x, gridSize.y) / detailedSize / 2f;
        for (int x = 0; x < maxX; ++x)
        {
            for (int z = 0; z < maxZ; ++z)
            {
                SamplerTree center = GetSubTree(x, z);
                //corners
                StitchCorner(x, z);
                //borders
                center.StitchBorder(SamplerTree.BBorder, SamplerTree.TBorder, minDis, GetSubTree(x, z - 1));
                center.StitchBorder(SamplerTree.LBorder, SamplerTree.RBorder, minDis, GetSubTree(x - 1, z));
                center.StitchBorder(SamplerTree.RBorder, SamplerTree.LBorder, minDis, GetSubTree(x + 1, z));
                center.StitchBorder(SamplerTree.TBorder, SamplerTree.BBorder, minDis, GetSubTree(x, z + 1));
            }
        }
        //merge boundary with verts for tessallation
        for (int i = 0; i < Trees.Length; ++i)
        {
            foreach (var l in Trees[i].Boundaries.Values)
                Trees[i].Vertices.AddRange(l);
        }
    }
}