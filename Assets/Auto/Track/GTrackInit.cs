
using System;
using System.Collections;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GTrackInit
{
    [Serializable]
    public class GTrackConf {
        public bool enable;
        public string[] enTrackers;
    }
    
    [Serializable]
    class UploadIpRes {
        public int code;
        public string msg;
        public GTrackConf data;
    }
    
    public static IEnumerator Init(GameObject mntObject)
    {
        var localIP = GetLocalIPAddress();
        StringBuilder baseInfo = new StringBuilder();
        baseInfo.Append("{");
        baseInfo.Append($"\"ip\":\"{localIP}\"");
        baseInfo.Append("}");
        string jsonData = baseInfo.ToString();
        
        //crash eye add ip data 
        //Crasheye.AddExtraData("localIP", localIP);
        
        using (UnityWebRequest www = UnityWebRequest.Put("http://10.11.67.131:8888/api/device/option", jsonData))
        {
            www.timeout = 3; //3s
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[GTrack]Response: " + www.downloadHandler.text);
                try
                {
                    var res = JsonUtility.FromJson<UploadIpRes>(www.downloadHandler.text);
                    if (res is { code: 0 } && res.data.enable)
                    {
                        //need capture
                        mntObject.AddComponent<GTrackSDK>()?.Init(res.data.enTrackers);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception:{e}");
                }
            }
            else
            {
                Debug.LogError("Error uploading data to master URL: " + www.error);
                // Attempt upload using slave URL
                // const string slaveUrl = "";
                // using (var www2 = UnityWebRequest.Put(slaveUrl, jsonData))
                // {
                //     www2.timeout = 3; // 3s
                //     www2.SetRequestHeader("Content-Type", "application/json");
                //     yield return www2.SendWebRequest();
                //     if (www2.result == UnityWebRequest.Result.Success)
                //     {
                //         Debug.Log("Response from slave URL: " + www2.downloadHandler.text);
                //         var res = JsonUtility.FromJson<UploadIpRes>(www2.downloadHandler.text);
                //         if (res is { code: 0 } && res.data.enable)
                //         {
                //             //need capture
                //             mntObject.AddComponent<GTrackSDK>();
                //         }
                //     }
                //     else
                //     {
                //         Debug.LogError("Error uploading data to slave URL: " + www2.error);
                //     }
                // }
            }
        }
    }

    private static string GetLocalIPAddress()
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