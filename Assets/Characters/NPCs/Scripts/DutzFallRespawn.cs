using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects falling off the playable area and shows a respawn dialog (stops infinite falling).
/// </summary>
[RequireComponent(typeof(DutzPlayerController))]
[DefaultExecutionOrder(100)]
public class DutzFallRespawn : MonoBehaviour
{
    [Header("Fall detection")]
    [SerializeField] float fallYThreshold = -2f;
    [SerializeField] float longFallStartY = 4f;
    [SerializeField] float longFallSeconds = 2.5f;

    static readonly int DeathId = Animator.StringToHash("Death_b");

    const float SpawnGraceSeconds = 5f;
    const float JumpFallGraceSeconds = 1.5f;
    const float WallBumpGraceSeconds = 0.6f;
    const float MinAirSecondsBeforeEdgeFall = 0.85f;
    const float BridgeNegotiationEdgeFallGraceSeconds = 1.5f;
    const float RoadFallMargin = 5f;
    const float DeathDialogConfirmDelay = 0.75f;
    const float RespawnPullBackDistance = 12f;

    DutzPlayerController player;
    CharacterController characterController;
    DutzPlayerParachute playerParachute;
    Animator animator;
    bool showingDialog;
    Vector3 deathWorldPosition;
    Quaternion deathWorldRotation;
    bool hasDeathPose;
    float dialogShownAt;
    float ungroundedTimer;
    float edgeFallAirTimer;
    float spawnGraceUntil;
    float jumpFallGraceUntil;
    float wallBumpGraceUntil;
    float lastGroundedDeckY = float.NaN;
    Vector3 lastSafeRoadPosition;
    Quaternion lastSafeRoadRotation = Quaternion.identity;
    bool hasLastSafeRoad;
    static bool forceFieldSuitEnsuredForScene;

    public bool IsShowingRespawnDialog => showingDialog;

    public bool IsSpawnGraceActive => Time.time < spawnGraceUntil;

    /// <summary>Suppress false fall deaths right after spawn/respawn while physics settles.</summary>
    public void BeginSpawnGrace(float seconds = SpawnGraceSeconds)
    {
        spawnGraceUntil = Time.time + seconds;
        showingDialog = false;
        ungroundedTimer = 0f;
        edgeFallAirTimer = 0f;
    }

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        characterController = GetComponent<CharacterController>();
        playerParachute = GetComponent<DutzPlayerParachute>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (player != null)
            player.Jumped += OnPlayerJumped;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForceFieldSuitSceneCache() => forceFieldSuitEnsuredForScene = false;

    void OnDisable()
    {
        if (player != null)
            player.Jumped -= OnPlayerJumped;
    }

    void OnPlayerJumped() => jumpFallGraceUntil = Time.time + JumpFallGraceSeconds;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null)
            return;

        if (DutzSenateBuildingMuralGoal.UsesSenateBuildingWin)
        {
            if (DutzSenateBuildingMuralGoal.IsSenateBuildingMuralCollider(hit.collider))
            {
                DutzLevelObjective.NotifySenateBuildingMuralReached();
                return;
            }
        }

        if (DutzEndHouseCollider.UsesHouseRoofWin)
        {
            if (DutzEndHouseCollider.IsHouseCollider(hit.collider)
                && hit.normal.y > 0.35f
                && DutzEndHouseCollider.IsRoofContact(hit.point, hit.normal.y))
            {
                DutzLevelObjective.NotifyEndGoalReached();
            }

            return;
        }

        if (DutzFlagPoleGoal.IsFlagPoleCollider(hit.collider))
        {
            DutzLevelObjective.NotifyEndGoalReached();
            return;
        }

        if (hit.normal.y > 0.55f)
            return;

        if (!IsRoadCollider(hit.collider))
            return;

        wallBumpGraceUntil = Mathf.Max(wallBumpGraceUntil, Time.time + WallBumpGraceSeconds);
    }

    public void NotifyGiantBumpGrace() =>
        wallBumpGraceUntil = Mathf.Max(wallBumpGraceUntil, Time.time + WallBumpGraceSeconds);

    static bool IsRoadCollider(Collider hit)
    {
        var t = hit.transform;
        while (t != null)
        {
            var name = t.name;
            if (name.Contains("Highway") || name.Contains("Bridge") || name.Contains("Road"))
                return true;

            t = t.parent;
        }

        return false;
    }

    /// <summary>Hippie bite or other instant death — same respawn dialog as falling.</summary>
    public void TriggerDeathDialog(string message)
    {
        if (DutzPoliceCaptureDialog.IsShowing)
            return;

        if (showingDialog)
            return;

        PlayMockingDeathSting();
        ShowRespawnDialog(message);
        SetPlayerDeathAnimation(true);
    }

    void FixedUpdate()
    {
        TrySuitPickup();
        TryEndGoalWin();
    }

    void TrySuitPickup()
    {
        if (player == null || characterController == null)
            return;

        if (showingDialog)
            return;

        if (DutzForceFieldSuitPickup.IsLevelCollected())
            return;

        if (!forceFieldSuitEnsuredForScene)
        {
            DutzForceFieldSuitPickup.EnsureOnSceneSuit();
            forceFieldSuitEnsuredForScene = true;
        }

        var pickup = DutzForceFieldSuitPickup.FindPickup();
        if (pickup == null || pickup.IsCollected || !pickup.gameObject.activeInHierarchy)
            return;

        if (!pickup.IsPlayerTouching(characterController))
            return;

        pickup.Collect(player);
    }

    void TryFlagPoleWin()
    {
        if (player == null || characterController == null)
            return;

        if (Time.time < spawnGraceUntil || showingDialog || player.ControlsLocked)
            return;

        if (DutzLevelObjective.IsLevelFinishedForActiveScene)
            return;

        if (!DutzLevelObjective.IsPlayerAtEndGoal())
            return;

        DutzLevelObjective.NotifyEndGoalReached();
    }

    void TryEndGoalWin() => TryFlagPoleWin();

    void FreezePlayerDuringCelebration()
    {
        if (!DutzLevelObjective.IsLevelFinishedForActiveScene || player == null || !player.ControlsLocked)
            return;

        if (transform.position.y >= fallYThreshold)
            return;

        var p = transform.position;
        p.y = fallYThreshold;
        transform.position = p;
    }

    void Update()
    {
        if (player == null)
            return;

        if (Time.time < spawnGraceUntil)
            return;

        if (DutzPoliceCaptureDialog.IsShowing)
            return;

        if (showingDialog)
        {
            DutzDialogCursor.EnsureUnlockedForDialog();

            if (CanConfirmDeathChoice() &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                Respawn();
            }

            return;
        }

        if (player.ControlsLocked || DutzLevelObjective.IsLevelFinishedForActiveScene)
        {
            ungroundedTimer = 0f;
            edgeFallAirTimer = 0f;
            FreezePlayerDuringCelebration();
            return;
        }

        if (IsParachuteProtected())
        {
            var parachutePos = transform.position;
            var parachuteGrounded = characterController != null && characterController.isGrounded;
            if (parachuteGrounded
                && DutzRoadGround.TrySampleSupportDeckBelowFeet(
                    parachutePos, parachutePos.y, characterController, out var deckY))
            {
                lastGroundedDeckY = deckY;
            }

            ungroundedTimer = 0f;
            return;
        }

        var pos = transform.position;
        var grounded = characterController != null && characterController.isGrounded;

        if (grounded)
        {
            edgeFallAirTimer = 0f;
            if (DutzRoadGround.TrySampleSupportDeckBelowFeet(pos, pos.y, characterController, out var deckY))
            {
                lastGroundedDeckY = deckY;
                RecordSafeRoadPose(pos, transform.rotation, deckY);
            }
        }
        else
        {
            edgeFallAirTimer += Time.deltaTime;
        }

        if (IsBelowPlayableSurface(pos, grounded))
        {
            TriggerFallDialog("You fell off the edge!");
            return;
        }

        if (grounded || pos.y >= longFallStartY)
        {
            ungroundedTimer = 0f;
            return;
        }

        if (IsParachuteProtected())
        {
            ungroundedTimer = 0f;
            return;
        }

        ungroundedTimer += Time.deltaTime;
        var longFallLimit = DutzForceField.IsPlayerShielded(player)
            ? DutzForceField.ShieldedLongFallSeconds
            : longFallSeconds;
        if (ungroundedTimer >= longFallLimit)
            TriggerFallDialog("You fell too far!");
    }

    void TriggerFallDialog(string message)
    {
        if (IsParachuteProtected())
            return;

        ShowRespawnDialog(message);

        // Stop endless drop — hold at threshold until respawn
        if (transform.position.y < fallYThreshold)
        {
            var p = transform.position;
            p.y = fallYThreshold;
            transform.position = p;
        }
    }

    void ShowRespawnDialog(string message)
    {
        if (showingDialog)
            return;

        deathWorldPosition = transform.position;
        deathWorldRotation = transform.rotation;
        hasDeathPose = true;

        DutzPlayerLives.ConsumeOne();
        showingDialog = true;
        dialogShownAt = Time.unscaledTime;
        ungroundedTimer = 0f;
        player.SetControlsLocked(true);
        fallMessage = AppendLivesHint(message);
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    static string AppendLivesHint(string message)
    {
        if (DutzPlayerLives.CanRespawn)
            return $"{message}\nLives left: {DutzPlayerLives.Current}";
        return $"{message}\nNo lives left. Restart requires watching an ad.";
    }

    void SetPlayerDeathAnimation(bool dead)
    {
        if (animator == null)
            return;

        animator.SetBool(DeathId, dead);
        if (dead)
        {
            animator.SetFloat(Animator.StringToHash("Speed_f"), 0f);
            animator.SetBool(Animator.StringToHash("Jump_b"), false);
        }
    }

    string fallMessage = "You fell off the edge!";

    bool CanConfirmDeathChoice() => Time.unscaledTime - dialogShownAt >= DeathDialogConfirmDelay;

    void OnGUI()
    {
        DutzGameplayModeHud.DrawStandaloneBadgeIfNeeded();
        DrawLivesHud();

        if (!showingDialog)
            return;

        DutzDialogCursor.EnsureUnlockedForDialog();
        SwallowGameplayKeysDuringDeathDialog();

        var previousDepth = GUI.depth;
        GUI.depth = -3200;
        DutzCartoonDialogGui.DrawDimOverlay();

        var height = DutzCartoonDialogGui.DeathDialogHeight(fallMessage);
        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);
        DutzCartoonDialogGui.DrawFrame(frame);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle(new Color(0.95f, 0.35f, 0.1f));
        var messageStyle = DutzCartoonDialogGui.BodyStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label("OOPS!", titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        GUILayout.Label(fallMessage, messageStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 14f));

        if (DutzPlayerLives.CanRespawn)
        {
            if (DutzCartoonDialogGui.ActionButton("RESPAWN", heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight("RESPAWN")))
                Respawn();

            GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 14f));
        }
        else
        {
            if (DutzCartoonDialogGui.DangerButton("RESTART LEVEL"))
                RequestRestartWithRewardedAd();

            GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 14f));
        }

        if (DutzCartoonDialogGui.DangerButton("EXIT THE GAME"))
            ExitGame();

        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        GUILayout.Label(
            CanConfirmDeathChoice()
                ? (DutzPlayerLives.CanRespawn
                    ? "Choose Respawn or Exit the Game."
                    : "Choose Restart Level (ad) or Exit the Game.")
                : "Choose an option above.",
            hintStyle);
        GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

    void SwallowGameplayKeysDuringDeathDialog()
    {
        if (Event.current.type != EventType.KeyDown)
            return;

        var key = Event.current.keyCode;
        if (key == KeyCode.Space || key == KeyCode.LeftShift || key == KeyCode.RightShift ||
            key == KeyCode.UpArrow || key == KeyCode.DownArrow || key == KeyCode.LeftArrow ||
            key == KeyCode.RightArrow)
        {
            Event.current.Use();
            return;
        }

        if (!CanConfirmDeathChoice() &&
            (key == KeyCode.Return || key == KeyCode.KeypadEnter))
        {
            Event.current.Use();
        }
    }

    public void PerformRespawnFromDialog()
    {
        showingDialog = false;
        SetPlayerDeathAnimation(false);

        // Senate / Airport: full start spawn so suitcases/coins can be re-gathered.
        bool useStartSpawn =
            DutzCollectibleProgress.IsLevel01 || DutzCollectibleProgress.IsLevel02;

        if (useStartSpawn || !hasDeathPose)
            player.Respawn();
        else
        {
            var rot = hasLastSafeRoad ? lastSafeRoadRotation : deathWorldRotation;
            player.RespawnNear(ComputePullBackPose(), rot);
        }

        BeginSpawnGrace();
    }

    void RecordSafeRoadPose(Vector3 worldPos, Quaternion worldRot, float deckY)
    {
        lastSafeRoadPosition = worldPos;
        lastSafeRoadPosition.y = deckY + 0.05f;
        lastSafeRoadRotation = worldRot;
        hasLastSafeRoad = true;
    }

    Vector3 ComputePullBackPose()
    {
        // Prefer last grounded road pose — death while falling is mid-air / below the deck.
        var pose = hasLastSafeRoad ? lastSafeRoadPosition : deathWorldPosition;
        var facing = hasLastSafeRoad ? lastSafeRoadRotation : deathWorldRotation;

        Vector3 back = facing * Vector3.back;
        back.y = 0f;
        if (back.sqrMagnitude < 0.01f)
            back = Vector3.back;
        pose += back.normalized * RespawnPullBackDistance;

        var hintY = !float.IsNaN(lastGroundedDeckY)
            ? lastGroundedDeckY
            : (hasLastSafeRoad ? lastSafeRoadPosition.y : deathWorldPosition.y);

        // Death Y can be far below the highway; lift the sample so deck raycasts can hit.
        if (pose.y < hintY - 1f)
            pose.y = hintY + 1f;

        DutzRoadGround.TrySnapPositionToNearestRoadDeck(ref pose, hintY, characterController);

        if (DutzRoadGround.TrySampleRoadDeckForPlacement(pose, hintY, characterController, out var roadY)
            || DutzRoadGround.TrySampleWalkableRoadDeckY(pose, hintY, characterController, out roadY))
        {
            pose.y = roadY + 0.15f;
        }
        else if (hasLastSafeRoad)
        {
            pose = lastSafeRoadPosition;
            pose.y = lastSafeRoadPosition.y + 0.15f;
        }
        else if (!float.IsNaN(lastGroundedDeckY))
        {
            pose.y = lastGroundedDeckY + 0.15f;
        }

        return pose;
    }

    void DrawLivesHud()
    {
        var previous = GUI.depth;
        GUI.depth = -80;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = new Color(1f, 0.45f, 0.55f, 1f);

        string pips = string.Empty;
        int max = DutzPlayerLives.Max;
        int cur = DutzPlayerLives.Current;
        for (int i = 0; i < max; i++)
            pips += i < cur ? "● " : "○ ";

        GUI.Label(
            new Rect(
                DutzUpperLeftHudLayout.PaddingX,
                DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.Lives),
                360f,
                DutzUpperLeftHudLayout.TextRowHeight),
            $"LIVES  {pips.Trim()}  {cur}/{max}",
            style);

        GUI.depth = previous;
    }

    public void CancelForPoliceCapture() => showingDialog = false;

    public void CaptureDeathPoseFromPlayer()
    {
        deathWorldPosition = transform.position;
        deathWorldRotation = transform.rotation;
        hasDeathPose = true;

        // If death is mid-air, keep lastSafeRoad for pull-back respawn onto the highway.
        if (characterController != null
            && characterController.isGrounded
            && DutzRoadGround.TrySampleSupportDeckBelowFeet(
                deathWorldPosition, deathWorldPosition.y, characterController, out var deckY))
        {
            RecordSafeRoadPose(deathWorldPosition, deathWorldRotation, deckY);
        }
    }

    public void SetCapturePose(bool captured) => SetPlayerDeathAnimation(captured);

    void Respawn()
    {
        if (!CanConfirmDeathChoice() || !DutzPlayerLives.CanRespawn)
            return;

        PerformRespawnFromDialog();
    }

    bool restartAdPending;

    void RequestRestartWithRewardedAd()
    {
        if (!CanConfirmDeathChoice() || DutzPlayerLives.CanRespawn || restartAdPending)
            return;

        restartAdPending = true;
        showingDialog = false;
        FloodRewardedAdStub.Show(
            onRewarded: PerformRestartLevel,
            onDismissedOrFailed: () =>
            {
                restartAdPending = false;
                showingDialog = true;
                dialogShownAt = Time.unscaledTime;
            });
    }

    void PerformRestartLevel()
    {
        restartAdPending = false;
        showingDialog = false;
        SetPlayerDeathAnimation(false);
        Time.timeScale = 1f;
        DutzPlayerLives.ResetToFull();
        DutzGameBootstrap.PrepareForSceneLoad();
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
            SceneManager.LoadScene(scene.buildIndex);
        else
            SceneManager.LoadScene(scene.name);
    }

    void ExitGame()
    {
        if (!CanConfirmDeathChoice())
            return;

        showingDialog = false;
        SetPlayerDeathAnimation(false);
        player.SetControlsLocked(false);
        DutzLevelObjective.ExitGameFromDialog();
    }

    bool IsBelowPlayableSurface(Vector3 pos, bool grounded)
    {
        if (grounded)
            return false;

        if (player != null && IsParachuteProtected())
            return false;

        if (Time.time < jumpFallGraceUntil || Time.time < wallBumpGraceUntil)
            return false;

        var minAir = MinAirSecondsBeforeEdgeFall;
        if (DutzForceField.IsPlayerShielded(player))
            minAir = DutzForceField.ShieldedMinAirSecondsBeforeEdgeFall;

        if (DutzRoadGround.IsNearBridgeStructure(pos, characterController))
            minAir += BridgeNegotiationEdgeFallGraceSeconds;

        if (edgeFallAirTimer < minAir)
            return false;

        if (player != null && player.VerticalSpeed > 0.75f)
            return false;

        if (DutzForceField.IsPlayerShielded(player) &&
            DutzRoadGround.TrySampleRoadDeckBelowForShieldedDrop(
                pos,
                pos.y,
                characterController,
                DutzForceField.ShieldedDeckLookaheadMeters,
                out var deckBelow))
        {
            var dropToDeck = pos.y - deckBelow;
            if (dropToDeck >= 0f && dropToDeck <= DutzForceField.ShieldedDeckLookaheadMeters)
                return false;
        }

        if (DutzRoadGround.TrySampleSupportDeckBelowFeet(pos, pos.y, characterController, out var roadY))
            return pos.y < roadY - RoadFallMargin;

        if (!float.IsNaN(lastGroundedDeckY))
            return pos.y < lastGroundedDeckY - RoadFallMargin;

        return pos.y < fallYThreshold;
    }

    bool IsParachuteProtected() =>
        playerParachute != null && playerParachute.IsParachuteActive;

    static AudioClip mockingDeathClip;

    static void PlayMockingDeathSting()
    {
        if (mockingDeathClip == null)
            mockingDeathClip = CreateMockingDeathClip();

        if (mockingDeathClip == null)
            return;

        var go = new GameObject("DutzDeathMockAudio");
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = DutzAudioSettings.ScaleSfx(0.9f);
        source.PlayOneShot(mockingDeathClip, source.volume);
        Destroy(go, mockingDeathClip.length + 0.15f);
    }

    static AudioClip CreateMockingDeathClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.35f;
        var samples = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
            data[i] = SampleMockingDeath(i / (float)sampleRate);

        var clip = AudioClip.Create("DutzMockingDeath", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float SampleMockingDeath(float t)
    {
        var sample = 0f;
        var wahStarts = new[] { 0.02f, 0.28f, 0.54f };
        const float wahDur = 0.22f;

        foreach (var start in wahStarts)
        {
            var local = (t - start) / wahDur;
            if (local < 0f || local > 1f)
                continue;

            var env = Mathf.Sin(local * Mathf.PI);
            var freq = Mathf.Lerp(520f, 180f, local);
            sample += Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.42f;
            sample += Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * env * 0.12f;
        }

        if (t >= 0.82f && t < 0.92f)
        {
            var local = (t - 0.82f) / 0.1f;
            var env = Mathf.Sin(local * Mathf.PI);
            sample += Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.22f;
        }

        if (t >= 0.96f && t < 1.08f)
        {
            var local = (t - 0.96f) / 0.12f;
            var env = Mathf.Sin(local * Mathf.PI);
            sample += Mathf.Sin(2f * Mathf.PI * 660f * t) * env * 0.2f;
        }

        if (t >= 1.05f)
        {
            var local = (t - 1.05f) / 0.3f;
            if (local <= 1f)
            {
                var env = (1f - local) * (1f - local);
                var buzz = Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.5f +
                           Mathf.Sin(2f * Mathf.PI * 210f * t) * 0.35f;
                sample += buzz * env * 0.28f;
            }
        }

        return Mathf.Clamp(sample * 0.95f, -1f, 1f);
    }
}
