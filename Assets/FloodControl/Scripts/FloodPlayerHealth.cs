using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Flood Control Player1 hit points, 6-life system, and death dialog.
/// Pipe contact burns HP over time. Lives gate Respawn vs Restart (rewarded stub).
/// </summary>
[DisallowMultipleComponent]
public class FloodPlayerHealth : MonoBehaviour
{
    const int DefaultMaxLives = 6;
    const float SafePoseLookbackSeconds = 0.75f;
    const int SafePoseBufferSize = 32;

    [Header("Hit Points")]
    [SerializeField] int maxHitPoints = 100;
    [SerializeField] int currentHitPoints = 100;
    [SerializeField] GameManager gameManager;

    [Header("Lives")]
    [SerializeField] int maxLives = DefaultMaxLives;
    [SerializeField] int currentLives = DefaultMaxLives;
    [SerializeField] float respawnInvulnerabilitySeconds = 2f;
    [SerializeField] float respawnPullBackDistance = 25f;

    [Header("Death Dialog")]
    [SerializeField] string deathTitle = "YOU DIED";
    [SerializeField] string deathHint = "The pipes burned you out. Try again?";
    [SerializeField] string respawnLabel = "RESPAWN";
    [SerializeField] string restartLabel = "RESTART";
    [SerializeField] string exitLabel = "EXIT GAME";

    Vector3 levelStartPosition;
    Quaternion levelStartRotation;
    Vector3 lastSafePosition;
    Quaternion lastSafeRotation;
    Vector3 deathWorldPosition;
    Quaternion deathWorldRotation;
    bool hasLastSafe;
    bool hasDeathPose;

    readonly Vector3[] safePosBuffer = new Vector3[SafePoseBufferSize];
    readonly Quaternion[] safeRotBuffer = new Quaternion[SafePoseBufferSize];
    readonly float[] safeTimeBuffer = new float[SafePoseBufferSize];
    int safeBufferCount;
    int safeBufferHead;

    string defaultDeathTitle;
    string defaultDeathHint;
    string deathHintBase;
    float burnAccumulator;
    float burnWarningPhase;
    float shieldExpiresAt;
    bool permanentShield;
    bool burnedThisFrame;
    bool showBurnWarning;
    bool isDead;
    bool showDeathDialog;
    bool restartAdPending;
    FloodBurnScreech burnScreech;

    public int MaxHitPoints => maxHitPoints;
    public int CurrentHitPoints => currentHitPoints;
    public int MaxLives => maxLives;
    public int CurrentLives => currentLives;
    public bool IsDead => isDead;
    public bool IsShowingDeathDialog => showDeathDialog;
    public bool IsShielded => permanentShield || Time.time < shieldExpiresAt;
    public float ShieldRemainingSeconds => permanentShield
        ? float.PositiveInfinity
        : Mathf.Max(0f, shieldExpiresAt - Time.time);

    static FloodPlayerHealth activeInstance;

    public static bool IsBlockingMobileInput =>
        activeInstance != null
        && (activeInstance.showDeathDialog
            || activeInstance.restartAdPending
            || FloodRewardedAdStub.IsShowing);

    public static bool IsShowingAnyDeathDialog =>
        activeInstance != null && activeInstance.showDeathDialog;

    void Awake()
    {
        activeInstance = this;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        burnScreech = GetComponent<FloodBurnScreech>();
        levelStartPosition = transform.position;
        levelStartRotation = transform.rotation;
        lastSafePosition = levelStartPosition;
        lastSafeRotation = levelStartRotation;
        hasLastSafe = true;
        defaultDeathTitle = deathTitle;
        defaultDeathHint = deathHint;

        maxLives = DefaultMaxLives;
        currentLives = DefaultMaxLives;

        currentHitPoints = Mathf.Clamp(currentHitPoints, 0, Mathf.Max(1, maxHitPoints));
        if (currentHitPoints <= 0)
            currentHitPoints = maxHitPoints;
    }

    void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
    }

    void Update()
    {
        bool burning = burnedThisFrame;
        if (!isDead && !burning)
            TryRecordSafePoseSample();

        // Physics contact marks burnedThisFrame; Update latches it for OnGUI this frame.
        showBurnWarning = burning && !isDead;
        burnedThisFrame = false;
        if (showBurnWarning)
            burnWarningPhase += Time.deltaTime * 10f;
    }

    /// <summary>
    /// Applies continuous burn damage. Call from pipe collision each frame.
    /// </summary>
    public void ApplyBurnDamage(float burnPerSecond)
    {
        if (isDead || IsShielded || burnPerSecond <= 0f)
            return;

        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        burnedThisFrame = true;
        if (burnScreech == null)
            burnScreech = GetComponent<FloodBurnScreech>();
        burnScreech?.NotifyBurning();

        burnAccumulator += burnPerSecond * Time.deltaTime;
        int wholePoints = Mathf.FloorToInt(burnAccumulator);
        if (wholePoints <= 0)
            return;

        burnAccumulator -= wholePoints;
        currentHitPoints = Mathf.Max(0, currentHitPoints - wholePoints);

        if (currentHitPoints <= 0)
            Die();
    }

    /// <summary>
    /// Instantly kills the player (used by crocodile contact).
    /// </summary>
    public void Kill(string reasonHint = null)
    {
        if (isDead || IsShielded)
            return;

        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        if (!string.IsNullOrWhiteSpace(reasonHint))
            deathHint = reasonHint;

        Die();
    }

    /// <summary>
    /// Kills the player with an exact dialog title and optional supporting hint.
    /// </summary>
    public void KillWithDialog(string title, string hint = null)
    {
        if (isDead)
            return;

        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        // The shield blocks contact hazards, but not the four-minute drowning limit.
        if (IsShielded && title != "YOU DIED OF DROWNING.")
            return;

        if (!string.IsNullOrWhiteSpace(title))
            deathTitle = title;
        deathHint = hint ?? string.Empty;

        Die();
    }

    /// <summary>
    /// Activates or extends the Flood Control force-field shield.
    /// </summary>
    public void ActivateShield(float durationSeconds)
    {
        if (isDead || durationSeconds <= 0f)
            return;

        shieldExpiresAt = Mathf.Max(shieldExpiresAt, Time.time + durationSeconds);
        burnedThisFrame = false;
        showBurnWarning = false;
    }

    /// <summary>Permanent shield for Flood Senior Citizen Mode.</summary>
    public void ActivatePermanentShield()
    {
        if (isDead)
            return;

        permanentShield = true;
        shieldExpiresAt = float.MaxValue;
        burnedThisFrame = false;
        showBurnWarning = false;
        EnsurePermanentShieldVisual();
    }

    void EnsurePermanentShieldVisual()
    {
        Transform existing = transform.Find(FloodForceFieldVisual.VisualName);
        if (existing != null)
        {
            FloodForceFieldVisual.FitToPlayer(existing, transform);
            FloodForceFieldVisual visual = existing.GetComponent<FloodForceFieldVisual>();
            if (visual != null)
                visual.CaptureBaseScale();
            return;
        }

        FloodForceFieldVisual.SpawnOnPlayer(transform, 0f, permanent: true);
    }

    /// <summary>
    /// Restores hit points, clamped to max HP (green potion style).
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        showDeathDialog = true;
        currentHitPoints = 0;
        burnAccumulator = 0f;
        burnedThisFrame = false;
        showBurnWarning = false;
        restartAdPending = false;

        deathWorldPosition = transform.position;
        deathWorldRotation = transform.rotation;
        hasDeathPose = true;

        currentLives = Mathf.Max(0, currentLives - 1);
        CommitLastSafeFromBuffer();
        deathHintBase = deathHint;
        deathHint = BuildDeathHintWithLives(deathHintBase);

        if (gameManager != null)
            gameManager.SetGameplayEnabled(false);

        var body = GetComponent<Rigidbody>();
        if (body != null)
            body.velocity = Vector3.zero;
    }

    string BuildDeathHintWithLives(string baseHint)
    {
        string core = string.IsNullOrWhiteSpace(baseHint) ? defaultDeathHint : baseHint;
        if (currentLives > 0)
            return $"{core}\nLives left: {currentLives}";

        return $"{core}\nNo lives left. Restart requires watching an ad.";
    }

    void TryRecordSafePoseSample()
    {
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        float now = Time.time;
        safePosBuffer[safeBufferHead] = transform.position;
        safeRotBuffer[safeBufferHead] = transform.rotation;
        safeTimeBuffer[safeBufferHead] = now;
        safeBufferHead = (safeBufferHead + 1) % SafePoseBufferSize;
        if (safeBufferCount < SafePoseBufferSize)
            safeBufferCount++;
    }

    void CommitLastSafeFromBuffer()
    {
        if (safeBufferCount <= 0)
        {
            lastSafePosition = levelStartPosition;
            lastSafeRotation = levelStartRotation;
            hasLastSafe = true;
            return;
        }

        float targetTime = Time.time - SafePoseLookbackSeconds;
        int bestIndex = -1;
        float bestDelta = float.MaxValue;

        for (int i = 0; i < safeBufferCount; i++)
        {
            int index = safeBufferHead - 1 - i;
            if (index < 0)
                index += SafePoseBufferSize;

            float delta = Mathf.Abs(safeTimeBuffer[index] - targetTime);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestIndex = index;
            }

            // Prefer a sample at or before the lookback target once we pass it.
            if (safeTimeBuffer[index] <= targetTime)
                break;
        }

        if (bestIndex < 0)
        {
            lastSafePosition = levelStartPosition;
            lastSafeRotation = levelStartRotation;
        }
        else
        {
            lastSafePosition = safePosBuffer[bestIndex];
            lastSafeRotation = safeRotBuffer[bestIndex];
        }

        hasLastSafe = true;
    }

    void OnGUI()
    {
        DrawHitPointsHud();
        DrawLivesHud();
        DrawShieldCountdownHud();

        if (showBurnWarning && !showDeathDialog)
            DrawBurnWarning();

        if (restartAdPending || FloodRewardedAdStub.IsShowing)
            DrawRewardedAdStubOverlay();

        if (!showDeathDialog || restartAdPending)
            return;

        DrawDeathDialog();
    }

    void DrawRewardedAdStubOverlay()
    {
        var previousDepth = GUI.depth;
        GUI.depth = -1200;
        DutzCartoonDialogGui.DrawDimOverlay(0.65f);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = DutzCartoonDialogGui.ScaleFont(26, 34),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;
        GUI.Label(
            new Rect(0f, Screen.height * 0.42f, Screen.width, DutzCartoonDialogGui.Scale(48f, 64f)),
            "Watching rewarded ad…",
            style);
        GUI.depth = previousDepth;
    }

    void DrawLivesHud()
    {
        var previous = GUI.depth;
        GUI.depth = -80;

        float pad = DutzCartoonDialogGui.Scale(16f, 24f);
        float y = pad + DutzCartoonDialogGui.Scale(38f, 52f);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = DutzCartoonDialogGui.ScaleFont(24, 32),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = new Color(1f, 0.45f, 0.55f, 1f);

        string pips = string.Empty;
        for (int i = 0; i < maxLives; i++)
            pips += i < currentLives ? "● " : "○ ";

        GUI.Label(
            new Rect(pad, y, Screen.width * 0.7f, DutzCartoonDialogGui.Scale(40f, 56f)),
            $"LIVES  {pips.Trim()}  {currentLives}/{maxLives}",
            style);

        GUI.depth = previous;
    }

    void DrawShieldCountdownHud()
    {
        if (isDead || !IsShielded)
            return;

        int seconds = Mathf.Max(1, Mathf.CeilToInt(ShieldRemainingSeconds));
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = DutzCartoonDialogGui.ScaleFont(22, 30),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = new Color(0.3f, 0.9f, 1f, 1f);

        float pad = DutzCartoonDialogGui.Scale(16f, 24f);
        float y = pad + DutzCartoonDialogGui.Scale(70f, 96f);
        GUI.Label(
            new Rect(
                pad,
                y,
                Screen.width * 0.5f,
                DutzCartoonDialogGui.Scale(34f, 48f)),
            $"FORCE FIELD: {seconds}s",
            style);
    }

    void DrawBurnWarning()
    {
        float pulse = 0.7f + 0.3f * Mathf.Sin(burnWarningPhase);
        int bangSize = Mathf.RoundToInt(88f * pulse);
        int labelSize = Mathf.RoundToInt(28f * pulse);

        var bangStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = bangSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        bangStyle.normal.textColor = new Color(1f, 0.2f, 0.05f, pulse);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = labelSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        labelStyle.normal.textColor = new Color(1f, 0.55f, 0.1f, pulse);

        var bangRect = new Rect(0f, Screen.height * 0.22f, Screen.width, bangSize + 8f);
        var labelRect = new Rect(0f, bangRect.yMax + 4f, Screen.width, labelSize + 8f);

        var shadowStyle = new GUIStyle(bangStyle);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, pulse * 0.55f);
        GUI.Label(new Rect(bangRect.x + 3f, bangRect.y + 3f, bangRect.width, bangRect.height), "!", shadowStyle);
        GUI.Label(bangRect, "!", bangStyle);
        GUI.Label(labelRect, "TOO HOT!", labelStyle);
    }

    void DrawHitPointsHud()
    {
        var previous = GUI.depth;
        GUI.depth = -50;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = DutzCartoonDialogGui.ScaleFont(28, 36),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = currentHitPoints <= 25
            ? new Color(1f, 0.25f, 0.2f, 1f)
            : Color.white;

        float pad = DutzCartoonDialogGui.Scale(16f, 24f);
        float rowHeight = DutzCartoonDialogGui.Scale(40f, 56f);
        var hpText = $"HP {currentHitPoints}/{maxHitPoints}";
        var labelRow = new Rect(pad, pad, Screen.width - pad * 2f, rowHeight);
        DutzGameplayModeHud.DrawCombinedRow(labelRow, hpText, style);

        GUI.depth = previous;
    }

    void DrawDeathDialog()
    {
        bool canRespawn = currentLives > 0;
        bool canRestart = currentLives <= 0;

        string[] labels = canRespawn
            ? new[] { respawnLabel, exitLabel }
            : new[] { restartLabel, exitLabel };

        var previousDepth = GUI.depth;
        GUI.depth = -1100;
        DutzCartoonDialogGui.DrawDimOverlay(0.55f);

        float height = DutzCartoonDialogGui.ChoiceDialogHeight(deathTitle, deathHint, labels);
        var requiredHeight = DutzCartoonDialogGui.MeasureStackedPanelHeight(deathTitle, deathHint, labels);
        Rect frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);

        GUI.depth = -800;
        DutzCartoonDialogGui.DrawFrame(frame);

        GUIStyle titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        GUIStyle hintStyle = DutzCartoonDialogGui.HintStyle();

        var scrolling = DutzCartoonDialogGui.BeginPanelContent(frame, requiredHeight);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(deathTitle, titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label(deathHint, hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));

        if (canRespawn)
        {
            if (DutzCartoonDialogGui.ActionButton(
                    respawnLabel,
                    DutzCartoonDialogGui.PlasticButtonColor.Blue,
                    DutzCartoonDialogGui.MeasureActionButtonHeight(respawnLabel)))
            {
                RespawnPlayer();
            }

            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        }

        if (canRestart)
        {
            if (DutzCartoonDialogGui.ActionButton(
                    restartLabel,
                    DutzCartoonDialogGui.PlasticButtonColor.Red,
                    DutzCartoonDialogGui.MeasureActionButtonHeight(restartLabel)))
            {
                RequestRestartWithRewardedAd();
            }

            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        }

        if (DutzCartoonDialogGui.DangerButton(
                exitLabel,
                DutzCartoonDialogGui.MeasureActionButtonHeight(exitLabel)))
        {
            ExitGame();
        }

        DutzCartoonDialogGui.EndPanelContent(scrolling);
        GUI.depth = previousDepth;
    }

    void RespawnPlayer()
    {
        if (currentLives <= 0)
            return;

        isDead = false;
        showDeathDialog = false;
        restartAdPending = false;
        currentHitPoints = maxHitPoints;
        burnAccumulator = 0f;
        burnWarningPhase = 0f;
        burnedThisFrame = false;
        showBurnWarning = false;
        deathTitle = defaultDeathTitle;
        deathHint = defaultDeathHint;

        Vector3 pose;
        Quaternion rot;
        if (hasDeathPose)
        {
            // Pull back from where you died — never snap to level start mid-run.
            pose = deathWorldPosition;
            rot = deathWorldRotation;
            if (hasLastSafe)
                pose.y = lastSafePosition.y;
        }
        else if (hasLastSafe)
        {
            pose = lastSafePosition;
            rot = lastSafeRotation;
        }
        else
        {
            pose = levelStartPosition;
            rot = levelStartRotation;
        }

        pose.x -= Mathf.Max(0f, respawnPullBackDistance);

        BoundaryLimiter bounds = GetComponent<BoundaryLimiter>();
        if (bounds != null)
            pose = bounds.ClampPosition(pose);
        else
            pose.z = FloodPlanarPickup.LockZ;

        // Keep a little forward of the absolute start so respawn never feels like a full restart.
        pose.x = Mathf.Max(pose.x, levelStartPosition.x);

        transform.SetPositionAndRotation(pose, rot);
        Physics.SyncTransforms();

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = pose;
            body.rotation = rot;
            body.WakeUp();
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = true;

        SwimmingAnimationController swimming = GetComponent<SwimmingAnimationController>();
        if (swimming != null)
        {
            swimming.enabled = true;
            swimming.EnterSwimmingPose();
        }

        // Keep remaining drown time on life-respawn (do not ResetForRespawn).

        // Invulnerability so the same rat/croc/pipe cannot kill instantly again.
        ActivateShield(Mathf.Max(0.1f, respawnInvulnerabilitySeconds));

        if (gameManager != null)
            gameManager.SetGameplayEnabled(true);
    }

    void RequestRestartWithRewardedAd()
    {
        if (currentLives > 0 || restartAdPending)
            return;

        restartAdPending = true;
        showDeathDialog = false;
        FloodRewardedAdStub.Show(
            onRewarded: PerformRestartLevel,
            onDismissedOrFailed: OnRestartAdDismissed);
    }

    void OnRestartAdDismissed()
    {
        restartAdPending = false;
        showDeathDialog = true;
        deathHint = BuildDeathHintWithLives(deathHintBase);
    }

    void PerformRestartLevel()
    {
        restartAdPending = false;
        showDeathDialog = false;
        currentLives = maxLives;
        DutzGameBootstrap.PrepareForSceneLoad();
        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
            SceneManager.LoadScene(active.buildIndex);
        else
            SceneManager.LoadScene(active.name);
    }

    void ExitGame()
    {
        showDeathDialog = false;
        restartAdPending = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnValidate()
    {
        maxHitPoints = Mathf.Max(1, maxHitPoints);
        currentHitPoints = Mathf.Clamp(currentHitPoints, 0, maxHitPoints);
        maxLives = DefaultMaxLives;
        currentLives = DefaultMaxLives;
        respawnInvulnerabilitySeconds = Mathf.Max(0.1f, respawnInvulnerabilitySeconds);
        respawnPullBackDistance = Mathf.Max(0f, respawnPullBackDistance);
    }
}
