using System.IO;
using UnityEngine;

/// <summary>
/// Optional player registration — name + photo for Level 3 DUTZ IS FREE share image.
/// Shown on Flood Control boot (before opening video).
/// </summary>
public static class DutzVictorySelfieProfile
{
    const string NamePrefsKey = "dutz_victory_selfie_name";
    const string PhotoFileName = "victory_selfie.png";

    static bool setupCompleteThisBoot;
    static bool registrationFinishedThisSession;
    static string displayName = string.Empty;
    static Texture2D cachedPhoto;

    /// <summary>Legacy name — registration now blocks on Flood Control, not EDSA.</summary>
    public static bool IsLevel00SetupBlocking => IsRegistrationBlocking;

    public static bool IsRegistrationBlocking =>
        DutzMobileRuntime.IsFloodControlScene
        && !registrationFinishedThisSession
        && !setupCompleteThisBoot;

    public static string DisplayName => displayName ?? string.Empty;

    public static bool HasPhoto => File.Exists(GetPhotoPath());

    public static void LoadSaved()
    {
        displayName = PlayerPrefs.GetString(NamePrefsKey, string.Empty) ?? string.Empty;
        ReleaseCachedPhoto();
    }

    public static void ResetLevel00Gate() => setupCompleteThisBoot = false;

    public static void ResetForSceneLoad()
    {
        setupCompleteThisBoot = false;
        ReleaseCachedPhoto();
        // Keep registrationFinishedThisSession so Flood Restart skips registration.
    }

    public static void CompleteLevel00Setup(string name, Texture2D photoOrNull)
    {
        displayName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        PlayerPrefs.SetString(NamePrefsKey, displayName);
        PlayerPrefs.Save();

        if (photoOrNull != null)
            SavePhotoTexture(photoOrNull);
        else if (!HasPhoto)
            DeletePhoto();

        ReleaseCachedPhoto();
        setupCompleteThisBoot = true;
        registrationFinishedThisSession = true;
    }

    public static void SkipLevel00Setup() => CompleteLevel00Setup(displayName, null);

    public static Texture2D GetPhotoTexture()
    {
        if (cachedPhoto != null)
            return cachedPhoto;

        var path = GetPhotoPath();
        if (!File.Exists(path))
            return null;

        var bytes = File.ReadAllBytes(path);
        cachedPhoto = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!cachedPhoto.LoadImage(bytes))
        {
            Object.Destroy(cachedPhoto);
            cachedPhoto = null;
        }

        return cachedPhoto;
    }

    public static void SavePhotoTexture(Texture2D source)
    {
        if (source == null)
            return;

        var readable = DutzVictorySelfieComposer.EnsureReadable(source);
        try
        {
            var png = readable.EncodeToPNG();
            if (png != null && png.Length > 0)
                File.WriteAllBytes(GetPhotoPath(), png);
        }
        finally
        {
            if (readable != source && readable != null)
                Object.Destroy(readable);
        }

        ReleaseCachedPhoto();
    }

    public static void DeletePhoto()
    {
        var path = GetPhotoPath();
        if (File.Exists(path))
            File.Delete(path);

        ReleaseCachedPhoto();
    }

    public static string GetPhotoPath() =>
        Path.Combine(Application.persistentDataPath, PhotoFileName);

    static void ReleaseCachedPhoto()
    {
        if (cachedPhoto == null)
            return;

        Object.Destroy(cachedPhoto);
        cachedPhoto = null;
    }
}
