using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Plays public/DUTZHOME.mp4 (synced to StreamingAssets) after the Level 3 score roll — the game finale.
/// </summary>
public static class DutzLevel03VictoryVideo
{
    public const string FileName = "DUTZHOME.mp4";

    public static bool IsPlaying { get; private set; }

    public static bool IsAvailable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return File.Exists(Path.Combine(Application.streamingAssetsPath, FileName));
#endif
    }

    public static IEnumerator Play()
    {
        if (!IsAvailable())
        {
            Debug.LogWarning(
                "[Dutz] DUTZHOME.mp4 not found in StreamingAssets.\n" +
                "Add public/DUTZHOME.mp4 and let the editor sync it on load.");
            yield break;
        }

        IsPlaying = true;

        var host = new GameObject("DutzLevel03VictoryVideo");
        var runner = host.AddComponent<DutzVictoryVideoRunner>();
        yield return runner.PlayToEnd(DutzVictoryVideoRunner.BuildVideoUrl(FileName), "DUTZHOME");

        IsPlaying = false;
        Object.Destroy(host);
    }

    public static void PlaySceneActions()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        DropJailMuralsOutOfView();
    }

    const string JailMuralPanelName = "DutzJailMural_End";
    const float JailDropMeters = 80f;

    static void DropJailMuralsOutOfView()
    {
        var panels = new List<Transform>();
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
            CollectJailMuralPanels(roots[i].transform, panels);

        if (panels.Count == 0)
            return;

        for (var i = 0; i < panels.Count; i++)
        {
            var panel = panels[i];
            if (panel == null)
                continue;

            var pos = panel.position;
            pos.y -= JailDropMeters;
            panel.position = pos;
        }

        Debug.Log("[Dutz] Victory — DUTZJAIL mural dropped out of view.");
    }

    static void CollectJailMuralPanels(Transform node, List<Transform> panels)
    {
        if (node.name == JailMuralPanelName)
            panels.Add(node);

        for (var i = 0; i < node.childCount; i++)
            CollectJailMuralPanels(node.GetChild(i), panels);
    }
}

/// <summary>
/// Plays public/FLYING TO MANILA.mp4 after Level 0 "GO TO PHILIPPINES SENATE" before loading Level 1.
/// </summary>
public static class DutzLevel00TransitionVideo
{
    public const string FileName = "FLYING TO MANILA.mp4";

    public static bool IsPlaying { get; private set; }

    public static bool IsAvailable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return File.Exists(Path.Combine(Application.streamingAssetsPath, FileName));
#endif
    }

    public static IEnumerator Play()
    {
        MarkPlayingStarted();

        if (!IsAvailable())
        {
            Debug.LogWarning(
                "[Dutz] FLYING TO MANILA.mp4 not found in StreamingAssets.\n" +
                "Add public/FLYING TO MANILA.mp4 and let the editor sync it on load.");
            MarkPlayingStopped();
            yield break;
        }

        var host = new GameObject("DutzLevel00TransitionVideo");
        var runner = host.AddComponent<DutzVictoryVideoRunner>();
        try
        {
            yield return runner.PlayToEnd(DutzVictoryVideoRunner.BuildVideoUrl(FileName), "FLYING TO MANILA");
        }
        finally
        {
            MarkPlayingStopped();
            Object.Destroy(host);
        }
    }

    public static void MarkPlayingStarted() => IsPlaying = true;

    public static void MarkPlayingStopped() => IsPlaying = false;
}

/// <summary>
/// Plays public/BATO_ESCAPE.mp4 after Level 1 "GO TO AIRPORT WITH IDOL BOY" before loading Level 2.
/// </summary>
public static class DutzLevel01VictoryVideo
{
    public const string FileName = "BATO_ESCAPE.mp4";

    public static bool IsPlaying { get; private set; }

    public static bool IsAvailable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return File.Exists(Path.Combine(Application.streamingAssetsPath, FileName));
#endif
    }

    public static IEnumerator Play()
    {
        if (!IsAvailable())
        {
            Debug.LogWarning(
                "[Dutz] BATO_ESCAPE.mp4 not found in StreamingAssets.\n" +
                "Add public/BATO_ESCAPE.mp4 and let the editor sync it on load.");
            yield break;
        }

        IsPlaying = true;

        var host = new GameObject("DutzLevel01VictoryVideo");
        var runner = host.AddComponent<DutzVictoryVideoRunner>();
        yield return runner.PlayToEnd(DutzVictoryVideoRunner.BuildVideoUrl(FileName), "BATO_ESCAPE");

        IsPlaying = false;
        Object.Destroy(host);
    }
}

/// <summary>
/// Plays public/MANILA_AMSTERDAM.mp4 after Level 2 "GO TO HAGUE" before loading Level 3.
/// </summary>
public static class DutzLevel02TransitionVideo
{
    public const string FileName = "MANILA_AMSTERDAM.mp4";

    public static bool IsPlaying { get; private set; }

    public static bool IsAvailable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return File.Exists(Path.Combine(Application.streamingAssetsPath, FileName));
#endif
    }

    public static IEnumerator Play()
    {
        if (!IsAvailable())
        {
            Debug.LogWarning(
                "[Dutz] MANILA_AMSTERDAM.mp4 not found in StreamingAssets.\n" +
                "Add public/MANILA_AMSTERDAM.mp4 and let the editor sync it on load.");
            yield break;
        }

        IsPlaying = true;

        var host = new GameObject("DutzLevel02TransitionVideo");
        var runner = host.AddComponent<DutzVictoryVideoRunner>();
        yield return runner.PlayToEnd(DutzVictoryVideoRunner.BuildVideoUrl(FileName), "MANILA_AMSTERDAM");

        IsPlaying = false;
        Object.Destroy(host);
    }
}

/// <summary>
/// Level07 impeachment videos — drop files in public/ then editor sync copies them to StreamingAssets.
/// Victory (Boy Idol defeated + 16 votes): Sara convicted.mp4
/// Fail (accept defeat / burned by Boy Idol / Senate buy still under 16): Sara acquitted.mp4
/// </summary>
public static class DutzLevel07ImpeachmentVideo
{
    public const string VictoryFileName = "Sara convicted.mp4";
    public const string FailFileName = "Sara acquitted.mp4";
    const string LegacyVictoryFileName = "IMPEACH_PRINCESS_Z.mp4";
    const string LegacyFailFileName = "FAIL_TO_IMPEACH.mp4";

    public static bool IsPlaying { get; private set; }

    public static bool IsVictoryAvailable() =>
        IsFileAvailable(VictoryFileName) || IsFileAvailable(LegacyVictoryFileName);

    public static bool IsFailAvailable() =>
        IsFileAvailable(FailFileName) || IsFileAvailable(LegacyFailFileName);

    static string ResolveVictoryFileName() =>
        IsFileAvailable(VictoryFileName) ? VictoryFileName : LegacyVictoryFileName;

    static string ResolveFailFileName() =>
        IsFileAvailable(FailFileName) ? FailFileName : LegacyFailFileName;

    static bool IsFileAvailable(string fileName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return File.Exists(Path.Combine(Application.streamingAssetsPath, fileName));
#endif
    }

    public static IEnumerator PlayVictoryIfAvailable()
    {
        if (!IsVictoryAvailable())
        {
            Debug.LogWarning(
                "[Dutz] Sara convicted.mp4 not found in StreamingAssets yet.\n" +
                "Add public/Sara convicted.mp4 when ready — editor sync will copy it.");
            yield break;
        }

        var fileName = ResolveVictoryFileName();
        IsPlaying = true;
        var host = new GameObject("DutzLevel07ImpeachVictoryVideo");
        var runner = host.AddComponent<DutzVictoryVideoRunner>();
        yield return runner.PlayToEnd(
            DutzVictoryVideoRunner.BuildVideoUrl(fileName),
            Path.GetFileNameWithoutExtension(fileName));
        IsPlaying = false;
        Object.Destroy(host);
    }

    public static IEnumerator PlayFailIfAvailable()
    {
        if (!IsFailAvailable())
        {
            Debug.LogWarning(
                "[Dutz] Sara acquitted.mp4 not found in StreamingAssets yet.\n" +
                "Add public/Sara acquitted.mp4 when ready — editor sync will copy it.");
            yield break;
        }

        var fileName = ResolveFailFileName();
        IsPlaying = true;
        var host = new GameObject("DutzLevel07ImpeachFailVideo");
        var runner = host.AddComponent<DutzVictoryVideoRunner>();
        yield return runner.PlayToEnd(
            DutzVictoryVideoRunner.BuildVideoUrl(fileName),
            Path.GetFileNameWithoutExtension(fileName));
        IsPlaying = false;
        Object.Destroy(host);
    }

    /// <summary>Play Sara acquitted, then reload Level07 respecting 3-lives + rewarded Restart.</summary>
    public static IEnumerator PlayFailThenReloadLevel()
    {
        Time.timeScale = 1f;
        yield return PlayFailIfAvailable();

        int livesLeft = DutzPlayerLives.ConsumeOne();
        if (livesLeft > 0)
        {
            DutzPlayerLives.MarkPreserveAcrossNextLoad();
            DutzGameBootstrap.PrepareForSceneLoad();
            LoadActiveScene();
            yield break;
        }

        bool finished = false;
        bool rewarded = false;
        FloodRewardedAdStub.Show(
            onRewarded: () =>
            {
                rewarded = true;
                finished = true;
            },
            onDismissedOrFailed: () =>
            {
                finished = true;
            });

        while (!finished)
            yield return null;

        if (!rewarded)
            yield break;

        DutzPlayerLives.ResetToFull();
        DutzGameBootstrap.PrepareForSceneLoad();
        LoadActiveScene();
    }

    static void LoadActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path))
            SceneManager.LoadScene(scene.path);
        else
            SceneManager.LoadScene(scene.name);
    }
}

/// <summary>Shared victory-video playback state for level win sequences.</summary>
public static class DutzVictoryVideoPlayback
{
    public static bool SuppressWinOverlays { get; private set; }

    public static bool IsPlaying =>
        DutzLevel00TransitionVideo.IsPlaying
        || DutzLevel01VictoryVideo.IsPlaying
        || DutzLevel02TransitionVideo.IsPlaying
        || DutzLevel03VictoryVideo.IsPlaying
        || DutzLevel07ImpeachmentVideo.IsPlaying;

    public static bool ShouldHideWinGui => IsPlaying || SuppressWinOverlays;

    public static void BeginTransitionOverlaySuppression() => SuppressWinOverlays = true;

    public static void ResetForSceneLoad() => SuppressWinOverlays = false;
}

/// <summary>Full-screen StreamingAssets video playback for level win sequences.</summary>
[DefaultExecutionOrder(10000)]
public sealed class DutzVictoryVideoRunner : MonoBehaviour
{
    VideoPlayer videoPlayer;
    RenderTexture renderTexture;

    public static string BuildVideoUrl(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileName);
#if UNITY_ANDROID && !UNITY_EDITOR
        return path;
#else
        var normalized = Path.GetFullPath(path).Replace('\\', '/');
        return "file:///" + normalized;
#endif
    }

    public IEnumerator PlayToEnd(string url, string logLabel)
    {
        renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            if (videoPlayer == null)
                yield break;

            yield return null;
        }

        videoPlayer.Play();
        Debug.Log("[Dutz] " + logLabel + " victory video playing.");

        // Prefer length-based end; isPlaying alone can stick on some Android/decoders.
        var lengthSeconds = videoPlayer.length > 0.1 ? videoPlayer.length : 0.0;
        var deadline = Time.realtimeSinceStartup + (float)(lengthSeconds > 0.1 ? lengthSeconds + 2.0 : 180.0);
        while (videoPlayer != null && videoPlayer.isPlaying)
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Debug.LogWarning("[Dutz] " + logLabel + " victory video timed out — continuing.");
                break;
            }

            if (lengthSeconds > 0.1
                && videoPlayer.time >= lengthSeconds - 0.05)
                break;

            yield return null;
        }

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    void OnGUI()
    {
        if (renderTexture == null)
            return;

        GUI.depth = -20000;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), renderTexture, ScaleMode.ScaleAndCrop);
    }
}
