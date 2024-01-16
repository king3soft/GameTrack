using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
// ReSharper disable InconsistentNaming

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
    public static extern void GameTrack_ProfInit(string header);
    
    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_ProfUpdate(long[] others, int length);

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
    public static extern void GameTrack_L1(string name, long arg1);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_L2(string name, long arg1, long arg2);

    [DllImport(TRACK_DLL)]
    public static extern void GameTrack_L3(string name, long arg1, long arg2, long arg3);

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
    
    // Init GTrack SDK
    public void Init(string[] enTrackers, bool useLocalTrack=false)
    {
        // UUID
        var localUuid = PlayerPrefs.GetString("track_uuid");
        if (string.IsNullOrEmpty(localUuid))
        {
            localUuid = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("track_uuid", localUuid);
        }
        
        DateTimeOffset beginTime = DateTimeOffset.UtcNow;
        try
        {
            // MetaInfo
            MetaInfo metaInfo = new MetaInfo
            {
                identifier = Application.identifier,
                identifierCode = $"{(uint)Application.identifier.GetHashCode()}",
                version = Application.version,
                operatingSystem = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                processorType = SystemInfo.processorType,
                systemMemorySize = $"{SystemInfo.systemMemorySize}MB",
                graphicsDeviceID = SystemInfo.graphicsDeviceID.ToString(),
                graphicsDeviceVendor = SystemInfo.graphicsDeviceName,
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                graphicsMemorySize = $"{SystemInfo.graphicsMemorySize}MB",
                ipAddress = GetLocalIPAddress(),
                localUUID = localUuid,
                useLocalTrack = useLocalTrack,
                enTrackers = enTrackers,
                beginTime = beginTime.ToUnixTimeMilliseconds()
            };
            var sMetaInfo = JsonUtility.ToJson(metaInfo);
            
            // Init
            string joinedTrackers = string.Join(",", enTrackers);
            var pLogZipFile = GameTrack_Init(Application.persistentDataPath, localUuid, joinedTrackers);
            _curLogZipFile = Marshal.PtrToStringUni(pLogZipFile);
            if (String.IsNullOrEmpty(_curLogZipFile))
            {
                // Have No Write Permissions
                Debug.LogError("GameTrack Init Failed, Have No Write Permissions");
                _inited = false;
                return;
            }
        
            // Add MetaInfo
            File.WriteAllText($"{_curLogZipFile}/meta.json", sMetaInfo);
            
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
            GTracks.RegisterTracker(new UITracker());
            //GTracks.Get<UITracker>()?.EnableTrack(true);
            
            GTracks.RegisterTracker(new SceneLoadTracker());
            GTracks.Get<SceneLoadTracker>()?.EnableTrack(true);
            
            // PlayerTracker
            GTracks.RegisterTracker(new PlayerTracker());
            
            // LogMaskTracker
            GTracks.RegisterTracker(new LogMaskTracker());
            // SSMTracker
            // GTracks.RegisterTracker(new SSMTracker());
            if(enTrackers != null)
            {
                foreach (var t in enTrackers)
                {
                    try
                    {
                        if (t.Equals("ProfilerTracker")) {
                            gameObject.AddComponent<ProfilerTracker>();
                            continue;
                        }
                        GTracks.Get(t)?.EnableTrack(true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GTrack]EnableTrack Exception:{e}");
                    }
                }
            }

            // Upload remain data 
            string trackDir = $"{Application.persistentDataPath}/track_data";
            ThreadPool.QueueUserWorkItem((System.Object stateInfo) => {
                UploadFailedTrackData(trackDir);
            });
            
            _inited = true;
            var ts = beginTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            //Crasheye.AddExtraData("beginTime", ts);
            Debug.LogFormat("[GTrack]GTrack Init Succeed. beginTime: {0}", ts);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    
    void UploadFailedTrackData(string trackDir)
    {
        try
        {
            var curFileDir = Path.GetFileName(_curLogZipFile);
            foreach (var dir in Directory.GetDirectories(trackDir))
            {
                //dir.get
                string folderName = Path.GetFileName(dir);
                if (string.Equals(curFileDir, folderName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string zipFileDir = $"{trackDir}/{folderName}";
                string zipFilePath = $"{zipFileDir}.zip";
                ZipDir(zipFileDir);
                bool bOk = MinioUtils.Upload(zipFilePath);
                if (bOk && !string.IsNullOrEmpty(zipFilePath))
                {
                    // Directory.Delete(zipFileDir, true);
                    // File.Delete(zipFilePath);
                }
            }

        }
        catch (Exception e)
        {
            // ignored
        }
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
                Debug.LogError($"Upload {_curLogZipFile}.zip Error");
            }
            else
            {
                if (!string.IsNullOrEmpty(_curLogZipFile))
                {
                    // Directory.Delete(_curLogZipFile, true);
                    // File.Delete($"{_curLogZipFile}.zip");
                }
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
    [System.Serializable]
    private class MetaInfo
    {
        public string identifier;
        public string identifierCode;
        public string version;
        public string operatingSystem;
        public string deviceModel;
        public string deviceName;
        public string processorType;
        public string systemMemorySize;
        public string graphicsDeviceID;
        public string graphicsDeviceVendor;
        public string graphicsDeviceVersion;
        public string graphicsMemorySize;
        public string ipAddress;
        public string localUUID;
        public bool useLocalTrack;
        public string[] enTrackers;
        public long beginTime;
    }
}