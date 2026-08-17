using UnityEngine;

/// <summary>
/// Recovers Level 0 when Android resumes a paused session — player locked with no start UI.
/// </summary>
[DisallowMultipleComponent]
public class DutzAppSessionGuard : MonoBehaviour
{
    static DutzAppSessionGuard instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureHost()
    {
        if (instance != null)
            return;

        var existing = FindObjectOfType<DutzAppSessionGuard>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        var go = new GameObject(nameof(DutzAppSessionGuard));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzAppSessionGuard>();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
            TryRecoverStuckLevel00Session();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            TryRecoverStuckLevel00Session();
    }

    static void TryRecoverStuckLevel00Session()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        if (DutzBootOverlay.State == DutzBootOverlay.OverlayState.Loading)
            return;

        if (!DutzGameBootstrap.IsReady)
            return;

        var player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
        if (player == null || !player.ControlsLocked)
            return;

        if (HasLegitimateGameplayLock(player))
            return;

        if (DutzVictorySelfieProfile.IsLevel00SetupBlocking)
        {
            RecreateLevel00StartUi();
            return;
        }

        player.SetControlsLocked(false);
        Debug.Log("[Dutz] Session guard unlocked player after resume (stuck lock, no start UI).");
    }

    static bool HasLegitimateGameplayLock(DutzPlayerController player)
    {
        if (DutzLevelStartGate.IsBlockingStart)
            return true;

        if (DutzLevelObjective.IsStartMessageActive)
            return true;

        var fallRespawn = player.GetComponent<DutzFallRespawn>();
        if (fallRespawn != null && fallRespawn.IsShowingRespawnDialog)
            return true;

        if (DutzPoliceCaptureDialog.IsShowing)
            return true;

        if (DutzGrandmaBossPowerShop.IsShowingDialog)
            return true;

        if (DutzSenateVotesOffer.IsShowing)
            return true;

        return false;
    }

    static void RecreateLevel00StartUi()
    {
        DutzLevel00WelcomeSplash.ResetForSceneLoad();

        if (DutzLevelUnlockProgress.HasJumpOptions())
            DutzLevelSelectHud.EnsureFromBoot();
        else
            DutzVictorySelfieSetupHud.EnsureForLevel00Play();

        Debug.Log("[Dutz] Session guard recreated Level 0 start UI after resume.");
    }
}
