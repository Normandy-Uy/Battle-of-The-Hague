using UnityEngine;

/// <summary>
/// Shared one-shot SFX for powerup pickups (Super Jump / Super Punch / Force Field Suit).
/// Procedural clips — same approach as gold coin / health potion — until custom WAVs are dropped in public/.
/// </summary>
public static class DutzPowerupPickupSounds
{
    public enum Kind
    {
        SuperJump,
        SuperPunch,
        ForceFieldSuit,
    }

    const int SampleRate = 44100;

    static AudioSource oneShotSource;
    static AudioClip jumpClip;
    static AudioClip punchClip;
    static AudioClip suitClip;

    public static void Play(Kind kind)
    {
        EnsureAudio();
        if (oneShotSource == null)
            return;

        var clip = GetClip(kind);
        if (clip == null)
            return;

        oneShotSource.pitch = Random.Range(0.96f, 1.05f);
        oneShotSource.PlayOneShot(clip, DutzAudioSettings.ScaleSfx(VolumeFor(kind)));
    }

    static float VolumeFor(Kind kind) => kind switch
    {
        Kind.SuperJump => 0.88f,
        Kind.SuperPunch => 0.92f,
        Kind.ForceFieldSuit => 0.9f,
        _ => 0.85f,
    };

    static AudioClip GetClip(Kind kind)
    {
        switch (kind)
        {
            case Kind.SuperJump:
                return jumpClip ??= CreateSuperJumpClip();
            case Kind.SuperPunch:
                return punchClip ??= CreateSuperPunchClip();
            case Kind.ForceFieldSuit:
                return suitClip ??= CreateForceFieldSuitClip();
            default:
                return null;
        }
    }

    static void EnsureAudio()
    {
        if (oneShotSource != null)
            return;

        var go = new GameObject("DutzPowerupPickupAudio");
        Object.DontDestroyOnLoad(go);
        oneShotSource = go.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.priority = 32;
        oneShotSource.ignoreListenerPause = true;
    }

    /// <summary>Rising springy chirp — bounce / launch feel.</summary>
    static AudioClip CreateSuperJumpClip()
    {
        const float length = 0.34f;
        var samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SampleRate;
            var rise = Mathf.Lerp(320f, 980f, Mathf.Clamp01(t / 0.22f));
            var body = Mathf.Sin(2f * Mathf.PI * rise * t) * Mathf.Exp(-t * 9f);
            var twang = Mathf.Sin(2f * Mathf.PI * (rise * 1.9f) * t) * Mathf.Exp(-t * 16f) * 0.35f;
            var whoosh = (Mathf.PerlinNoise(t * 40f, 0.2f) * 2f - 1f) * Mathf.Exp(-t * 14f) * 0.18f;
            data[i] = (body * 0.7f + twang + whoosh) * 0.72f;
        }

        return BuildClip("DutzSuperJumpCollect", data);
    }

    /// <summary>Sharp hit + glove thud.</summary>
    static AudioClip CreateSuperPunchClip()
    {
        const float length = 0.26f;
        var samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SampleRate;
            var crack = Mathf.Exp(-t * 90f) * (Random.Range(-1f, 1f) * 0.55f);
            var thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 22f);
            var slap = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 38f) * 0.45f;
            data[i] = (crack * 0.55f + thud * 0.7f + slap) * 0.78f;
        }

        return BuildClip("DutzSuperPunchCollect", data);
    }

    /// <summary>Shimmering shield power-up.</summary>
    static AudioClip CreateForceFieldSuitClip()
    {
        const float length = 0.42f;
        var samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SampleRate;
            var hum = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 5.5f) * 0.45f;
            var shimmerA = Mathf.Sin(2f * Mathf.PI * 740f * t) * Mathf.Exp(-t * 11f) * 0.4f;
            var shimmerB = Mathf.Sin(2f * Mathf.PI * 1180f * t) * Mathf.Exp(-t * 14f) * 0.28f;
            var sparkle = Mathf.Sin(2f * Mathf.PI * 2100f * t) * Mathf.Exp(-t * 22f) * 0.18f;
            data[i] = (hum + shimmerA + shimmerB + sparkle) * 0.75f;
        }

        return BuildClip("DutzForceFieldSuitCollect", data);
    }

    static AudioClip BuildClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
