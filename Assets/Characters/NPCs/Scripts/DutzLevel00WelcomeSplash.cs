using UnityEngine;

/// <summary>
/// Level 0 — large transparent welcome flash when gameplay starts (Play / Level 0 pick).
/// </summary>
public static class DutzLevel00WelcomeSplash
{
    public const string Message = "WELCOME TO EDSA";
    public const float DurationSeconds = 1f;

    static float timeLeft;

    public static bool IsActive => timeLeft > 0f;

    public static bool IsBlockingStart =>
        DutzCollectibleProgress.IsLevel00 && IsActive;

    public static void EnsureHost()
    {
        if (Object.FindObjectOfType<DutzLevel00WelcomeSplashHost>() != null)
            return;

        var host = new GameObject("DutzLevel00WelcomeSplashHost");
        host.AddComponent<DutzLevel00WelcomeSplashHost>();
    }

    /// <summary>
    /// After registration moved to Flood, EDSA boots need a welcome → unlock path.
    /// Skip when the level-select jump menu is showing.
    /// </summary>
    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        EnsureHost();

        if (DutzLevelUnlockProgress.HasJumpOptions())
            return;

        if (IsActive)
            return;

        Trigger();
    }

    public static void ResetForSceneLoad() => timeLeft = 0f;

    public static void Trigger()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        EnsureHost();
        timeLeft = DurationSeconds;

        var player = DutzPlayerController.Instance ?? Object.FindObjectOfType<DutzPlayerController>();
        player?.SetControlsLocked(true);
    }

    public static void Tick()
    {
        if (timeLeft <= 0f)
            return;

        timeLeft -= Time.unscaledDeltaTime;
        if (timeLeft > 0f)
            return;

        timeLeft = 0f;
        ReleasePlayer();
    }

    public static void Draw()
    {
        if (!IsActive)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = -3100;
        DutzCartoonDialogGui.DrawLargeWelcomeSplash(
            Message,
            DutzAnnouncementHud.DefaultFlashColor);
        GUI.depth = previousDepth;
    }

    static void ReleasePlayer()
    {
        var player = DutzPlayerController.Instance ?? Object.FindObjectOfType<DutzPlayerController>();
        if (player != null)
        {
            var difficulty = player.GetComponent<DutzDifficultySelect>();
            if (difficulty != null && difficulty.AwaitingSelection)
            {
                player.SetControlsLocked(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            player.SetControlsLocked(false);
        }

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

[DisallowMultipleComponent]
public class DutzLevel00WelcomeSplashHost : MonoBehaviour
{
    void Update() => DutzLevel00WelcomeSplash.Tick();

    void OnGUI() => DutzLevel00WelcomeSplash.Draw();
}
