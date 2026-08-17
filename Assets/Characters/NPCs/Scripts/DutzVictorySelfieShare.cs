using System.IO;
using UnityEngine;

/// <summary>Shares victory card image via Android/iOS share sheet (pick Facebook, etc.).</summary>
public static class DutzVictorySelfieShare
{
    public static string BuildShareCaption(string playerName, int finalScore)
    {
        var namePart = string.IsNullOrWhiteSpace(playerName) ? "I" : playerName.Trim();
        return $"{namePart} freed Dutz! Battle of The Hague - Free Dutz — score {finalScore:N0} #FreeDutz #DutzIsFree";
    }

    public static bool ShareVictoryCard(string imagePath, string caption)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            Debug.LogWarning("[Dutz] Victory share — image file missing.");
            return false;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        return DutzAndroidShareHelper.ShareImage(imagePath, caption);
#elif UNITY_IOS && !UNITY_EDITOR
        Debug.Log("[Dutz] Victory share caption: " + caption);
        return true;
#else
        Debug.Log($"[Dutz] Victory share (editor): {imagePath}\n{caption}");
        return true;
#endif
    }

    public static bool SaveVictoryCardToGallery(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            Debug.LogWarning("[Dutz] Victory download — image file missing.");
            return false;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        DutzAndroidStoragePermission.EnsureLegacyWriteAccess();
        return DutzAndroidShareHelper.SaveToGallery(imagePath);
#elif UNITY_EDITOR
        return SaveToDownloadsFolder(imagePath);
#else
        return SaveToDownloadsFolder(imagePath);
#endif
    }

    static bool SaveToDownloadsFolder(string imagePath)
    {
        try
        {
            var fileName = Path.GetFileName(imagePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = "DutzIsFree.png";

            var downloads = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Downloads");
            Directory.CreateDirectory(downloads);
            var dest = Path.Combine(downloads, fileName);
            File.Copy(imagePath, dest, overwrite: true);
            Debug.Log("[Dutz] Victory image saved to Downloads: " + dest);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Dutz] Victory download failed: " + ex.Message);
            return false;
        }
    }
}

#if UNITY_ANDROID && !UNITY_EDITOR
static class DutzAndroidStoragePermission
{
    public static void EnsureLegacyWriteAccess()
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        if (GetSdkInt() >= 29)
            return;

        if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
            return;

        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
    }

    public static void EnsureLegacyReadAccess()
    {
        if (Application.platform != RuntimePlatform.Android)
            return;

        if (GetSdkInt() >= 33)
            return;

        if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
            return;

        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
    }

    static int GetSdkInt()
    {
        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        return version.GetStatic<int>("SDK_INT");
    }
}

static class DutzAndroidShareHelper
{
    public static bool ShareImage(string filePath, string text)
    {
        try
        {
            DutzAndroidStoragePermission.EnsureLegacyReadAccess();
            using var helper = new AndroidJavaClass("com.dutz.game.DutzShareHelper");
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return helper.CallStatic<bool>("shareImage", activity, filePath, text ?? string.Empty);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Dutz] Victory share failed: " + ex.Message);
            return false;
        }
    }

    public static bool SaveToGallery(string filePath)
    {
        try
        {
            using var helper = new AndroidJavaClass("com.dutz.game.DutzShareHelper");
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return helper.CallStatic<bool>("saveImageToGallery", activity, filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Dutz] Victory download failed: " + ex.Message);
            return false;
        }
    }
}
#endif
