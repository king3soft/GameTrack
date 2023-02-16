using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameTrackSDK : MonoBehaviour
{
    [DllImport("track")]
    private static extern IntPtr GameTrack_Init(string persistentDataPath, string track_uuid, string gpu_vendor);
    
    [DllImport("track")]
    private static extern void GameTrack_Update(float unscaledDeltaTime, int targetFrameRate);

    [DllImport("track")]
    private static extern void GameTrack_Flush();
    
    [DllImport("track")]
    private static extern void GameTrack_Event(string eventName);
    
    [DllImport("track")]
    private static extern void GameTrack_Scene(string scene_name);
    
    [DllImport("track")]
    private static extern IntPtr /* char const * */ GameTrack_GetToken();

    private UGUITracker uGUITracker;

    // Init GamePerf SDK
    private void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // UUID
        var localUUID = PlayerPrefs.GetString("track_uuid");
        if (string.IsNullOrEmpty(localUUID))
        {
            localUUID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("track_uuid", localUUID);
        }
        
        // BaseInfo
        StringBuilder baseInfo = new StringBuilder();
        baseInfo.AppendFormat("{0},{1},{2},{3},{4},{5},{6}", Application.identifier, SystemInfo.operatingSystem, SystemInfo.deviceModel, SystemInfo.deviceName, SystemInfo.graphicsDeviceVendor, SystemInfo.deviceName, SystemInfo.graphicsDeviceVersion);
        
        // Init
        var _logFile = GameTrack_Init(Application.persistentDataPath, localUUID, baseInfo.ToString());
        string logFile = Marshal.PtrToStringAnsi(_logFile);
        // Upload Last Files
        StartCoroutine(UploadData(logFile));

        // Track Scene
        SceneManager.sceneLoaded += SceneLoadedTrack;
        // Track UI Event
        uGUITracker = new UGUITracker(this);
        uGUITracker.Run();
        #endif
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        GameTrack_Update(Time.unscaledDeltaTime, Application.targetFrameRate);
#endif
    }
    
    // Track User Click 
    public void UserClickTrack(string eventName)
    {
        //Demo: GameTrack_Event("Button1"); 
        //Todo: @lixiaofeng
        GameTrack_Event(eventName); 
    }

    // Track Scene
    public void OnSceneChange(string sceneName)
    {
        GameTrack_Scene(sceneName);
    }

    // Save GamePerf
    // public void Save()
    // {
    //     GameTrack_Save(Application.persistentDataPath + "/tmp.txt");
    // }

    private void OnApplicationPause(bool pauseStatus)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // enter background
        if (pauseStatus == true)
        {
            GameTrack_Flush();
        }
#endif
    }
    
    IEnumerator UploadData(string currentFile)
    {
        var token = Marshal.PtrToStringAnsi(GameTrack_GetToken());
        DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath + "/track_data");
        foreach (FileInfo file in directory.GetFiles())
        {
            if(file.FullName.Equals(currentFile))
                continue;
            if (!File.Exists(file.FullName))
            {
                Debug.LogFormat("File does not exist: {0}", file);
                continue;
            }
            WWWForm form = new WWWForm();
            form.AddField("token", token);
            form.AddField("key", file.Name);
            form.AddField("fileName", file.Name);

            byte[] data = File.ReadAllBytes(file.FullName);
            form.AddBinaryData("file", data, file.Name, "text/plain");

            UnityWebRequest www = UnityWebRequest.Post("https://up-z2.qiniup.com", form);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("upload file: {0} error: {1} / {2}", file, www.error, www.result);
            }
            else
            {
                string text = www.downloadHandler.text;
                Debug.Log("upload succeed:" + text);
                File.Delete(file.FullName);
            }
        }
    }

    private void SceneLoadedTrack(Scene scene, LoadSceneMode mode)
    {
        OnSceneChange(scene.name);
    }

 }

public class UGUITracker
{
    private GameTrackSDK gameTrackSDK;

    private int curTouchCount = 0;

    public UGUITracker(GameTrackSDK gameTrackSDK)
    {
        this.gameTrackSDK = gameTrackSDK;
    }

    public void Run()
    {
        gameTrackSDK.StartCoroutine(ClickTrack());
    }

    private System.Collections.IEnumerator ClickTrack()
    {
        while (true)
        {
            if (IsPressDown())
            {
                Vector2 pos = Input.mousePosition;
                Touch touch = new Touch { position = pos };
                PointerEventData pointerEventData = MockUpPointerInputModule.GetPointerEventData(touch);
                if (pointerEventData.pointerPress != null)
                {
                    GameObject curPressGameObject = pointerEventData.pointerPress;
                    Selectable selectable = curPressGameObject.GetComponent<Selectable>();
                    gameTrackSDK.UserClickTrack(GetGameObjectPath(curPressGameObject));
                }
            }

            curTouchCount = Input.touchCount;
            yield return null;
        }
    }

    private bool IsPressDown()
    {
        if (Input.GetMouseButtonDown(0))
            return true;
        if (Input.touchCount == 1 && curTouchCount == 0)
            return true;
        return false;
    }

    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";
        string path = "/" + obj.name;
        Transform parentTransform = obj.transform.parent;
        while (parentTransform != null)
        {
            path = "/" + parentTransform.name + path;
            parentTransform = parentTransform.parent;
        }
        return path;
    }
}