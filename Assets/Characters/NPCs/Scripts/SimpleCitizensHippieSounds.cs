using UnityEngine;

/// <summary>
/// SC_Hippie zombie audio: looping gnarl while roaming/chasing, chomps during bite.
/// Audibility uses distance + raycast occlusion (not camera visibility).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class SimpleCitizensHippieSounds : MonoBehaviour
{
    const int SampleRate = 44100;
    const float MaxHearingDistance = 72f;
    const float AudioUpdateInterval = 0.12f;

    static AudioClip sharedGnarlLoopClip;
    static AudioClip sharedChompClip;

    [Header("Volumes")]
    [SerializeField] float gnarlVolume = 0.82f;
    [SerializeField] float gnarlVolumeWhileBiting = 0.38f;
    [SerializeField] float chompVolume = 1f;

    [Header("3D audio")]
    [SerializeField] float spatialBlend = 1f;
    [SerializeField] float minDistance = 2f;
    [SerializeField] float maxDistance = 65f;

    [Header("Occlusion")]
    [SerializeField] bool useOcclusion = true;
    [SerializeField] float muffledGnarlMultiplier = 0.28f;

    AudioSource gnarlSource;
    AudioSource chompSource;
    AudioLowPassFilter gnarlLowPass;
    SimpleCitizensNpcPhysics npcPhysics;
    bool biting;
    float nextAudioUpdateTime;
    float currentOcclusionAudibility = 1f;

    public bool IsBiting => biting;

    public static void EnsureOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null || !IsSmallHippieRoot(physics.gameObject.name))
            return;

        if (physics.GetComponent<SimpleCitizensHippieSounds>() == null)
            physics.gameObject.AddComponent<SimpleCitizensHippieSounds>();
    }

    static bool IsSmallHippieRoot(string objectName) =>
        objectName.StartsWith("SimpleCitizens_Hippie_Black")
        || objectName.StartsWith("SimpleCitizens_Hippie_Extra_")
        || (!string.IsNullOrEmpty(objectName) && objectName.StartsWith("DutzSegmentHippie_"))
        || SimpleCitizensFlyingHippie.IsFlyingHippieName(objectName);

    void Awake()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        EnsureSharedClips();
        EnsureAudioSources();
    }

    void OnEnable() => RefreshAudio(force: true);

    void Update()
    {
        if (gnarlSource == null)
            return;

        if (Time.time < nextAudioUpdateTime)
        {
            if (biting || !gnarlSource.isPlaying)
                return;

            gnarlSource.pitch = 0.92f + Mathf.PerlinNoise(Time.time * 0.45f, transform.position.x * 0.1f) * 0.18f;
            return;
        }

        nextAudioUpdateTime = Time.time + AudioUpdateInterval;
        RefreshAudio(force: false);

        if (biting || !gnarlSource.isPlaying)
            return;

        gnarlSource.pitch = 0.92f + Mathf.PerlinNoise(Time.time * 0.45f, transform.position.x * 0.1f) * 0.18f;
    }

    void EnsureAudioSources()
    {
        var sources = GetComponents<AudioSource>();
        AudioSource firstNonLoop = null;

        foreach (var source in sources)
        {
            if (source == null)
                continue;

            if (source.loop && gnarlSource == null)
                gnarlSource = source;
            else if (!source.loop && chompSource == null)
                chompSource = source;
            else if (firstNonLoop == null && !source.loop)
                firstNonLoop = source;
        }

        if (gnarlSource == null)
        {
            gnarlSource = gameObject.AddComponent<AudioSource>();
            gnarlSource.loop = true;
        }

        if (chompSource == null)
        {
            chompSource = firstNonLoop != null ? firstNonLoop : gameObject.AddComponent<AudioSource>();
            chompSource.loop = false;
        }

        gnarlLowPass = gnarlSource.GetComponent<AudioLowPassFilter>();
        if (gnarlLowPass == null)
            gnarlLowPass = gnarlSource.gameObject.AddComponent<AudioLowPassFilter>();

        ConfigureSource(gnarlSource, priority: 160, loop: true);
        gnarlSource.clip = sharedGnarlLoopClip;

        ConfigureSource(chompSource, priority: 24, loop: false);
        chompSource.clip = sharedChompClip;

        ApplyVolumes();
    }

    void ConfigureSource(AudioSource source, int priority, bool loop)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.spread = 60f;
        source.priority = priority;
        source.ignoreListenerPause = true;
    }

    void RefreshAudio(bool force)
    {
        if (gnarlSource == null)
            return;

        currentOcclusionAudibility = useOcclusion
            ? DutzAudioOcclusion.Evaluate(transform, transform.position).audibility
            : 1f;

        ApplyVolumes();

        var shouldPlay = ShouldPlayGnarl() && currentOcclusionAudibility > 0.02f;
        if (!force && shouldPlay == gnarlSource.isPlaying)
            return;

        if (shouldPlay)
        {
            if (!gnarlSource.isPlaying)
                gnarlSource.Play();
        }
        else if (gnarlSource.isPlaying)
        {
            gnarlSource.Stop();
        }
    }

    void ApplyVolumes()
    {
        if (gnarlSource == null)
            return;

        var baseVolume = biting ? gnarlVolumeWhileBiting : gnarlVolume;
        var occlusionScale = currentOcclusionAudibility >= 0.99f
            ? 1f
            : Mathf.Lerp(muffledGnarlMultiplier, 1f, currentOcclusionAudibility);

        gnarlSource.volume = DutzAudioSettings.ScaleSfx(baseVolume * occlusionScale);

        if (gnarlLowPass != null)
        {
            var muffled = useOcclusion && currentOcclusionAudibility < 0.99f;
            gnarlLowPass.enabled = muffled;
            gnarlLowPass.cutoffFrequency = muffled ? 900f : 22000f;
        }

        if (chompSource != null)
            chompSource.volume = DutzAudioSettings.ScaleSfx(chompVolume * Mathf.Max(currentOcclusionAudibility, 0.35f));
    }

    bool ShouldPlayGnarl()
    {
        if (!IsWithinHearingDistance())
            return false;

        if (biting)
            return true;

        return npcPhysics != null && npcPhysics.IsChasing;
    }

    bool IsWithinHearingDistance()
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
            return false;

        var delta = transform.position - player.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= MaxHearingDistance * MaxHearingDistance;
    }

    public void SetBiting(bool isBiting)
    {
        biting = isBiting;
        if (gnarlSource == null)
            return;

        if (isBiting)
            gnarlSource.pitch = 1.05f;

        RefreshAudio(force: true);
    }

    public void PlayChomp(float pitch = 1f)
    {
        if (chompSource == null)
            return;

        EnsureSharedClips();

        if (useOcclusion)
            currentOcclusionAudibility = DutzAudioOcclusion.Evaluate(transform, transform.position).audibility;

        chompSource.pitch = pitch;
        chompSource.volume = DutzAudioSettings.ScaleSfx(chompVolume * Mathf.Max(currentOcclusionAudibility, 0.4f));
        chompSource.PlayOneShot(sharedChompClip, chompSource.volume);
    }

    static void EnsureSharedClips()
    {
        if (sharedGnarlLoopClip == null)
            sharedGnarlLoopClip = CreateGnarlLoopClip("Hippie_ZombieGnarl_Shared", 2.5f);

        if (sharedChompClip == null)
            sharedChompClip = CreateChompClip("Hippie_Chomp_Shared", 0.14f);
    }

    static AudioClip CreateGnarlLoopClip(string name, float lengthSeconds)
    {
        var sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * lengthSeconds));
        var data = new float[sampleCount];
        var lpNoise = 0f;

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)SampleRate;
            var cycle = t / lengthSeconds;

            var growlLfo = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 2f * cycle);
            var raspLfo = 0.4f + 0.6f * Mathf.Sin(2f * Mathf.PI * 5f * cycle + 0.7f);
            var baseHz = 62f + 22f * Mathf.Sin(2f * Mathf.PI * 1f * cycle);

            var growl =
                Mathf.Sin(2f * Mathf.PI * baseHz * t) * 0.5f +
                Mathf.Sin(2f * Mathf.PI * baseHz * 1.97f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * baseHz * 3.1f * t) * 0.14f;

            var rawNoise = Mathf.PerlinNoise(t * 28f, 0.15f) * 2f - 1f;
            lpNoise = Mathf.Lerp(lpNoise, rawNoise, 0.22f);
            var rasp = lpNoise * raspLfo;

            data[i] = (growl * growlLfo * 0.62f + rasp * 0.38f) * 0.72f;
        }

        return BuildClip(name, data);
    }

    static AudioClip CreateChompClip(string name, float lengthSeconds)
    {
        var sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * lengthSeconds));
        var data = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)SampleRate;
            var norm = t / lengthSeconds;

            var snap = Mathf.Exp(-t * 95f);
            var crack = Mathf.Exp(-Mathf.Max(0f, t - 0.012f) * 120f);
            var thump = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 48f);
            var noise = (Random.Range(-1f, 1f) * 0.7f + Random.Range(-1f, 1f) * 0.3f) * snap;

            var bitePeak = norm < 0.35f ? 1f : Mathf.Exp(-(norm - 0.35f) * 14f);
            data[i] = (noise * 0.65f + thump * 0.35f) * crack * bitePeak * 1.05f;
        }

        return BuildClip(name, data);
    }

    static AudioClip BuildClip(string clipName, float[] data)
    {
        var clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
