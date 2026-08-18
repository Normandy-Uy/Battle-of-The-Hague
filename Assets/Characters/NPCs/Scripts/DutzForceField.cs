using UnityEngine;

/// <summary>
/// Timed force-field shield. Hosted on the Force Field Suit pickup (not Player1).
/// On Activate, the bubble visual follows the player for the duration.
/// Shop purchases on other levels may still add a temporary instance on the player.
/// </summary>
[DisallowMultipleComponent]
public class DutzForceField : MonoBehaviour
{
    public const float ShieldedMinAirSecondsBeforeEdgeFall = 3.5f;
    public const float ShieldedDeckLookaheadMeters = 25f;
    public const float ShieldedLongFallSeconds = 6f;

    const string VisualChildName = "ForceFieldVisual";
    const float PulseSpeed = 2.2f;
    const float PulseAmount = 0.06f;
    const float UiPadding = 16f;
    const float UiWidth = 220f;
    const float UiBarHeight = 22f;

    [SerializeField, Min(0.1f), Tooltip("How long the force field stays active after pickup (seconds).")]
    float durationSeconds = 60f;

    [SerializeField] Color fieldColor = new Color(0.35f, 0.85f, 1f, 0.28f);
    [SerializeField] float fieldScale = 1.35f;

    static DutzForceField activeInstance;

    DutzPlayerController shieldedPlayer;
    CharacterController characterController;
    Transform visualRoot;
    Transform visualHome;
    Vector3 visualBaseScale;
    bool active;
    bool permanent;
    float expiresAt;
    float collectedFlashUntil;
    float activeFlashUntil;
    float expiredFlashUntil;
    Material runtimeMaterial;

    public float DurationSeconds => Mathf.Max(0.1f, durationSeconds);

    public bool IsActive => active && (permanent || Time.time < expiresAt);

    public float RemainingSeconds => permanent ? float.PositiveInfinity : (active ? Mathf.Max(0f, expiresAt - Time.time) : 0f);

    public float GetShieldWorldRadius()
    {
        EnsureVisual();
        if (visualRoot == null)
            return 0.75f;

        var scale = visualRoot.lossyScale;
        return Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;
    }

    public static bool IsPlayerShielded(DutzPlayerController player)
    {
        if (player == null)
            return false;

        if (activeInstance != null
            && activeInstance.IsActive
            && activeInstance.shieldedPlayer == player)
            return true;

        var field = player.GetComponent<DutzForceField>();
        return field != null && field.IsActive;
    }

    public static DutzForceField FindForPlayer(DutzPlayerController player)
    {
        if (player == null)
            return null;

        if (activeInstance != null
            && activeInstance.IsActive
            && activeInstance.shieldedPlayer == player)
            return activeInstance;

        return player.GetComponent<DutzForceField>();
    }

    public static void DeactivateForPlayer(DutzPlayerController player)
    {
        if (player == null)
            return;

        if (activeInstance != null && activeInstance.shieldedPlayer == player)
            activeInstance.Deactivate();

        player.GetComponent<DutzForceField>()?.Deactivate();
    }

    /// <summary>Re-attaches the bubble after mid-life respawn without clearing the timer.</summary>
    public static void RefreshForPlayer(DutzPlayerController player)
    {
        FindForPlayer(player)?.RefreshForActivePlayer();
    }

    /// <summary>Ensures the suit hosts the field and strips any leftover Player1 instance.</summary>
    public static DutzForceField EnsureOnSuit(GameObject suit)
    {
        StripFromPlayers();

        if (suit == null)
            return null;

        var field = suit.GetComponent<DutzForceField>();
        if (field == null)
            field = suit.AddComponent<DutzForceField>();

        return field;
    }

    public static void StripFromPlayers()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            if (player == null)
                continue;

            foreach (var field in player.GetComponents<DutzForceField>())
            {
                if (field == null)
                    continue;

                // Keep an active shop-purchased shield on the player.
                if (field.IsActive)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(field);
                else
                    Object.DestroyImmediate(field);
            }

            var leftoverVisual = player.transform.Find(VisualChildName);
            if (leftoverVisual == null)
                continue;

            if (Application.isPlaying)
                Object.Destroy(leftoverVisual.gameObject);
            else
                Object.DestroyImmediate(leftoverVisual.gameObject);
        }
    }

    void Awake()
    {
        visualHome = transform;
        characterController = GetComponent<CharacterController>();
        EnsureVisual();
        SetVisualActive(false);
    }

    void OnEnable()
    {
        if (active && shieldedPlayer != null)
            activeInstance = this;
    }

    void OnDisable()
    {
        if (activeInstance == this)
            activeInstance = null;
    }

    void EnsureVisual()
    {
        if (visualRoot != null)
            return;

        var existing = transform.Find(VisualChildName);
        if (existing == null && shieldedPlayer != null)
            existing = shieldedPlayer.transform.Find(VisualChildName);

        if (existing != null)
        {
            visualRoot = existing;
            visualBaseScale = visualRoot.localScale;
            return;
        }

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = VisualChildName;
        sphere.transform.SetParent(transform, false);

        var col = sphere.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        runtimeMaterial = new Material(Shader.Find("Sprites/Default"));
        runtimeMaterial.color = fieldColor;
        var renderer = sphere.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = runtimeMaterial;

        visualRoot = sphere.transform;
        ApplyVisualScaleToHost(null);
        SetVisualActive(false);
    }

    void ApplyVisualScaleToHost(DutzPlayerController player)
    {
        if (visualRoot == null)
            return;

        var cc = player != null
            ? player.GetComponent<CharacterController>()
            : characterController;
        var radius = cc != null ? cc.radius : 0.4f;
        var height = cc != null ? cc.height : 1.8f;
        visualBaseScale = new Vector3(radius * 2.2f, height * fieldScale, radius * 2.2f) * fieldScale;
        visualRoot.localScale = visualBaseScale;
        if (cc != null)
            visualRoot.localPosition = cc.center;
        else
            visualRoot.localPosition = Vector3.zero;
    }

    void Update()
    {
        if (!active || permanent)
            return;

        if (Time.time >= expiresAt)
        {
            expiredFlashUntil = Time.time + 2.5f;
            Deactivate();
            Debug.Log("[Dutz] Force field expired.");
        }
    }

    void LateUpdate()
    {
        if (!active || visualRoot == null)
            return;

        var pulse = 1f + Mathf.Sin(Time.time * PulseSpeed) * PulseAmount;
        visualRoot.localScale = visualBaseScale * pulse;
    }

    public void Activate() => Activate(DutzPlayerController.Instance);

    public void Activate(DutzPlayerController player)
    {
        if (player == null)
            return;

        EnsureVisual();
        shieldedPlayer = player;
        activeInstance = this;
        AttachVisualToPlayer(player);
        active = true;
        permanent = false;
        var duration = DurationSeconds;
        expiresAt = Time.time + duration;
        collectedFlashUntil = Time.time + 3.5f;
        activeFlashUntil = Time.time + 7f;
        SetVisualActive(true);
        Debug.Log($"[Dutz] Force field active for {duration:F0}s.");
    }

    public void ActivatePermanent(DutzPlayerController player)
    {
        if (player == null)
            return;

        EnsureVisual();
        shieldedPlayer = player;
        activeInstance = this;
        AttachVisualToPlayer(player);
        active = true;
        permanent = true;
        expiresAt = float.MaxValue;
        collectedFlashUntil = Time.time + 3.5f;
        activeFlashUntil = Time.time + 7f;
        SetVisualActive(true);
        Debug.Log("[Dutz] Force field active — unlimited (Senior Citizen Mode).");
    }

    public void Deactivate()
    {
        active = false;
        permanent = false;
        expiresAt = 0f;
        SetVisualActive(false);
        RestoreVisualHome();
        if (activeInstance == this)
            activeInstance = null;
        shieldedPlayer = null;
    }

    public void RefreshForActivePlayer()
    {
        if (!active || shieldedPlayer == null)
            return;

        EnsureVisual();
        AttachVisualToPlayer(shieldedPlayer);
        SetVisualActive(true);
    }

    void AttachVisualToPlayer(DutzPlayerController player)
    {
        if (visualRoot == null || player == null)
            return;

        visualRoot.SetParent(player.transform, false);
        ApplyVisualScaleToHost(player);
    }

    void RestoreVisualHome()
    {
        if (visualRoot == null)
            return;

        var home = visualHome != null ? visualHome : transform;
        visualRoot.SetParent(home, false);
        ApplyVisualScaleToHost(null);
    }

    void SetVisualActive(bool on)
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(on);
    }

    void OnGUI()
    {
        var startMessageUp = DutzLevelObjective.IsStartMessageActive;

        if (!startMessageUp && Time.time < expiredFlashUntil)
            DutzAnnouncementHud.DrawFlash("FORCE FIELD EXPIRED", new Color(0.95f, 0.55f, 0.2f));

        if (!active)
            return;

        if (!startMessageUp)
        {
            if (Time.time < collectedFlashUntil)
                DutzAnnouncementHud.DrawFlash("FORCE FIELD SUIT COLLECTED!", new Color(0.35f, 1f, 0.55f));
            else if (Time.time < activeFlashUntil)
                DutzAnnouncementHud.DrawFlash(
                    permanent ? "Force Field active — UNLIMITED!" : $"Force Field active — {DurationSeconds:F0}s!",
                    new Color(0.4f, 0.9f, 1f));
        }

        DrawDurationHud();
    }

    void DrawDurationHud()
    {
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.45f, 0.95f, 1f) }
        };

        var timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.75f, 0.98f, 1f) }
        };

        var top = DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.ForceField);
        var titleRect = new Rect(UiPadding, top, UiWidth + 40f, 24f);
        var barRect = new Rect(UiPadding, titleRect.yMax + 4f, UiWidth, UiBarHeight);
        var timerRect = new Rect(UiPadding, barRect.yMax + 4f, UiWidth + 40f, 24f);

        GUI.Label(titleRect, "FORCE FIELD", titleStyle);

        if (permanent)
        {
            GUI.Label(timerRect, "UNLIMITED", timerStyle);
            return;
        }

        var remaining = RemainingSeconds;
        var fill = DurationSeconds > 0f ? Mathf.Clamp01(remaining / DurationSeconds) : 0f;
        var secondsLeft = Mathf.CeilToInt(remaining);
        var fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * fill, barRect.height - 4f);

        GUI.Box(barRect, GUIContent.none, GUI.skin.box);
        GUI.color = Color.Lerp(new Color(0.95f, 0.45f, 0.15f), new Color(0.35f, 0.9f, 1f), fill);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(timerRect, $"{secondsLeft}s remaining", timerStyle);
    }

    void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
