using UnityEngine;

/// <summary>
/// Pterodactyl squeak SFX for giant birds — repeated 3D squeaks whenever the bird
/// is within hearing range of Player1. Clip synced from public/ into Resources.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(60)]
public class DutzGiantBirdSounds : MonoBehaviour
{
    public const string SqueakResourceName = "DutzGiantBirdSqueak";

    const string AudioChildName = "BirdSqueakAudio";
    const float MaxHearingDistance = 120f;
    const float MinSqueakInterval = 1.4f;
    const float MaxSqueakInterval = 3.6f;
    const float OutOfRangeRecheckSeconds = 0.5f;

    [SerializeField] float squeakVolume = 0.9f;
    [SerializeField] float minDistance = 8f;
    [SerializeField] float maxDistance = 110f;

    static AudioClip sharedSqueakClip;
    static bool warnedMissingClip;

    AudioSource source;
    DutzNpcHitPoints hitPoints;
    float nextSqueakTime;

    public static DutzGiantBirdSounds EnsureOn(GameObject bird)
    {
        if (bird == null)
            return null;

        var sounds = bird.GetComponent<DutzGiantBirdSounds>();
        if (sounds == null)
            sounds = bird.AddComponent<DutzGiantBirdSounds>();

        return sounds;
    }

    void Awake()
    {
        hitPoints = GetComponent<DutzNpcHitPoints>();
        EnsureSharedClip();
        EnsureAudioSource();
        // Random start phase so multiple birds don't squeak in unison.
        nextSqueakTime = Time.time + Random.Range(0f, MaxSqueakInterval);
    }

    void Update()
    {
        if (Time.time < nextSqueakTime || source == null)
            return;

        EnsureSharedClip();
        if (sharedSqueakClip == null)
        {
            nextSqueakTime = Time.time + OutOfRangeRecheckSeconds;
            return;
        }

        if (hitPoints == null)
            hitPoints = GetComponent<DutzNpcHitPoints>();

        if (hitPoints != null && hitPoints.IsDead)
            return;

        if (!IsPlayerInVicinity())
        {
            nextSqueakTime = Time.time + OutOfRangeRecheckSeconds;
            return;
        }

        source.pitch = Random.Range(0.9f, 1.12f);
        source.volume = DutzAudioSettings.ScaleSfx(squeakVolume);
        source.PlayOneShot(sharedSqueakClip);
        nextSqueakTime = Time.time + Random.Range(MinSqueakInterval, MaxSqueakInterval);
    }

    void EnsureAudioSource()
    {
        // Dedicated child so any other AudioSources on the bird stay untouched.
        var child = transform.Find(AudioChildName);
        if (child == null)
        {
            child = new GameObject(AudioChildName).transform;
            child.SetParent(transform, false);
            child.localPosition = Vector3.zero;
        }

        source = child.GetComponent<AudioSource>();
        if (source == null)
            source = child.gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.spread = 45f;
        source.priority = 140;
    }

    bool IsPlayerInVicinity()
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
            return false;

        // Full 3D distance — birds fly overhead, height counts.
        return (transform.position - player.transform.position).sqrMagnitude
            <= MaxHearingDistance * MaxHearingDistance;
    }

    static void EnsureSharedClip()
    {
        if (sharedSqueakClip != null)
            return;

        sharedSqueakClip = Resources.Load<AudioClip>(SqueakResourceName);
        if (sharedSqueakClip == null && !warnedMissingClip)
        {
            warnedMissingClip = true;
            Debug.LogWarning("[Dutz] Missing giant bird squeak clip in Resources: " + SqueakResourceName);
        }
    }
}
