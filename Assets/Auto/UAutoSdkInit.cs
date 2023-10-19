using System;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class UAutoInit : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitUAutoSdk()
    {
#if !UNITY_EDITOR
        try
        {
            GameObject uautosdk = new GameObject("UAutosdk");
            uautosdk.AddComponent<UAutoSDK.UAutoSdkInit>();
            DontDestroyOnLoad(uautosdk);
            Debug.Log("InitUAutoSdk(UGUI)");
            UAutoSDK.XDirectory.CreateIfNotExists($"{Application.persistentDataPath}/TestPlus");
        }
        catch(Exception ex)
        {
            Log.Info(string.Format("===== start uautosdk error:{0} =====", ex.Message));
        }
#else
        Debug.Log("Fake InitUAutoSdk");
#endif
    }
}

namespace UAutoSDK
{
    public class UAutoSdkInit : MonoBehaviour
    {
        private StringBuilder response = new StringBuilder();
        UAutoRuner _runner = null;
        void Awake()
        {
            _runner = gameObject.AddComponent<UAutoRuner>();
            try
            {
                if (_runner != null)
                {
                    _runner.Init();
                    _runner.m_Handlers.addMsgHandler("EnableGrack", strings =>
                    {
                        gameObject.AddComponent<GTrackSDK>();
                        return "EnableGrack(true)";
                    });
                    _runner.m_Handlers.addMsgHandler("DumpPlayerInfo", strings =>
                    {
                        //StartCoroutine(new PlayerDumper().DumpAllPlayer());
                        return $"DumpPlayerInfo";
                    });
                    _runner.Run();
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("[ERROR] UAutoSDk Reg MsgHandler Error. {0}", e.ToString());
            }
        }

        void OnDestroy()
        {
            response = null;
        }
        
        //用来方便测试自己的注册功能
        public void HandleMsg(string args)
        {
            _runner.m_Handlers.HandleMessage(args);
        }

        //注册自动化点击UI的回调
        public void AddTapObjectCallback(TapObjectCallback callback)
        {
            _runner.OnTapObject += callback;
        }
    }
    
    public static class XDirectory
    {
        /// <summary>
        /// If Director is not exists , create it. | 如果目录不存在，则创建
        /// </summary>
        /// <param name="path"></param>
        public static void CreateIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void CleanDir(string dir)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
        }
    }
}
