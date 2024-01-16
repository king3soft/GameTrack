using System;
using System.Collections.Generic;
using System.IO;
// using Cysharp.Threading.Tasks;
using GTrack.Internal;
// using MTAssets.IngameLogsViewer;
using UnityEngine;

namespace GTrack.Internal
{
    #region IOC

    public class IOCContainer
    {
        private Dictionary<Type, object> mInstances = new Dictionary<Type, object>();

        public void Register<T>(T instance)
        {
            var key = typeof(T);

            if (mInstances.ContainsKey(key))
            {
                mInstances[key] = instance;
            }
            else
            {
                mInstances.Add(key, instance);
            }
        }

        public T Get<T>() where T : class
        {
            var key = typeof(T);

            if (mInstances.TryGetValue(key, out var retInstance))
            {
                return retInstance as T;
            }

            return null;
        }
    }
    #endregion

    #region AbsTracker
    public interface ITracker
    {
    }
    
    public abstract class AbsTracker : ITracker
    {
        protected bool m_Enable = false;
        public virtual void EnableTrack(bool enable)
        {
            m_Enable = enable;
        }
        public bool Enabled => m_Enable;
    }
    #endregion
}

//export to public without using it
public sealed class UITracker : AbsTracker
{
    public void TrackLoadRes(string name, string arg1, string arg2, string arg3)
    {
        if (m_Enable)
        {
            GTrackSDK.GameTrack_S3(name, arg1, arg2, arg3);
        }
    }
    
    public void TrackUnloadRes(string name, string arg1)
    {
        if (m_Enable)
        {
            GTrackSDK.GameTrack_S1(name, arg1);
        }
    }
    
    public void TrackLoadUIRes(string name, string arg1, string arg2)
    {
        if (m_Enable)
        {
            GTrackSDK.GameTrack_S2(name, arg1, arg2);
        }
    }
}

public sealed class PlayerTracker : AbsTracker
{
    private Vector3 lastPos = Vector3.zero;
    // public async UniTask UpdatePos()
    // {
    //     while (m_Enable)
    //     {
    //         var player = FamilyMgr.m_myFamily?.GetActivePlayer();
    //         if(player != null && player.IsInScene())
    //         {
    //             var curPos = player.GetPosition();
    //             float dist = Vector3.Distance(lastPos, curPos);
    //             if (dist > 2) //移动距离大米2米
    //             {
    //                 lastPos = curPos;
    //                 GTrackSDK.GameTrack_F3("player.pos", curPos.x, curPos.y, curPos.z);
    //             }
    //             await UniTask.Delay(TimeSpan.FromSeconds(3));
    //         }
    //         else
    //         {
    //             await UniTask.Delay(TimeSpan.FromSeconds(5));
    //         }    
    //     }
    // }

    public override void EnableTrack(bool enable)
    {
        // ignore repeatedly enableTrack
        // if (enable && m_Enable == false)
        // {
        //     base.EnableTrack(true);
        //     UpdatePos().Forget();
        // }
    }
}

public sealed class SceneLoadTracker : AbsTracker
{
    public void Begin(string name)
    {
        if (m_Enable)
        {
            GTrackSDK.GameTrack_S1("Scene.BeginLoad", name);
        }
    }
    
    public void End(string name)
    {
        if (m_Enable)
        {
            GTrackSDK.GameTrack_S1("Scene.EndLoad", name);
        }
    }
}

public sealed class SSMTracker : AbsTracker
{
    public void DumpEvent()
    {
        if (m_Enable)
        {
            
        }
    }
}

public sealed class LogMaskTracker : AbsTracker
{
    // private IngameLogsViewer _ingameLogsViewer;
    // public LogMaskTracker()
    // {
    //     _ingameLogsViewer = GameObject.FindObjectOfType<MTAssets.IngameLogsViewer.IngameLogsViewer>();
    // }
    // public override void EnableTrack(bool enable)
    // {
    //     base.EnableTrack(enable);
    //     if (enable)
    //     {
    //         _ingameLogsViewer.DisableLogMessageReceived();
    //     }
    //     else
    //     {
    //         _ingameLogsViewer.EnableLogMessageReceived();
    //     }
    // }
}

public static class GTracks
{
    private static GTrack.Internal.IOCContainer tContainer = new GTrack.Internal.IOCContainer();
    private static Dictionary<string, AbsTracker> sContainer = new Dictionary<string, AbsTracker>(); 
    public static TTracker Get<TTracker>() where TTracker : class, GTrack.Internal.ITracker => tContainer.Get<TTracker>();

    public static AbsTracker Get(string k)
    {
        return sContainer.TryGetValue(k, out var result) ? result : null;
    }
    
    public static void RegisterTracker<TTracker>(TTracker tracker) where TTracker : GTrack.Internal.AbsTracker, new()
    {
        tContainer.Register(tracker);
        sContainer.Add(typeof(TTracker).ToString(), tracker);
    }
}