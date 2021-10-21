using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FBXPostprocess : AssetPostprocessor
{
    private void OnPostprocessModel()
    {
        var expectedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/custom_m.mat");

        using (var so = new SerializedObject(assetImporter))
        {
            var materials = so.FindProperty("m_Materials");
            var externalObjects = so.FindProperty("m_ExternalObjects");

            for (int materialIndex = 0; materialIndex < materials.arraySize; materialIndex++)
            {
                var id = materials.GetArrayElementAtIndex(materialIndex);
                var name = id.FindPropertyRelative("name").stringValue;
                var type = id.FindPropertyRelative("type").stringValue;
                var assembly = id.FindPropertyRelative("assembly").stringValue;

                SerializedProperty materialProperty = null;

                for (int externalObjectIndex = 0; externalObjectIndex < externalObjects.arraySize; externalObjectIndex++)
                {
                    var currentSerializedProperty = externalObjects.GetArrayElementAtIndex(externalObjectIndex);
                    var externalName = currentSerializedProperty.FindPropertyRelative("first.name").stringValue;
                    var externalType = currentSerializedProperty.FindPropertyRelative("first.type").stringValue;

                    if (externalType == type && externalName == name)
                    {
                        materialProperty = currentSerializedProperty.FindPropertyRelative("second");
                        break;
                    }
                }

                if (materialProperty == null)
                {
                    var lastIndex = externalObjects.arraySize++;
                    var currentSerializedProperty = externalObjects.GetArrayElementAtIndex(lastIndex);
                    currentSerializedProperty.FindPropertyRelative("first.name").stringValue = name;
                    currentSerializedProperty.FindPropertyRelative("first.type").stringValue = type;
                    currentSerializedProperty.FindPropertyRelative("first.assembly").stringValue = assembly;
                    currentSerializedProperty.FindPropertyRelative("second").objectReferenceValue = expectedMaterial;
                }
                else
                {
                    materialProperty.objectReferenceValue = expectedMaterial;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        assetImporter.SaveAndReimport();
    }
}