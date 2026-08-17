using UnityEngine;

/// <summary>
/// Level 0 start screen — jump to any unlocked level (saved in PlayerPrefs).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(2090)]
public class DutzLevelSelectHud : MonoBehaviour
{
    const string ManagerName = "DutzLevelSelectHud";

    static DutzLevelSelectHud instance;
    static bool awaitingSelection = true;

    public static bool IsBlockingStart =>
        instance != null && awaitingSelection && DutzCollectibleProgress.IsLevel00;

    /// <summary>START GAME / SKIP from registration — play Level 0, not the jump menu.</summary>
    public static void DismissForLevel00Start()
    {
        if (instance == null)
            return;

        awaitingSelection = false;
        Object.Destroy(instance.gameObject);
    }

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        if (!DutzLevelUnlockProgress.HasJumpOptions())
            return;

        if (FindObjectOfType<DutzLevelSelectHud>() != null)
            return;

        DutzLevelUnlockProgress.ReloadFromDisk();

        var go = new GameObject(ManagerName);
        instance = go.AddComponent<DutzLevelSelectHud>();
    }

    void Awake()
    {
        instance = this;
        awaitingSelection = true;
    }

    void Start()
    {
        LockPlayer(true);
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
        if (!awaitingSelection)
            return;

        if (DutzVictorySelfieSetupHud.IsBlockingStart)
            return;

        if (DutzBootOverlay.State == DutzBootOverlay.OverlayState.Loading
            || DutzBootOverlay.State == DutzBootOverlay.OverlayState.Failed)
            return;

        DrawLevelSelectDialog();
    }

    void DrawLevelSelectDialog()
    {
        var previousDepth = GUI.depth;
        GUI.depth = -1200;
        DutzCartoonDialogGui.DrawDimOverlay(0.55f);

        var labels = BuildButtonLabels(out var unlockedFlags);
        var requiredHeight = DutzCartoonDialogGui.MeasureStackedPanelHeight(
            "WELCOME BACK!",
            "Pick a level to play.",
            labels);
        var height = DutzCartoonDialogGui.ChoiceDialogHeight(
            "WELCOME BACK!",
            "Pick a level to play.",
            labels);
        var frame = DutzCartoonDialogGui.LevelCompleteChoiceFrame(height);
        GUI.depth = -900;
        DutzCartoonDialogGui.DrawFrame(frame);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();
        var lockedStyle = DutzCartoonDialogGui.BodyStyle();
        lockedStyle.fontStyle = FontStyle.Italic;

        var scrolling = DutzCartoonDialogGui.BeginPanelContent(frame, requiredHeight);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label("WELCOME BACK!", titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label("Pick a level to play.", hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));

        for (var i = 0; i < labels.Length; i++)
        {
            if (i > 0)
                GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));

            if (unlockedFlags[i])
            {
                var levelIndex = i;
                if (DutzCartoonDialogGui.ActionButton(
                        labels[i],
                        DutzCartoonDialogGui.ButtonColorForIndex(i),
                        DutzCartoonDialogGui.MeasureActionButtonHeight(labels[i])))
                    ChooseLevel(levelIndex);
            }
            else
            {
                GUILayout.Label(labels[i] + "  (LOCKED)", lockedStyle);
            }
        }

        DutzCartoonDialogGui.EndPanelContent(scrolling);
        GUI.depth = previousDepth;
    }

    static string[] BuildButtonLabels(out bool[] unlockedFlags)
    {
        unlockedFlags = new bool[DutzLevelUnlockProgress.LevelCount];
        var labels = new string[DutzLevelUnlockProgress.LevelCount];
        for (var i = 0; i < labels.Length; i++)
        {
            unlockedFlags[i] = DutzLevelUnlockProgress.IsUnlocked(i);
            labels[i] = DutzLevelUnlockProgress.GetMenuLabel(i);
        }

        return labels;
    }

    void ChooseLevel(int levelIndex)
    {
        if (!DutzLevelUnlockProgress.IsUnlocked(levelIndex))
            return;

        awaitingSelection = false;
        LockPlayer(true);

        if (levelIndex == 0)
        {
            awaitingSelection = false;
            LockPlayer(true);
            DutzVictorySelfieSetupHud.EnsureForLevel00Play();
            Destroy(gameObject);
            return;
        }

        DutzLevelUnlockProgress.LoadLevel(levelIndex);
    }
}
