using System;
using UnityEngine;

/// <summary>
/// Owns level difficulty settings and the shared gameplay enabled gate.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Difficulty")]
    [SerializeField] int currentLevel = 1;
    [SerializeField] float minimumGap = 7.8f;
    [SerializeField] float maximumGap = 20.8f;
    [SerializeField] float gapReductionPerLevel = 1.3f;

    bool gameplayEnabled;

    public int CurrentLevel => currentLevel;
    public float MinimumGap => minimumGap;
    public float MaximumGap => maximumGap;
    public float GapReductionPerLevel => gapReductionPerLevel;
    public bool IsGameplayEnabled => gameplayEnabled;

    /// <summary>Fired whenever gameplay is enabled or disabled.</summary>
    public event Action<bool> GameplayEnabledChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameplayEnabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Gap size for the current level. Level 1 uses MaximumGap, then shrinks toward MinimumGap.
    /// </summary>
    public float GetGapForCurrentLevel()
    {
        int safeLevel = Mathf.Max(1, currentLevel);
        float reduced = maximumGap - gapReductionPerLevel * (safeLevel - 1);
        return Mathf.Clamp(reduced, minimumGap, maximumGap);
    }

    public void SetGameplayEnabled(bool enabled)
    {
        if (gameplayEnabled == enabled)
            return;

        gameplayEnabled = enabled;
        GameplayEnabledChanged?.Invoke(gameplayEnabled);
    }

    void OnValidate()
    {
        currentLevel = Mathf.Max(1, currentLevel);
        minimumGap = Mathf.Max(0.1f, minimumGap);
        maximumGap = Mathf.Max(minimumGap, maximumGap);
        gapReductionPerLevel = Mathf.Max(0f, gapReductionPerLevel);
    }
}

/// <summary>Flood Control play mode — separate from campaign Easy/Medium/Hard.</summary>
public enum FloodDifficultyMode
{
    Normal,
    SeniorCitizen
}

public static class FloodDifficulty
{
    static bool chosen;
    static FloodDifficultyMode selected = FloodDifficultyMode.Normal;

    public static bool HasChosen => chosen;
    public static FloodDifficultyMode Selected => selected;

    public static bool IsSeniorCitizenMode() => selected == FloodDifficultyMode.SeniorCitizen;

    public static void ResetForNewRun()
    {
        chosen = false;
        selected = FloodDifficultyMode.Normal;
    }

    public static void Choose(FloodDifficultyMode mode)
    {
        selected = mode;
        chosen = true;
    }

    public static string GetDisplayName(FloodDifficultyMode mode) => mode switch
    {
        FloodDifficultyMode.SeniorCitizen => "Senior Citizen Mode",
        _ => "Normal Mode"
    };

    public static string GetDetailText(FloodDifficultyMode mode) => mode switch
    {
        FloodDifficultyMode.SeniorCitizen => "Unlimited force field for the run  •  GRAVITY STILL APPLIES.",
        _ => "Standard Flood Control gameplay"
    };

    public static string GetButtonLabel(FloodDifficultyMode mode, bool isDefault = false)
    {
        var label = GetDisplayName(mode).ToUpperInvariant();
        if (isDefault)
            label += " (DEFAULT)";
        return label;
    }
}

/// <summary>Flood Control start picker: Normal Mode or Senior Citizen Mode only.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class FloodDifficultySelect : MonoBehaviour
{
    const string ManagerName = "FloodDifficultySelect";

    static FloodDifficultySelect instance;

    bool awaitingSelection = true;

    public static bool IsBlockingStart => instance != null && instance.awaitingSelection;

    public static void Ensure()
    {
        if (instance != null)
            return;

        var go = new GameObject(ManagerName);
        instance = go.AddComponent<FloodDifficultySelect>();
    }

    public static void ResetForSceneLoad()
    {
        FloodDifficulty.ResetForNewRun();
        if (instance == null)
            return;

        instance.awaitingSelection = true;
    }

    void Awake()
    {
        instance = this;
        FloodDifficulty.ResetForNewRun();
        awaitingSelection = true;

        var gameManager = FindObjectOfType<GameManager>();
        gameManager?.SetGameplayEnabled(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (!awaitingSelection)
            return;

        if (DutzVictorySelfieProfile.IsRegistrationBlocking)
            return;

        if (DutzGameplayInput.GetKeyDown(KeyCode.Alpha1) || DutzGameplayInput.GetKeyDown(KeyCode.Keypad1))
            Choose(FloodDifficultyMode.Normal);
        else if (DutzGameplayInput.GetKeyDown(KeyCode.Alpha2) || DutzGameplayInput.GetKeyDown(KeyCode.Keypad2))
            Choose(FloodDifficultyMode.SeniorCitizen);
    }

    void OnGUI()
    {
        if (!awaitingSelection)
            return;

        if (DutzVictorySelfieProfile.IsRegistrationBlocking)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = -2900;
        DutzCartoonDialogGui.DrawDimOverlay();

        var title = "CHOOSE MODE";
        var subtitle = "Flood Control difficulty";
        var footer = Application.isMobilePlatform
            ? "Tap a mode to continue"
            : "Pick a mode to continue (or press 1 / 2)";
        var buttonLabels = new[]
        {
            FloodDifficulty.GetButtonLabel(FloodDifficultyMode.Normal, isDefault: true),
            FloodDifficulty.GetButtonLabel(FloodDifficultyMode.SeniorCitizen)
        };
        var detailLines = new[]
        {
            FloodDifficulty.GetDetailText(FloodDifficultyMode.Normal),
            FloodDifficulty.GetDetailText(FloodDifficultyMode.SeniorCitizen)
        };

        var requiredHeight = DutzCartoonDialogGui.MeasureStackedPanelHeight(
            title, subtitle, buttonLabels, detailLines, footer);
        var frame = DutzCartoonDialogGui.CenteredPanel(DutzCartoonDialogGui.ClampPanelHeight(requiredHeight));
        DutzCartoonDialogGui.DrawFrame(frame);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();
        var bodyStyle = DutzCartoonDialogGui.BodyStyle();
        bodyStyle.fontSize = DutzCartoonDialogGui.ScaleFont(20, 30);
        bodyStyle.fontStyle = FontStyle.Normal;

        var scrolling = DutzCartoonDialogGui.BeginPanelContent(frame, requiredHeight);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(title, titleStyle);
        GUILayout.Space(8f);
        GUILayout.Label(subtitle, hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 18f));

        DrawModeOption(FloodDifficultyMode.Normal, bodyStyle, DutzCartoonDialogGui.PlasticButtonColor.Blue, isDefault: true);
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 12f));
        DrawModeOption(FloodDifficultyMode.SeniorCitizen, bodyStyle, DutzCartoonDialogGui.PlasticButtonColor.Red);

        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 14f));
        GUILayout.Label(footer, hintStyle);
        DutzCartoonDialogGui.EndPanelContent(scrolling);
        GUI.depth = previousDepth;
    }

    void DrawModeOption(FloodDifficultyMode mode, GUIStyle bodyStyle, DutzCartoonDialogGui.PlasticButtonColor buttonColor, bool isDefault = false)
    {
        var label = FloodDifficulty.GetButtonLabel(mode, isDefault);
        if (DutzCartoonDialogGui.ActionButton(label, buttonColor, DutzCartoonDialogGui.MeasureActionButtonHeight(label)))
            Choose(mode);

        GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
        GUILayout.Label(FloodDifficulty.GetDetailText(mode), bodyStyle);
    }

    void Choose(FloodDifficultyMode mode)
    {
        FloodDifficulty.Choose(mode);
        awaitingSelection = false;
        Debug.Log($"[FloodControl] Mode: {FloodDifficulty.GetDisplayName(mode)}.");
        IntroSequenceController.TryContinueAfterModeSelection();
    }
}
