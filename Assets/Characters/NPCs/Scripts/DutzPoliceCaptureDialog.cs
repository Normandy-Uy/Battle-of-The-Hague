using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Police capture game-over dialog with portrait and respawn / restart / exit choices.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(2500)]
public class DutzPoliceCaptureDialog : MonoBehaviour
{
    public const string CaptureMessage = "YOU WERE CAPTURED BY POLICE!";
    const string CaptureDetailMessage = "Your hands are cuffed behind your back.";
    const string ManagerName = "DutzPoliceCaptureDialog";
    const string PortraitResource = "DutzPoliceCapturePortrait";
    const float ConfirmDelay = 0.75f;
    const int OverlayGuiDepth = -30000;

    static readonly string[] ChoiceLabels = { "RESPAWN", "RESTART LEVEL", "EXIT THE GAME" };

    static DutzPoliceCaptureDialog instance;
    static Texture2D portrait;
    static Texture2D dimOverlay;

    DutzPlayerController player;
    DutzFallRespawn fallRespawn;
    bool showing;
    float shownAt;

    public static bool IsShowing => instance != null && instance.showing;

    public static void EnsureFromBoot()
    {
        if (instance != null)
            return;

        var go = new GameObject(ManagerName);
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzPoliceCaptureDialog>();
    }

    public static bool TryCapture(DutzPlayerController target)
    {
        if (target == null)
            return false;

        if (DutzLevelObjective.IsLevelFinishedForActiveScene)
            return false;

        var respawn = target.GetComponent<DutzFallRespawn>();
        if (respawn != null && respawn.IsSpawnGraceActive)
            return false;

        if (IsShowing)
            return false;

        EnsureFromBoot();

        bool replacingFallDialog = respawn != null && respawn.IsShowingRespawnDialog;
        if (replacingFallDialog)
            respawn.CancelForPoliceCapture();

        instance.Show(target, respawn, consumeLife: !replacingFallDialog);
        return true;
    }

    public static void ResetForSceneLoad()
    {
        if (instance == null)
            return;

        instance.showing = false;
        instance.player = null;
        instance.fallRespawn = null;
    }

    void Show(DutzPlayerController target, DutzFallRespawn respawn, bool consumeLife)
    {
        player = target;
        fallRespawn = respawn;
        if (consumeLife)
            DutzPlayerLives.ConsumeOne();
        showing = true;
        shownAt = Time.unscaledTime;
        player.SetControlsLocked(true);
        fallRespawn?.CaptureDeathPoseFromPlayer();
        fallRespawn?.SetCapturePose(true);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        PlayCaptureSting();
    }

    void OnGUI()
    {
        if (!showing)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = OverlayGuiDepth;

        DrawFullscreenDim();
        SwallowGameplayKeys();

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle(new Color(1f, 0.22f, 0.18f));
        var messageStyle = DutzCartoonDialogGui.BodyStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();

        var portraitTex = GetPortrait();
        var height = DutzCartoonDialogGui.PoliceDialogHeight(
            CaptureMessage,
            CaptureDetailMessage,
            portraitTex != null,
            ChoiceLabels);
        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);
        DutzCartoonDialogGui.DrawFrame(frame);

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(CaptureMessage, titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));

        if (portraitTex != null)
        {
            var imageMaxHeight = DutzCartoonDialogGui.IsCompactLayout
                ? DutzCartoonDialogGui.Scale(100f, 140f)
                : DutzCartoonDialogGui.Scale(280f, 360f);
            var aspect = portraitTex.width / Mathf.Max(1f, portraitTex.height);
            var imageHeight = imageMaxHeight;
            var imageWidth = imageHeight * aspect;
            var imageRect = GUILayoutUtility.GetRect(imageWidth, imageHeight, GUILayout.ExpandWidth(false));
            imageRect.x = (DutzCartoonDialogGui.PanelWidth - DutzCartoonDialogGui.ContentInset * 2f - imageWidth) * 0.5f
                + DutzCartoonDialogGui.ContentInset;
            GUI.DrawTexture(imageRect, portraitTex, ScaleMode.ScaleToFit, true);
            GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        }

        GUILayout.Label(CaptureDetailMessage, messageStyle);
        if (DutzPlayerLives.CanRespawn)
            GUILayout.Label($"Lives left: {DutzPlayerLives.Current}", hintStyle);
        else
            GUILayout.Label("No lives left. Restart requires watching an ad.", hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 14f));

        if (DutzPlayerLives.CanRespawn)
        {
            if (DutzCartoonDialogGui.ActionButton(ChoiceLabels[0], heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(ChoiceLabels[0])))
                Respawn();
            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        }
        else
        {
            if (DutzCartoonDialogGui.DangerButton(ChoiceLabels[1], heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(ChoiceLabels[1])))
                RequestRestartWithRewardedAd();
            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        }

        if (DutzCartoonDialogGui.DangerButton(ChoiceLabels[2], heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(ChoiceLabels[2])))
            ExitGame();

        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));
        GUILayout.Label(CanConfirm() ? "Choose an option above." : "…", hintStyle);
        GUILayout.EndArea();

        GUI.depth = previousDepth;
    }

    static void DrawFullscreenDim()
    {
        EnsureDimOverlay();
        var previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), dimOverlay);
        GUI.color = previousColor;
    }

    static void EnsureDimOverlay()
    {
        if (dimOverlay != null)
            return;

        dimOverlay = Texture2D.whiteTexture;
    }

    static Texture2D GetPortrait()
    {
        if (portrait != null)
            return portrait;

        portrait = Resources.Load<Texture2D>(PortraitResource);
        return portrait;
    }

    bool CanConfirm() => Time.unscaledTime - shownAt >= ConfirmDelay;

    void SwallowGameplayKeys()
    {
        if (Event.current.type != EventType.KeyDown)
            return;

        var key = Event.current.keyCode;
        if (key == KeyCode.Space || key == KeyCode.LeftShift || key == KeyCode.RightShift ||
            key == KeyCode.UpArrow || key == KeyCode.DownArrow || key == KeyCode.LeftArrow ||
            key == KeyCode.RightArrow)
        {
            Event.current.Use();
        }
    }

    void Respawn()
    {
        if (!CanConfirm() || !DutzPlayerLives.CanRespawn)
            return;

        showing = false;
        fallRespawn?.SetCapturePose(false);
        if (fallRespawn != null)
            fallRespawn.PerformRespawnFromDialog();
        else if (player != null)
            player.Respawn();
    }

    bool restartAdPending;

    void RequestRestartWithRewardedAd()
    {
        if (!CanConfirm() || DutzPlayerLives.CanRespawn || restartAdPending)
            return;

        restartAdPending = true;
        showing = false;
        fallRespawn?.SetCapturePose(false);
        FloodRewardedAdStub.Show(
            onRewarded: PerformRestartLevel,
            onDismissedOrFailed: () =>
            {
                restartAdPending = false;
                showing = true;
                shownAt = Time.unscaledTime;
                fallRespawn?.SetCapturePose(true);
            });
    }

    void PerformRestartLevel()
    {
        restartAdPending = false;
        showing = false;
        Time.timeScale = 1f;
        DutzPlayerLives.ResetToFull();
        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ExitGame()
    {
        if (!CanConfirm())
            return;

        showing = false;
        fallRespawn?.SetCapturePose(false);
        Time.timeScale = 1f;
        DutzLevelObjective.ExitGameFromDialog();
#if UNITY_EDITOR
        if (DutzLevelObjective.Instance == null)
            UnityEditor.EditorApplication.isPlaying = false;
#else
        if (DutzLevelObjective.Instance == null)
            Application.Quit();
#endif
    }

    static void PlayCaptureSting()
    {
        var clip = Resources.Load<AudioClip>("DutzPoliceCaptureSting");
        if (clip == null)
            return;

        var go = new GameObject("DutzPoliceCaptureSting");
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.85f;
        source.PlayOneShot(clip);
        Destroy(go, clip.length + 0.1f);
    }
}
