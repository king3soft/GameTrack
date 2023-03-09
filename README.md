# GameTrack_Demo

Dev Demo

## Features





## Quick Start

### Install

1. 复制 `GameTrack` 文件夹到项目中。
2. 挂载 `GameTrackSDK` 组件到全局 GameObject（DontDestroyOnLoad）。

### Setup

1. 设置 `Minio Bucket` ，表示数据上传到该 Minio Bucket。

2. 设置监控自动化操作：

   1. 替换项目中的自动化 SDK：`UAutoSDK.dll` 和 `UAutoSdkInit.cs`
   2. 将挂载 `UAutoSdkInit` 组件的 GameObject 赋值给 `UAuto Game Object`
   3. 取消 `GameTrackSDK` 以下代码的注释：

   ```C#
   // Track UAuto Tag Object
   if (uAutoGameObject != null)
   {
   	UAutoSDK.UAutoSdkInit uauto = uautoGameObject.GetComponent<UAutoSDK.UAutoSdkInit>();
   	uauto?.AddTapObjectCallback(UserClickTrack);
   }
   ```



