using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{

    protected override void Initialize()
    {
        Debug.Log("GameManager Initilize");
    }
}
