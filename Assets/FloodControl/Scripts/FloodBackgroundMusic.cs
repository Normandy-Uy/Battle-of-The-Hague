using System.Collections;
using UnityEngine;

/// <summary>
/// Scene-local looping music for LEVEL FLOOD CONTROL.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class FloodBackgroundMusic : MonoBehaviour
{
    [SerializeField] AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] AudioSource source;

    void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        ConfigureSource();
    }

    IEnumerator Start()
    {
        // Flood Control has its own track; do not overlap the campaign soundtrack.
        DutzBackgroundMusic.StopForCelebration();

        IntroSequenceController intro = FindObjectOfType<IntroSequenceController>();
        while (intro != null && !intro.IsIntroFinished)
            yield return null;

        Play();
    }

    public void Configure(AudioClip clip, float playbackVolume)
    {
        musicClip = clip;
        volume = Mathf.Clamp01(playbackVolume);

        if (source == null)
            source = GetComponent<AudioSource>();
        ConfigureSource();
    }

    void ConfigureSource()
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 0;
        source.clip = musicClip;
        source.volume = volume * DutzAudioSettings.MusicVolume;
    }

    void Play()
    {
        if (source == null || musicClip == null || source.isPlaying)
            return;

        source.Play();
    }

    void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
    }
}
