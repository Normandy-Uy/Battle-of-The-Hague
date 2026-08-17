using UnityEngine;

/// <summary>
/// Underwater arm-flap / swimming splash one-shots synced to Player1 swim motion.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FloodSwimSounds : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] FloodPlayerHealth playerHealth;
    [SerializeField] SwimmingAnimationController swimmingAnimation;
    [SerializeField] Rigidbody body;
    [SerializeField] AudioSource source;

    [Header("Playback")]
    [SerializeField] float volume = 1f;
    [SerializeField] Vector2 pitchRange = new Vector2(0.94f, 1.08f);

    float lastPhase;
    bool wasGameplayActive;
    bool thrustWasHeld;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (playerHealth == null)
            playerHealth = GetComponent<FloodPlayerHealth>();
        if (swimmingAnimation == null)
            swimmingAnimation = GetComponent<SwimmingAnimationController>();
        if (body == null)
            body = GetComponent<Rigidbody>();

        EnsureSource();
    }

    void Update()
    {
        if (source == null)
            return;
        bool gameplayActive = gameManager == null || gameManager.IsGameplayEnabled;
        if (!gameplayActive)
        {
            wasGameplayActive = false;
            thrustWasHeld = false;
            source.Stop();
            return;
        }
        if (playerHealth != null && playerHealth.IsDead)
        {
            thrustWasHeld = false;
            source.Stop();
            return;
        }

        if (!wasGameplayActive)
        {
            wasGameplayActive = true;
            lastPhase = swimmingAnimation != null ? swimmingAnimation.SwimPhase : 0f;
        }

        bool thrustHeld = Input.GetKey(KeyCode.UpArrow);
        if (DutzRobloxMobileInput.IsMobileControlsActive)
        {
            Vector2 move = DutzRobloxMobileInput.MoveAxis;
            thrustHeld = move.y > 0.1f || DutzRobloxMobileInput.JumpHeld;
        }

        if (!thrustHeld)
        {
            thrustWasHeld = false;
            source.Stop();
            return;
        }

        if (!thrustWasHeld)
        {
            thrustWasHeld = true;
            lastPhase = swimmingAnimation != null ? swimmingAnimation.SwimPhase : 0f;
            PlayFlap();
            return;
        }

        if (swimmingAnimation != null)
        {
            float phase = swimmingAnimation.SwimPhase;
            int prev = Mathf.FloorToInt(lastPhase / Mathf.PI);
            int curr = Mathf.FloorToInt(phase / Mathf.PI);
            if (curr != prev)
                PlayFlap();
            lastPhase = phase;
        }
    }

    void EnsureSource()
    {
        Transform audioChild = transform.Find("SwimFlapAudio");
        GameObject host;
        if (audioChild != null)
        {
            host = audioChild.gameObject;
        }
        else
        {
            host = new GameObject("SwimFlapAudio");
            host.transform.SetParent(transform, false);
            host.transform.localPosition = Vector3.zero;
        }

        source = host.GetComponent<AudioSource>();
        if (source == null)
            source = host.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.priority = 32;
        source.dopplerLevel = 0f;
        source.ignoreListenerPause = true;
        source.volume = 1f;
    }

    public void ConfigureAudibility(float playbackVolume)
    {
        volume = Mathf.Clamp01(playbackVolume);
        EnsureSource();
    }

    void PlayFlap()
    {
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(FloodAudioClips.SwimFlap, DutzAudioSettings.ScaleSfx(volume));
    }

    void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
    }
}
