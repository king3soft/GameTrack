using System;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;

public class CallNativeCode : MonoBehaviour {
    public void UWAInit()
    {
        UWAEngine.StaticInit();
    }

    public void GameTrackInit()
    {
        gameObject.AddComponent<GameTrackSDK>();
    }
}
