using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Writes boot logs on phone so crashes can be diagnosed without full logcat setup.
/// Pull with: adb shell run-as com.dutz.game cat files/dutz_boot.log
/// or browse Android/data/com.dutz.game/files/dutz_boot.log on device.
/// </summary>
public static class DutzAndroidBootLog
{
    const string LogFileName = "dutz_boot.log";
    static string logPath;
    static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        initialized = false;
        TryWriteEarly("SubsystemRegistration");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (!Application.isMobilePlatform || initialized)
            return;

        initialized = true;
        logPath = Path.Combine(Application.persistentDataPath, LogFileName);

        try
        {
            File.WriteAllText(logPath, $"=== Dutz boot {DateTime.Now:u} ===\n");
        }
        catch
        {
            return;
        }

        Application.logMessageReceived += OnLog;
        Write(
            "BeforeSceneLoad\n" +
            $"device={SystemInfo.deviceModel}\n" +
            $"os={SystemInfo.operatingSystem}\n" +
            $"ramMB={SystemInfo.systemMemorySize}\n" +
            $"gpu={SystemInfo.graphicsDeviceName}\n" +
            $"graphicsAPI={SystemInfo.graphicsDeviceType}\n" +
            $"dataPath={Application.persistentDataPath}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoad() => Write("AfterSceneLoad — scene loaded OK");

    public static void Write(string message)
    {
        if (!Application.isMobilePlatform || string.IsNullOrEmpty(logPath))
            return;

        try
        {
            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
        }
        catch
        {
            // ignored
        }
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            return;

        Write($"[{type}] {condition}\n{stackTrace}");
    }

    static void TryWriteEarly(string stage)
    {
        if (!Application.isMobilePlatform)
            return;

        try
        {
            var path = Path.Combine(Application.persistentDataPath, LogFileName);
            File.AppendAllText(path, $"{DateTime.Now:HH:mm:ss.fff} {stage}\n");
        }
        catch
        {
            // ignored
        }
    }
}
