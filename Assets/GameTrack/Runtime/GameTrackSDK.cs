using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class GameTrackSDK : MonoBehaviour
{
    [DllImport("track")]
    private static extern IntPtr GameTrack_Init(string persistentDataPath, string track_uuid, string base_info);
    
    [DllImport("track")]
    private static extern void GameTrack_Update(float unscaledDeltaTime, int targetFrameRate);

    [DllImport("track")]
    private static extern void GameTrack_Flush();
    
    [DllImport("track")]
    public static extern void GameTrack_Event(string eventName);
    
    [DllImport("track")]
    private static extern void GameTrack_Scene(string sceneName);
    
    [DllImport("track")]
    private static extern IntPtr /* char const * */ GameTrack_GetToken();

    // Init GamePerf SDK
    private void Start()
    {
#if UNITY_ANDROID //&& !UNITY_EDITOR
        // UUID
        var localUUID = PlayerPrefs.GetString("track_uuid");
        if (string.IsNullOrEmpty(localUUID))
        {
            localUUID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("track_uuid", localUUID);
        }
        
        // BaseInfo
        StringBuilder baseInfo = new StringBuilder();
        baseInfo.AppendFormat("{0}&{1}&{2}&{3}&{4}&{5}&{6}", Application.identifier.GetHashCode(), 
            Application.identifier, 
            SystemInfo.operatingSystem, 
            SystemInfo.deviceModel, 
            SystemInfo.deviceName, 
            SystemInfo.graphicsDeviceVendor, 
            SystemInfo.graphicsDeviceVersion);
        
        // Init
        var _logFile = GameTrack_Init(Application.persistentDataPath, localUUID, baseInfo.ToString());
        string logFile = Marshal.PtrToStringAnsi(_logFile);
        // Upload Last Files
        // StartCoroutine(UploadData(logFile));
        // send to minio
        StartCoroutine(MinioUpdateFile(logFile));
        // send to web
        // StartCoroutine(WebPostUpdateFile(logFile));

        // Track Scene
        SceneManager.sceneLoaded += SceneLoadedTrack;
        
        // Track UI Event
        gameObject.AddComponent<UGUITracker>();
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
        GameTrack_Event(eventName); 
    }

    // Track Scene
    private void SceneLoadedTrack(Scene scene, LoadSceneMode mode)
    {
        GameTrack_Scene(scene.name);
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

    IEnumerator MinioUpdateFile(string currentFile)
    {
        DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath + "/track_data");
        foreach (FileInfo file in directory.GetFiles())
        {
            if (file.FullName.Equals(currentFile))
                continue;
            if (file.Length == 0)
            {
                File.Delete(file.FullName);
                Debug.LogFormat("File size is 0 delete: {0}", file);
                continue;
            }
            if (!File.Exists(file.FullName))
            {
                Debug.LogFormat("File does not exist: {0}", file);
                continue;
            }
            UnityWebRequest www = MinioUtils.CreateUploadFileRequest("gametrack", file.FullName);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("minio upload file: {0} error: {1} / {2}", file, www.error, www.result);
            }
            else
            {
                string text = www.downloadHandler.text;
                Debug.Log("minio upload succeed:" + text);
                File.Delete(file.FullName);
            }
        }
    }

    IEnumerator WebPostUpdateFile(string currentFile)
    {
        DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath + "/track_data");
        foreach (FileInfo file in directory.GetFiles())
        {
            if (file.FullName.Equals(currentFile))
                continue;
            if (file.Length == 0)
            {
                File.Delete(file.FullName);
                Debug.LogFormat("File size is 0 delete: {0}", file);
                continue;
            }
            if (!File.Exists(file.FullName))
            {
                Debug.LogFormat("File does not exist: {0}", file);
                continue;
            }
            UnityWebRequest www = HTTPUtils.CreateUploadFileRequest(file.FullName, file.Name);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("web post upload file: {0} error: {1} / {2}", file, www.error, www.result);
            }
            else
            {
                string text = www.downloadHandler.text;
                Debug.Log("web post upload succeed:" + text);
                File.Delete(file.FullName);
            }
        }
    }

    IEnumerator UploadData(string currentFile)
    {
        var token = Marshal.PtrToStringAnsi(GameTrack_GetToken());
        DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath + "/track_data");
        foreach (FileInfo file in directory.GetFiles())
        {
            if(file.FullName.Equals(currentFile))
                continue;
            if (file.Length == 0)
            {
                File.Delete(file.FullName);
                Debug.LogFormat("File size is 0 delete: {0}", file);
                continue;
            }
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
}