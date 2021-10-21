using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MTSceneSlicer : MonoBehaviour
{
    public Terrain TileTerrain;

    public GameObject[] SplitSceneObjects;

    private void OnEnable()
    {
        TileTerrain = GetComponentInChildren<Terrain>();
        SplitSceneObjects = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            SplitSceneObjects[i] = transform.GetChild(i).gameObject;
        }
    }
}
