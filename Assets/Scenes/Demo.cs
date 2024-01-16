using System;
using System.IO;
using Unity.Profiling;
using UnityEngine;

public class Demo : MonoBehaviour
{
    private ProfilerRecorder profilerRecorder;

    private void Awake()
    {
        // StartCoroutine(GTrackInit.Init(gameObject));
        
        //var enTrackers = new string[] { "PlayerTracker", "UITracker" };
        // gameObject.AddComponent<GTrackSDK>()?.Init(enTrackers);
        
        Debug.LogFormat($"{sizeof(long)}");
        
        if (File.Exists($"{Application.persistentDataPath}/GTrack.json"))
        {
            try
            {
                var sGTrack = File.ReadAllText($"{Application.persistentDataPath}/GTrack.json");
                GTrackInit.GTrackConf conf = JsonUtility.FromJson<GTrackInit.GTrackConf>(sGTrack);
                gameObject.AddComponent<GTrackSDK>()?.Init(conf.enTrackers);
                Debug.Log("InitGTrackSDK(GTrack.json)");
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception:{e}");
            }
        }
        else
        {
            StartCoroutine(GTrackInit.Init(gameObject));    
        }
    }

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
