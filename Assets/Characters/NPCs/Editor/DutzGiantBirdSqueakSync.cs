using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies public/tunetank.com_pterodactyl-dinosaur-squeak.wav into Resources as the
/// giant bird squeak clip (DutzGiantBirdSqueak) so DutzGiantBirdSounds can load it.
/// </summary>
public static class DutzGiantBirdSqueakSync
{
    const string PublicFileName = "tunetank.com_pterodactyl-dinosaur-squeak.wav";
    const string ResourceAssetPath = "Assets/Resources/" + DutzGiantBirdSounds.SqueakResourceName + ".wav";

    [InitializeOnLoadMethod]
    static void AutoSyncOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Sync(log: false);
        };
    }

    [MenuItem("Assets/Dutz Authoring/Sync Giant Bird Squeak Sound")]
    static void SyncFromMenu()
    {
        if (!Sync(log: true))
            Debug.LogWarning("[Dutz] Missing public/" + PublicFileName);
    }

    static bool Sync(bool log)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var source = Path.Combine(projectRoot, "public", PublicFileName);
        if (!File.Exists(source))
            return false;

        var dest = Path.GetFullPath(ResourceAssetPath);
        if (File.Exists(dest) && File.GetLastWriteTimeUtc(dest) >= File.GetLastWriteTimeUtc(source))
            return true;

        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        File.Copy(source, dest, overwrite: true);
        AssetDatabase.ImportAsset(ResourceAssetPath);
        Debug.Log("[Dutz] Synced giant bird squeak → " + ResourceAssetPath);
        return true;
    }
}
