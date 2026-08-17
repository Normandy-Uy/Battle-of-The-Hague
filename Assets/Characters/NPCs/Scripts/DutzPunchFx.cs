using System.Collections.Generic;
using UnityEngine;

/// <summary>Comic punch feedback — floating KAPOW text, fist flash, screen pop.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public class DutzPunchFx : MonoBehaviour
{
    struct Burst
    {
        public string Text;
        public Vector3 WorldPosition;
        public float StartTime;
        public float Duration;
        public Color Color;
        public int FontSize;
        public float RiseSpeed;
    }

    static readonly string[] HitWords = { "KAPOW!", "POW!", "BAM!", "WHAM!", "SMASH!" };
    static readonly string[] MissWords = { "SWISH!", "WHOOSH!", "HI-YAH!" };

    static DutzPunchFx instance;
    static readonly List<Burst> Bursts = new List<Burst>();

    float screenFlashAlpha;
    float screenFlashDuration = 0.18f;
    Color screenFlashColor = Color.white;
    float fistFlashUntil;
    Vector3 fistFlashWorld;
    Camera viewCamera;

    public static void EnsureFromBoot()
    {
        var player = DutzPlayerController.Instance
            ?? Object.FindObjectOfType<DutzPlayerController>();
        if (player == null)
            return;

        if (player.GetComponent<DutzPunchFx>() == null)
            player.gameObject.AddComponent<DutzPunchFx>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        viewCamera = Camera.main;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void PlayWindup(Vector3 fistWorldPosition)
    {
        EnsureInstance();
        if (instance == null)
            return;

        instance.fistFlashWorld = fistWorldPosition;
        instance.fistFlashUntil = Time.time + 0.16f;
        instance.AddBurst(
            MissWords[Random.Range(0, MissWords.Length)],
            fistWorldPosition + Vector3.up * 0.35f,
            new Color(1f, 0.92f, 0.35f),
            34,
            1.6f,
            0.55f);
    }

    public static void PlayHit(Vector3 worldPosition)
    {
        EnsureInstance();
        if (instance == null)
            return;

        instance.screenFlashColor = new Color(1f, 0.35f, 0.15f, 1f);
        instance.screenFlashAlpha = 0.42f;
        instance.screenFlashDuration = 0.16f;
        instance.AddBurst(
            HitWords[Random.Range(0, HitWords.Length)],
            worldPosition + Vector3.up * 1.4f,
            new Color(1f, 0.2f, 0.1f),
            52,
            2.4f,
            0.75f);
        instance.AddBurst(
            "PUNCH!",
            worldPosition + Vector3.up * 0.6f + Random.insideUnitSphere * 0.25f,
            new Color(1f, 0.85f, 0.1f),
            28,
            1.8f,
            0.5f);
    }

    public static void PlayMiss(Vector3 fistWorldPosition)
    {
        EnsureInstance();
        if (instance == null)
            return;

        instance.AddBurst(
            "MISS!",
            fistWorldPosition + Vector3.up * 0.8f,
            new Color(0.75f, 0.85f, 1f),
            30,
            1.4f,
            0.45f);
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        EnsureFromBoot();
    }

    void AddBurst(string text, Vector3 worldPosition, Color color, int fontSize, float riseSpeed, float duration)
    {
        Bursts.Add(new Burst
        {
            Text = text,
            WorldPosition = worldPosition,
            StartTime = Time.time,
            Duration = duration,
            Color = color,
            FontSize = fontSize,
            RiseSpeed = riseSpeed
        });
    }

    void Update()
    {
        if (screenFlashAlpha > 0f && screenFlashDuration > 0f)
            screenFlashAlpha = Mathf.MoveTowards(screenFlashAlpha, 0f, Time.deltaTime / screenFlashDuration);

        for (var i = Bursts.Count - 1; i >= 0; i--)
        {
            if (Time.time - Bursts[i].StartTime > Bursts[i].Duration)
                Bursts.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        GUI.depth = -4000;
        DrawScreenFlash();
        DrawFistFlash();
        DrawBursts();
    }

    void DrawScreenFlash()
    {
        if (screenFlashAlpha <= 0.001f)
            return;

        var prev = GUI.color;
        GUI.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, screenFlashAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    void DrawFistFlash()
    {
        if (Time.time > fistFlashUntil)
            return;

        var cam = GetViewCamera();
        if (cam == null)
            return;

        var screen = cam.WorldToScreenPoint(fistFlashWorld);
        if (screen.z <= 0f)
            return;

        var t = 1f - (fistFlashUntil - Time.time) / 0.16f;
        var size = Mathf.Lerp(18f, 110f, t);
        var alpha = Mathf.Lerp(0.85f, 0f, t);
        var center = new Vector2(screen.x, Screen.height - screen.y);
        var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);

        var prev = GUI.color;
        GUI.color = new Color(1f, 0.75f, 0.15f, alpha);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.35f, 0.05f, alpha * 0.65f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = prev;
    }

    void DrawBursts()
    {
        if (Bursts.Count == 0)
            return;

        var cam = GetViewCamera();
        if (cam == null)
            return;

        for (var i = 0; i < Bursts.Count; i++)
        {
            var burst = Bursts[i];
            var age = Time.time - burst.StartTime;
            if (age > burst.Duration)
                continue;

            var world = burst.WorldPosition + Vector3.up * (burst.RiseSpeed * age);
            var screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                continue;

            var fade = 1f - Mathf.Clamp01(age / burst.Duration);
            var scale = 1f + Mathf.Sin(Mathf.Clamp01(age / 0.08f) * Mathf.PI) * 0.35f;
            DrawComicLabel(
                burst.Text,
                new Vector2(screen.x, Screen.height - screen.y),
                burst.Color,
                Mathf.RoundToInt(burst.FontSize * scale),
                fade);
        }
    }

    static void DrawComicLabel(string text, Vector2 screenCenter, Color color, int fontSize, float alpha)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        var shadow = new GUIStyle(style)
        {
            normal = { textColor = new Color(0f, 0f, 0f, alpha * 0.85f) }
        };
        style.normal.textColor = new Color(color.r, color.g, color.b, alpha);

        var content = new GUIContent(text);
        var size = style.CalcSize(content);
        var rect = new Rect(screenCenter.x - size.x * 0.5f, screenCenter.y - size.y * 0.5f, size.x, size.y);
        var shadowRect = new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height);

        GUI.Label(shadowRect, content, shadow);
        GUI.Label(rect, content, style);
    }

    Camera GetViewCamera()
    {
        if (viewCamera != null && viewCamera.isActiveAndEnabled)
            return viewCamera;

        viewCamera = Camera.main;
        return viewCamera;
    }
}
