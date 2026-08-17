using UnityEngine;

/// <summary>
/// Footsteps while moving; jump/land one-shots. Works with or without DutzWalkAnimation.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(400)]
public class DutzMovementSounds : MonoBehaviour
{
    [SerializeField] AudioClip walkStepClip;
    [SerializeField] AudioClip runStepClip;
    [SerializeField] AudioClip jumpClip;
    [SerializeField] AudioClip landClip;
    [SerializeField] AudioClip punchSwingClip;
    [SerializeField] AudioClip punchHitClip;
    [SerializeField] AudioClip punchMissClip;
    [SerializeField] float walkStepVolume = 0.45f;
    [SerializeField] float runStepVolume = 0.55f;
    [SerializeField] float jumpVolume = 0.6f;
    [SerializeField] float landVolume = 0.35f;
    [SerializeField] float punchSwingVolume = 0.5f;
    [SerializeField] float punchHitVolume = 0.7f;
    [SerializeField] float punchMissVolume = 0.35f;
    [SerializeField] float walkStepInterval = 0.42f;
    [SerializeField] float runStepInterval = 0.26f;
    [SerializeField] Vector2 walkPitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField] Vector2 runPitchRange = new Vector2(1.05f, 1.2f);

    AudioSource source;
    DutzPlayerController player;
    DutzWalkAnimation walkAnim;
    CharacterController cc;
    float lastWalkPhase;
    float stepTimer;
    bool wasGrounded = true;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.minDistance = 1f;
        source.maxDistance = 500f;
        source.rolloffMode = AudioRolloffMode.Linear;

        player = GetComponent<DutzPlayerController>();
        walkAnim = GetComponent<DutzWalkAnimation>();
        cc = GetComponent<CharacterController>();

        if (walkStepClip == null)
            walkStepClip = CreateFootstepClip("Dutz_WalkStep", 0.09f, 0.55f, 90f);
        if (runStepClip == null)
            runStepClip = CreateFootstepClip("Dutz_RunStep", 0.07f, 0.7f, 130f);
        if (jumpClip == null)
            jumpClip = CreateJumpClip("Dutz_Jump", 0.18f);
        if (landClip == null)
            landClip = CreateLandClip("Dutz_Land", 0.1f);
        if (punchSwingClip == null)
            punchSwingClip = CreatePunchSwingClip("Dutz_PunchSwing", 0.1f);
        if (punchHitClip == null)
            punchHitClip = CreatePunchHitClip("Dutz_PunchHit", 0.12f);
        if (punchMissClip == null)
            punchMissClip = CreatePunchMissClip("Dutz_PunchMiss", 0.08f);
    }

    void OnEnable()
    {
        if (player != null)
            player.Jumped += OnJumped;
    }

    void OnDisable()
    {
        if (player != null)
            player.Jumped -= OnJumped;
    }

    void OnJumped()
    {
        PlayOneShot(jumpClip, jumpVolume, 1f);
    }

    public void PlayPunchSwing() => PlayOneShot(punchSwingClip, punchSwingVolume, Random.Range(0.98f, 1.08f));

    public void PlayPunchHit() => PlayOneShot(punchHitClip, punchHitVolume, Random.Range(0.95f, 1.1f));

    public void PlayPunchMiss() => PlayOneShot(punchMissClip, punchMissVolume, Random.Range(0.92f, 1.05f));

    void LateUpdate()
    {
        if (player == null || cc == null)
            return;

        if (player.ControlsLocked)
        {
            stepTimer = 0f;
            return;
        }

        var grounded = IsEffectivelyGrounded();
        if (grounded && !wasGrounded)
            PlayOneShot(landClip, landVolume, Random.Range(0.9f, 1.05f));

        wasGrounded = grounded;

        if (!grounded || !player.IsMoving)
        {
            stepTimer = 0f;
            if (walkAnim != null)
                lastWalkPhase = walkAnim.WalkPhase;
            return;
        }

        if (walkAnim != null && walkAnim.MoveBlendAmount >= 0.35f)
            PlayFootstepsFromWalkCycle();
        else
            PlayFootstepsFromTimer();
    }

    void PlayFootstepsFromWalkCycle()
    {
        var phase = walkAnim.WalkPhase;
        var prevIndex = Mathf.FloorToInt(lastWalkPhase / Mathf.PI);
        var currIndex = Mathf.FloorToInt(phase / Mathf.PI);
        if (currIndex != prevIndex)
            PlayFootstep(player.IsRunning);

        lastWalkPhase = phase;
    }

    void PlayFootstepsFromTimer()
    {
        var interval = player.IsRunning ? runStepInterval : walkStepInterval;
        stepTimer += Time.deltaTime;
        if (stepTimer < interval)
            return;

        stepTimer = 0f;
        PlayFootstep(player.IsRunning);
    }

    void PlayFootstep(bool running)
    {
        var clip = running ? runStepClip : walkStepClip;
        var vol = running ? runStepVolume : walkStepVolume;
        var pitch = running
            ? Random.Range(runPitchRange.x, runPitchRange.y)
            : Random.Range(walkPitchRange.x, walkPitchRange.y);
        PlayOneShot(clip, vol, pitch);
    }

    bool IsEffectivelyGrounded()
    {
        if (cc.isGrounded && player.VerticalSpeed <= 0.5f)
            return true;

        var origin = transform.position + Vector3.up * 0.15f;
        var rayLength = 0.6f * Mathf.Max(transform.lossyScale.y, 1f);
        return Physics.Raycast(origin, Vector3.down, rayLength, ~0, QueryTriggerInteraction.Ignore);
    }

    void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || source == null)
            return;

        source.pitch = pitch;
        source.PlayOneShot(clip, DutzAudioSettings.ScaleSfx(volume));
    }

    const int SfxSampleRate = 44100;

    static AudioClip CreateFootstepClip(string name, float length, float volume, float thumpHz)
    {
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];
        var lp = 0f;

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var env = Mathf.Exp(-t * 42f);
            var noise = Random.Range(-1f, 1f);
            lp = Mathf.Lerp(lp, noise, 0.35f);
            var thump = Mathf.Sin(2f * Mathf.PI * thumpHz * t) * Mathf.Exp(-t * 55f);
            data[i] = (lp * 0.55f + thump * 0.45f) * env * volume;
        }

        return BuildClip(name, data);
    }

    static AudioClip CreateJumpClip(string name, float length)
    {
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var norm = t / length;
            var env = Mathf.Sin(norm * Mathf.PI);
            var freq = Mathf.Lerp(180f, 520f, norm);
            var whoosh = Mathf.Sin(2f * Mathf.PI * freq * t);
            var noise = Random.Range(-0.25f, 0.25f) * (1f - norm);
            data[i] = (whoosh * 0.65f + noise * 0.35f) * env * 0.55f;
        }

        return BuildClip(name, data);
    }

    static AudioClip CreateLandClip(string name, float length)
    {
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var env = Mathf.Exp(-t * 38f);
            var thump = Mathf.Sin(2f * Mathf.PI * 70f * t);
            data[i] = thump * env * 0.4f;
        }

        return BuildClip(name, data);
    }

    static AudioClip CreatePunchSwingClip(string name, float length)
    {
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var norm = t / length;
            var env = Mathf.Sin(norm * Mathf.PI);
            var freq = Mathf.Lerp(240f, 680f, norm);
            var whoosh = Mathf.Sin(2f * Mathf.PI * freq * t);
            var noise = Random.Range(-0.3f, 0.3f) * (1f - norm * 0.85f);
            data[i] = (whoosh * 0.6f + noise * 0.4f) * env * 0.58f;
        }

        return BuildClip(name, data);
    }

    static AudioClip CreatePunchHitClip(string name, float length)
    {
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];
        var lp = 0f;

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var env = Mathf.Exp(-t * 32f);
            var thump = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 48f);
            var crack = Mathf.Sin(2f * Mathf.PI * 280f * t) * Mathf.Exp(-t * 70f);
            var noise = Random.Range(-1f, 1f);
            lp = Mathf.Lerp(lp, noise, 0.45f);
            data[i] = (thump * 0.5f + crack * 0.25f + lp * 0.25f) * env * 0.72f;
        }

        return BuildClip(name, data);
    }

    static AudioClip CreatePunchMissClip(string name, float length)
    {
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var norm = t / length;
            var env = Mathf.Sin(norm * Mathf.PI);
            var freq = Mathf.Lerp(320f, 520f, norm);
            var whoosh = Mathf.Sin(2f * Mathf.PI * freq * t);
            data[i] = whoosh * env * 0.32f;
        }

        return BuildClip(name, data);
    }

    static AudioClip BuildClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SfxSampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
