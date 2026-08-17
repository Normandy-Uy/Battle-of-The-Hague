using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Finishes LEVEL FLOOD CONTROL only when Player1 meets both position gates,
/// then plays the completion video before the victory dialog.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodVictoryGoal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] string edsaSceneName = DutzMobileRuntime.Level00SceneName;

    [Header("Completion Video")]
    [SerializeField] string completionVideoFileName = "Level_Flood_Complete.mp4";
    [SerializeField] float prepareTimeoutSeconds = 60f;

    [Header("Dialog")]
    [SerializeField] string victoryTitle = "FLOOD CONTROL COMPLETE!";
    [SerializeField] string victoryHint = "You reached the freeway. Where do you want to go?";
    [SerializeField] string goToEdsaLabel = "GO TO LEVEL 2 — EDSA";
    [SerializeField] string repeatLabel = "REPEAT LEVEL";
    [SerializeField] string exitLabel = "EXIT GAME";

    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] PlayerController player;

    [Header("Completion Gates")]
    [Tooltip("Victory requires player X greater than this value.")]
    [SerializeField] float completionX = 483f;
    [Tooltip("Victory requires player Y greater than this value.")]
    [SerializeField] float completionY = 26f;
    [SerializeField] bool requirePositionGatesForTriggers = true;

    VideoPlayer videoPlayer;
    AudioSource videoAudioSource;
    RenderTexture renderTexture;
    bool victoryStarted;
    bool playingVictoryVideo;
    bool showingVictory;
    bool pendingSceneTransition;
    float victoryDialogShownAt;
    const float VictoryDialogConfirmDelay = 0.45f;
    bool waitingForPreparation;
    bool videoFailed;
    float prepareStartedAt;
    string statusMessage;

    public bool IsShowingVictory => showingVictory;

    static FloodVictoryGoal activeInstance;

    public static bool IsBlockingMobileInput =>
        activeInstance != null
        && (activeInstance.victoryStarted
            || activeInstance.playingVictoryVideo
            || activeInstance.showingVictory);

    void Awake()
    {
        activeInstance = this;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        EnsurePlayerCanReachGates();
    }

    void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
        CleanupVideo();
    }

    void OnDisable()
    {
        CleanupVideo();
    }

    void OnCollisionEnter(Collision collision) =>
        TryComplete(collision != null ? collision.collider : null);

    void OnCollisionStay(Collision collision) =>
        TryComplete(collision != null ? collision.collider : null);

    void OnTriggerEnter(Collider other) => TryComplete(other);
    void OnTriggerStay(Collider other) => TryComplete(other);

    void Update()
    {
        if (!victoryStarted
            && !showingVictory
            && player != null
            && (gameManager == null || gameManager.IsGameplayEnabled)
            && MeetsCompletionGates(player.transform.position))
        {
            FloodPlayerHealth health = player.GetComponent<FloodPlayerHealth>();
            if (health != null && !health.IsDead)
                BeginVictorySequence(health);
        }

        if (!waitingForPreparation || videoFailed || !playingVictoryVideo)
            return;

        if (Time.realtimeSinceStartup - prepareStartedAt >= prepareTimeoutSeconds)
        {
            videoFailed = true;
            waitingForPreparation = false;
            Debug.LogError("[FloodControl] Completion video preparation timed out.");
            ShowVictoryDialog();
        }
    }

    public void Configure(
        GameManager manager,
        PlayerController playerController,
        float requiredX,
        float requiredY)
    {
        gameManager = manager;
        player = playerController;
        completionX = requiredX;
        completionY = requiredY;
        EnsurePlayerCanReachGates();
    }

    void EnsurePlayerCanReachGates()
    {
        if (player == null)
            return;

        BoundaryLimiter limiter = player.GetComponent<BoundaryLimiter>();
        if (limiter == null)
            return;

        // Keep Max X just past the finish gate so victory is reachable,
        // but the player cannot swim far beyond the level end.
        limiter.SetMaxX(completionX + 1f);
        limiter.ExtendMaxY(completionY + 1f);
    }

    bool MeetsCompletionGates(Vector3 position) =>
        position.x > completionX && position.y > completionY;

    void TryComplete(Collider other)
    {
        if (victoryStarted || showingVictory || other == null)
            return;

        FloodPlayerHealth health = other.GetComponentInParent<FloodPlayerHealth>();
        if (health == null || health.IsDead)
            return;

        if (requirePositionGatesForTriggers
            && !MeetsCompletionGates(health.transform.position))
        {
            return;
        }

        BeginVictorySequence(health);
    }

    void BeginVictorySequence(FloodPlayerHealth health)
    {
        if (victoryStarted || health == null || health.IsDead)
            return;

        victoryStarted = true;
        statusMessage = string.Empty;

        if (gameManager != null)
            gameManager.SetGameplayEnabled(false);

        PlayerController controller = health.GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        Rigidbody body = health.GetComponent<Rigidbody>();
        if (body != null)
            body.velocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        FloodVideoPlayback.PauseSceneMusic();
        StartCompletionVideo();
    }

    void StartCompletionVideo()
    {
        CleanupVideo();
        playingVictoryVideo = true;
        videoFailed = false;
        waitingForPreparation = false;

#if !UNITY_ANDROID || UNITY_EDITOR
        string localPath = Path.Combine(Application.streamingAssetsPath, completionVideoFileName);
        if (!File.Exists(localPath) || new FileInfo(localPath).Length <= 0)
        {
            Debug.LogError(
                "[FloodControl] Completion video is missing or empty: " + localPath);
            ShowVictoryDialog();
            return;
        }
#endif

        renderTexture = FloodVideoPlayback.CreateTarget(
            "FloodControlCompletionVideo",
            Application.isMobilePlatform);

        videoAudioSource = GetComponent<AudioSource>();
        if (videoAudioSource == null)
            videoAudioSource = gameObject.AddComponent<AudioSource>();

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        FloodVideoPlayback.ConfigurePlayer(
            videoPlayer,
            videoAudioSource,
            BuildVideoUrl(completionVideoFileName),
            renderTexture);
        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.loopPointReached += HandleVideoCompleted;
        videoPlayer.errorReceived += HandleVideoError;
        waitingForPreparation = true;
        prepareStartedAt = Time.realtimeSinceStartup;
        videoPlayer.Prepare();
    }

    void HandleVideoPrepared(VideoPlayer preparedPlayer)
    {
        if (preparedPlayer != videoPlayer || videoFailed || showingVictory)
            return;

        waitingForPreparation = false;
        FloodVideoPlayback.BindAudioAfterPrepare(preparedPlayer, videoAudioSource);
        preparedPlayer.Play();
    }

    void HandleVideoCompleted(VideoPlayer completedPlayer)
    {
        if (completedPlayer != videoPlayer || showingVictory)
            return;

        ShowVictoryDialog();
    }

    void HandleVideoError(VideoPlayer _, string message)
    {
        videoFailed = true;
        waitingForPreparation = false;
        Debug.LogError("[FloodControl] Completion video failed: " + message);
        ShowVictoryDialog();
    }

    void ShowVictoryDialog()
    {
        CleanupVideo();
        playingVictoryVideo = false;
        showingVictory = true;
        pendingSceneTransition = false;
        victoryDialogShownAt = Time.unscaledTime;
    }

    bool CanConfirmVictoryChoice() =>
        !pendingSceneTransition
        && Time.unscaledTime - victoryDialogShownAt >= VictoryDialogConfirmDelay;

    static string BuildVideoUrl(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
#if UNITY_ANDROID && !UNITY_EDITOR
        return path;
#else
        string normalized = Path.GetFullPath(path).Replace('\\', '/');
        return "file:///" + normalized;
#endif
    }

    void CleanupVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandleVideoPrepared;
            videoPlayer.loopPointReached -= HandleVideoCompleted;
            videoPlayer.errorReceived -= HandleVideoError;
            videoPlayer.Stop();
            Destroy(videoPlayer);
            videoPlayer = null;
        }

        if (videoAudioSource != null)
            videoAudioSource.Stop();

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        waitingForPreparation = false;
    }

    void OnGUI()
    {
        if (playingVictoryVideo && renderTexture != null)
        {
            int previousDepth = GUI.depth;
            GUI.depth = -20000;
            GUI.color = Color.black;
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                renderTexture,
                ScaleMode.ScaleAndCrop);
            GUI.depth = previousDepth;
            return;
        }

        if (!showingVictory)
            return;

        DutzSeniorCitizenNextLevelGate.DrawOverlayIfShowing();
        if (FloodRewardedAdStub.IsShowing || pendingSceneTransition)
            return;

        int previousDialogDepth = GUI.depth;
        GUI.depth = -1200;
        DutzCartoonDialogGui.DrawDimOverlay(0.58f);

        string[] labels = { goToEdsaLabel, repeatLabel, exitLabel };
        var requiredHeight = DutzCartoonDialogGui.MeasureStackedPanelHeight(victoryTitle, victoryHint, labels);
        float height = DutzCartoonDialogGui.ChoiceDialogHeight(victoryTitle, victoryHint, labels);
        Rect frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);

        GUI.depth = -900;
        DutzCartoonDialogGui.DrawFrame(frame);

        GUIStyle titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        GUIStyle hintStyle = DutzCartoonDialogGui.HintStyle();

        var scrolling = DutzCartoonDialogGui.BeginPanelContent(frame, requiredHeight);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(victoryTitle, titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label(victoryHint, hintStyle);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
            GUILayout.Label(statusMessage, hintStyle);
        }

        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        GUI.enabled = CanConfirmVictoryChoice();
        if (DutzCartoonDialogGui.ActionButton(
                goToEdsaLabel,
                DutzCartoonDialogGui.PlasticButtonColor.Blue,
                DutzCartoonDialogGui.MeasureActionButtonHeight(goToEdsaLabel)))
        {
            GoToEdsa();
        }

        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        if (DutzCartoonDialogGui.ActionButton(
                repeatLabel,
                DutzCartoonDialogGui.PlasticButtonColor.Red,
                DutzCartoonDialogGui.MeasureActionButtonHeight(repeatLabel)))
        {
            RepeatLevel();
        }

        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        if (DutzCartoonDialogGui.DangerButton(
                exitLabel,
                DutzCartoonDialogGui.MeasureActionButtonHeight(exitLabel)))
        {
            ExitGame();
        }

        GUI.enabled = true;

        DutzCartoonDialogGui.EndPanelContent(scrolling);
        GUI.depth = previousDialogDepth;
    }

    void GoToEdsa()
    {
        if (!CanConfirmVictoryChoice())
            return;

        pendingSceneTransition = true;
        showingVictory = false;

        if (DutzSeniorCitizenNextLevelGate.RequiresRewardedAdForNextLevel())
        {
            FloodRewardedAdStub.Show(
                onRewarded: DoGoToEdsa,
                onDismissedOrFailed: RestoreVictoryDialog);
            return;
        }

        DoGoToEdsa();
    }

    void RestoreVictoryDialog()
    {
        pendingSceneTransition = false;
        showingVictory = true;
    }

    void DoGoToEdsa()
    {
        if (string.IsNullOrWhiteSpace(edsaSceneName)
            || !Application.CanStreamedLevelBeLoaded(edsaSceneName))
        {
            RestoreVictoryDialog();
            statusMessage = "EDSA is not available in the build yet.";
            return;
        }

        DutzSceneLoadRunner.LoadDutzLevel(edsaSceneName);
    }

    void RepeatLevel()
    {
        DutzGameBootstrap.PrepareForSceneLoad();
        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
            SceneManager.LoadScene(active.buildIndex);
        else
            SceneManager.LoadScene(active.name);
    }

    void ExitGame()
    {
        showingVictory = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
