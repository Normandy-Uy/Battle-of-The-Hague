using UnityEngine;

/// <summary>
/// Short squeaks while a Flood Control rat is near Player1.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodRatSounds : MonoBehaviour
{
    [Header("Proximity")]
    [SerializeField] float hearDistance = 14f;
    [SerializeField] float volume = 0.55f;
    [SerializeField] Vector2 squeakInterval = new Vector2(0.45f, 0.95f);
    [SerializeField] Vector2 pitchRange = new Vector2(0.92f, 1.2f);
    [SerializeField] float spatialBlend = 1f;
    [SerializeField] float minDistance = 1.5f;
    [SerializeField] float maxDistance = 22f;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] GameManager gameManager;
    [SerializeField] AudioSource source;

    float nextSqueakTime;

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
        nextSqueakTime = Time.time + Random.Range(0.1f, squeakInterval.y);
    }

    void Update()
    {
        if (source == null || !gameObject.activeInHierarchy)
            return;
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;
        if (player == null || Time.time < nextSqueakTime)
            return;

        nextSqueakTime = Time.time + Random.Range(squeakInterval.x, squeakInterval.y);

        Vector3 delta = transform.position - player.position;
        if (delta.sqrMagnitude > hearDistance * hearDistance)
            return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(FloodAudioClips.RatSqueak, DutzAudioSettings.ScaleSfx(volume));
    }

    void EnsureSource()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.priority = 150;
    }

    void OnValidate()
    {
        hearDistance = Mathf.Max(0.5f, hearDistance);
        volume = Mathf.Clamp01(volume);
        squeakInterval.x = Mathf.Max(0.1f, squeakInterval.x);
        squeakInterval.y = Mathf.Max(squeakInterval.x, squeakInterval.y);
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }
}
