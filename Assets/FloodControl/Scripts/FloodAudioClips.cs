using UnityEngine;

/// <summary>
/// Shared procedural Flood Control SFX clips. No external audio assets required.
/// </summary>
public static class FloodAudioClips
{
    public const int SampleRate = 44100;

    static AudioClip swimFlap;
    static AudioClip crocGrowlLoop;
    static AudioClip crocBite;
    static AudioClip ratSqueak;
    static AudioClip burnScreechLoop;
    static AudioClip punchHit;

    public static AudioClip SwimFlap =>
        swimFlap != null ? swimFlap : (swimFlap = CreateSwimFlap("Flood_SwimFlap", 0.22f));

    public static AudioClip CrocGrowlLoop =>
        crocGrowlLoop != null
            ? crocGrowlLoop
            : (crocGrowlLoop = CreateCrocGrowlLoop("Flood_CrocGrowl", 2.4f));

    public static AudioClip CrocBite =>
        crocBite != null ? crocBite : (crocBite = CreateCrocBite("Flood_CrocBite", 0.28f));

    public static AudioClip RatSqueak =>
        ratSqueak != null ? ratSqueak : (ratSqueak = CreateRatSqueak("Flood_RatSqueak", 0.16f));

    public static AudioClip BurnScreechLoop =>
        burnScreechLoop != null
            ? burnScreechLoop
            : (burnScreechLoop = CreateBurnScreechLoop("Flood_BurnScreech", 1.1f));

    public static AudioClip PunchHit =>
        punchHit != null ? punchHit : (punchHit = CreatePunchHit("Flood_PunchHit", 0.18f));

    static AudioClip CreateSwimFlap(string name, float length)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        float[] data = new float[samples];
        float lp = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float norm = t / length;
            float env = Mathf.Sin(norm * Mathf.PI);
            float whooshHz = Mathf.Lerp(130f, 520f, norm);
            float whoosh = Mathf.Sin(2f * Mathf.PI * whooshHz * t);
            float splash = Random.Range(-1f, 1f);
            lp = Mathf.Lerp(lp, splash, 0.55f);
            float firstSlap = Mathf.Exp(-t * 65f);
            float secondTime = Mathf.Max(0f, t - 0.075f);
            float secondSlap = t >= 0.075f ? Mathf.Exp(-secondTime * 58f) : 0f;
            float waterThump =
                Mathf.Sin(2f * Mathf.PI * 125f * t) * Mathf.Exp(-t * 22f);
            float bubble =
                Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-t * 30f);
            data[i] = Mathf.Clamp(
                whoosh * env * 0.35f
                + lp * (firstSlap * 0.65f + secondSlap * 0.5f)
                + waterThump * 0.5f
                + bubble * firstSlap * 0.15f,
                -0.98f,
                0.98f);
        }

        return Build(name, data);
    }

    static AudioClip CreateCrocGrowlLoop(string name, float length)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        float[] data = new float[samples];
        float lp = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float cycle = t / length;
            float baseHz = 48f + 14f * Mathf.Sin(2f * Mathf.PI * cycle);
            float breath = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 1.6f * cycle);
            float growl =
                Mathf.Sin(2f * Mathf.PI * baseHz * t) * 0.55f +
                Mathf.Sin(2f * Mathf.PI * baseHz * 1.95f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * baseHz * 2.9f * t) * 0.12f;
            float raw = Mathf.PerlinNoise(t * 18f, 0.33f) * 2f - 1f;
            lp = Mathf.Lerp(lp, raw, 0.18f);
            data[i] = (growl * breath * 0.7f + lp * 0.3f) * 0.78f;
        }

        return Build(name, data);
    }

    static AudioClip CreateCrocBite(string name, float length)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        float[] data = new float[samples];
        float lp = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float snapEnvelope = Mathf.Exp(-t * 38f);
            float crunchEnvelope = Mathf.Exp(-Mathf.Max(0f, t - 0.035f) * 18f);
            float jawSnap = Mathf.Sin(2f * Mathf.PI * 105f * t) * snapEnvelope;
            float boneCrack = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 65f);
            float raw = Random.Range(-1f, 1f);
            lp = Mathf.Lerp(lp, raw, 0.5f);
            data[i] = (jawSnap * 0.55f + boneCrack * 0.25f + lp * crunchEnvelope * 0.4f) * 0.95f;
        }

        return Build(name, data);
    }

    static AudioClip CreateRatSqueak(string name, float length)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float norm = t / length;
            float env = Mathf.Sin(norm * Mathf.PI);
            float freq = Mathf.Lerp(2100f, 3400f, norm);
            float chirp = Mathf.Sin(2f * Mathf.PI * freq * t);
            float vibrato = Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.18f;
            float noise = Random.Range(-0.2f, 0.2f) * (1f - norm);
            data[i] = (chirp * (0.82f + vibrato) + noise) * env * 0.42f;
        }

        return Build(name, data);
    }

    static AudioClip CreateBurnScreechLoop(string name, float length)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        float[] data = new float[samples];
        float lp = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float cycle = t / length;
            float screamHz = 780f + 420f * Mathf.Sin(2f * Mathf.PI * 3.2f * cycle);
            float scream =
                Mathf.Sin(2f * Mathf.PI * screamHz * t) * 0.55f +
                Mathf.Sin(2f * Mathf.PI * screamHz * 1.51f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * screamHz * 2.07f * t) * 0.12f;
            float hiss = Random.Range(-1f, 1f);
            lp = Mathf.Lerp(lp, hiss, 0.55f);
            float pulse = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 7f * cycle);
            data[i] = (scream * 0.72f + lp * 0.28f) * pulse * 0.8f;
        }

        return Build(name, data);
    }

    static AudioClip CreatePunchHit(string name, float length)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * length));
        float[] data = new float[samples];
        float lowPassNoise = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float thump = Mathf.Sin(2f * Mathf.PI * 92f * t) * Mathf.Exp(-t * 32f);
            float crack = Mathf.Sin(2f * Mathf.PI * 510f * t) * Mathf.Exp(-t * 58f);
            float raw = Random.Range(-1f, 1f);
            lowPassNoise = Mathf.Lerp(lowPassNoise, raw, 0.42f);
            float splash = lowPassNoise * Mathf.Exp(-t * 24f);
            data[i] = Mathf.Clamp(
                thump * 0.68f + crack * 0.28f + splash * 0.32f,
                -0.98f,
                0.98f);
        }

        return Build(name, data);
    }

    static AudioClip Build(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
