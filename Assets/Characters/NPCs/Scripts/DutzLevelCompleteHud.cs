using UnityEngine;

/// <summary>
/// Win dialog after the score roll — Level 0, 1, 2, and 7 offer different next-step choices.
/// Level 02 splits Sample (no Level 03 in build) vs Paid (Level 03 present).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(2000)]
public class DutzLevelCompleteHud : MonoBehaviour
{
    const string ManagerName = "DutzLevelCompleteHud";

    public static void EnsureFromBoot()
    {
        if (FindObjectOfType<DutzLevelCompleteHud>() != null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzLevelCompleteHud>();
    }

    void LateUpdate()
    {
        if (!Application.isMobilePlatform)
            return;

        if (DutzLevelObjective.ShouldShowLevelCompleteDialog())
            DutzImGuiTouchPoll.Poll();
        else if (DutzImGuiTouchPoll.SessionActive)
            DutzImGuiTouchPoll.EndSession();
    }

    void OnGUI()
    {
        if (DutzVictoryVideoPlayback.ShouldHideWinGui)
            return;

        DutzSeniorCitizenNextLevelGate.DrawOverlayIfShowing();
        if (FloodRewardedAdStub.IsShowing)
            return;

        if (DutzLevelObjective.ShouldShowLevelCompleteDialog())
        {
            DutzDialogCursor.EnsureUnlockedForDialog();
            DutzLevelObjective.Active?.UnlockCursorForLevelChoice();
        }

        if (DutzLevelObjective.ShouldShowLevel00CompleteDialog())
            DrawLevel00Dialog();
        else if (DutzLevelObjective.ShouldShowLevel01CompleteDialog())
            DrawLevel01Dialog();
        else if (DutzLevelObjective.ShouldShowLevel02CompleteDialog())
            DrawLevel02Dialog();
        else if (DutzLevelObjective.ShouldShowLevel07CompleteDialog())
            DrawLevel07Dialog();
    }

    static void DrawLevel00Dialog()
    {
        DrawChoiceDialog(
            "GREAT JOB! YOU REACHED THE PHILIPPINES SENATE!",
            "What would you like to do next?",
            new[]
            {
                ("GO TO LEVEL 3 — SENATE", (System.Action)DutzLevelObjective.LoadLevel1FromDialog),
                ("RESTART LEVEL 2", DutzLevelObjective.RestartLevel0FromDialog),
                ("EXIT THE GAME", DutzLevelObjective.ExitGameFromDialog)
            });
    }

    static void DrawLevel01Dialog()
    {
        DrawChoiceDialog(
            "YOU ESCAPED FROM THE SENATE.",
            "What would you like to do next?",
            new[]
            {
                ("GO TO LEVEL 4 — AIRPORT", (System.Action)DutzLevelObjective.LoadLevel2FromDialog),
                ("RESTART LEVEL 3", DutzLevelObjective.RestartLevel1FromDialog),
                ("EXIT THE GAME", DutzLevelObjective.ExitGameFromDialog)
            });
    }

    static void DrawLevel02Dialog()
    {
        if (DutzLevelObjective.HasLevel03InBuild())
            DrawLevel02PaidDialog();
        else
            DrawLevel02SampleDialog();
    }

    static void DrawLevel02PaidDialog()
    {
        DrawChoiceDialog(
            "YOU REACHED THE PLANE IN TIME. FLY TO THE HAGUE NOW!",
            "What would you like to do next?",
            new[]
            {
                ("GO TO LEVEL 5 — THE HAGUE", (System.Action)DutzLevelObjective.LoadLevel3FromDialog),
                ("REPEAT THIS LEVEL (4)", DutzLevelObjective.RestartLevel2FromDialog),
                ("GO BACK TO LEVEL 3", DutzLevelObjective.RestartLevel1FromDialog),
                ("EXIT THE GAME", DutzLevelObjective.ExitGameFromDialog)
            });
    }

    static void DrawLevel02SampleDialog()
    {
        DrawChoiceDialog(
            "SAMPLE COMPLETE! YOU REACHED THE PLANE.",
            "What would you like to do next?",
            new[]
            {
                ("Free Dutz, download the full game at Google Playstore.",
                    (System.Action)DutzLevelObjective.OpenCampaignPlayStoreFromDialog),
                ("REPEAT THIS LEVEL (4)", DutzLevelObjective.RestartLevel2FromDialog),
                ("GO BACK TO LEVEL 3", DutzLevelObjective.RestartLevel1FromDialog),
                ("EXIT THE GAME", DutzLevelObjective.ExitGameFromDialog)
            });
    }

    static void DrawLevel07Dialog()
    {
        DrawChoiceDialog(
            "PRINCESS Z IMPEACHED!",
            "What would you like to do next?",
            new[]
            {
                ("REPEAT THIS LEVEL (7)", (System.Action)DutzLevelObjective.RestartLevel07FromDialog),
                ("GO TO UNLOCKED LEVELS", DutzLevelObjective.GoToUnlockedLevelsFromDialog),
                ("EXIT THE GAME", DutzLevelObjective.ExitGameFromDialog)
            });
    }

    static void DrawChoiceDialog(string title, string hint, (string label, System.Action action)[] buttons)
    {
        DutzDialogCursor.EnsureUnlockedForDialog();
        DutzCartoonDialogGui.ResetPanelScroll();

        var useTouchPoll = Application.isMobilePlatform;
        if (useTouchPoll)
        {
            DutzImGuiTouchPoll.BeginSession();
            DutzImGuiTouchPoll.ClearEntries();
        }

        var previousDepth = GUI.depth;
        GUI.depth = -3200;
        DutzCartoonDialogGui.DrawDimOverlay(0.5f);

        var labels = new string[buttons.Length];
        for (var i = 0; i < buttons.Length; i++)
            labels[i] = buttons[i].label;

        var requiredHeight = DutzCartoonDialogGui.MeasureStackedPanelHeight(title, hint, labels);
        var height = DutzCartoonDialogGui.ChoiceDialogHeight(title, hint, labels);
        var frame = DutzCartoonDialogGui.LevelCompleteChoiceFrame(height);

        GUI.depth = -900;
        DutzCartoonDialogGui.DrawFrame(frame);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();
        var inner = DutzCartoonDialogGui.ContentRect(frame);
        if (useTouchPoll)
            DutzImGuiTouchPoll.SetAreaOrigin(new Vector2(inner.x, inner.y));

        var scrolling = DutzCartoonDialogGui.BeginPanelContent(frame, requiredHeight);
        if (useTouchPoll)
            DutzImGuiTouchPoll.SetScrollOffset(scrolling ? DutzCartoonDialogGui.PanelScrollPosition : Vector2.zero);

        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(title, titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label(hint, hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(8f, 12f));

        for (var i = 0; i < buttons.Length; i++)
        {
            if (i > 0)
                GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));

            var action = buttons[i].action;
            if (useTouchPoll)
            {
                DutzCartoonDialogGui.ActionButtonWithCallback(
                    buttons[i].label,
                    DutzCartoonDialogGui.ButtonColorForIndex(i),
                    DutzCartoonDialogGui.MeasureActionButtonHeight(buttons[i].label),
                    action);
            }
            else if (DutzCartoonDialogGui.ActionButton(
                         buttons[i].label,
                         DutzCartoonDialogGui.ButtonColorForIndex(i),
                         DutzCartoonDialogGui.MeasureActionButtonHeight(buttons[i].label)))
            {
                action?.Invoke();
            }
        }

        DutzCartoonDialogGui.EndPanelContent(scrolling);
        GUI.depth = previousDepth;
    }
}
