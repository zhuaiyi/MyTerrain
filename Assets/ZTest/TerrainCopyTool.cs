using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class TerrainCopyTool : MonoBehaviour
{
    public Terrain tempTerrain;

    public Terrain targetTerrain;


    public CopyDirection copyDirection = CopyDirection.Vertical;


    [ContextMenu("CopyTerrainData")]
    public void CopyTerrainData()
    {
        if (tempTerrain == null || targetTerrain == null)
            return;
        CopySplatAndHeight();
        CopyGameObjects();
    }


    void CopySplatAndHeight()
    {
        targetTerrain.terrainData.size = tempTerrain.terrainData.size;
        targetTerrain.terrainData.terrainLayers = tempTerrain.terrainData.terrainLayers;
        var heightmapResolution = tempTerrain.terrainData.heightmapResolution;
        var heightMaps = tempTerrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
        targetTerrain.terrainData.heightmapResolution = heightmapResolution;
        int mid = heightMaps.GetLength(0) / 2;
        switch(copyDirection)
        {
            case CopyDirection.Vertical:
                for (int i = 0; i < heightmapResolution; i++)
                {
                    for (int j = 0; j < mid; j++)
                    {
                        var temp = heightMaps[i, j];
                        var upindex = heightmapResolution - 1 - j;
                        heightMaps[i, j] = heightMaps[i, upindex];
                        heightMaps[i, upindex] = temp;
                    }
                }
                break;
            case CopyDirection.Horizontal:
                for (int i = 0; i < mid; i++)
                {
                    for (int j = 0; j < heightmapResolution; j++)
                    {
                        var temp = heightMaps[i, j];
                        var rightIndex = heightmapResolution - 1 - i;
                        heightMaps[i, j] = heightMaps[rightIndex, j];
                        heightMaps[rightIndex, j] = temp;
                    }
                }
                break;
            case CopyDirection.Diagonal:
                for (int i = 0; i < mid; i++)
                {
                    for (int j = 0; j < heightmapResolution; j++)
                    {
                        var temp = heightMaps[i, j];
                        var rightIndex = heightmapResolution - 1 - i;
                        var upindex = heightmapResolution - 1 - j;
                        heightMaps[i, j] = heightMaps[rightIndex, upindex];
                        heightMaps[rightIndex, upindex] = temp;
                    }
                }
                break;
        }
        targetTerrain.terrainData.SetHeights(0, 0, heightMaps);

        var alphamapResolution = tempTerrain.terrainData.alphamapResolution;
        var alphaMaps = tempTerrain.terrainData.GetAlphamaps(0, 0, alphamapResolution, alphamapResolution);
        targetTerrain.terrainData.alphamapResolution = alphamapResolution;
        mid = alphamapResolution / 2;
        for (int m = 0; m < alphaMaps.GetLength(2); m++)
        {
            switch (copyDirection)
            {
                case CopyDirection.Vertical:
                    for (int i = 0; i < alphamapResolution; i++)
                    {
                        for (int j = 0; j < mid; j++)
                        {
                            var temp = alphaMaps[i, j, m];
                            var upindex = alphamapResolution - 1 - j;
                            alphaMaps[i, j, m] = alphaMaps[i, upindex, m];
                            alphaMaps[i, upindex, m] = temp;
                        }
                    }
                    break;
                case CopyDirection.Horizontal:
                    for (int i = 0; i < mid; i++)
                    {
                        for (int j = 0; j < alphamapResolution; j++)
                        {
                            if (j == mid)
                                continue;
                            var temp = alphaMaps[i, j, m];
                            var rightIndex = alphamapResolution - 1 - i;
                            alphaMaps[i, j, m] = alphaMaps[rightIndex, j, m];
                            alphaMaps[rightIndex, j, m] = temp;
                        }
                    }
                    break;
                case CopyDirection.Diagonal:
                    for (int i = 0; i < mid; i++)
                    {
                        for (int j = 0; j < alphamapResolution; j++)
                        {
                            var temp = alphaMaps[i, j, m];
                            var rightIndex = alphamapResolution - 1 - i;
                            var upindex = alphamapResolution - 1 - j;
                            alphaMaps[i, j, m] = alphaMaps[rightIndex, upindex, m];
                            alphaMaps[rightIndex, upindex, m] = temp;
                        }
                    }
                    break;
            }
        }
        targetTerrain.terrainData.SetAlphamaps(0, 0, alphaMaps);
        
    }

    public bool ContainsTerrain = true;

    Matrix4x4 ZYPlane = new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));
    Matrix4x4 XYPlane = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, -1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

    //Matrix4x4 GetMirrorMatrix(Vector3 planeNormal, Vector3 normalPoint)
    //{

    //}

    void CopyGameObjects()
    {
        var targetTerrainTF = targetTerrain.transform;
        var startPos = tempTerrain.transform.position;
        var terrainSize = tempTerrain.terrainData.size;
        for (int i = 0; i < tempTerrain.transform.childCount; i++)
        {
            var root = tempTerrain.transform.GetChild(i);
            if (targetTerrainTF.Find(root.name))
            {
                DestroyImmediate(targetTerrainTF.Find(root.name).gameObject);
            }
            var newRoot = new GameObject(root.name);
            newRoot.transform.parent = targetTerrainTF;
            for (int j = 0; j < root.childCount; j++)
            {
                var go = root.GetChild(j);
                var pos = go.position;
                Vector3 targetPos = Vector3.zero;
                var rot = go.rotation;
                switch (copyDirection)
                {
                    case CopyDirection.Vertical:
                        targetPos = new Vector3(2 * terrainSize.x - pos.x, pos.y, pos.z);
                        //rot = (ZYPlane * Matrix4x4.TRS(go.position, rot, go.lossyScale)).rotation;
                        rot = Quaternion.AngleAxis(180, Vector3.up) * rot;
                        break;
                    case CopyDirection.Horizontal:
                        targetPos = new Vector3(pos.x, pos.y, 2 * terrainSize.z - pos.z);
                        //rot = (XYPlane * Matrix4x4.TRS(go.position, rot, go.lossyScale)).rotation;
                        
                        break;
                }
                var newGo = Instantiate(go.gameObject, targetPos, rot, newRoot.transform);
                newGo.name = go.name;
            }
        }
        if(ContainsTerrain)
        {
            //Trees
            var treeCount = tempTerrain.terrainData.treeInstanceCount;
            if(treeCount > 0)
            {
                var treeInstances = new TreeInstance[treeCount];
                targetTerrain.terrainData.treePrototypes = tempTerrain.terrainData.treePrototypes;
                for (int i = 0; i < treeCount; i++)
                {
                    var treeInstance = tempTerrain.terrainData.GetTreeInstance(i);
                    var pos = treeInstance.position;
                    Vector3 targetPos = Vector3.zero;
                    switch (copyDirection)
                    {
                        case CopyDirection.Vertical:
                            targetPos = new Vector3(1 - pos.x, pos.y, pos.z);
                            break;
                        case CopyDirection.Horizontal:
                            targetPos = new Vector3(pos.x, pos.y, 1 - pos.z);
                            break;
                    }
                    treeInstance.position = targetPos;
                    //treeInstance.rotation = treeInstance.rotation * Quaternion.Euler(0, 180, 0);
                    treeInstances[i] = treeInstance;
                }
                targetTerrain.terrainData.SetTreeInstances(treeInstances, true);
            }

            //Details 由于Unity 对Detail的处理有Patch块的概念，没法获得每个Detail的位置信息，所以大地形转换的时暂时不考虑
            var detailCount = tempTerrain.terrainData.detailPatchCount;
            if(detailCount > 0)
            {
                targetTerrain.terrainData.detailPrototypes = tempTerrain.terrainData.detailPrototypes;
                targetTerrain.terrainData.SetDetailResolution(tempTerrain.terrainData.detailResolution, tempTerrain.terrainData.detailResolutionPerPatch);
                for (int i = 0; i < tempTerrain.terrainData.detailPrototypes.Length; i++)
                {
                    var detailMap = tempTerrain.terrainData.GetDetailLayer(0, 0, tempTerrain.terrainData.detailWidth, tempTerrain.terrainData.detailHeight, i);
                    targetTerrain.terrainData.SetDetailLayer(0, 0, i, detailMap);
                }
                
            }
        }
    }

    [ContextMenu("Process TempTerrain SceneObject")]
    public void ProcessSceneObejcts()
    {
        for (int i = 0; i < tempTerrain.transform.childCount; i++)
        {
            var root = tempTerrain.transform.GetChild(i);
            for (int j = 0; j < root.childCount; j++)
            {
                var go = root.GetChild(j);
                var prefabName = go.name;
                if (prefabName.EndsWith(")"))
                    prefabName = prefabName.Substring(0, prefabName.Length - 4);
                if (prefabName.Contains("Prefab_"))
                    prefabName = prefabName.Remove(0, 7);
                if (prefabName.Contains("SM_"))
                    prefabName = prefabName.Remove(0, 3);
                go.name = prefabName;
                if (PrefabUtility.IsAnyPrefabInstanceRoot(go.gameObject))
                    PrefabUtility.UnpackPrefabInstance(go.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                if (go.GetComponent<LODGroup>())
                {
                    DestroyImmediate(go.GetComponent<LODGroup>());
                }
                var childCount = go.childCount;
                if(childCount > 1)
                {
                    for (int k = childCount - 1; k >= 0; k--)
                    {
                        if (k != childCount - 1)
                        {
                            DestroyImmediate(go.GetChild(k).gameObject);
                        }
                    }
                }
            }   
                
        }
    }
}

public enum CopyDirection
{
    Vertical,
    Horizontal,
    Diagonal
}

#endif