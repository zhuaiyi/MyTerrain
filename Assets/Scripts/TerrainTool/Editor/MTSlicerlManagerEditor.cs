using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MTSlicerManager))]
public class MTSlicerlManagerEditor : Editor
{
    private void DrawLodSetting(int idx, MTMeshLODSetting setting)
    {
        bool bFold = EditorGUILayout.Foldout(setting.bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (bFold)
        {
            int subdivision = EditorGUILayout.IntField("Subdivision(1 ~ 7)", setting.Subdivision);
            if (setting.Subdivision != subdivision)
            {
                setting.Subdivision = Mathf.Clamp(subdivision, 1, 7);
            }
            float slopeErr = EditorGUILayout.FloatField("Slope Tolerance(Max 45)", setting.SlopeAngleError);
            if (setting.SlopeAngleError != slopeErr)
            {
                setting.SlopeAngleError = Mathf.Clamp(slopeErr, 0, 45);
            }
        }
        if (setting.bEditorUIFoldout != bFold)
        {
            setting.bEditorUIFoldout = bFold;
        }
    }

    private void DrawMaptileSlicerArray(SerializedObject obj, int count)
    {
        obj.FindProperty("MapTiles.Array.size").intValue = count;
        bool bFold = EditorGUILayout.Foldout(isSlicerFoldout, string.Format("Slicer_Count {0}", count));
        if (bFold)
        {
            for (int i = 0; i < count; i++)
            {
                var prop = obj.FindProperty(string.Format("MapTiles.Array.data[{0}]", i));
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, new GUIContent("Slicer " + i));
            }
        }
        if (bFold != isSlicerFoldout)
        {
            isSlicerFoldout = bFold;
        }
    }

    private void DrawSharedGoArray(SerializedObject obj, int count)
    {
        obj.FindProperty("SharedGameObjects.Array.size").intValue = count;
        bool bFold = EditorGUILayout.Foldout(isSharedFoldout, string.Format("SharedGo_Count {0}", count));
        if (bFold)
        {
            for (int i = 0; i < count; i++)
            {
                var prop = obj.FindProperty(string.Format("SharedGameObjects.Array.data[{0}]", i));
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, new GUIContent("RootGo "+ i));
            }
        }
        if (bFold != isSharedFoldout)
        {
            isSharedFoldout = bFold;
        }
    }

    private SerializedProperty mapTileArray;
    private SerializedProperty sceneObjectArray;
    private bool isSlicerFoldout = true;
    private bool isSharedFoldout = true;

    private void OnEnable()
    {
        mapTileArray = serializedObject.FindProperty("MapTiles");
        sceneObjectArray = serializedObject.FindProperty("Shared Scene Objects");
        MTSlicerManager mtsm = (MTSlicerManager)target;
        mtsm.SetConfigDataName();
    }

    public override void OnInspectorGUI()
    {
        MTSlicerManager mtsm = (MTSlicerManager)target;
        serializedObject.Update();
        base.OnInspectorGUI();

        string demoName = mtsm.dataName;
        MTWorldConfig.CurrentDataName = demoName;

        mtsm.splitScenePath = MTWorldConfig.GetSplitSceneFlodPath();
        mtsm.splitScenePath = EditorGUILayout.TextField("Split Scene Path", mtsm.splitScenePath);

        int lodCount = mtsm.LOD.Length;
        int lod = EditorGUILayout.IntField("LOD (1 ~ 4)", lodCount);
        if (lod != lodCount)
        {
            lodCount = Mathf.Clamp(lod, 1, 4);
            MTMeshLODSetting[] old = mtsm.LOD;
            mtsm.LOD = new MTMeshLODSetting[lodCount];
            for (int i = 0; i < lodCount; ++i)
            {
                mtsm.LOD[i] = new MTMeshLODSetting();
                if (i < old.Length)
                {
                    mtsm.LOD[i].Subdivision = old[i].Subdivision;
                    mtsm.LOD[i].SlopeAngleError = old[i].SlopeAngleError;
                }
            }
        }
        for (int i = 0; i < lodCount; ++i)
        {
            DrawLodSetting(i, mtsm.LOD[i]);
        }

        Vector2 tileSize = mtsm.tileGridSize;
        mtsm.tileGridSize = EditorGUILayout.Vector2Field("MapTile Grid Size", tileSize);
        int tileSizeX = mtsm.tileSizeX;
        mtsm.tileSizeX = EditorGUILayout.IntSlider("MapTile Size X", tileSizeX, 1, 99);
        int tileSizeY = mtsm.tileSizeY;
        mtsm.tileSizeY = EditorGUILayout.IntSlider("MapTile Size Y", tileSizeY, 1, 99);

        if (GUILayout.Button("Auto Fill In Slicer"))
            mtsm.RefreshSlicerArray();
        
        DrawMaptileSlicerArray(serializedObject, tileSizeX * tileSizeY);

        int c = mtsm.SharedGameObjects == null ? 0 : mtsm.SharedGameObjects.Length;
        c = EditorGUILayout.IntField("Shared Scene Objects", c); ;
        DrawSharedGoArray(serializedObject, c);

        SerializedProperty mapTileHeaderProperty = serializedObject.FindProperty("MTMapTileHeader");
        EditorGUILayout.PropertyField(mapTileHeaderProperty);



        if (GUILayout.Button("Slice MapTile Scene"))
        {
            mtsm.MapTileSplit();
        }

        if (GUILayout.Button("Refresh MTTool Data"))
        {
            mtsm.RefreshSliceData();
        }

        GUILayout.Space(15);
        GUILayout.Box("Build AssetBundle");
        if (GUILayout.Button("MTBuild AssetBundles"))
        {
            MTAssetBundleTool.DoBuildAssetBundle();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
