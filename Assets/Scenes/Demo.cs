using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameTrackSDK.GameTrack_S1("Demo", "Start");
        GameTrackSDK.GameTrack_S2("Resource", "Load", "Role1");
        GameTrackSDK.GameTrack_S2("Resource", "Load", "Role2");

    }

    // Update is called once per frame
    void Update()
    {
        GameTrackSDK.GameTrack_S1("Demo", "Update");
    }

    void Destory()
    {
        GameTrackSDK.GameTrack_S1("Demo", "Destory");
        GameTrackSDK.GameTrack_S2("Resource", "Destory", "Role1");
        GameTrackSDK.GameTrack_S2("Resource", "Destory", "Role2");
    }
}
