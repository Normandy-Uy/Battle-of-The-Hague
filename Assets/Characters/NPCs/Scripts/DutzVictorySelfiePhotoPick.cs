using System;
using System.IO;
using UnityEngine;

/// <summary>Gallery photo picker for victory selfie (Android activity + editor file dialog).</summary>
[DefaultExecutionOrder(2600)]
public class DutzVictorySelfiePhotoPick : MonoBehaviour
{
    const string ReceiverName = "DutzVictorySelfiePhotoPick";

    static DutzVictorySelfiePhotoPick instance;
    static Action<Texture2D> pendingCallback;

    public static void PickFromGallery(Action<Texture2D> onFinished)
    {
        EnsureInstance();
        pendingCallback = onFinished;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var pickClass = new AndroidJavaClass("com.dutz.game.DutzGalleryPickActivity");
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = new AndroidJavaObject(
                "android.content.Intent",
                activity,
                pickClass);
            activity.Call("startActivity", intent);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Dutz] Gallery pick failed: " + ex.Message);
            FinishPick(null);
        }
#elif UNITY_EDITOR
        if (DutzVictorySelfieCaptureHud.TryPickPhotoFromDisk(out var picked))
            FinishPick(picked);
        else
            FinishPick(null);
#else
        FinishPick(null);
#endif
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject(ReceiverName);
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzVictorySelfiePhotoPick>();
    }

    void OnAndroidPhotoPicked(string path)
    {
        Texture2D texture = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            var bytes = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                texture = null;
            }
        }

        FinishPick(texture);
    }

    static void FinishPick(Texture2D texture)
    {
        var cb = pendingCallback;
        pendingCallback = null;
        cb?.Invoke(texture);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
