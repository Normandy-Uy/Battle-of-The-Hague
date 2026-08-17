using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies public/game_music.mp3 or .mp4 and victory videos into StreamingAssets for runtime playback.
/// </summary>
public static class DutzGameMusicSetup
{
    static readonly string[] PublicCandidates = { "game_music.mp3", "game_music.mp4" };
    static readonly string[] VictoryVideoFileNames =
    {
        "FLYING TO MANILA.mp4",
        "DUTZHOME.mp4",
        "BATO_ESCAPE.mp4",
        "MANILA_AMSTERDAM.mp4",
        "Sara convicted.mp4",
        "Sara acquitted.mp4",
        // Legacy Level07 names (kept so old drops still sync if present).
        "IMPEACH_PRINCESS_Z.mp4",
        "FAIL_TO_IMPEACH.mp4",
    };

    public static bool SyncMusicFile()
    {
        return SyncMusicFile(out _);
    }

    public static bool SyncMusicFile(out string syncedFileName)
    {
        syncedFileName = null;
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var publicDir = Path.Combine(projectRoot, "public");

        string source = null;
        foreach (var name in PublicCandidates)
        {
            var candidate = Path.Combine(publicDir, name);
            if (File.Exists(candidate))
            {
                source = candidate;
                syncedFileName = name;
                break;
            }
        }

        if (source == null)
            return false;

        var destDir = Path.Combine(Application.dataPath, "StreamingAssets");
        Directory.CreateDirectory(destDir);

        foreach (var oldName in PublicCandidates)
        {
            var oldPath = Path.Combine(destDir, oldName);
            if (File.Exists(oldPath) && oldName != syncedFileName)
                File.Delete(oldPath);
        }

        var dest = Path.Combine(destDir, syncedFileName);
        File.Copy(source, dest, overwrite: true);
        Debug.Log("[Dutz] Synced game music → Assets/StreamingAssets/" + syncedFileName);
        return true;
    }

    public static bool SyncVictoryVideoFile()
    {
        var syncedAny = false;
        foreach (var fileName in VictoryVideoFileNames)
            syncedAny |= SyncVictoryVideoFile(fileName);

        return syncedAny;
    }

    static bool SyncVictoryVideoFile(string fileName)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var source = Path.Combine(projectRoot, "public", fileName);
        if (!File.Exists(source))
            return false;

        var destDir = Path.Combine(Application.dataPath, "StreamingAssets");
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, fileName);

        // Skip rewrite when StreamingAssets is already current (same size + not older).
        if (File.Exists(dest))
        {
            var srcInfo = new FileInfo(source);
            var dstInfo = new FileInfo(dest);
            if (dstInfo.Length == srcInfo.Length
                && dstInfo.LastWriteTimeUtc >= srcInfo.LastWriteTimeUtc)
                return false;
        }

        File.Copy(source, dest, overwrite: true);
        Debug.Log("[Dutz] Synced victory video → Assets/StreamingAssets/" + fileName);
        return true;
    }

    public static void SyncAllPublicMedia()
    {
        SyncMusicFile();
        SyncVictoryVideoFile();
        SyncVictorySelfieTemplate();
    }

    /// <summary>WebGL L00 demo — music only; drop heavy transition/finale videos from the build.</summary>
    public static void SyncWebGlDemoMedia()
    {
        SyncMusicFile();
        RemoveVictoryVideosFromStreamingAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Dutz] WebGL demo StreamingAssets: game music only (transition videos removed).");
    }

    public static void RemoveVictoryVideosFromStreamingAssets()
    {
        var destDir = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(destDir))
            return;

        foreach (var fileName in VictoryVideoFileNames)
        {
            var path = Path.Combine(destDir, fileName);
            if (!File.Exists(path))
                continue;

            File.Delete(path);
            var meta = path + ".meta";
            if (File.Exists(meta))
                File.Delete(meta);
        }
    }

    public static bool SyncVictorySelfieTemplate()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var source = Path.Combine(projectRoot, "public", "DUTZSELFIE.png");
        if (!File.Exists(source))
            return false;

        var destDir = Path.Combine(Application.dataPath, "StreamingAssets");
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, "DUTZSELFIE.png");

        if (File.Exists(dest))
        {
            var srcInfo = new FileInfo(source);
            var dstInfo = new FileInfo(dest);
            if (dstInfo.Length == srcInfo.Length
                && dstInfo.LastWriteTimeUtc >= srcInfo.LastWriteTimeUtc)
                return false;
        }

        File.Copy(source, dest, overwrite: true);
        Debug.Log("[Dutz] Synced victory selfie template → Assets/StreamingAssets/DUTZSELFIE.png");
        return true;
    }

    [InitializeOnLoadMethod]
    static void AutoSyncOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (FindSyncedMusicInStreamingAssets() != null)
                SyncVictoryVideoFile();
            else
                SyncAllPublicMedia();
        };
    }

    static string FindSyncedMusicInStreamingAssets()
    {
        var dir = Path.Combine(Application.dataPath, "StreamingAssets");
        foreach (var name in PublicCandidates)
        {
            var path = Path.Combine(dir, name);
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}
