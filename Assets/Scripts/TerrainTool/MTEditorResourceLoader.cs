#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MTEditorResourceLoader
{
    public static T LoadAssetAtPath<T>(string assetPath) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    public static T[] LoadAllAssetsAtPath<T>(string assetFolder) where T : Object
    {
        string searchType = "";
        searchType = "t:" + typeof(T).Name;

        string[] guids = AssetDatabase.FindAssets(searchType, new string[] { assetFolder });
        List<T> objs = new List<T>();

        for (int i = 0; i < guids.Length; ++i)
        {
            T obj = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (obj != null) objs.Add(obj);
        }

        return objs.ToArray();
    }
}
#endif