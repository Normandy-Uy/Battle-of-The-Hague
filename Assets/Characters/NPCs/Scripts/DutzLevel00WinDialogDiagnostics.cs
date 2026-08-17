using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Play-mode diagnostics for Level 00 Senate win dialog (MCP / batch).
/// Batch: -executeMethod DutzLevel00WinDialogDiagnostics.DiagnosePlayModeBatch
/// </summary>
public static class DutzLevel00WinDialogDiagnostics
{
    public static void DiagnosePlayModeBatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Dutz] Win dialog diagnostics require Play mode on Dutz_Level00.");
            return;
        }

        Debug.Log(BuildReport(forceWin: false));
    }

    /// <summary>Force win state so choice dialog preconditions can be verified in Play mode.</summary>
    public static void SimulateWinPlayModeBatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Dutz] Win dialog simulate requires Play mode on Dutz_Level00.");
            return;
        }

        if (!DutzDifficulty.HasChosen)
            DutzDifficulty.Choose(DutzDifficultyLevel.Hard);

        DutzLevelObjective.NotifySenateBuildingMuralReached();
        Debug.Log("[Dutz] Win dialog simulate — triggered NotifySenateBuildingMuralReached().");
        Debug.Log(BuildReport(forceWin: false));
    }

    public static string BuildReport(bool forceWin)
    {
        var scene = SceneManager.GetActiveScene().name;
        var objective = DutzLevelObjective.Active;
        var hud = Object.FindObjectOfType<DutzLevelCompleteHud>();
        var player = DutzPlayerController.Instance ?? Object.FindObjectOfType<DutzPlayerController>();

        var lines =
            "[Dutz] Win dialog diagnostic\n" +
            $"  scene={scene} isLevel00={DutzCollectibleProgress.IsLevel00}\n" +
            $"  objective={(objective != null ? objective.name : "MISSING")} Instance={(DutzLevelObjective.Instance != null)}\n" +
            $"  hud={(hud != null ? hud.name : "MISSING")} enabled={(hud != null && hud.enabled)}\n" +
            $"  player controlsLocked={(player != null && player.ControlsLocked)} paused={DutzGamePause.IsPaused} timeScale={Time.timeScale:F2}\n" +
            $"  hideWinGui={DutzVictoryVideoPlayback.ShouldHideWinGui} adShowing={FloodRewardedAdStub.IsShowing}\n" +
            $"  shouldShowComplete={DutzLevelObjective.ShouldShowLevelCompleteDialog()} shouldShowL00={DutzLevelObjective.ShouldShowLevel00CompleteDialog()}\n" +
            $"  isShowingChoice={DutzLevelObjective.IsShowingLevelCompleteChoice} isLevelFinished={DutzLevelObjective.IsLevelFinishedForActiveScene}\n" +
            $"  touchPollSession={DutzImGuiTouchPoll.SessionActive} screen={Screen.width}x{Screen.height} mobile={Application.isMobilePlatform}";

        if (objective != null)
        {
            var flags = objective.GetType();
            var binding = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var finished = (bool)flags.GetField("finished", binding).GetValue(objective);
            var won = (bool)flags.GetField("won", binding).GetValue(objective);
            var scoreRoll = (bool)flags.GetField("scoreRollComplete", binding).GetValue(objective);
            lines +=
                $"\n  finished={finished} won={won} scoreRollComplete={scoreRoll}";
        }

        return lines;
    }
}
