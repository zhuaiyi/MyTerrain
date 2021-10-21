#define UWA_GOT

using UnityEngine;
using System.Collections;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if UNITY_IPHONE
using UWAPlatform = UWA.IOS;            // Import the iOS package
#elif UNITY_ANDROID
using UWAPlatform = UWA.Android;        // Import the Android package
#elif UNITY_STANDALONE_WIN
using UWAPlatform = UWA.Windows;        // Import the Windows package
#else   // platforms not supported
using UWAPlatform = UWA;
namespace UWA
{
    class GUIWrapper : MonoBehaviour
    {
        public static bool ControlByPoco;
    }
    class UWAEngine
    {
        public static int FrameId;
        public static void StaticInit() { }
        public enum Mode { Test };
        public static void Start(Mode mode) { }
        public static void Stop() { }
        public static void PushSample(string sampleName) { }
        public static void PopSample() { }
        public static void LogValue(string valueName, float value) { }
        public static void LogValue(string valueName, Vector3 value) { }
        public static void LogValue(string valueName, int value) { }
        public static void LogValue(string valueName, bool value) { }
        public static void AddMarker(string valueName) { }
        public static void SetOverrideLuaLib(string luaLib) { }
        public static void Upload(Action<bool> callback, string user, string password, string projectName, int timeLimitS){}
        public static void Upload(Action<bool> callback, string user, string password, int projectId, int timeLimitS){}
        public static void Tag(string tag){}
        public static void SetUIActive(bool active){}
        public static void AssetDumpInOverview(){}
    }
}
#endif

[ExecuteInEditMode]
public class UWA_Launcher : MonoBehaviour {

#if UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern void RegisterPluginLoad();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnLoad()
    {
        RegisterPluginLoad();
    }
#endif

    /// <summary>
    /// Enable this to make UWA GOT controlled by Poco
    /// </summary>
    [Tooltip("Enable this to make UWA GOT controlled by Poco. [Not supported on IL2CPP]")]
    public bool ControlByPoco = false;
    
    void Awake () { Refresh(true); }

#if UNITY_EDITOR
    void OnEnable() { Refresh(true); }
#endif

    private void Refresh(bool removeOthers)
    {
        UWAPlatform.GUIWrapper wrapper = gameObject.GetComponent<UWAPlatform.GUIWrapper>();
        if (wrapper == null)
        {
            wrapper = gameObject.AddComponent<UWAPlatform.GUIWrapper>();
        }
        UWAPlatform.GUIWrapper.ControlByPoco = ControlByPoco;

#if UNITY_EDITOR
        if (removeOthers)
        {
            Component[] coms = gameObject.GetComponents<Component>();
            for (int i = 0; i < coms.Length; i++)
            {
                if (coms[i] != null &&
                    coms[i] != this &&
                    coms[i] != wrapper &&
                    coms[i].GetType() != typeof(Transform))
                    DestroyImmediate(coms[i]);
            }
        }
#endif
    }
}

public class UWAEngine
{
    /// <summary>
    /// [UWA GOT | UWA GPM] This api can be used to initialize the UWA SDK, instead of draging the UWA_Launcher.prefab into your scene.
    /// </summary>
    public static void StaticInit(bool poco = false)
    {
        UWAPlatform.UWAEngine.StaticInit();
        UWAPlatform.GUIWrapper.ControlByPoco = poco;
    }

    /// <summary>
    /// [UWA GOT | UWA GPM] The recorded frame count
    /// </summary>
    public static int FrameId { get { return UWAPlatform.UWAEngine.FrameId; } }

    /// <summary>
    /// [UWA GOT] The profiling mode 
    /// </summary>
    public enum Mode
    {
        Overview = 0,
        Mono = 1,
        Assets = 2,
        Lua = 3,
        Unset = 4,
    }

    /// <summary>
    /// [UWA GOT] This api can be used to start the test with the given mode, instead of pressing the button in GUI panel.
    /// Test can be started only once.
    /// </summary>
    /// <param name="mode"> the profiling mode to be started</param>
    [Conditional("ENABLE_PROFILER")]
    public static void Start(Mode mode)
    {
        UWAPlatform.UWAEngine.Start((UWAPlatform.UWAEngine.Mode)mode);
    }

    /// <summary>
    /// [UWA GOT] This api can be used to stop the test, instead of pressing the button in GUI panel.
    /// Test can be stopped only once.
    /// </summary>
    [Conditional("ENABLE_PROFILER")]
    public static void Stop()
    {
        UWAPlatform.UWAEngine.Stop();
    }


    /// <summary>
    /// [UWA GOT] Give a tag to the following test case. This tag will override the scene name got from unity
    /// in the performance reports
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_PROFILER")]
    public static void Tag(string tag)
    {
        UWAPlatform.UWAEngine.Tag(tag);
    }

    /// <summary>
    /// Set active state of the UI panel
    /// in the performance reports
    /// </summary>
    /// <param name="tag"></param>
    [Conditional("ENABLE_PROFILER")]
    public static void SetUIActive(bool active)
    {
        UWAPlatform.UWAEngine.SetUIActive(active);
    }
    
    /// <summary>
    /// Do simple asset dump in overview mode
    /// </summary>
    [Conditional("ENABLE_PROFILER")]
    public static void AssetDumpInOverview()
    {
        UWAPlatform.UWAEngine.AssetDumpInOverview();
    }

    /// <summary>
    /// [UWA GOT] Add a sample into the function lists in the UWAEngine, so the performance 
    /// between a Push and a Pop will be recorded with the given name.
    /// It is supported to call the PushSample and PopSample recursively, and they must be called in pairs.
    /// </summary>
    /// <param name="sampleName"></param>
    [Conditional("ENABLE_PROFILER")]
    public static void PushSample(string sampleName)
    {
        UWAPlatform.UWAEngine.PushSample(sampleName);
    }
    /// <summary>
    /// [UWA GOT] Add a sample into the function lists in the UWAEngine, so the performance
    /// between a Push and a Pop will be recorded with the given name.
    /// It is supported to call the PushSample and PopSample recursively, and they must be called in pairs.
    /// </summary>
    [Conditional("ENABLE_PROFILER")]
    public static void PopSample()
    {
        UWAPlatform.UWAEngine.PopSample();
    }

#if UNITY_2018_1_OR_NEWER
    /// <summary>
    /// [UWA GOT] Upload the profiling data to GOT Online with project name
    /// </summary>
    /// <param name="callback">callback with true if the uploading succeed</param>
    /// <param name="user">user name for uwa4d.com</param>
    /// <param name="password">password for uwa4d.com</param>
    /// <param name="projectName">name of the target project</param>
    /// <param name="timeLimitS">the duration of the profiling data to upload by api has to be smaller than the time limit,
    /// or you have to upload it manually.</param>
    [Conditional("ENABLE_PROFILER")]
    public static void Upload(Action<bool> callback, string user, string password, string projectName, int timeLimitS)
    {
        UWAPlatform.UWAEngine.Upload(callback, user, password, projectName, timeLimitS);
    }

    /// <summary>
    /// [UWA GOT] Upload the profiling data to GOT Online with project id
    /// </summary>
    /// <param name="callback">callback with true if the uploading succeed</param>
    /// <param name="user">user name for uwa4d.com</param>
    /// <param name="password">password for uwa4d.com</param>
    /// <param name="projectName">id of the target project</param>
    /// <param name="timeLimitS">the duration of the profiling data to upload by api has to be smaller than the time limit,
    /// or you have to upload it manually.</param>
    [Conditional("ENABLE_PROFILER")]
    public static void Upload(Action<bool> callback, string user, string password, int projectId, int timeLimitS)
    {
        UWAPlatform.UWAEngine.Upload(callback, user, password, projectId, timeLimitS);
    }
#endif

    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, float value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, int value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, Vector3 value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, bool value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void AddMarker(string valueName)
    {
        UWAPlatform.UWAEngine.AddMarker(valueName);
    }

    /// <summary>
    /// [UWA GOT] Change the lua lib to a custom name, e.g. 'libgamex.so'.
    /// There is no need to call it when you use the default ulua/tolua/slua/xlua lib.
    /// </summary>
    [Conditional("ENABLE_PROFILER")]
    public static void SetOverrideLuaLib(string luaLib)
    {
#if !UNITY_IPHONE
        UWAPlatform.UWAEngine.SetOverrideLuaLib(luaLib);
#endif
    }
}