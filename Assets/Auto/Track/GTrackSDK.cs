using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GTrackSDK : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
	const string TRACK_DLL = "__Internal";
#else
    const string TRACK_DLL = "track";
#endif
    [DllImport(TRACK_DLL)]
    private static extern IntPtr GameTrack_Init(string persistentDataPath, string trackUuid, string baseInfo);
    
    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_Update(float unscaledDeltaTime, int targetFrameRate);

    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_OnDestroy();

    
    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_Flush();

    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_Pause(bool bPause);
    
    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_OnQuit();
    
    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_Event(string eventName);
    
    [DllImport(TRACK_DLL)]
    private static extern void GameTrack_Scene(string sceneName);
    
    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_S1(string name, string arg1);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_S2(string name, string arg1, string arg2);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_S3(string name, string arg1, string arg2, string arg3);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_I1(string name, int arg1);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_I2(string name, int arg1, int arg2);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_I3(string name, int arg1, int arg2, int arg3);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_F1(string name, float arg1);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_F2(string name, float arg1, float arg2);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_F3(string name, float arg1, float arg2, float arg3);

    [DllImport(TRACK_DLL)]
    private static extern IntPtr /* char const * */ GameTrack_GetToken();

    [DllImport(TRACK_DLL)]
    public static extern bool ZipDir(string dirPath);
    
    
    [DllImport(TRACK_DLL)]
    public static extern bool UploadFile(string url, string bucket, string minioPath, string filePath, string authorization, string data);
    
    private bool _inited;
    private bool _pause = false;
    
    public GameObject uAutoGameObject = null;

    private string _curLogZipFile = null;
    // Init GamePerf SDK
    private void Start()
    {
        // UUID
        var localUUID = PlayerPrefs.GetString("track_uuid");
        if (string.IsNullOrEmpty(localUUID))
        {
            localUUID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("track_uuid", localUUID);
        }
        
        // BaseInfo
        StringBuilder baseInfo = new StringBuilder();
        baseInfo.Append("{");
        baseInfo.Append($"\"identifier\":\"{Application.identifier}\",");
        baseInfo.Append($"\"identifierCode\":\"{(uint)Application.identifier.GetHashCode()}\",");
        baseInfo.Append($"\"operatingSystem\":\"{SystemInfo.operatingSystem}\",");
        baseInfo.Append($"\"deviceModel\":\"{SystemInfo.deviceModel}\",");
        baseInfo.Append($"\"deviceName\":\"{SystemInfo.deviceName}\",");
        baseInfo.Append($"\"graphicsDeviceVendor\":\"{SystemInfo.graphicsDeviceVendor}\",");
        baseInfo.Append($"\"graphicsDeviceVersion\":\"{SystemInfo.graphicsDeviceVersion}\",");
        baseInfo.Append($"\"ipAddress\":\"{GetLocalIPAddress()}\",");
        baseInfo.Append($"\"beginTime\":\"{DateTime.Now.ToFileTime()}\"");
        baseInfo.Append("}");
        
        // Init
        // Debug.Log(baseInfo);
        var pLogZipFile = GameTrack_Init(Application.persistentDataPath, localUUID, baseInfo.ToString());
        _curLogZipFile = Marshal.PtrToStringUni(pLogZipFile);
        if (String.IsNullOrEmpty(_curLogZipFile))
        {
            // Have No Write Permissions
            Debug.LogError("GameTrack Init Failed, Have No Write Permissions");
            _inited = false;
            return;
        }
        
        // Add MetaInfo
        File.WriteAllText($"{_curLogZipFile}/meta.json", baseInfo.ToString());
        
        //Debug.Log(_curLogZipFile);
        
        // Upload Last Files
        // StartCoroutine(UploadData(logFile));
        
        // send to minio
        // StartCoroutine(MinioUpdateFile(logFile));
        
        // send to web
        // StartCoroutine(WebPostUpdateFile(logFile));
        
        // Track Scene
        SceneManager.sceneLoaded += SceneLoadedTrack;
        
        // Track UI Event
        gameObject.AddComponent<UGUITracker>();

        // Track UAuto Tag Object
        /*
        if (uAutoGameObject != null)
        {
            UAutoSDK.UAutoSdkInit uauto = uAutoGameObject.GetComponent<UAutoSDK.UAutoSdkInit>();
            uauto?.AddTapObjectCallback(UserClickTrack);
        }
        */
        Debug.Log("GameTrack Inited");
        _inited = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (_inited && !_pause)
        {
            GameTrack_Update(Time.unscaledDeltaTime, Application.targetFrameRate);
        }
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

    private void OnApplicationPause(bool pauseStatus)
    {
        _pause = pauseStatus;
        // enter background
        GameTrack_Pause(pauseStatus);
    }

    private void OnDestroy()
    {
        Debug.Log("GameTrack_OnApplicationQuit");
        if (_inited)
        {
            // shutdown the log file
            GameTrack_OnDestroy();
            ZipDir(_curLogZipFile);
            bool bOk = MinioUtils.Upload($"{_curLogZipFile}.zip");
            if (!bOk)
            {
                Debug.LogError($"Upload {_curLogZipFile}.zip");
            }
        }
    }
    
    IEnumerator UpdateMinioFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            UnityWebRequest www = MinioUtils.CreateUploadFileRequest(MinioUtils.minioBucket, filePath);
            yield return www.SendWebRequest();
#if UNITY_2020_3_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("minio upload file: {0} error: {1} / {2}", filePath, www.error, www.result);
            }
#else
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.LogFormat("minio upload file: {0} error: {1}", file, www.error);
            }
#endif
            else
            {
                string text = www.downloadHandler.text;
                Debug.Log("minio upload succeed:" + text);
                File.Delete(filePath);
            }
        }
        else
        {
            Debug.LogError($"{filePath} Not Found");
        }
    }

    IEnumerator MinioUpdateFile(string currentFile)
    {
        DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath + "/track_data");
        foreach (FileInfo file in directory.GetFiles())
        {
            if (file.FullName.Contains(currentFile))
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
            UnityWebRequest www = MinioUtils.CreateUploadFileRequest(MinioUtils.minioBucket, file.FullName);
            yield return www.SendWebRequest();
#if UNITY_2020_3_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("minio upload file: {0} error: {1} / {2}", file, www.error, www.result);
            }
#else
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.LogFormat("minio upload file: {0} error: {1}", file, www.error);
            }
#endif
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
#if UNITY_2020_3_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("web post upload file: {0} error: {1} / {2}", file, www.error, www.result);
            }
#else
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.LogFormat("web post upload file: {0} error: {1}", file, www.error);
            }
#endif
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
#if UNITY_2020_3_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogFormat("upload file: {0} error: {1} / {2}", file, www.error, www.result);
            }
#else
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.LogFormat("upload file: {0} error: {1}", file, www.error);
            }
#endif
            else
            {
                string text = www.downloadHandler.text;
                Debug.Log("upload succeed:" + text);
                File.Delete(file.FullName);
            }
        }
    }
    
    private string GetLocalIPAddress()
    {
        IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
        foreach (IPAddress address in addresses)
        {
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return address.ToString();
            }
        }
        return "0.0.0.0";
    }
}