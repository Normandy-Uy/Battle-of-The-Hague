using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Shared VideoPlayer wiring for Flood Control.
/// Avoids VideoAudioOutputMode.Direct and the Unity 6 AudioSampleProvider
/// overflow path (skipOnDrop + UnscaledGameTime), which can freeze Play Mode.
/// </summary>
public static class FloodVideoPlayback
{
    public static void ConfigurePlayer(
        VideoPlayer videoPlayer,
        AudioSource audioSource,
        string url,
        RenderTexture target)
    {
        if (videoPlayer == null)
            return;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = target;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        // skipOnDrop + UnscaledGameTime triggers AudioSampleProvider overflows
        // and freezes on recent Unity 6 editors (and some devices).
        videoPlayer.skipOnDrop = false;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.GameTime;

        if (audioSource != null)
        {
            audioSource.enabled = true;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.mute = false;
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.priority = 16;
            audioSource.Stop();

            // AudioSource mode is far more stable than Direct.
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }
    }

    public static void BindAudioAfterPrepare(VideoPlayer videoPlayer, AudioSource audioSource)
    {
        if (videoPlayer == null)
            return;

        if (audioSource == null || videoPlayer.audioOutputMode != VideoAudioOutputMode.AudioSource)
        {
            // Last-resort silence path if no usable AudioSource target.
            if (videoPlayer.audioTrackCount > 0)
                videoPlayer.EnableAudioTrack(0, false);
            return;
        }

        if (videoPlayer.controlledAudioTrackCount < 1)
            videoPlayer.controlledAudioTrackCount = 1;

        // Disable first, bind, then enable — avoids dumping samples with no consumer.
        if (videoPlayer.audioTrackCount > 0)
            videoPlayer.EnableAudioTrack(0, false);

        videoPlayer.SetTargetAudioSource(0, audioSource);

        if (videoPlayer.audioTrackCount > 0)
            videoPlayer.EnableAudioTrack(0, true);

        audioSource.enabled = true;
    }

    public static RenderTexture CreateTarget(string name, bool mobileFriendly)
    {
        int width = mobileFriendly ? 854 : 1280;
        int height = mobileFriendly ? 480 : 720;
        return new RenderTexture(width, height, 0)
        {
            name = name,
            useMipMap = false,
            autoGenerateMips = false
        };
    }

    public static void PauseSceneMusic()
    {
        FloodBackgroundMusic music = Object.FindObjectOfType<FloodBackgroundMusic>();
        if (music == null)
            return;

        AudioSource source = music.GetComponent<AudioSource>();
        if (source != null && source.isPlaying)
            source.Pause();
    }

    public static void ResumeSceneMusic()
    {
        FloodBackgroundMusic music = Object.FindObjectOfType<FloodBackgroundMusic>();
        if (music == null)
            return;

        AudioSource source = music.GetComponent<AudioSource>();
        if (source != null && !source.isPlaying && source.clip != null)
            source.UnPause();
    }
}
