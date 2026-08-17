using UnityEngine;

/// <summary>
/// Low growl loop while a Flood Control crocodile is near Player1.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodCrocodileSounds : MonoBehaviour
{
    [Header("Proximity")]
    [SerializeField] float hearDistance = 30f;
    [SerializeField] float volume = 1f;
    [SerializeField] float spatialBlend = 0.45f;
    [SerializeField] float minDistance = 10f;
    [SerializeField] float maxDistance = 40f;
    [SerializeField] float biteVolume = 1f;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] GameManager gameManager;
    [SerializeField] AudioSource source;

    const float UpdateInterval = 0.15f;
    float nextUpdateTime;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (player == null)
        {
            PlayerController controller = FindObjectOfType<PlayerController>();
            if (controller != null)
                player = controller.transform;
        }

        EnsureSource();
    }

    void Update()
    {
        if (source == null)
            return;

        if (Time.time < nextUpdateTime)
        {
            if (source.isPlaying)
            {
                source.pitch = 0.88f
                    + Mathf.PerlinNoise(Time.time * 0.35f, transform.position.x * 0.08f) * 0.2f;
            }

            return;
        }

        nextUpdateTime = Time.time + UpdateInterval;
        bool shouldPlay = ShouldGrowl();
        if (shouldPlay == source.isPlaying)
            return;

        if (shouldPlay)
            source.Play();
        else
            source.Stop();
    }

    bool ShouldGrowl()
    {
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return false;
        if (player == null)
            return false;

        Vector3 delta = transform.position - player.position;
        return delta.sqrMagnitude <= hearDistance * hearDistance;
    }

    public void ConfigureAudibility(
        float proximityDistance,
        float growlVolume,
        float stereoBlend,
        float nearDistance,
        float farDistance)
    {
        hearDistance = Mathf.Max(0.5f, proximityDistance);
        volume = Mathf.Clamp01(growlVolume);
        spatialBlend = Mathf.Clamp01(stereoBlend);
        minDistance = Mathf.Max(0.1f, nearDistance);
        maxDistance = Mathf.Max(minDistance, farDistance);
        EnsureSource();
    }

    public void PlayBite()
    {
        EnsureSource();
        if (source == null)
            return;

        source.PlayOneShot(FloodAudioClips.CrocBite, DutzAudioSettings.ScaleSfx(biteVolume));
    }

    void EnsureSource()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = true;
        source.clip = FloodAudioClips.CrocGrowlLoop;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.priority = 140;
        source.volume = DutzAudioSettings.ScaleSfx(volume);
    }

    void OnValidate()
    {
        hearDistance = Mathf.Max(0.5f, hearDistance);
        volume = Mathf.Clamp01(volume);
        biteVolume = Mathf.Clamp01(biteVolume);
        spatialBlend = Mathf.Clamp01(spatialBlend);
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }
}
