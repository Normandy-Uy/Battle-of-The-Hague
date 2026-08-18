using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level objective: reach the house before timer expires.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class DutzLevelObjective : MonoBehaviour
{
    const string ManagerName = "DutzLevelObjective";
    const string FlagPoleName = "FlagPole";

    [SerializeField] float levelDurationSeconds = 240f;
    [SerializeField] float winScoreHoldSeconds = 1.4f;
    [SerializeField] string startMessage = "Addicts Incoming!!";
    [SerializeField] float startMessageDuration = 2f;
    [SerializeField] string timeoutMessage = "Time is up! You lose.";
    [SerializeField] string winMessage = "Great job! You reached the goal!";
    const int TimeBonusPerSecond = 100;
    const int CoinBonusEach = 200;

    DutzPlayerController player;
    DutzFallRespawn respawn;
    Transform flagPole;
    Collider flagPoleCollider;
    float timeLeft;
    bool finished;
    bool won;
    string statusMessage;
    float startMessageTimeLeft;
    bool startMessagePending = true;
    bool startMessageAwaitingOk;
    AudioSource oneShotSource;
    int winRemainingSeconds;
    int winTimeBonus;
    int winCoinBonus;
    int winBaseScore;
    int winDifficultyMultiplier;
    int winFinalScore;
    int displayedScore;
    int displayedTimeBonus;
    int displayedCoinBonus;
    bool scoreRollComplete;
    bool showingLevelCompleteChoice;
    bool level00TransitionStarted;
    bool level03TransitionStarted;
    bool level03VictoryVideoStarted;

    static GUIStyle gameplayTimerStyle;
    static GUIStyle pauseButtonStyle;
    bool level03AwaitingVictoryContinue;
    bool level03ShareInProgress;
    string level03ComposedSharePath;
    Texture2D level03ComposedSharePreview;
    string level03ShareStatus;
    Vector2 level03ShareBodyScroll;
    float winCelebrationStartedAt;

    public static DutzLevelObjective Instance { get; private set; }

    public static bool IsStartMessageActive =>
        Instance != null
        && DutzDifficulty.HasChosen
        && !string.IsNullOrEmpty(Instance.startMessage)
        && (Instance.startMessageAwaitingOk || Instance.startMessageTimeLeft > 0f);

    public static bool IsLevelFinished { get; private set; }

    static string levelFinishedSceneName;

    /// <summary>True only when the active scene is the one that was won/lost — avoids L2 win blocking L3.</summary>
    public static bool IsLevelFinishedForActiveScene =>
        IsLevelFinished && levelFinishedSceneName == SceneManager.GetActiveScene().name;

    public static bool IsShowingLevelCompleteChoice =>
        Instance != null && Instance.showingLevelCompleteChoice;

    static bool ShouldOfferLevelCompleteChoice()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        return sceneName == DutzMobileRuntime.Level00SceneName
            || sceneName == DutzMobileRuntime.Level01SceneName
            || sceneName == DutzMobileRuntime.Level02SceneName
            || sceneName == DutzMobileRuntime.Level07SceneName;
    }

    static bool IsLevel00CompleteChoiceScene() =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level00SceneName;

    static bool IsLevel01CompleteChoiceScene() =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level01SceneName;

    static bool IsLevel02CompleteChoiceScene() =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level02SceneName;

    static bool IsLevel07CompleteChoiceScene() =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level07SceneName;

    public static bool IsPlayerAtEndGoal() =>
        Instance != null && Instance.IsAtEndGoal();

    static bool UsesHouseRoofWin() => DutzEndHouseCollider.UsesHouseRoofWin;

    bool IsTransitionVideoActiveOrPending() =>
        DutzVictoryVideoPlayback.IsPlaying
        || level00TransitionStarted
        || level03TransitionStarted;

    public static bool ShouldShowLevelCompleteDialog()
    {
        var objective = Active;
        return objective != null
            && !DutzVictoryVideoPlayback.ShouldHideWinGui
            && objective.IsReadyForLevelCompleteDialog();
    }

    public static bool ShouldShowLevel00CompleteDialog() =>
        ShouldShowLevelCompleteDialog() && IsLevel00CompleteChoiceScene();

    public static bool ShouldShowLevel01CompleteDialog() =>
        ShouldShowLevelCompleteDialog() && IsLevel01CompleteChoiceScene();

    public static bool ShouldShowLevel02CompleteDialog() =>
        ShouldShowLevelCompleteDialog() && IsLevel02CompleteChoiceScene();

    public static bool ShouldShowLevel07CompleteDialog() =>
        ShouldShowLevelCompleteDialog() && IsLevel07CompleteChoiceScene();

    /// <summary>
    /// True when Dutz_Level03 can be loaded (enabled in File → Build Settings — required even in Editor Play).
    /// </summary>
    public static bool HasLevel03InBuild() =>
        Application.CanStreamedLevelBeLoaded(DutzMobileRuntime.Level03SceneName);

    public const string CampaignPlayStorePackageId = "com.dutz.battleofthehague";

    public static void OpenCampaignPlayStoreFromDialog() => Active?.OpenCampaignPlayStore();

    public void OpenCampaignPlayStore()
    {
#if UNITY_EDITOR
        var web = "https://play.google.com/store/apps/details?id=" + CampaignPlayStorePackageId;
        Application.OpenURL(web);
        Debug.Log("[Dutz] Opened Campaign Play Store page for " + CampaignPlayStorePackageId
            + " (listing may be unpublished).");
#else
        Application.OpenURL("market://details?id=" + CampaignPlayStorePackageId);
#endif
    }

    public static void LoadLevel1FromDialog()
    {
        Debug.Log("[Dutz] Win dialog — GO TO LEVEL 3 tapped.");
        DutzSeniorCitizenNextLevelGate.ProceedToNextLevel(() =>
        {
            if (Active == null)
                Debug.LogError("[Dutz] Win dialog — DutzLevelObjective.Active is null; cannot load Level 01.");
            Active?.PlayLevel1();
        });
    }

    public static void LoadLevel2FromDialog()
    {
        DutzSeniorCitizenNextLevelGate.ProceedToNextLevel(() => Active?.PlayLevel2());
    }

    public static void LoadLevel3FromDialog()
    {
        DutzSeniorCitizenNextLevelGate.ProceedToNextLevel(() => Active?.PlayLevel3WithTransitionVideo());
    }

    public static void RestartLevel0FromDialog()
    {
        Debug.Log("[Dutz] Win dialog — RESTART LEVEL 2 tapped.");
        Active?.RestartLevel0();
    }

    public static void RestartLevel1FromDialog() => Active?.RestartLevel1();

    public static void RestartLevel2FromDialog() => Active?.RestartLevel2();

    public static void RestartLevel07FromDialog() => Active?.RestartLevel07();

    public static void GoToUnlockedLevelsFromDialog()
    {
        DutzSeniorCitizenNextLevelGate.ProceedToNextLevel(() => Active?.GoToUnlockedLevels());
    }

    public static void ExitGameFromDialog()
    {
        Debug.Log("[Dutz] Win dialog — EXIT THE GAME tapped.");
        Active?.ExitGame();
    }

    /// <summary>Clears win/finished flags that survive across SceneManager.LoadScene within one play session.</summary>
    public static void ResetStaticStateForNewScene()
    {
        IsLevelFinished = false;
        levelFinishedSceneName = null;
        Instance = null;
    }

    public static DutzLevelObjective Active =>
        Instance != null ? Instance : FindObjectOfType<DutzLevelObjective>();

    static DutzLevelObjective GetObjective() => Active;

    public static void EnsureFromBoot()
    {
        ResetStaticStateForNewScene();
        DutzForceFieldSuitPickup.EnsureOnSceneSuit();

        var objectives = FindObjectsOfType<DutzLevelObjective>();
        for (var i = 0; i < objectives.Length; i++)
        {
            if (objectives[i] == null)
                continue;

            var host = objectives[i].gameObject;
            var isManager = host.name == ManagerName;
            if (isManager)
                continue;

            if (Instance == objectives[i])
                Instance = null;

            Object.Destroy(objectives[i]);
        }

        var managerGo = GameObject.Find(ManagerName);
        var existingObjective = managerGo != null ? managerGo.GetComponent<DutzLevelObjective>() : null;
        if (existingObjective != null)
        {
            if (Instance == null)
                Instance = existingObjective;
            existingObjective.ConfigureForActiveScene();
            return;
        }

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzLevelObjective>();
    }

    public static void ResetTimerOnPlayerRespawn()
    {
        var objective = GetObjective();
        if (objective == null)
            return;

        objective.ResetTimerState();
    }

    void Awake()
    {
        if (DutzGoldCoin.IsTrackCoinRoot(gameObject))
        {
            Destroy(this);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ConfigureForActiveScene();
        if (DutzEndHouseCollider.UsesHouseRoofWin)
            DutzEndHouseCollider.EnsureFromBoot();
        player = FindObjectOfType<DutzPlayerController>();
        if (player != null)
            respawn = player.GetComponent<DutzFallRespawn>();

        var flagPoleGo = GameObject.Find(FlagPoleName);
        if (flagPoleGo != null)
        {
            flagPole = flagPoleGo.transform;
            flagPoleCollider = flagPoleGo.GetComponentInChildren<Collider>();
        }

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.volume = 0.8f;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void ApplySceneStartMessage()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == DutzMobileRuntime.Level01SceneName)
        {
            startMessage = "Crocodiles Incoming!";
            winMessage = string.Empty;
        }
        else if (sceneName == DutzMobileRuntime.Level02SceneName)
        {
            winMessage = "YOU REACHED THE PLANE IN TIME. FLY TO THE HAGUE NOW!";
        }
        else if (sceneName == DutzMobileRuntime.Level00SceneName)
        {
            startMessage = string.Empty;
            winMessage = "GREAT JOB! YOU REACHED THE PHILIPPINES SENATE!";
            levelDurationSeconds = 300f;
        }
        else if (sceneName == DutzMobileRuntime.Level03SceneName)
        {
            startMessage = "WELCOME TO THE HAGUE";
            winMessage = "BEYBI M defeated! Dutz is free!";
            levelDurationSeconds = 300f;
        }
        else if (sceneName == DutzMobileRuntime.Level07SceneName)
        {
            startMessage =
                "WELCOME TO THE IMPEACHMENT TRIAL\nYou need 16 votes to impeach Princess Z.";
            winMessage = "PRINCESS Z IMPEACHED!";
            levelDurationSeconds = 600f;
        }
    }

    void Update()
    {
        // Pause/resume must work with timeScale 0 and must not rely on flaky GUI.Button taps.
        if (ShouldShowGameplayTimer())
            DutzGamePause.PollTouchToggle();
        else
        {
            DutzGamePause.GuiHitRect = default;
        }

        if (DutzLevelStartGate.IsBlockingStart)
            return;

        if (startMessagePending && DutzDifficulty.HasChosen)
        {
            startMessagePending = false;
            if (UsesStartMessageOk())
            {
                startMessageAwaitingOk = true;
                if (player != null)
                    player.SetControlsLocked(true);
                UnlockCursorForLevelChoice();
            }
            else
            {
                startMessageTimeLeft = startMessageDuration;
            }
        }

        if (startMessageTimeLeft > 0f)
            startMessageTimeLeft -= Time.deltaTime;

        if (finished)
        {
            EnsureLevelCompleteChoiceReady();
            return;
        }

        if (player == null)
        {
            player = FindObjectOfType<DutzPlayerController>();
            if (player != null)
                respawn = player.GetComponent<DutzFallRespawn>();
            return;
        }

        if (flagPole == null)
        {
            var flagPoleGo = GameObject.Find(FlagPoleName);
            if (flagPoleGo != null)
            {
                flagPole = flagPoleGo.transform;
                flagPoleCollider = flagPoleGo.GetComponentInChildren<Collider>();
            }
        }

        if (!DutzDifficulty.HasChosen)
            return;

        if (!finished && DutzSenateBuildingMuralGoal.UsesSenateBuildingWin && IsTouchingSenateBuildingMural())
        {
            NotifySenateBuildingMuralReached();
            return;
        }

        if (!finished && DutzEndHouseCollider.UsesHouseRoofWin && IsOnEndHouseRoof())
        {
            NotifyEndGoalReached();
            return;
        }

        if (startMessageAwaitingOk)
            return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            LoseLevel();
            return;
        }
    }

    public static void NotifyFlagPoleTouched() => NotifyEndGoalReached();

    public static void NotifySenateBuildingMuralReached()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        var objective = GetObjective();
        if (objective == null || objective.finished)
            return;

        if (!DutzDifficulty.HasChosen || objective.startMessageAwaitingOk)
            return;

        objective.WinLevel();
    }

    public static void NotifyEndGoalReached()
    {
        if (DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var objective = GetObjective();
        if (objective == null || objective.finished)
            return;

        if (!DutzDifficulty.HasChosen)
            return;

        if (!objective.IsAtEndGoal())
            return;

        objective.WinLevel();
    }

    public static void NotifyLevel03BossDefeated(string message)
    {
        // Level 7 keeps BEYBI M fightable but wins via Senate impeachment votes.
        if (!DutzCollectibleProgress.IsLevel03 || DutzCollectibleProgress.IsLevel07)
            return;

        var objective = GetObjective();
        if (objective == null)
        {
            var go = new GameObject(ManagerName);
            objective = go.AddComponent<DutzLevelObjective>();
        }

        if (objective.finished)
        {
            Debug.LogWarning("[Dutz] BEYBI M defeated but level objective already finished.");
            return;
        }

        if (!DutzDifficulty.HasChosen)
            DutzDifficulty.Choose(DutzDifficultyLevel.Hard);

        if (!string.IsNullOrEmpty(message))
            objective.winMessage = message;

        objective.WinLevel();
    }

    /// <summary>Level07 — 16+ votes after Boy Idol defeated → Sara convicted.</summary>
    public static void NotifyLevel07ImpeachmentWon()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        if (DutzVotesCounter.Votes < DutzSenateVotesOffer.VotesToImpeach)
            return;

        if (!DutzLevel07BoyIdolGate.IsBoyIdolDefeated)
            return;

        var objective = GetObjective();
        if (objective == null)
        {
            var go = new GameObject(ManagerName);
            objective = go.AddComponent<DutzLevelObjective>();
        }

        if (objective.finished)
            return;

        if (!DutzDifficulty.HasChosen)
            DutzDifficulty.Choose(DutzDifficultyLevel.Hard);

        objective.winMessage = "PRINCESS Z IMPEACHED!";
        objective.WinLevel();
    }

    /// <summary>
    /// Level07 — Senate purchase still short of 16 votes: Sara acquitted, then reload.
    /// </summary>
    public static void NotifyLevel07ImpeachmentFailedNotEnoughVotes()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        var objective = GetObjective();
        if (objective == null)
        {
            var go = new GameObject(ManagerName);
            objective = go.AddComponent<DutzLevelObjective>();
        }

        if (objective.finished)
            return;

        objective.StartCoroutine(objective.Level07NotEnoughVotesFailRoutine());
    }

    IEnumerator Level07NotEnoughVotesFailRoutine()
    {
        if (finished)
            yield break;

        finished = true;
        won = false;
        IsLevelFinished = true;
        levelFinishedSceneName = SceneManager.GetActiveScene().name;
        statusMessage = "NOT ENOUGH VOTES — SARA ACQUITTED";

        if (player != null)
            player.SetControlsLocked(true);

        Debug.Log("[Dutz] Senate purchase fell short of 16 votes — playing Sara acquitted.");
        yield return DutzLevel07ImpeachmentVideo.PlayFailThenReloadLevel();
    }

    void ConfigureForActiveScene()
    {
        ApplySceneStartMessage();
        ResetSessionStateForScene();
        timeLeft = levelDurationSeconds;
        startMessageTimeLeft = 0f;
        startMessagePending = true;
        startMessageAwaitingOk = false;
    }

    void ResetSessionStateForScene()
    {
        IsLevelFinished = false;
        levelFinishedSceneName = null;
        finished = false;
        won = false;
        showingLevelCompleteChoice = false;
        level03VictoryVideoStarted = false;
        level03AwaitingVictoryContinue = false;
        level03ShareInProgress = false;
        level03ComposedSharePath = null;
        ReleaseLevel03SharePreview();
        level03ShareStatus = null;
        statusMessage = string.Empty;
        scoreRollComplete = false;
        winCelebrationStartedAt = 0f;
    }

    void ResetTimerState()
    {
        ResetSessionStateForScene();
        timeLeft = levelDurationSeconds;

        if (DutzDifficulty.HasChosen && !string.IsNullOrEmpty(startMessage))
            startMessageTimeLeft = startMessageDuration;
    }

    bool ShouldShowGameplayTimer()
    {
        if (finished || DutzVictoryVideoPlayback.ShouldHideWinGui || IsLevelFinishedForActiveScene)
            return false;

        if (startMessageAwaitingOk)
            return false;

        // IMGUI timer/pause draws over AdMob full-screen and can block Close/Reward UI.
        if (FloodRewardedAdStub.IsShowing)
            return false;

        return true;
    }

    void DrawPauseButtonBesideTimer(string timerText)
    {
        var scale = Mathf.Max(1f, Screen.height / 720f);
        var textSize = gameplayTimerStyle.CalcSize(new GUIContent(timerText));
        var timerBandWidth = Screen.width - DutzCollectibleHudDraw.TimerRightMargin;
        var timerCenterX = timerBandWidth * 0.5f;
        // Finger-sized hit target — GUI.Button alone is unreliable on Android IMGUI.
        var buttonWidth = Mathf.Max(180f, 168f * scale);
        var buttonHeight = Mathf.Max(72f, 64f * scale);
        var gap = 10f * scale;
        var x = timerCenterX + textSize.x * 0.5f + gap;
        var maxX = Screen.width - buttonWidth - 8f;
        if (x > maxX)
            x = maxX;
        var y = 2f * scale;
        var rect = new Rect(x, y, buttonWidth, buttonHeight);
        DutzGamePause.GuiHitRect = rect;

        // Transparent background: label only. Toggle is handled by PollTouchToggle in Update.
        if (pauseButtonStyle == null)
        {
            pauseButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(26f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
        }

        var label = DutzGamePause.IsPaused ? "RESUME" : "PAUSE";
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), label, pauseButtonStyle);
        GUI.color = new Color(1f, 1f, 1f, 0.95f);
        GUI.Label(rect, label, pauseButtonStyle);
        GUI.color = prev;
    }

    static bool UsesStartMessageOk() => false;

    bool IsTouchingSenateBuildingMural() =>
        player != null && DutzSenateBuildingMuralGoal.IsPlayerTouchingSenateBuildingMural(player);

    void DismissStartMessageOk()
    {
        startMessageAwaitingOk = false;
        if (player != null)
            player.SetControlsLocked(false);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    bool IsAtEndGoal()
    {
        if (DutzEndHouseCollider.UsesHouseRoofWin)
            return IsOnEndHouseRoof();

        return false;
    }

    bool IsOnEndHouseRoof()
    {
        if (player == null)
            return false;

        var cc = player.GetComponent<CharacterController>();
        return DutzEndHouseCollider.IsPlayerOnRoof(cc);
    }

    bool IsTouchingFlagPole()
    {
        if (player == null)
            return false;

        if (flagPole == null)
        {
            var flagPoleGo = GameObject.Find(FlagPoleName);
            if (flagPoleGo == null)
                return false;

            flagPole = flagPoleGo.transform;
            flagPoleCollider = flagPoleGo.GetComponentInChildren<Collider>();
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        var playerBounds = DutzHippieBiteCollider.GetPlayerBodyBounds(cc);
        var reach = DutzFlagPoleGoal.PlayerTouchReachMeters;
        foreach (var col in flagPole.GetComponentsInChildren<Collider>())
        {
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            if (!DutzFlagPoleGoal.IsFlagPoleCollider(col))
                continue;

            if (DutzHippieBiteCollider.IsTouchingPlayerBody(col, playerBounds, reach))
                return true;
        }

        return false;
    }

    bool IsReadyForLevelCompleteDialog() =>
        won && finished && scoreRollComplete && ShouldOfferLevelCompleteChoice();

    void LoseLevel()
    {
        IsLevelFinished = true;
        levelFinishedSceneName = SceneManager.GetActiveScene().name;
        finished = true;
        won = false;
        statusMessage = timeoutMessage;

        if (respawn != null)
            respawn.TriggerDeathDialog(timeoutMessage);
        else if (player != null)
            player.SetControlsLocked(true);
    }

    void WinLevel()
    {
        IsLevelFinished = true;
        levelFinishedSceneName = SceneManager.GetActiveScene().name;
        DutzLevelUnlockProgress.UnlockOnLevelComplete(levelFinishedSceneName);

        if (player != null)
            player.SetControlsLocked(true);

        DutzBackgroundMusic.StopForCelebration();

        if (DutzCollectibleProgress.IsLevel03)
            DutzLevel03VictoryVideo.PlaySceneActions();

        BeginWinCelebration();
    }

    public void DismissLevel03FinaleShare()
    {
        level03AwaitingVictoryContinue = false;
        ReleaseLevel03SharePreview();
    }

    public void ShareLevel03VictorySelfie()
    {
        if (level03ShareInProgress || !won || !DutzCollectibleProgress.IsLevel03)
            return;

        StartCoroutine(ShareLevel03VictorySelfieRoutine());
    }

    public void DownloadLevel03VictorySelfie()
    {
        if (level03ShareInProgress || !won || !DutzCollectibleProgress.IsLevel03)
            return;

        StartCoroutine(DownloadLevel03VictorySelfieRoutine());
    }

    public void PromptLevel03VictoryPhoto()
    {
        if (!won || !level03AwaitingVictoryContinue)
            return;

        DutzVictorySelfieCaptureHud.BeginCapture(Level03VictoryPhotoCapturedBridge);
    }

    public void PromptLevel03VictoryGalleryPhoto()
    {
        if (!won || !level03AwaitingVictoryContinue)
            return;

        DutzVictorySelfiePhotoPick.PickFromGallery(Level03VictoryPhotoCapturedBridge);
    }

    static void Level03VictoryPhotoCapturedBridge(Texture2D photo) =>
        Instance?.HandleLevel03VictoryPhotoCaptured(photo);

    void HandleLevel03VictoryPhotoCaptured(Texture2D photo)
    {
        if (photo == null)
            return;

        DutzVictorySelfieProfile.SavePhotoTexture(photo);
        InvalidateLevel03ShareComposition();
        StartCoroutine(RecomposeLevel03SharePreviewRoutine(DutzVictorySelfieProfile.GetPhotoTexture() ?? photo));
    }

    void InvalidateLevel03ShareComposition()
    {
        level03ComposedSharePath = null;
        ReleaseLevel03SharePreview();
    }

    IEnumerator RecomposeLevel03SharePreviewRoutine(Texture2D userPhoto)
    {
        level03ShareInProgress = true;
        level03ShareStatus = "Photo added — updating share image…";

        string savedPath = null;
        yield return DutzVictorySelfieComposer.ComposeAndSaveAsync(userPhoto, path => savedPath = path);
        level03ComposedSharePath = savedPath;
        level03ShareInProgress = false;
        LoadLevel03SharePreviewFromPath(level03ComposedSharePath);
        level03ShareStatus = string.IsNullOrEmpty(level03ComposedSharePath)
            ? "Could not update share image."
            : "Photo ready in frame.";
    }

    IEnumerator EnsureLevel03ShareImageReady()
    {
        if (!string.IsNullOrEmpty(level03ComposedSharePath) && System.IO.File.Exists(level03ComposedSharePath))
        {
            if (level03ComposedSharePreview == null && level03AwaitingVictoryContinue)
                LoadLevel03SharePreviewFromPath(level03ComposedSharePath);
            yield break;
        }

        level03ShareInProgress = true;
        level03ShareStatus = null;
        var selfie = DutzVictorySelfieProfile.GetPhotoTexture();
        string savedPath = null;
        yield return DutzVictorySelfieComposer.ComposeAndSaveAsync(selfie, path => savedPath = path);
        level03ComposedSharePath = savedPath;
        level03ShareInProgress = false;

        if (level03AwaitingVictoryContinue)
            LoadLevel03SharePreviewFromPath(level03ComposedSharePath);
    }

    void ReleaseLevel03SharePreview()
    {
        if (level03ComposedSharePreview == null)
            return;

        Destroy(level03ComposedSharePreview);
        level03ComposedSharePreview = null;
    }

    void LoadLevel03SharePreviewFromPath(string path)
    {
        ReleaseLevel03SharePreview();
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            return;

        var bytes = System.IO.File.ReadAllBytes(path);
        level03ComposedSharePreview = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!level03ComposedSharePreview.LoadImage(bytes))
        {
            Destroy(level03ComposedSharePreview);
            level03ComposedSharePreview = null;
        }
    }

    IEnumerator PrepareLevel03SharePreviewRoutine()
    {
        yield return EnsureLevel03ShareImageReady();
        LoadLevel03SharePreviewFromPath(level03ComposedSharePath);
    }

    IEnumerator ShareLevel03VictorySelfieRoutine()
    {
        yield return EnsureLevel03ShareImageReady();

        if (string.IsNullOrEmpty(level03ComposedSharePath))
        {
            level03ShareStatus = "Could not build share image.";
            yield break;
        }

        var caption = DutzVictorySelfieShare.BuildShareCaption(DutzVictorySelfieProfile.DisplayName, winFinalScore);
        var shared = DutzVictorySelfieShare.ShareVictoryCard(level03ComposedSharePath, caption);
        if (!shared)
            level03ShareStatus = "Share failed — check storage permission and try again.";
    }

    IEnumerator DownloadLevel03VictorySelfieRoutine()
    {
        yield return EnsureLevel03ShareImageReady();

        if (string.IsNullOrEmpty(level03ComposedSharePath))
        {
            level03ShareStatus = "Could not build share image.";
            yield break;
        }

        var saved = DutzVictorySelfieShare.SaveVictoryCardToGallery(level03ComposedSharePath);
        level03ShareStatus = saved
            ? "Saved to Download/BattleOfTheHague — check your Downloads folder."
            : "Download failed — allow storage access and try again.";
    }

    IEnumerator PlayLevel03FinaleVideoRoutine()
    {
        if (level03VictoryVideoStarted)
            yield break;

        level03VictoryVideoStarted = true;

        if (DutzLevel03VictoryVideo.IsAvailable())
            yield return DutzLevel03VictoryVideo.Play();
        else
            Debug.LogWarning("[Dutz] DUTZHOME.mp4 not available — skipping victory video.");
    }

    void BeginWinCelebration()
    {
        finished = true;
        won = true;
        ComputeWinScore();
        statusMessage = winMessage;
        winCelebrationStartedAt = Time.unscaledTime;
        PlayCelebrationTune();
        StartCoroutine(WinScoreSequence());
    }

    IEnumerator WinScoreSequence()
    {
        if (DutzCollectibleProgress.IsLevel03)
        {
            yield return AnimateWinScoreRoll();
            yield return new WaitForSecondsRealtime(winScoreHoldSeconds);
            yield return PlayLevel03FinaleVideoRoutine();
            level03AwaitingVictoryContinue = true;
            StartCoroutine(PrepareLevel03SharePreviewRoutine());
            UnlockCursorForLevelChoice();
            while (level03AwaitingVictoryContinue)
                yield return null;

            ShowLevelCompleteChoiceOrAdvance();
            yield break;
        }

        if (DutzCollectibleProgress.IsLevel07)
        {
            yield return AnimateWinScoreRoll();
            // AnimateWinScoreRoll marks the roll done — clear it so choices wait until after the video.
            scoreRollComplete = false;
            yield return new WaitForSecondsRealtime(winScoreHoldSeconds);
            yield return DutzLevel07ImpeachmentVideo.PlayVictoryIfAvailable();
            scoreRollComplete = true;
            displayedScore = winFinalScore;
            displayedTimeBonus = winTimeBonus;
            displayedCoinBonus = winCoinBonus;
            ShowLevelCompleteChoiceOrAdvance();
            yield break;
        }

        yield return AnimateWinScoreRoll();
        yield return new WaitForSecondsRealtime(winScoreHoldSeconds);
        ShowLevelCompleteChoiceOrAdvance();
    }

    void EnsureLevelCompleteChoiceReady()
    {
        if (!won)
            return;

        if (DutzVictoryVideoPlayback.IsPlaying)
            return;

        if (scoreRollComplete)
        {
            if (!showingLevelCompleteChoice || !ShouldShowLevelCompleteDialog())
                ShowLevelCompleteChoiceOrAdvance();
            return;
        }

        var graceSeconds = DutzCollectibleProgress.IsLevel03 ? 120f : 4f;
        if (Time.unscaledTime - winCelebrationStartedAt < graceSeconds)
            return;

        scoreRollComplete = true;
        displayedScore = winFinalScore;
        displayedTimeBonus = winTimeBonus;
        displayedCoinBonus = winCoinBonus;
        ShowLevelCompleteChoiceOrAdvance();
    }

    void ShowLevelCompleteChoiceOrAdvance()
    {
        if (!won || !scoreRollComplete)
            return;

        if (DutzCollectibleProgress.IsLevel03)
            return;

        if (ShouldOfferLevelCompleteChoice())
        {
            if (showingLevelCompleteChoice)
                return;

            showingLevelCompleteChoice = true;
            UnlockCursorForLevelChoice();
            return;
        }

        TryLoadNextBuildScene();
    }

    bool ShouldDrawLevelCompleteChoice() => IsReadyForLevelCompleteDialog();

    void TryLoadNextBuildScene()
    {
        var current = SceneManager.GetActiveScene().buildIndex;
        var next = current + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            statusMessage = winMessage + " (No next level in Build Settings.)";
    }

    public void PlayLevel1WithTransitionVideo()
    {
        if (level00TransitionStarted)
            return;

        level00TransitionStarted = true;
        showingLevelCompleteChoice = false;
        statusMessage = string.Empty;
        DutzVictoryVideoPlayback.BeginTransitionOverlaySuppression();
        DutzLevel00TransitionVideo.MarkPlayingStarted();
        StartCoroutine(PlayLevel1AfterTransitionVideo());
    }

    IEnumerator PlayLevel1AfterTransitionVideo()
    {
        yield return DutzLevel00TransitionVideo.Play();
        PlayLevel1();
    }

    public void PlayLevel1()
    {
        Time.timeScale = 1f;
        DutzGamePause.Resume();
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level01SceneName);
    }

    public void PlayLevel2WithTransitionVideo()
    {
        PlayLevel2();
    }

    public void PlayLevel2()
    {
        Time.timeScale = 1f;
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level02SceneName);
    }

    public void PlayLevel3WithTransitionVideo()
    {
        if (level03TransitionStarted)
            return;

        if (!HasLevel03InBuild())
        {
            Debug.LogError(
                "[Dutz] GO TO HAGUE aborted — Dutz_Level03 is not in File → Build Settings.\n" +
                "Prepare Campaign build (L00–L03) or add Dutz_Level03 to Build Settings, then try again.");
            statusMessage = "Level 3 is not in Build Settings. Prepare Campaign (L00–L03) first.";
            showingLevelCompleteChoice = true;
            UnlockCursorForLevelChoice();
            return;
        }

        level03TransitionStarted = true;
        showingLevelCompleteChoice = false;
        statusMessage = string.Empty;
        DutzVictoryVideoPlayback.BeginTransitionOverlaySuppression();
        StartCoroutine(PlayLevel3AfterTransitionVideo());
    }

    IEnumerator PlayLevel3AfterTransitionVideo()
    {
        yield return DutzLevel02TransitionVideo.Play();

        if (!HasLevel03InBuild())
        {
            RestoreLevel02CompleteChoiceAfterFailedLevel3Load(
                "Level 3 is not in Build Settings — cannot load after Hague video.");
            yield break;
        }

        PlayLevel3();
    }

    void RestoreLevel02CompleteChoiceAfterFailedLevel3Load(string message)
    {
        level03TransitionStarted = false;
        DutzVictoryVideoPlayback.ResetForSceneLoad();
        showingLevelCompleteChoice = true;
        statusMessage = message;
        UnlockCursorForLevelChoice();
        Debug.LogError("[Dutz] " + message);
    }

    public void PlayLevel3()
    {
        if (!HasLevel03InBuild())
        {
            RestoreLevel02CompleteChoiceAfterFailedLevel3Load(
                "Level 3 is not in Build Settings. Add Dutz_Level03 (File → Build Settings) for Editor Play.");
            return;
        }

        Time.timeScale = 1f;
        ResetStaticStateForNewScene();
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level03SceneName);
    }

    public void RestartLevel0()
    {
        Time.timeScale = 1f;
        DutzGamePause.Resume();
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level00SceneName);
    }

    public void RestartLevel1()
    {
        Time.timeScale = 1f;
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level01SceneName);
    }

    public void RestartLevel2()
    {
        Time.timeScale = 1f;
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level02SceneName);
    }

    public void RestartLevel07()
    {
        Time.timeScale = 1f;
        ResetStaticStateForNewScene();
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level07SceneName);
    }

    /// <summary>Level07 win — jump to Level 0 so the unlocked-levels picker can show.</summary>
    public void GoToUnlockedLevels()
    {
        Time.timeScale = 1f;
        ResetStaticStateForNewScene();
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(DutzMobileRuntime.Level00SceneName);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        DutzGamePause.Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void UnlockCursorForLevelChoice()
    {
        DutzGamePause.Resume();
        DutzCartoonDialogGui.ResetPanelScroll();

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    IEnumerator AnimateWinScoreRoll()
    {
        displayedScore = 0;
        displayedTimeBonus = 0;
        displayedCoinBonus = 0;
        scoreRollComplete = false;

        if (winFinalScore <= 0)
        {
            scoreRollComplete = true;
            yield break;
        }

        var timePhase = winTimeBonus > 0
            ? Mathf.Clamp(1.1f + winTimeBonus / 2800f, 1.1f, 2.4f)
            : 0f;
        var coinPhase = winCoinBonus > 0
            ? Mathf.Clamp(1.1f + winCoinBonus / 2800f, 1.1f, 2.4f)
            : 0f;

        if (timePhase > 0f)
        {
            var elapsed = 0f;
            while (elapsed < timePhase)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(Mathf.Clamp01(elapsed / timePhase));
                displayedTimeBonus = Mathf.RoundToInt(winTimeBonus * t);
                displayedScore = displayedTimeBonus;
                yield return null;
            }
        }

        displayedTimeBonus = winTimeBonus;
        displayedScore = winTimeBonus;

        if (coinPhase > 0f)
        {
            if (timePhase > 0f)
                yield return new WaitForSecondsRealtime(0.12f);

            var elapsed = 0f;
            while (elapsed < coinPhase)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(Mathf.Clamp01(elapsed / coinPhase));
                displayedCoinBonus = Mathf.RoundToInt(winCoinBonus * t);
                displayedScore = winTimeBonus + displayedCoinBonus;
                yield return null;
            }
        }

        displayedTimeBonus = winTimeBonus;
        displayedCoinBonus = winCoinBonus;
        displayedScore = winBaseScore;

        if (winDifficultyMultiplier > 1)
        {
            if (coinPhase > 0f || timePhase > 0f)
                yield return new WaitForSecondsRealtime(0.12f);

            var multPhase = Mathf.Clamp(1f + winFinalScore / 4000f, 1f, 2f);
            var elapsed = 0f;
            while (elapsed < multPhase)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(Mathf.Clamp01(elapsed / multPhase));
                displayedScore = Mathf.RoundToInt(Mathf.Lerp(winBaseScore, winFinalScore, t));
                yield return null;
            }
        }

        displayedTimeBonus = winTimeBonus;
        displayedCoinBonus = winCoinBonus;
        displayedScore = winFinalScore;
        scoreRollComplete = true;
    }

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    void ComputeWinScore()
    {
        winRemainingSeconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
        winTimeBonus = winRemainingSeconds * TimeBonusPerSecond;
        winCoinBonus = DutzCollectibleProgress.CollectedCount * CoinBonusEach;
        winBaseScore = winTimeBonus + winCoinBonus;
        winDifficultyMultiplier = DutzDifficulty.GetScoreMultiplier();
        winFinalScore = winBaseScore * winDifficultyMultiplier;
    }

    void PlayCelebrationTune()
    {
        var clip = CreateCelebrationClip();
        if (clip != null)
            oneShotSource.PlayOneShot(clip, DutzAudioSettings.ScaleSfx(0.9f));
    }

    static AudioClip CreateCelebrationClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.6f;
        var samples = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[samples];
        var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)sampleRate;
            var noteIndex = Mathf.Min((int)(t / 0.4f), notes.Length - 1);
            var freq = notes[noteIndex];
            var env = Mathf.Clamp01(1f - (t % 0.4f) * 2f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
        }

        var clip = AudioClip.Create("DutzCelebrationTune", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void OnGUI()
    {
        if (DutzVictoryVideoPlayback.ShouldHideWinGui)
            return;

        if (ShouldDrawLevelCompleteChoice() && !showingLevelCompleteChoice)
        {
            showingLevelCompleteChoice = true;
            UnlockCursorForLevelChoice();
        }

        if (IsStartMessageActive)
            DrawStartMessage();

        if (ShouldShowGameplayTimer())
        {
            var timer = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
            var minutes = timer / 60;
            var seconds = timer % 60;
            var timerText = $"TIME LEFT: {minutes:00}:{seconds:00}";

            if (gameplayTimerStyle == null)
            {
                gameplayTimerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow },
                    alignment = TextAnchor.UpperCenter
                };
            }

            // Keep timer out of the top-right collectible HUD band.
            var timerBandWidth = Screen.width - DutzCollectibleHudDraw.TimerRightMargin;
            var timerRect = new Rect(0f, 8f, timerBandWidth, 34f);
            GUI.Label(timerRect, timerText, gameplayTimerStyle);

            DrawPauseButtonBesideTimer(timerText);
        }

        if (!finished)
            return;

        if (IsTransitionVideoActiveOrPending())
            return;

        if (won)
        {
            var level03ShareScreen = DutzCollectibleProgress.IsLevel03
                && scoreRollComplete
                && level03AwaitingVictoryContinue;

            if (level03ShareScreen || DutzVictorySelfieCaptureHud.IsActive)
            {
                if (level03ShareScreen && !DutzVictorySelfieCaptureHud.IsActive)
                {
                    var previousDepth = GUI.depth;
                    GUI.depth = -1000;
                    DrawLevel03ShareDialog();
                    GUI.depth = previousDepth;
                }

                return;
            }

            if (ShouldDrawLevelCompleteChoice())
                return;

            var previousWinDepth = GUI.depth;
            GUI.depth = -1000;
            DrawWinScoreUi();
            GUI.depth = previousWinDepth;
            return;
        }

        if (string.IsNullOrEmpty(statusMessage))
            return;

        var msgStyle = DutzCartoonDialogGui.BodyStyle();
        msgStyle.normal.textColor = Color.red;
        var boxWidth = DutzCartoonDialogGui.FitMessageBoxWidth(statusMessage, msgStyle, DutzCartoonDialogGui.PanelWidth);
        var boxHeight = Mathf.Max(
            DutzCartoonDialogGui.FitMessageBoxHeight(statusMessage, msgStyle, boxWidth),
            DutzCartoonDialogGui.Scale(130f, 200f));
        var boxRect = new Rect(
            (Screen.width - boxWidth) * 0.5f,
            Screen.height * 0.2f,
            boxWidth,
            boxHeight);
        DutzCartoonDialogGui.DrawFrame(boxRect);
        DutzCartoonDialogGui.DrawOutlinedLabel(DutzCartoonDialogGui.ContentRect(boxRect), statusMessage, msgStyle, Color.black);
    }

    void DrawWinScoreUi()
    {
        var compact = DutzCartoonDialogGui.IsCompactLayout;

        var timeLine =
            $"Time bonus: {winRemainingSeconds} sec × {TimeBonusPerSecond} = {displayedTimeBonus:N0}";
        var coinLine =
            $"{DutzCollectibleProgress.BonusNoun} bonus: {DutzCollectibleProgress.CollectedCount} × {CoinBonusEach} = {displayedCoinBonus:N0}";
        var subtotalLine =
            $"Subtotal: {displayedTimeBonus:N0} + {displayedCoinBonus:N0} = {winBaseScore:N0}";
        var difficultyLine =
            $"{DutzDifficulty.GetDisplayName(DutzDifficulty.Selected)} (×{winDifficultyMultiplier}): {displayedScore:N0}";
        var breakdownLines = new[] { timeLine, coinLine, subtotalLine, difficultyLine };

        var height = DutzCartoonDialogGui.WinScoreDialogHeight(
            winMessage,
            $"SCORE: {displayedScore:N0}",
            breakdownLines,
            includeChoices: false,
            choiceButtonLabels: null);

        var boxRect = new Rect(
            (Screen.width - DutzCartoonDialogGui.PanelWidth) * 0.5f,
            compact ? Screen.height * 0.06f : Screen.height * 0.16f,
            DutzCartoonDialogGui.PanelWidth,
            height);
        DutzCartoonDialogGui.DrawFrame(boxRect);

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(boxRect));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle(new Color(0.1f, 0.55f, 0.18f));
        if (!string.IsNullOrEmpty(winMessage))
        {
            GUILayout.Label(winMessage, titleStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        }

        var scoreStyle = DutzCartoonDialogGui.TitleStyle(new Color(1f, 0.9f, 0.2f));
        scoreStyle.fontSize = DutzCartoonDialogGui.ScaleFont(42, compact ? 48 : 64);
        var scoreColor = scoreRollComplete
            ? new Color(1f, 0.9f, 0.2f)
            : new Color(1f, 0.72f, 0.1f);
        scoreStyle.normal.textColor = scoreColor;
        GUILayout.Label($"SCORE: {displayedScore:N0}", scoreStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));

        var lineStyle = DutzCartoonDialogGui.HintStyle();
        lineStyle.fontSize = DutzCartoonDialogGui.ScaleFont(22, compact ? 24 : 30);
        GUILayout.Label(timeLine, lineStyle);
        GUILayout.Label(coinLine, lineStyle);
        GUILayout.Space(8f);
        GUILayout.Label(subtotalLine, lineStyle);
        GUILayout.Label(difficultyLine, lineStyle);

        GUILayout.EndArea();
    }

    void DrawLevel03ShareDialog()
    {
        const string takeSelfieLabel = "TAKE SELFIE";
        const string choosePhotoLabel = "CHOOSE PHOTO";
        const string shareLabel = "SHARE DUTZ IS FREE";
        const string downloadLabel = "DOWNLOAD IMAGE";
        const string exitLabel = "EXIT THE GAME";

        var footerLabels = new[]
        {
            takeSelfieLabel,
            choosePhotoLabel,
            shareLabel,
            downloadLabel,
            exitLabel
        };

        var spacing = DutzCartoonDialogGui.Scale(6f, 10f);
        var footerHeight = DutzCartoonDialogGui.PanelPadding;
        for (var i = 0; i < footerLabels.Length; i++)
        {
            if (i > 0)
                footerHeight += spacing;
            footerHeight += DutzCartoonDialogGui.MeasureActionButtonHeight(footerLabels[i]);
        }

        var hintText = level03ShareInProgress
            ? "Building DUTZ IS FREE share image…"
            : string.IsNullOrEmpty(level03ShareStatus)
                ? "Add your photo to the frame, then share or download."
                : level03ShareStatus;

        var frameHeight = DutzCartoonDialogGui.Level03ShareDialogHeight(hintText, footerLabels);
        DutzCartoonDialogGui.DrawDimOverlay(0.55f);

        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(frameHeight);
        DutzCartoonDialogGui.DrawFrame(frame);
        var content = DutzCartoonDialogGui.ContentRect(frame);

        var footerRect = new Rect(
            content.x,
            content.yMax - footerHeight,
            content.width,
            footerHeight);
        var bodyRect = new Rect(
            content.x,
            content.y,
            content.width,
            Mathf.Max(0f, content.height - footerHeight));

        var hintStyle = DutzCartoonDialogGui.HintStyle();
        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle(new Color(0.1f, 0.55f, 0.18f));

        GUILayout.BeginArea(bodyRect);
        level03ShareBodyScroll = GUILayout.BeginScrollView(level03ShareBodyScroll);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label("DUTZ IS FREE!", titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label($"Final score: {winFinalScore:N0}", hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 14f));

        var previewHeight = DutzCartoonDialogGui.VictorySharePreviewHeight();
        var previewRect = GUILayoutUtility.GetRect(
            content.width - DutzCartoonDialogGui.PanelPadding * 2f,
            previewHeight);
        GUI.color = new Color(0.12f, 0.12f, 0.16f, 1f);
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (level03ComposedSharePreview != null)
            GUI.DrawTexture(previewRect, level03ComposedSharePreview, ScaleMode.ScaleToFit);
        else if (level03ShareInProgress)
            GUI.Label(previewRect, "Building DUTZ IS FREE image…", hintStyle);
        else
            GUI.Label(previewRect, "Preview unavailable", hintStyle);

        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        GUILayout.Label(hintText, hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUILayout.BeginArea(footerRect);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding * 0.5f);

        if (DutzCartoonDialogGui.ActionButton(
                takeSelfieLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(takeSelfieLabel)))
            PromptLevel03VictoryPhoto();

        GUILayout.Space(spacing);
        if (DutzCartoonDialogGui.ActionButton(
                choosePhotoLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(choosePhotoLabel)))
            PromptLevel03VictoryGalleryPhoto();

        GUILayout.Space(spacing);
        if (!level03ShareInProgress
            && DutzCartoonDialogGui.ActionButton(
                shareLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(shareLabel)))
            ShareLevel03VictorySelfie();

        GUILayout.Space(spacing);
        if (!level03ShareInProgress
            && DutzCartoonDialogGui.ActionButton(
                downloadLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(downloadLabel)))
            DownloadLevel03VictorySelfie();

        GUILayout.Space(spacing);
        if (DutzCartoonDialogGui.DismissButton(
                exitLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(exitLabel)))
            ExitGame();

        GUILayout.EndArea();
    }

    void DrawStartMessage()
    {
        if (startMessageAwaitingOk)
        {
            var previousDepth = GUI.depth;
            GUI.depth = -2500;
            DutzCartoonDialogGui.DrawDimOverlay(0.55f);

            var height = DutzCartoonDialogGui.ChoiceDialogHeight(
                startMessage,
                string.Empty,
                new[] { "OK" });
            var frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);
            DutzCartoonDialogGui.DrawFrame(frame);

            var bodyStyle = DutzCartoonDialogGui.BodyStyle();

            GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
            GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
            GUILayout.Label(startMessage, bodyStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));

            if (DutzCartoonDialogGui.ActionButton("OK", heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight("OK")))
                DismissStartMessageOk();

            GUILayout.EndArea();
            GUI.depth = previousDepth;
            return;
        }

        DutzAnnouncementHud.DrawCartoonBanner(
            startMessage,
            DutzAnnouncementHud.DefaultFlashColor,
            DutzAnnouncementHud.StartMessageLine,
            fontScale: DutzCollectibleProgress.IsLevel07 ? 2f : 1f);
    }
}

/// <summary>
/// Proximity shop dialogs — opens after the player touches or comes within ~2 m of a shop giant.
/// Level 1 Sen Gong Bong sells Force Field + Super Jump;
/// Level 2 Princess Zara sells Force Field; Cawetan sells Super Jump.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(2500)]
public class DutzGrandmaBossPowerShop : MonoBehaviour
{
    const string ManagerName = "DutzGrandmaBossPowerShop";
    const float ShopNearReachMeters = 2f;
    const int SuperJumpCostLevel01 = 12;
    const int SuperJumpCostLevel02 = 10;
    const int ForceFieldCost = 10;
    const string CrypticHint = "FORCE FIELD IS FREE SOME PLACE ELSE.";

    enum ShopKind
    {
        PrincessZara,
        Cawetan,
        SenGongBong
    }

    static bool cawetanPurchasedThisLife;
    static DutzGrandmaBossPowerShop instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState()
    {
        cawetanPurchasedThisLife = false;
        instance = null;
    }

    DutzPlayerController player;
    Transform princessZara;
    Transform cawetan;
    ShopKind activeShop;
    bool showingDialog;
    bool wasInRangePrincessZara;
    bool wasInRangeCawetan;
    string statusMessage;

    public static bool IsShowingDialog => instance != null && instance.showingDialog;

    public static void EnsureFromBoot()
    {
        if (DutzCollectibleProgress.IsLevel00 || DutzCollectibleProgress.IsLevel07)
            return;

        if (DutzGiantBossNames.FindPrincessZara() == null && DutzGiantBossNames.FindCawetan() == null)
            return;

        DutzShopGiantTouch.EnsureOnAllShopGiants();

        if (FindObjectOfType<DutzGrandmaBossPowerShop>() != null)
            return;

        cawetanPurchasedThisLife = false;
        var go = new GameObject(ManagerName);
        go.AddComponent<DutzGrandmaBossPowerShop>();
    }

    void Awake()
    {
        instance = this;
        player = FindObjectOfType<DutzPlayerController>();
        CacheGiants();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void CacheGiants()
    {
        var zara = DutzGiantBossNames.FindPrincessZara();
        princessZara = zara != null ? zara.transform : null;

        var cawetanGiant = DutzGiantBossNames.FindCawetan();
        cawetan = cawetanGiant != null ? cawetanGiant.transform : null;
    }

    void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<DutzPlayerController>();
            if (player == null)
                return;
        }

        if (princessZara == null || cawetan == null)
            CacheGiants();

        if (showingDialog)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                CloseDialog();
            return;
        }

        if (!CanOpenShop())
            return;

        UpdateGiantShop(princessZara, ResolveGrandmaGiantShop(), ref wasInRangePrincessZara);

        if (DutzCollectibleProgress.IsLevel02)
            UpdateGiantShop(cawetan, ShopKind.Cawetan, ref wasInRangeCawetan);
    }

    static ShopKind ResolveGrandmaGiantShop() =>
        DutzCollectibleProgress.IsLevel01 ? ShopKind.SenGongBong : ShopKind.PrincessZara;

    static int GetSuperJumpCost() =>
        DutzCollectibleProgress.IsLevel01 ? SuperJumpCostLevel01 : SuperJumpCostLevel02;

    void UpdateGiantShop(Transform giant, ShopKind shop, ref bool wasInRange)
    {
        if (giant == null)
            return;

        var touching = IsPlayerTouchingGiant(giant);
        if (!touching)
        {
            wasInRange = false;
            return;
        }

        if (IsPurchased(shop) || !ShouldPromptShop(shop))
            return;

        if (touching && !wasInRange)
            OpenDialog(shop);

        wasInRange = touching;
    }

    bool IsPlayerTouchingGiant(Transform giant)
    {
        if (player == null || giant == null)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        var foundCollider = false;
        foreach (var col in giant.GetComponentsInChildren<Collider>())
        {
            if (col == null || !col.enabled)
                continue;

            foundCollider = true;

            if (DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(
                    col,
                    cc,
                    DutzHippieBiteCollider.PlayerCapsulePadding,
                    ShopNearReachMeters))
                return true;
        }

        if (foundCollider)
            return false;

        return IsPlayerNearGiantBounds(giant, cc);
    }

    static bool IsPlayerNearGiantBounds(Transform giant, CharacterController cc)
    {
        var renderers = giant.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return false;

        var playerBounds = DutzHippieBiteCollider.GetPlayerBodyBounds(cc);
        var reach = ShopNearReachMeters;

        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            var bounds = renderer.bounds;
            bounds.Expand(reach);
            if (bounds.Intersects(playerBounds))
                return true;
        }

        return false;
    }

    static bool IsPurchased(ShopKind shop)
    {
        if (shop == ShopKind.SenGongBong)
            return false;

        if (shop == ShopKind.PrincessZara)
        {
            var player = instance?.player ?? FindObjectOfType<DutzPlayerController>();
            return DutzForceField.IsPlayerShielded(player);
        }

        return cawetanPurchasedThisLife;
    }

    bool ShouldPromptShop(ShopKind shop)
    {
        if (player == null)
            return false;

        if (shop == ShopKind.SenGongBong)
        {
            return !DutzForceField.IsPlayerShielded(player)
                || (!cawetanPurchasedThisLife && !player.HasSuperJumpActive);
        }

        if (shop == ShopKind.PrincessZara)
            return !DutzForceField.IsPlayerShielded(player);

        return !cawetanPurchasedThisLife && !player.HasSuperJumpActive;
    }

    bool CanOfferShop(ShopKind shop)
    {
        if (player == null)
            return false;

        if (shop == ShopKind.SenGongBong)
            return CanOfferForceField() || CanOfferSuperJump();

        if (DutzCollectibleProgress.CollectedCount < GetCost(shop))
            return false;

        if (shop == ShopKind.PrincessZara)
            return CanOfferForceField();

        return CanOfferSuperJump();
    }

    bool CanOfferForceField() =>
        !DutzForceField.IsPlayerShielded(player)
        && DutzCollectibleProgress.CollectedCount >= ForceFieldCost;

    bool CanOfferSuperJump() =>
        !cawetanPurchasedThisLife
        && !player.HasSuperJumpActive
        && DutzCollectibleProgress.CollectedCount >= GetSuperJumpCost();

    static int GetCost(ShopKind shop) =>
        shop == ShopKind.PrincessZara ? ForceFieldCost : GetSuperJumpCost();

    static bool CanOpenShop()
    {
        if (DutzLevelObjective.IsLevelFinishedForActiveScene)
            return false;

        var activePlayer = FindObjectOfType<DutzPlayerController>();
        if (activePlayer == null)
            return false;

        var fallRespawn = activePlayer.GetComponent<DutzFallRespawn>();
        if (fallRespawn != null && fallRespawn.IsShowingRespawnDialog)
            return false;

        var difficultySelect = activePlayer.GetComponent<DutzDifficultySelect>();
        if (difficultySelect != null && difficultySelect.AwaitingSelection)
            return false;

        if (DutzLevelObjective.IsStartMessageActive)
            return false;

        return DutzDifficulty.HasChosen;
    }

    void OpenDialog(ShopKind shop)
    {
        if (player == null)
            return;

        activeShop = shop;
        showingDialog = true;
        statusMessage = string.Empty;
        player.SetControlsLocked(true);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void CloseDialog()
    {
        showingDialog = false;
        statusMessage = string.Empty;

        if (player != null)
            player.SetControlsLocked(false);

        if (!Application.isMobilePlatform && CanOpenShop())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void CompletePurchase(ShopKind purchasedItem)
    {
        if (purchasedItem == ShopKind.Cawetan)
            cawetanPurchasedThisLife = true;

        if (activeShop == ShopKind.SenGongBong)
        {
            if (!CanOfferForceField() && !CanOfferSuperJump())
                CloseDialog();
            return;
        }

        CloseDialog();
    }

    void TryPurchaseSuperJump()
    {
        if (player == null)
            return;

        if (player.HasSuperJumpActive)
        {
            statusMessage = "You already have Super Jump.";
            return;
        }

        if (!DutzCollectibleProgress.TrySpend(GetSuperJumpCost()))
        {
            statusMessage = $"Need {GetSuperJumpCost()} {DutzCollectibleProgress.SpendNounPlural} for Super Jump.";
            return;
        }

        player.EnableSuperJumpForLife();
        Debug.Log("[Dutz] Super Jump purchased.");
        CompletePurchase(ShopKind.Cawetan);
    }

    void TryPurchaseForceField()
    {
        if (player == null)
            return;

        if (DutzForceField.IsPlayerShielded(player))
        {
            statusMessage = "You already have a force field.";
            return;
        }

        if (!DutzCollectibleProgress.TrySpend(ForceFieldCost))
        {
            statusMessage = $"Need {ForceFieldCost} {DutzCollectibleProgress.SpendNounPlural} for Force Field.";
            return;
        }

        var field = player.GetComponent<DutzForceField>();
        if (field == null)
            field = player.gameObject.AddComponent<DutzForceField>();

        field.Activate(player);
        Debug.Log("[Dutz] Force Field purchased.");
        CompletePurchase(ShopKind.PrincessZara);
    }

    public static void ResetOnPlayerRespawn()
    {
        cawetanPurchasedThisLife = false;

        if (instance == null)
            return;

        instance.showingDialog = false;
        instance.wasInRangePrincessZara = false;
        instance.wasInRangeCawetan = false;
        instance.statusMessage = string.Empty;
    }

    static string GetShopTitle(ShopKind shop)
    {
        return shop switch
        {
            ShopKind.Cawetan => "CAYTEN",
            ShopKind.SenGongBong => "SEN GONG BONG",
            _ => "PRINCESS ZARA"
        };
    }

    void OnGUI()
    {
        if (!showingDialog || player == null)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = -2500;
        DutzCartoonDialogGui.DrawDimOverlay();

        var expandedShop = activeShop == ShopKind.SenGongBong;
        var shopTitle = GetShopTitle(activeShop);
        var forceFieldLabel = $"FORCE FIELD — {ForceFieldCost} {DutzCollectibleProgress.SpendNounPlural}";
        var superJumpLabel = $"SUPER JUMP — {GetSuperJumpCost()} {DutzCollectibleProgress.SpendNounPlural}";
        var forceFieldNeedHint =
            $"Need {ForceFieldCost} {DutzCollectibleProgress.SpendNounPlural} " +
            $"(you have {DutzCollectibleProgress.CollectedCount}).";
        var superJumpNeedHint =
            $"Need {GetSuperJumpCost()} {DutzCollectibleProgress.SpendNounPlural} " +
            $"(you have {DutzCollectibleProgress.CollectedCount}).";
        var height = DutzCartoonDialogGui.ShopDialogHeight(
            shopTitle,
            includeForceField: activeShop != ShopKind.Cawetan,
            includeSuperJump: activeShop == ShopKind.SenGongBong || activeShop == ShopKind.Cawetan,
            includeCrypticHint: activeShop != ShopKind.Cawetan,
            crypticHintText: CrypticHint,
            forceFieldButtonLabel: forceFieldLabel,
            superJumpButtonLabel: superJumpLabel,
            forceFieldNeedHint: forceFieldNeedHint,
            superJumpNeedHint: superJumpNeedHint,
            statusMessage);
        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);
        DutzCartoonDialogGui.DrawFrame(frame);

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);

        if (DutzCartoonDialogGui.DismissButton())
            CloseDialog();

        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label(GetShopTitle(activeShop), DutzCartoonDialogGui.ShopHeaderStyle());
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));

        DrawShopOffers(expandedShop);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
            GUILayout.Label(statusMessage, DutzCartoonDialogGui.BodyStyle());
        }

        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        if (DutzCartoonDialogGui.DismissButton())
            CloseDialog();

        GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

    void DrawShopOffers(bool expandedShop)
    {
        switch (activeShop)
        {
            case ShopKind.Cawetan:
                DrawCawetanShop();
                break;
            case ShopKind.SenGongBong:
                DrawSenGongBongShop();
                break;
            default:
                DrawPrincessZaraShop();
                break;
        }
    }

    void DrawPrincessZaraShop()
    {
        DrawForceFieldOffer(includeCrypticHint: true);
    }

    void DrawSenGongBongShop()
    {
        DrawForceFieldOffer(includeCrypticHint: true);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        DrawSuperJumpOffer();
    }

    void DrawForceFieldOffer(bool includeCrypticHint)
    {
        GUILayout.Label("Force Field for 60 seconds:", DutzCartoonDialogGui.BodyStyle());
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 14f));

        var forceFieldLabel = $"FORCE FIELD — {ForceFieldCost} {DutzCollectibleProgress.SpendNounPlural}";
        var canBuyForceField = CanOfferForceField();
        GUI.enabled = canBuyForceField;
        if (DutzCartoonDialogGui.ActionButton(forceFieldLabel, heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(forceFieldLabel)))
            TryPurchaseForceField();
        GUI.enabled = true;

        if (!canBuyForceField)
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
            if (DutzForceField.IsPlayerShielded(player))
                GUILayout.Label("Force Field already active.", DutzCartoonDialogGui.HintStyle());
            else if (DutzCollectibleProgress.CollectedCount < ForceFieldCost)
            {
                GUILayout.Label(
                    $"Need {ForceFieldCost} {DutzCollectibleProgress.SpendNounPlural} " +
                    $"(you have {DutzCollectibleProgress.CollectedCount}).",
                    DutzCartoonDialogGui.HintStyle());
            }
        }

        if (includeCrypticHint)
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 16f));
            var hintStyle = DutzCartoonDialogGui.HintStyle();
            hintStyle.fontStyle = FontStyle.Italic;
            GUILayout.Label(CrypticHint, hintStyle);
        }
    }

    void DrawSuperJumpOffer()
    {
        GUILayout.Label("Super Jump for this run:", DutzCartoonDialogGui.BodyStyle());
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 14f));

        var superJumpLabel = $"SUPER JUMP — {GetSuperJumpCost()} {DutzCollectibleProgress.SpendNounPlural}";
        var canBuySuperJump = CanOfferSuperJump();
        GUI.enabled = canBuySuperJump;
        if (DutzCartoonDialogGui.ActionButton(superJumpLabel, heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(superJumpLabel)))
            TryPurchaseSuperJump();
        GUI.enabled = true;

        if (!canBuySuperJump)
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
            if (player.HasSuperJumpActive)
                GUILayout.Label("Super Jump already active.", DutzCartoonDialogGui.HintStyle());
            else if (cawetanPurchasedThisLife)
                GUILayout.Label("Super Jump already purchased.", DutzCartoonDialogGui.HintStyle());
            else if (DutzCollectibleProgress.CollectedCount < GetSuperJumpCost())
            {
                GUILayout.Label(
                    $"Need {GetSuperJumpCost()} {DutzCollectibleProgress.SpendNounPlural} " +
                    $"(you have {DutzCollectibleProgress.CollectedCount}).",
                    DutzCartoonDialogGui.HintStyle());
            }
        }
    }

    void DrawCawetanShop()
    {
        DrawSuperJumpOffer();
    }
}

/// <summary>
/// Senior Citizen Mode must watch a rewarded ad before advancing to the next level after a win.
/// </summary>
public static class DutzSeniorCitizenNextLevelGate
{
    public static bool RequiresRewardedAdForNextLevel()
    {
        if (DutzMobileRuntime.IsFloodControlScene)
            return FloodDifficulty.IsSeniorCitizenMode();

        var sceneName = SceneManager.GetActiveScene().name;
        if (DutzMobileRuntime.IsDutzLevelScene(sceneName))
            return DutzDifficulty.IsSeniorCitizenMode();

        return false;
    }

    public static void ProceedToNextLevel(System.Action proceed)
    {
        if (proceed == null)
            return;

        if (!RequiresRewardedAdForNextLevel())
        {
            proceed();
            return;
        }

        FloodRewardedAdStub.Show(proceed);
    }

    public static void DrawOverlayIfShowing()
    {
        if (!FloodRewardedAdStub.IsShowing)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = -15000;
        DutzCartoonDialogGui.DrawDimOverlay(0.65f);

        var style = new GUIStyle(GUI.skin.label)
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
}

