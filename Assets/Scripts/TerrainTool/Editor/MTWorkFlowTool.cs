using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


public class MTWorkFlowTool : Editor
{
    [MenuItem("MeshTerrain/1.AutoWork_GenerateData(necessary)", priority = 1)]
    public static void AutoDoWorkAllScene()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        var workScenes = GetWorkSceneAssetPath(MTWorldConfig.GetSplitSceneFlodPath());
        for (int i = 0; i < workScenes.Count; i++)
        {
            EditorSceneManager.OpenScene(workScenes[i], OpenSceneMode.Single);

            //Do Auto Work Flow
            SingleSceneAutoWorkFlow();
        }
        EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
    }

    private static void SingleSceneAutoWorkFlow()
    {
        MeshTerrainTool mt = GameObject.FindObjectOfType<MeshTerrainTool>();
        //step 1
        mt.EditorCreateDataBegin();
        while (true)
        {
            mt.EditorCreateDataUpdate();
            EditorUtility.DisplayProgressBar("creating data", "scaning volumn", mt.EditorCreateDataProgress);
            if (mt.IsEditorCreateDataDone)
                break;
        }
        mt.EditorCreateDataEnd();
        mt.EditorTessBegin();
        while (true)
        {
            mt.EditorTessUpdate();
            EditorUtility.DisplayProgressBar("creating data", "tessellation", mt.EditorTessProgress);
            if (mt.IsEditorTessDone)
                break;
        }
        EditorUtility.DisplayProgressBar("saving data", "processing", 1f);
        mt.EditorTessEnd();
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    [MenuItem("MeshTerrain/2.AutoWork_DevideSceneObject(necessary)", priority = 2)]
    public static void AutoDividScene()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        var workScenes = GetWorkSceneAssetPath(MTWorldConfig.GetSplitSceneFlodPath());
        foreach (var scene in workScenes)
        {
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            Wait(10000);
            var mt = GameObject.FindObjectOfType<MeshTerrainTool>();
            if (mt != null)
            {
                mt.DivideSceneObject();
            }
            EditorSceneManager.SaveOpenScenes();
        }
        EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
    }

    private static bool isBaking;

    [MenuItem("MeshTerrain/4.AutoWork_Bake", priority = 4)]
    public static void AutoDoBake()
    {
        if (isBaking)
        {
            Debug.LogWarning("Process Baking is On.");
            return;
        }
        isBaking = true;
        var bakeSceneList = GetWorkSceneAssetPath(MTWorldConfig.GetSplitSceneFlodPath());
        var bakeStartScene = EditorSceneManager.GetActiveScene().path;
        foreach (var scene in bakeSceneList)
        {

            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            Wait(10000);
            MTLightingSettingsHepler.SetLightingSettings();
            var mt = GameObject.FindObjectOfType<MeshTerrainTool>();
            if (mt != null)
                mt.SetSceneObjectRootStaticFlags(0);
            Lightmapping.Bake();
            EditorSceneManager.SaveOpenScenes();
        }

        EditorSceneManager.OpenScene(bakeStartScene, OpenSceneMode.Single);
        isBaking = false;
    }

    [MenuItem("MeshTerrain/3.AutoWork_PrepareSceneForBake", priority = 3)]
    public static void AutoPrepareSceneForBake()
    {
        string startScenePath = EditorSceneManager.GetActiveScene().path;
        var workScenes = GetWorkSceneAssetPath(MTWorldConfig.GetSplitSceneFlodPath());
        for (int i = 0; i < workScenes.Count; i++)
        {
            var currentScene = EditorSceneManager.OpenScene(workScenes[i], OpenSceneMode.Single);
            var mt = GameObject.FindObjectOfType<MeshTerrainTool>();
            if (mt != null)
            {
                mt.EditorClearPreview();
                //step 3-1
                mt.EditorCreateTerrainPreview();
                //step 3-2
                mt.EditorCreateSceneObjectPreview();
            }
            EditorSceneManager.SaveScene(currentScene);
        }
        EditorSceneManager.OpenScene(startScenePath, OpenSceneMode.Single);
    }

    [MenuItem("MeshTerrain/5.AutoWork_CollectLightMap", priority = 5)]
    public static void CollectAndRefreshLightMapData()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        var workScenes = GetWorkSceneAssetPath(MTWorldConfig.GetSplitSceneFlodPath());
        foreach (var scene in workScenes)
        {
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            Wait(10000);
            var mt = GameObject.FindObjectOfType<MeshTerrainTool>();
            if (mt != null)
            {
                mt.EditorCRLightmapInfo(0);
            }
            EditorSceneManager.SaveOpenScenes();
        }
        EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
    }

    [MenuItem("MeshTerrain/RefreshMaterialAssts", priority = 6)]
    public static void RefreshMaterialAssts()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        var workScenes = GetWorkSceneAssetPath(MTWorldConfig.GetSplitSceneFlodPath());
        foreach (var scene in workScenes)
        {
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            var mt = GameObject.FindObjectOfType<MeshTerrainTool>();
            if (mt != null)
            {
                mt.RefreshMaterialAssets();
            }
            EditorSceneManager.SaveOpenScenes();
        }
        EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
    }

    private static void Wait(int times)
    {
        while (times > 0)
            times--;
    }

    public static List<string> GetWorkSceneAssetPath(string workSceneRoot)
    {
        List<string> re = new List<string>();
        if (!Directory.Exists(workSceneRoot))
            return re;
        DirectoryInfo directoryInfo = new DirectoryInfo(workSceneRoot);
        FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < fileInfos.Length; i++)
        {
            if (fileInfos[i].Name.EndsWith(".meta"))
                continue;
            string workSceneAssetPath = workSceneRoot + "/" + fileInfos[i].Name;
            re.Add(workSceneAssetPath);
        }
        return re;
    }


}
