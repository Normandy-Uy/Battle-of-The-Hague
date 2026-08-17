using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Background music from StreamingAssets (public/game_music.mp3 or .mp4).
/// Detects real format by file header on desktop — MP3 renamed to .mp4 still plays.
/// Android uses StreamingAssets URLs (APK jar path); File.Exists does not work there.
/// </summary>
public class DutzBackgroundMusic : MonoBehaviour
{
    static readonly string[] MusicFileNames = { "game_music.mp3", "game_music.mp4" };
    const string FloodControlSceneName = "LEVEL FLOOD CONTROL";

    static bool spawned;
    static DutzBackgroundMusic instance;

    AudioSource audioSource;
    VideoPlayer videoPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSpawned() => spawned = false;

    public static void EnsureFromBoot()
    {
        if (IsFloodControlScene())
        {
            StopForCelebration();
            return;
        }

        if (spawned)
            return;

        if (FindObjectOfType<DutzBackgroundMusic>() != null)
            return;

        spawned = true;
        var go = new GameObject("DutzBackgroundMusic");
        DontDestroyOnLoad(go);
        go.AddComponent<DutzBackgroundMusic>();
    }

    void Awake()
    {
        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = DutzAudioSettings.MusicVolume;

        StartCoroutine(LoadAndPlayMusic());
    }

    IEnumerator LoadAndPlayMusic()
    {
        if (IsFloodControlScene())
        {
            StopPlayback();
            yield break;
        }

        var loaded = false;

        foreach (var fileName in MusicFileNames)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!ShouldTryMusicFile(path))
                continue;

            var url = BuildStreamingAssetUrl(fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (fileName.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase))
            {
                yield return TryLoadAudioClip(url, AudioType.MPEG);
                if (audioSource.clip != null)
                {
                    Debug.Log("[Dutz] Playing background music (audio): " + fileName);
                    loaded = true;
                    break;
                }

                PlayWithVideoPlayer(url);
                Debug.Log("[Dutz] Playing background music (video/mp4): " + fileName);
                loaded = true;
                break;
            }

            yield return TryLoadAudioClip(url, AudioType.MPEG);
            if (audioSource.clip != null)
            {
                Debug.Log("[Dutz] Playing background music (audio): " + fileName);
                loaded = true;
                break;
            }
#else
            if (IsMp4VideoFile(path))
            {
                PlayWithVideoPlayer(url);
                Debug.Log("[Dutz] Playing background music (video/mp4).");
                loaded = true;
                break;
            }

            var audioType = IsMp3File(path) ? AudioType.MPEG : AudioType.UNKNOWN;
            yield return TryLoadAudioClip(url, audioType);
            if (audioSource.clip != null)
            {
                Debug.Log("[Dutz] Playing background music (audio): " + Path.GetFileName(path));
                loaded = true;
                break;
            }
#endif
        }

        if (!loaded)
        {
            Debug.LogWarning(
                "[Dutz] No game music in StreamingAssets.\n" +
                "Put game_music.mp3 (or .mp4) in public/ — Unity copies it to StreamingAssets on load.");
        }
    }

    IEnumerator TryLoadAudioClip(string url, AudioType audioType)
    {
        using var request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
        yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
            yield break;

        audioSource.clip = clip;
        audioSource.Play();
    }

    void PlayWithVideoPlayer(string url)
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.playOnAwake = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    /// <summary>Stops looping game music so win stingers are audible.</summary>
    public static void StopForCelebration()
    {
        if (instance != null)
        {
            instance.StopPlayback();
            return;
        }

        var music = FindObjectOfType<DutzBackgroundMusic>();
        if (music != null)
            music.StopPlayback();
    }

    /// <summary>Apply saved music volume from settings.</summary>
    public static void ApplyVolume()
    {
        if (instance == null)
            instance = FindObjectOfType<DutzBackgroundMusic>();

        if (instance?.audioSource != null)
            instance.audioSource.volume = DutzAudioSettings.MusicVolume;
    }

    /// <summary>Restart background music after a scene load (e.g. L2 win stopped it before L3).</summary>
    public static void ResumeForSceneLoad()
    {
        if (instance == null)
            instance = FindObjectOfType<DutzBackgroundMusic>();

        if (IsFloodControlScene())
        {
            instance?.StopPlayback();
            return;
        }

        if (instance != null)
        {
            instance.RestartIfNeeded();
            return;
        }

        spawned = false;
        EnsureFromBoot();
    }

    void RestartIfNeeded()
    {
        if (audioSource != null && audioSource.isPlaying)
            return;

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null && videoPlayer.isPlaying)
            return;

        StopPlayback();
        StartCoroutine(LoadAndPlayMusic());
    }

    void StopPlayback()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
            videoPlayer.Stop();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    static bool ShouldTryMusicFile(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return File.Exists(path);
#endif
    }

    static bool IsFloodControlScene() =>
        SceneManager.GetActiveScene().name == FloodControlSceneName;

    static string BuildStreamingAssetUrl(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, fileName);
#if UNITY_ANDROID && !UNITY_EDITOR
        return path;
#else
        var normalized = Path.GetFullPath(path).Replace('\\', '/');
        return "file:///" + normalized;
#endif
    }

    /// <summary>MP3: ID3 tag or MPEG frame sync (handles .mp4 extension).</summary>
    static bool IsMp3File(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length < 4)
                return false;

            var header = new byte[12];
            var read = stream.Read(header, 0, header.Length);
            if (read < 2)
                return false;

            if (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
                return true;

            if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Real MP4 video container (ftyp box).</summary>
    static bool IsMp4VideoFile(string path)
    {
        if (!IsMp3File(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Length < 8)
                    return false;

                var header = new byte[12];
                stream.Read(header, 0, header.Length);
                return header[4] == (byte)'f'
                    && header[5] == (byte)'t'
                    && header[6] == (byte)'y'
                    && header[7] == (byte)'p';
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}

/// <summary>Persisted music and SFX volume — used by settings on Level 0 and gameplay audio.</summary>
public static class DutzAudioSettings
{
    const string MusicVolumeKey = "DutzMusicVolume";
    const string SfxVolumeKey = "DutzSfxVolume";

    public const float DefaultMusicVolume = 0.55f;
    public const float DefaultSfxVolume = 1f;

    public static float MusicVolume { get; private set; } = DefaultMusicVolume;
    public static float SfxVolume { get; private set; } = DefaultSfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        MusicVolume = DefaultMusicVolume;
        SfxVolume = DefaultSfxVolume;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadSavedVolumes()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume);
    }

    public static void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        DutzBackgroundMusic.ApplyVolume();
    }

    public static void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    public static float ScaleSfx(float baseVolume) => Mathf.Clamp01(baseVolume) * SfxVolume;
}
