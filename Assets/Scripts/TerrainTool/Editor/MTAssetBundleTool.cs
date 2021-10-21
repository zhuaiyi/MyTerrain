using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MTAssetBundleTool 
{
    private static readonly string DataName = "Demo1";

    [MenuItem("MeshTerrain/BuildAssetBundle")]

    public static void DoBuildAssetBundle()
    {
        MTWorldConfig.CurrentDataName = DataName;
        var bundleRootFlod = MTWorldConfig.GetAssetBundleRoot();
        if (!Directory.Exists(bundleRootFlod))
            Directory.CreateDirectory(bundleRootFlod);

        List<AssetBundleBuild> buildInfos = new List<AssetBundleBuild>();

        //MapTileHeader
        AssetBundleBuild mapTileHeaderInfo = new AssetBundleBuild();
        mapTileHeaderInfo.assetBundleName = MTWorldConfig.MapTileHeaderAssetName;
        mapTileHeaderInfo.assetNames = new string[1] { MTWorldConfig.GetMapTileHeaderAssetPath() };
        buildInfos.Add(mapTileHeaderInfo);

        //MapTileDataSet
        AssetBundleBuild chunkDataSetInfo = new AssetBundleBuild();
        chunkDataSetInfo.assetBundleName = MTWorldConfig.MapTileDataSetAssetBundleName;
        var tempPaths = new List<string>();
        var dataSets = MTEditorResourceLoader.LoadAllAssetsAtPath<MTMapTileDataSet>(MTWorldConfig.GetMapTileDataSetResourcePath());
        foreach (var dataSet in dataSets)
        {
            tempPaths.Add(AssetDatabase.GetAssetPath(dataSet));
            Resources.UnloadAsset(dataSet);
        }
        chunkDataSetInfo.assetNames = tempPaths.ToArray();
        dataSets = null;
        buildInfos.Add(chunkDataSetInfo);

        //QuadTreeHeader
        AssetBundleBuild quadTreeHeaderInfo = new AssetBundleBuild();
        quadTreeHeaderInfo.assetBundleName = MTWorldConfig.QuadTreeHeaderAssetBundleName;
        tempPaths.Clear();
        var qtHeaders = MTEditorResourceLoader.LoadAllAssetsAtPath<MTQuadTreeHeader>(MTWorldConfig.GetQuadTreeHeaderResourcePath());
        foreach (var header in qtHeaders)
        {
            tempPaths.Add(AssetDatabase.GetAssetPath(header));
            Resources.UnloadAsset(header);
        }
        quadTreeHeaderInfo.assetNames = tempPaths.ToArray();
        qtHeaders = null;
        buildInfos.Add(quadTreeHeaderInfo);

        //Material
        var matFiles = Directory.GetDirectories(MTWorldConfig.GetMaterialFlodPath());
        foreach (var matFlod in matFiles)
        {
            var resourcePath = matFlod.Replace("Assets/ArtResources/", "");
            var mats = MTEditorResourceLoader.LoadAllAssetsAtPath<Material>(matFlod);
            var tileName = resourcePath.Split('\\')[1];
            tempPaths.Clear();
            for (int i = 0; i < mats.Length; i++)
            {
                tempPaths.Add(AssetDatabase.GetAssetPath(mats[i]));
                Resources.UnloadAsset(mats[i]);
            }
            AssetBundleBuild aBuildInfo = new AssetBundleBuild();
            aBuildInfo.assetBundleName = MTWorldConfig.GetMaterialAssetBundleName(tileName);
            aBuildInfo.assetNames = tempPaths.ToArray();
            buildInfos.Add(aBuildInfo);
        }

        //SceneObject SharedAssets
        AssetBundleBuild sharedBuildInfo = new AssetBundleBuild();
        sharedBuildInfo.assetBundleName = MTWorldConfig.SharedAssetBundleName;
        List<string> sharedAssetsList = new List<string>();
        var sharedMaterials = MTEditorResourceLoader.LoadAllAssetsAtPath<Material>(MTWorldConfig.SharedMaterialPath);
        foreach (var mat in sharedMaterials)
        {
            sharedAssetsList.Add(AssetDatabase.GetAssetPath(mat));
            Resources.UnloadAsset(mat);
        }
        sharedMaterials = null;

        var sharedMeshes = MTEditorResourceLoader.LoadAllAssetsAtPath<GameObject>(MTWorldConfig.SharedMeshPath);
        foreach (var meshGo in sharedMeshes)
        {
            sharedAssetsList.Add(AssetDatabase.GetAssetPath(meshGo));
            Object.DestroyImmediate(meshGo, true);
        }
        sharedMeshes = null;

        var sharedTextures = MTEditorResourceLoader.LoadAllAssetsAtPath<Texture>(MTWorldConfig.SharedTexturePath);
        foreach (var tex in sharedTextures)
        {
            var path = AssetDatabase.GetAssetPath(tex);
            if (path.Contains("Terrain"))
                continue;
            sharedAssetsList.Add(path);
            Resources.UnloadAsset(tex);
        }
        sharedTextures = null;
        sharedBuildInfo.assetNames = sharedAssetsList.ToArray();
        buildInfos.Add(sharedBuildInfo);

        //SceneObject Prefabs
        var prefabResourceRoot = MTWorldConfig.GetPrefabResourcePath();
        var prefabs = MTEditorResourceLoader.LoadAllAssetsAtPath<GameObject>(prefabResourceRoot);
        for (int i = 0; i < prefabs.Length; i++)
        {
            var assetPath = AssetDatabase.GetAssetPath(prefabs[i]);
            AssetBundleBuild aBuildInfo = new AssetBundleBuild();
            aBuildInfo.assetBundleName = MTWorldConfig.GetPrefabAssetBundleName(prefabs[i].name);
            aBuildInfo.assetNames = new string[] { assetPath };
            buildInfos.Add(aBuildInfo);
        }

        //Meshes
        var meshFiles = Directory.GetDirectories(MTWorldConfig.GetMeshFlodPath());
        foreach (var meshFlod in meshFiles)
        {
            var resourcePath = meshFlod.Replace("Assets/ArtResources/", "");
            var meshes = MTEditorResourceLoader.LoadAllAssetsAtPath<Mesh>(meshFlod);
            var tileName = resourcePath.Split('\\')[1];
            string[] meshAssetNames = new string[meshes.Length];
            for (int i = 0; i < meshes.Length; i ++)
            {
                meshAssetNames[i] = AssetDatabase.GetAssetPath(meshes[i]);
                Resources.UnloadAsset(meshes[i]);
            }
            AssetBundleBuild aBuildInfo = new AssetBundleBuild();
            aBuildInfo.assetBundleName = MTWorldConfig.GetMeshAssetBundleName(tileName);
            aBuildInfo.assetNames = meshAssetNames;
            buildInfos.Add(aBuildInfo);
        }

        //Shader
        AssetBundleBuild shaderAB = new AssetBundleBuild();
        shaderAB.assetBundleName = MTWorldConfig.ShaderAssetBundleName;
        List<string> shaderList = new List<string>();
        var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(MTWorldConfig.ShaderVariantCollectionPath);
        shaderList.Add(MTWorldConfig.ShaderVariantCollectionPath);
        var svcDependencies = EditorUtility.CollectDependencies(new Object[1] { svc });
        foreach (var dep in svcDependencies)
        {
            if (dep is Shader)
            {
                shaderList.Add(AssetDatabase.GetAssetPath(dep));
            }
        }
        shaderAB.assetNames = shaderList.ToArray();
        buildInfos.Add(shaderAB);

        //LightMaps
        string lightmapPath = MTWorldConfig.GetLightMapRootFlod();
        var lightmaps = new DirectoryInfo(lightmapPath);
        //lightmapFiles = RemoveMeta(lightmapFiles);
        //AssetBundleBuild lightmapInfo = new AssetBundleBuild();
        //lightmapInfo.assetBundleName = string.Format("{0}/Lightmaps{1}", MTAssetBundleConfig.DataRelativeFolder, MTAssetBundleConfig.BundleExt);
        //string[] assets = new string[lightmapFiles.Length];
        //for (int i = 0; i < lightmapFiles.Length; i++)
        //{
        //    assets[i] = lightmapFiles[i];
        //}
        //lightmapInfo.assetNames = assets;
        //buildInfos.Add(lightmapInfo);

        BuildPipeline.BuildAssetBundles(bundleRootFlod, buildInfos.ToArray(), MTAssetBundleConfig.BundleBuildOptions, MTAssetBundleConfig.BundleTarget);
    }
}