using System.Collections;
using UnityEngine;

/// <summary>
/// Optional player registration — name + photo before Flood Control opening video.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(2100)]
public class DutzVictorySelfieSetupHud : MonoBehaviour
{
    const string ManagerName = "DutzVictorySelfieSetupHud";
    const string StartLabel = "START GAME";
    const string SettingsLabel = "SETTINGS";
    const string SkipLabel = "SKIP — PLAY WITHOUT REGISTRATION";
    const string BackLabel = "BACK";

    static DutzVictorySelfieSetupHud instance;

    string nameDraft = string.Empty;
    Texture2D pendingPhoto;
    Texture2D previewPhoto;
    string photoStatus = "No photo yet (optional)";
    Vector2 bodyScroll;
    bool showingSettings;
    float musicVolumeDraft;
    float sfxVolumeDraft;

    public static bool IsBlockingStart => instance != null && DutzVictorySelfieProfile.IsRegistrationBlocking;

    public static void EnsureFromBoot()
    {
        if (!DutzMobileRuntime.IsFloodControlScene)
            return;

        if (!DutzVictorySelfieProfile.IsRegistrationBlocking)
            return;

        EnsureForFloodPlay();
    }

    /// <summary>After picking Level 0 from jump menu — EDSA welcome then unlock.</summary>
    public static void EnsureForLevel00Play()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        DutzLevel00WelcomeSplash.Trigger();
    }

    /// <summary>Registration dialog on Flood Control before the opening video.</summary>
    public static void EnsureForFloodPlay()
    {
        if (!DutzMobileRuntime.IsFloodControlScene)
            return;

        if (!DutzVictorySelfieProfile.IsRegistrationBlocking)
            return;

        if (FindObjectOfType<DutzVictorySelfieSetupHud>() != null)
            return;

        DutzVictorySelfieProfile.LoadSaved();
        DutzVictorySelfieProfile.ResetLevel00Gate();

        var go = new GameObject(ManagerName);
        instance = go.AddComponent<DutzVictorySelfieSetupHud>();
    }

    void Awake()
    {
        instance = this;
        nameDraft = DutzVictorySelfieProfile.DisplayName;
        previewPhoto = DutzVictorySelfieProfile.GetPhotoTexture();
        musicVolumeDraft = DutzAudioSettings.MusicVolume;
        sfxVolumeDraft = DutzAudioSettings.SfxVolume;
        if (previewPhoto != null)
            photoStatus = "Saved victory selfie loaded";
    }

    void Start()
    {
        LockPlayer(true);
        var floodGame = FindObjectOfType<GameManager>();
        if (floodGame != null)
            floodGame.SetGameplayEnabled(false);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void LockPlayer(bool locked)
    {
        var player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
        player?.SetControlsLocked(locked);
    }

    void OnGUI()
    {
        if (!DutzVictorySelfieProfile.IsRegistrationBlocking)
            return;

        if (DutzVictorySelfieCaptureHud.IsActive)
            return;

        if (DutzBootOverlay.State == DutzBootOverlay.OverlayState.Loading
            || DutzBootOverlay.State == DutzBootOverlay.OverlayState.Failed)
            return;

        if (showingSettings)
        {
            DrawSettingsDialog();
            return;
        }

        var photo = pendingPhoto != null ? pendingPhoto : previewPhoto;
        var hasPhoto = photo != null;
#if UNITY_EDITOR
        var includeEditorPick = true;
#else
        var includeEditorPick = false;
#endif

        var spacing = DutzCartoonDialogGui.Scale(6f, 10f);
        var footerLabels = new[] { StartLabel, SettingsLabel, SkipLabel };
        var footerHeight = DutzCartoonDialogGui.PanelPadding;
        for (var i = 0; i < footerLabels.Length; i++)
        {
            if (i > 0)
                footerHeight += spacing;
            footerHeight += DutzCartoonDialogGui.MeasureActionButtonHeight(footerLabels[i]);
        }

        var frameHeight = DutzCartoonDialogGui.RegistrationSetupDialogHeight(
            "PLAYER REGISTRATION (OPTIONAL)",
            "Optional name and photo for Level 5 — your photo goes in the DUTZ IS FREE share image.",
            photoStatus,
            hasPhoto,
            includeEditorPick,
            footerLabels);

        var previousDepth = GUI.depth;
        GUI.depth = -2900;
        DutzCartoonDialogGui.DrawDimOverlay();

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

        GUILayout.BeginArea(bodyRect);
        bodyScroll = GUILayout.BeginScrollView(bodyScroll);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        titleStyle.fontSize = DutzCartoonDialogGui.ScaleFont(26, 34);
        GUILayout.Label("PLAYER REGISTRATION (OPTIONAL)", titleStyle);
        GUILayout.Space(6f);

        var hintStyle = DutzCartoonDialogGui.HintStyle();
        GUILayout.Label(
            "Optional name and photo for Level 5 — your photo goes in the DUTZ IS FREE share image.",
            hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 14f));

        var fieldStyle = DutzCartoonDialogGui.TextFieldStyle();
        GUILayout.Label("Your name (optional)", hintStyle);
        nameDraft = GUILayout.TextField(
            nameDraft ?? string.Empty,
            32,
            fieldStyle,
            GUILayout.Height(DutzCartoonDialogGui.Scale(36f, 52f)));
        GUILayout.Space(8f);

        var thumbSize = DutzCartoonDialogGui.IsCompactLayout
            ? DutzCartoonDialogGui.Scale(72f, 96f)
            : DutzCartoonDialogGui.Scale(96f, 140f);
        var thumbRect = GUILayoutUtility.GetRect(thumbSize, thumbSize, GUILayout.ExpandWidth(false));
        GUI.color = new Color(0.2f, 0.22f, 0.3f, 1f);
        GUI.DrawTexture(thumbRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (hasPhoto)
            GUI.DrawTexture(thumbRect, photo, ScaleMode.ScaleToFit);

        GUILayout.Space(4f);
        GUILayout.Label(photoStatus, hintStyle);
        GUILayout.Space(8f);

        if (DutzCartoonDialogGui.ActionButton("ADD YOUR PHOTO"))
            DutzVictorySelfieCaptureHud.BeginCapture(OnPhotoCaptured);

#if UNITY_EDITOR
        GUILayout.Space(6f);
        if (DutzCartoonDialogGui.ActionButton("PICK PHOTO (EDITOR)"))
        {
            if (DutzVictorySelfieCaptureHud.TryPickPhotoFromDisk(out var picked))
                OnPhotoCaptured(picked);
        }
#endif

        if (hasPhoto)
        {
            GUILayout.Space(6f);
            if (DutzCartoonDialogGui.DismissButton("CLEAR PHOTO"))
            {
                if (pendingPhoto != null)
                {
                    Destroy(pendingPhoto);
                    pendingPhoto = null;
                }

                previewPhoto = null;
                DutzVictorySelfieProfile.DeletePhoto();
                photoStatus = "Photo cleared";
            }
        }

        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 16f));
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUILayout.BeginArea(footerRect);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding * 0.5f);
        if (DutzCartoonDialogGui.ActionButton(
                StartLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(StartLabel)))
            StartGame();

        GUILayout.Space(spacing);
        if (DutzCartoonDialogGui.ActionButton(
                SettingsLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(SettingsLabel)))
            OpenSettings();

        GUILayout.Space(spacing);
        if (DutzCartoonDialogGui.DismissButton(
                SkipLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(SkipLabel)))
            SkipRegistration();

        GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

    void OpenSettings()
    {
        musicVolumeDraft = DutzAudioSettings.MusicVolume;
        sfxVolumeDraft = DutzAudioSettings.SfxVolume;
        showingSettings = true;
    }

    void DrawSettingsDialog()
    {
        var spacing = DutzCartoonDialogGui.Scale(6f, 10f);
        var footerLabels = new[] { BackLabel };
        var frameHeight = DutzCartoonDialogGui.RegistrationSetupDialogHeight(
            "SETTINGS",
            "Adjust music and sound effect volume.",
            string.Empty,
            false,
            false,
            footerLabels) + DutzCartoonDialogGui.Scale(120f, 160f);

        var previousDepth = GUI.depth;
        GUI.depth = -2900;
        DutzCartoonDialogGui.DrawDimOverlay();

        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(frameHeight);
        DutzCartoonDialogGui.DrawFrame(frame);
        var content = DutzCartoonDialogGui.ContentRect(frame);

        var footerHeight = DutzCartoonDialogGui.PanelPadding
            + DutzCartoonDialogGui.MeasureActionButtonHeight(BackLabel);
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

        GUILayout.BeginArea(bodyRect);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        titleStyle.fontSize = DutzCartoonDialogGui.ScaleFont(26, 34);
        GUILayout.Label("SETTINGS", titleStyle);
        GUILayout.Space(6f);

        var hintStyle = DutzCartoonDialogGui.HintStyle();
        GUILayout.Label("Adjust music and sound effect volume.", hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(12f, 18f));

        DrawVolumeSlider("Music", ref musicVolumeDraft, DutzAudioSettings.SetMusicVolume);
        GUILayout.Space(spacing);
        DrawVolumeSlider("Sound effects", ref sfxVolumeDraft, DutzAudioSettings.SetSfxVolume);
        GUILayout.Space(spacing);
        if (DutzCartoonDialogGui.ActionButton(
                "Privacy and cookie settings",
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight("Privacy and cookie settings")))
            DutzAdMobConsent.ShowPrivacyOptions();

        GUILayout.EndArea();

        GUILayout.BeginArea(footerRect);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding * 0.5f);
        if (DutzCartoonDialogGui.DismissButton(
                BackLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(BackLabel)))
            showingSettings = false;

        GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

    static void DrawVolumeSlider(string label, ref float draft, System.Action<float> apply)
    {
        var hintStyle = DutzCartoonDialogGui.HintStyle();
        GUILayout.Label($"{label}: {Mathf.RoundToInt(draft * 100f)}%", hintStyle);
        var next = GUILayout.HorizontalSlider(
            draft,
            0f,
            1f,
            GUILayout.Height(DutzCartoonDialogGui.Scale(24f, 32f)));
        if (!Mathf.Approximately(next, draft))
        {
            draft = next;
            apply(draft);
        }
    }

    void OnPhotoCaptured(Texture2D photo)
    {
        if (pendingPhoto != null && pendingPhoto != previewPhoto)
            Destroy(pendingPhoto);

        pendingPhoto = photo;
        photoStatus = photo != null ? "Selfie ready" : photoStatus;
    }

    void StartGame()
    {
        var photo = pendingPhoto != null ? pendingPhoto : previewPhoto;
        DutzVictorySelfieProfile.CompleteLevel00Setup(nameDraft, photo);
        pendingPhoto = null;
        FinishSetup();
    }

    void SkipRegistration()
    {
        if (pendingPhoto != null)
        {
            Destroy(pendingPhoto);
            pendingPhoto = null;
        }

        DutzVictorySelfieProfile.SkipLevel00Setup();
        FinishSetup();
    }

    void FinishSetup()
    {
        LockPlayer(false);
        DutzLevelSelectHud.DismissForLevel00Start();
        StartCoroutine(BeginFloodAfterRegistration());
    }

    IEnumerator BeginFloodAfterRegistration()
    {
        yield return null;
        if (this == null)
            yield break;

        IntroSequenceController intro = FindObjectOfType<IntroSequenceController>();
        if (intro != null)
            intro.BeginIntroAfterRegistration();
        else
        {
            var floodGame = FindObjectOfType<GameManager>();
            if (floodGame != null)
                floodGame.SetGameplayEnabled(true);
        }

        Destroy(gameObject);
    }
}
