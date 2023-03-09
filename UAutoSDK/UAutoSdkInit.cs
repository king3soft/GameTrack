using System;
using System.Text;
using LuaInterface;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UAutoSDK
{
    public class UAutoSdkInit : MonoBehaviour
    {
        
#if !GAME_RELEASE
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
#endif

        //由lua脚本AutomatedTesting.lua 在游戏逻辑运行前注册 
        public void AddMsgHandler(string name, LuaFunction luaFunction)
        {
#if !GAME_RELEASE
            _runner.m_Handlers.addMsgHandler(name, strings =>
            {
                response.Clear();
                
                var code = luaFunction.Invoke<string[], int>(strings);
                response.Append($"\"code\": {code}");
                return response.ToString();
            });
#endif
        }
        
        //用来方便测试自己的注册功能
        public void HandleMsg(string args)
        {
#if !GAME_RELEASE
            _runner.m_Handlers.HandleMessage(args);
#endif
        }

        //注册自动化点击UI的回调
        public void AddTapObjectCallback(TapObjectCallback callback)
        {
#if !GAME_RELEASE
            _runner.OnTapObject += callback;
#endif
        }
    }
}