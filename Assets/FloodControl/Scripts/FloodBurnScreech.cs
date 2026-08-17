using UnityEngine;

/// <summary>
/// Pain screech while Player1 is actively burning on steam pipes.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodBurnScreech : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] float volume = 0.78f;
    [SerializeField] float stopGraceSeconds = 0.12f;

    [Header("References")]
    [SerializeField] FloodPlayerHealth playerHealth;
    [SerializeField] AudioSource source;

    float lastBurnTime = -999f;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<FloodPlayerHealth>();
        EnsureSource();
    }

    void Update()
    {
        if (source == null)
            return;

        bool shouldPlay = Time.time - lastBurnTime <= stopGraceSeconds
            && (playerHealth == null || !playerHealth.IsDead);

        if (shouldPlay)
        {
            if (!source.isPlaying)
                source.Play();

            source.pitch = 0.95f
                + Mathf.PerlinNoise(Time.time * 4.5f, 0.2f) * 0.22f;
            source.volume = DutzAudioSettings.ScaleSfx(volume);
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }

    /// <summary>Called by FloodPlayerHealth while burn damage is applied.</summary>
    public void NotifyBurning()
    {
        lastBurnTime = Time.time;
    }

    void EnsureSource()
    {
        Transform existingChild = transform.Find("BurnScreechAudio");
        GameObject host;
        if (existingChild != null)
        {
            host = existingChild.gameObject;
        }
        else
        {
            host = new GameObject("BurnScreechAudio");
            host.transform.SetParent(transform, false);
            host.transform.localPosition = Vector3.zero;
        }

        source = host.GetComponent<AudioSource>();
        if (source == null)
            source = host.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = true;
        source.clip = FloodAudioClips.BurnScreechLoop;
        source.spatialBlend = 0f;
        source.priority = 40;
        source.dopplerLevel = 0f;
        source.volume = DutzAudioSettings.ScaleSfx(volume);
    }

    void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        stopGraceSeconds = Mathf.Max(0.02f, stopGraceSeconds);
    }
}
