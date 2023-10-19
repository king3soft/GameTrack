using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class Demo : MonoBehaviour
{
    private ProfilerRecorder profilerRecorder;
    // Start is called before the first frame update
    void Start()
    {
        GTrackSDK.GameTrack_S1("Demo", "Start");
        GTrackSDK.GameTrack_S2("Resource", "Load", "Role1");
        GTrackSDK.GameTrack_S2("Resource", "Load", "Role2");
        profilerRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Gui, "Layout");
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log($"{profilerRecorder.LastValue}");
        //profilerRecorder.
        GTrackSDK.GameTrack_S1("Demo", "Update");
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 40, 100, 30), "Click Me"))
        {
            MinioUtils.Upload($"{Application.persistentDataPath}/track_data/1697685897.zip");
        }
    }

    void Destory()
    {
        GTrackSDK.GameTrack_S1("Demo", "Destory");
        GTrackSDK.GameTrack_S2("Resource", "Destory", "Role1");
        GTrackSDK.GameTrack_S2("Resource", "Destory", "Role2");
    }
}
