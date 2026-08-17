using System.IO;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays the Flood Control opening video and enables gameplay when it ends.
/// After the first play in a session, Restart/reload skips the video but still
/// shows the welcome splash before gameplay.
/// </summary>
[DisallowMultipleComponent]
public class IntroSequenceController : MonoBehaviour
{
    [Header("Opening Video")]
    [SerializeField] string videoFileName = "FLOOD CONTROL OPENING.mp4";
    [SerializeField] GameManager gameManager;
    [SerializeField] bool startIntroOnEnable = true;
    [SerializeField] float prepareTimeoutSeconds = 60f;

    [Header("Welcome")]
    [SerializeField] string welcomeMessage =
        "WELCOME TO BATTLE OF THE HAGUE - FLOOD CONTROL";
    [SerializeField] float welcomeDurationSeconds = 1.5f;

    VideoPlayer videoPlayer;
    AudioSource videoAudioSource;
    RenderTexture renderTexture;
    bool introFinished;
    bool videoCompleted;
    bool videoFailed;
    bool waitingForPreparation;
    bool welcomeActive;
    float prepareStartedAt;
    float welcomeEndsAt;

    /// <summary>
    /// Session flag so Restart reloads skip the opening video (welcome still plays).
    /// </summary>
    static bool hasPlayedOpeningThisSession;

    public bool IsIntroFinished => introFinished;

    static IntroSequenceController activeInstance;

    public static bool IsBlockingMobileInput =>
        activeInstance != null && activeInstance.isActiveAndEnabled && !activeInstance.introFinished;

    void Awake()
    {
        activeInstance = this;
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        FloodDifficultySelect.ResetForSceneLoad();
        FloodDifficultySelect.Ensure();
    }

    void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
    }

    void OnEnable()
    {
        if (hasPlayedOpeningThisSession)
        {
            TryBeginAfterGates(BeginRestartWelcomeOnly);
            return;
        }

        if (!startIntroOnEnable)
            return;

        TryBeginAfterGates(BeginIntro);
    }

    void TryBeginAfterGates(System.Action beginAction)
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        gameManager?.SetGameplayEnabled(false);
        FloodDifficultySelect.Ensure();

        if (DutzVictorySelfieProfile.IsRegistrationBlocking)
        {
            DutzVictorySelfieSetupHud.EnsureForFloodPlay();
            return;
        }

        if (FloodDifficultySelect.IsBlockingStart)
            return;

        beginAction?.Invoke();
    }

    /// <summary>Called after Flood registration START/SKIP or mode selection.</summary>
    public static void TryContinueAfterModeSelection()
    {
        if (activeInstance == null || !activeInstance.isActiveAndEnabled)
            return;

        if (FloodDifficultySelect.IsBlockingStart || DutzVictorySelfieProfile.IsRegistrationBlocking)
            return;

        if (hasPlayedOpeningThisSession)
            activeInstance.BeginRestartWelcomeOnly();
        else
            activeInstance.BeginIntro();
    }

    /// <summary>Called after Flood registration START/SKIP.</summary>
    public void BeginIntroAfterRegistration()
    {
        if (hasPlayedOpeningThisSession || introFinished || welcomeActive)
            return;

        TryContinueAfterModeSelection();
    }

    void BeginRestartWelcomeOnly()
    {
        CleanupVideo();
        introFinished = false;
        videoCompleted = false;
        videoFailed = false;
        waitingForPreparation = false;
        welcomeActive = false;

        if (gameManager != null)
            gameManager.SetGameplayEnabled(false);

        FloodVideoPlayback.PauseSceneMusic();

        if (welcomeDurationSeconds > 0f)
        {
            welcomeActive = true;
            welcomeEndsAt = Time.realtimeSinceStartup + welcomeDurationSeconds;
            return;
        }

        CompleteIntro();
    }

    void OnDisable()
    {
        welcomeActive = false;
        CleanupVideo();
    }

    public void BeginIntro()
    {
        introFinished = false;
        videoCompleted = false;
        videoFailed = false;
        waitingForPreparation = false;
        welcomeActive = false;

        if (gameManager != null)
            gameManager.SetGameplayEnabled(false);

        FloodVideoPlayback.PauseSceneMusic();
        CleanupVideo();
        StartOpeningVideo();
    }

    /// <summary>Safe external completion hook for Timeline or future animation events.</summary>
    public void NotifyIntroFinished()
    {
        if (introFinished || welcomeActive)
            return;

        hasPlayedOpeningThisSession = true;
        CleanupVideo();

        if (welcomeDurationSeconds > 0f)
        {
            welcomeActive = true;
            welcomeEndsAt = Time.realtimeSinceStartup + welcomeDurationSeconds;
            return;
        }

        CompleteIntro();
    }

    void CompleteIntro()
    {
        hasPlayedOpeningThisSession = true;
        introFinished = true;
        welcomeActive = false;

        FloodVideoPlayback.ResumeSceneMusic();
        if (FloodDifficulty.IsSeniorCitizenMode())
        {
            var health = FindObjectOfType<FloodPlayerHealth>();
            health?.ActivatePermanentShield();
        }

        if (gameManager != null)
            gameManager.SetGameplayEnabled(true);
    }

    void StartOpeningVideo()
    {
        string videoUrl = BuildVideoUrl(videoFileName);
#if !UNITY_ANDROID || UNITY_EDITOR
        string localPath = Path.Combine(Application.streamingAssetsPath, videoFileName);
        if (!File.Exists(localPath) || new FileInfo(localPath).Length <= 0)
        {
            Debug.LogError(
                "[FloodControl] Opening video is missing or empty: " + localPath);
            NotifyIntroFinished();
            return;
        }
#endif

        renderTexture = FloodVideoPlayback.CreateTarget(
            "FloodControlOpeningVideo",
            Application.isMobilePlatform);

        videoAudioSource = GetComponent<AudioSource>();
        if (videoAudioSource == null)
            videoAudioSource = gameObject.AddComponent<AudioSource>();

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        FloodVideoPlayback.ConfigurePlayer(
            videoPlayer,
            videoAudioSource,
            videoUrl,
            renderTexture);
        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.loopPointReached += HandleVideoCompleted;
        videoPlayer.errorReceived += HandleVideoError;
        waitingForPreparation = true;
        prepareStartedAt = Time.realtimeSinceStartup;
        videoPlayer.Prepare();
    }

    void Update()
    {
        if (welcomeActive)
        {
            if (Time.realtimeSinceStartup >= welcomeEndsAt)
                CompleteIntro();
            return;
        }

        if (!waitingForPreparation || videoFailed || introFinished)
            return;

        if (Time.realtimeSinceStartup - prepareStartedAt >= prepareTimeoutSeconds)
        {
            videoFailed = true;
            waitingForPreparation = false;
            Debug.LogError("[FloodControl] Opening video preparation timed out.");
            NotifyIntroFinished();
        }
    }

    void HandleVideoPrepared(VideoPlayer preparedPlayer)
    {
        if (preparedPlayer != videoPlayer || videoFailed || introFinished)
            return;

        waitingForPreparation = false;
        FloodVideoPlayback.BindAudioAfterPrepare(preparedPlayer, videoAudioSource);
        preparedPlayer.Play();
    }

    void HandleVideoCompleted(VideoPlayer completedPlayer)
    {
        if (completedPlayer != videoPlayer || introFinished)
            return;

        videoCompleted = true;
        NotifyIntroFinished();
    }

    void HandleVideoError(VideoPlayer _, string message)
    {
        videoFailed = true;
        waitingForPreparation = false;
        Debug.LogError("[FloodControl] Opening video failed: " + message);
        NotifyIntroFinished();
    }

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
        if (introFinished)
            return;

        int previousDepth = GUI.depth;
        if (welcomeActive)
        {
            GUI.depth = -20000;
            DutzCartoonDialogGui.DrawLargeWelcomeSplash(
                welcomeMessage,
                DutzAnnouncementHud.DefaultFlashColor);
            GUI.depth = previousDepth;
            return;
        }

        if (renderTexture == null)
            return;

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
    }

    void OnValidate()
    {
        prepareTimeoutSeconds = Mathf.Max(1f, prepareTimeoutSeconds);
        welcomeDurationSeconds = Mathf.Max(0f, welcomeDurationSeconds);
    }
}
