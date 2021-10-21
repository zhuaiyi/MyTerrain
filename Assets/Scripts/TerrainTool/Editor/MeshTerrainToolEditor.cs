using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshTerrainTool))]
public class MeshTerrainToolEditor : Editor
{
    private void DrawSetting(int idx, MTMeshLODSetting setting)
    {
        bool bFold = EditorGUILayout.Foldout(setting.bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (!bFold)
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

    private void DrawSceneObjectRootArray(SerializedObject obj, string name)
    {
        int no = obj.FindProperty(name + ".Array.size").intValue;
        EditorGUI.indentLevel = 3;
        int c = EditorGUILayout.IntField("SceneObjectRoot Size", no);
        if (c != no)
            obj.FindProperty(name + ".Array.size").intValue = c;

        for (int i = 0; i < no; i++)
        {
            var prop = obj.FindProperty(string.Format("{0}.Array.data[{1}]", name, i));
            if (prop != null)
                EditorGUILayout.PropertyField(prop);
        }
    }

    private int comlightMapLodLevel;
    private int showMeshLodLevel;
    private int preShowMeshLodLevel;

    public override void OnInspectorGUI()
    {
        MeshTerrainTool comp = (MeshTerrainTool)target;
        // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
        serializedObject.Update();
        base.OnInspectorGUI();
        int lodCount = comp.LOD.Length;
        int lod = EditorGUILayout.IntField("LOD (1 ~ 4)", lodCount);
        if (lod != lodCount)
        {
            lodCount = Mathf.Clamp(lod, 1, 4);
            MTMeshLODSetting[] old = comp.LOD;
            comp.LOD = new MTMeshLODSetting[lodCount];
            for (int i = 0; i < lodCount; ++i)
            {
                comp.LOD[i] = new MTMeshLODSetting();
                if (i < old.Length)
                {
                    comp.LOD[i].Subdivision = old[i].Subdivision;
                    comp.LOD[i].SlopeAngleError = old[i].SlopeAngleError;
                }
            }
        }
        for (int i = 0; i < lodCount; ++i)
        {
            DrawSetting(i, comp.LOD[i]);
        }


        SerializedProperty chunkDataSetProperty = serializedObject.FindProperty("MTMapTileDataSet");
        EditorGUILayout.PropertyField(chunkDataSetProperty);

        SerializedProperty headerProperty = serializedObject.FindProperty("header");
        EditorGUILayout.PropertyField(headerProperty);

        if (GUILayout.Button("Step 1 : Generate TerrainData"))
        {
            if (comp.TileName == "")
            {
                Debug.LogError("data should have a name");
                return;
            }
            comp.EditorCreateDataBegin();
            for (int i = 0; i < int.MaxValue; ++i)
            {
                comp.EditorCreateDataUpdate();
                EditorUtility.DisplayProgressBar("creating data", "scaning volumn", comp.EditorCreateDataProgress);
                if (comp.IsEditorCreateDataDone)
                    break;
            }
            comp.EditorCreateDataEnd();
            comp.EditorTessBegin();
            for (int i = 0; i < int.MaxValue; ++i)
            {
                comp.EditorTessUpdate();
                EditorUtility.DisplayProgressBar("creating data", "tessellation", comp.EditorTessProgress);
                if (comp.IsEditorTessDone)
                    break;
            }
            EditorUtility.DisplayProgressBar("saving data", "processing", 1f);
            comp.EditorTessEnd();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        GUILayout.Space(15);
        GUILayout.Box("Generate MTSceneObject Resource");
        DrawSceneObjectRootArray(serializedObject, "MTSceneObjectRootList");
        if (GUILayout.Button("Step 2 : MTSceneObject Divide"))
        {
            comp.DivideSceneObject();
        }

        GUILayout.Space(15);
        GUILayout.Box("Preview : Terrain Mesh (Bake Lightmap)");
        string[] lodOptions = new string[lodCount];
        for (int i = 0; i < lodCount; i++)
            lodOptions[i] = "LOD " + i;
        showMeshLodLevel = EditorGUILayout.Popup("Show Mesh Lod Level", showMeshLodLevel, lodOptions);
        if (GUILayout.Button("Step 3-1 : CreatePreview — Terrain Mesh"))
        {
            comp.EditorCreateTerrainPreview();
            comp.OnMeshLodLevelChange(preShowMeshLodLevel);
        }
        if (GUILayout.Button("Step 3-2 : CreatePreview — SceneObject"))
        {
            comp.EditorCreateSceneObjectPreview();
        }
        if (GUILayout.Button("ClearPreview"))
        {
            comp.EditorClearPreview();
        }
        if (preShowMeshLodLevel != showMeshLodLevel)
        {
            comp.OnMeshLodLevelChange(showMeshLodLevel);
        }
        preShowMeshLodLevel = showMeshLodLevel;

        GUILayout.Space(15);
        GUILayout.Box("Lightmap Info Collect");

        comlightMapLodLevel = EditorGUILayout.Popup("Baked Lightmap Lod Level", comlightMapLodLevel, lodOptions);

        if (GUILayout.Button("Step 4 : Collect & Refresh LightmapInfo"))
        {
            comp.EditorCRLightmapInfo(comlightMapLodLevel);
        }

        GUILayout.Space(15);
        GUILayout.Box("Refresh Material Assets");
        if (GUILayout.Button("Refresh Materials"))
        {
            comp.RefreshMaterialAssets();
        }

        // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }
}