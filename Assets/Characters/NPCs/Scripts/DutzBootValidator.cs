using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit startup checks — verifies outcomes, not Console log output.
/// </summary>
public static class DutzBootValidator
{
    const string GrandmaDialogObjectName = "GrandmaGiantDialog";
    const string CawetanDialogObjectName = "CawetanGiantDialog";
    const float DeferredBootstrapTimeoutSeconds = 90f;

    public static bool Validate(out string error)
    {
        error = null;

        if (!DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name))
            return true;

        if (DutzMobileRuntime.ShouldDeferNpcBootstrap && !DutzMobileShowcaseDeferredBootstrap.IsFinished)
        {
            error = "Mobile NPC bootstrap did not finish in time.";
            return false;
        }

        if (!ValidatePlayer(out error))
            return false;

        if (!ValidateCamera(out error))
            return false;

        if (!ValidateCollectibles(out error))
            return false;

        if (!ValidateSegmentPool(out error))
            return false;

        if (!ValidateGrandmaDialog(out error))
            return false;

        if (!ValidateCawetanDialog(out error))
            return false;

        if (!DutzCollectibleProgress.IsLevel03Gameplay && !ValidateEndGoal(out error))
            return false;

        if (!ValidateLevelObjective(out error))
            return false;

        if (DutzCollectibleProgress.IsLevel00 && !ValidateCrossroadCrowdRespawn(out error))
            return false;

        return true;
    }

    public static bool ValidateSceneHierarchy(out string error)
    {
        error = null;

        if (!DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name))
        {
            error = "Active scene is not a Dutz level scene.";
            return false;
        }

        if (GameObject.Find(DutzPlayerController.PlayerObjectName) == null &&
            Object.FindObjectOfType<DutzPlayerController>() == null)
        {
            error = "Player not found in scene hierarchy.";
            return false;
        }

        if (!DutzCollectibleProgress.IsLevel03Gameplay && !ValidateEndGoalInHierarchy(out error))
            return false;

        var poolRoot = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
        if (poolRoot != null
            && !DutzCollectibleProgress.IsLevel07
            && !HasTeleportSlotsOnPool(poolRoot.transform))
        {
            error = "DutzSegmentHippiePool exists but pool members lack DutzSegmentHippieTeleportSlots.";
            return false;
        }

        if (DutzCollectibleProgress.UsesSuitcases)
        {
            if (GameObject.Find("DutzSuitcases") == null)
            {
                error = "DutzSuitcases root not found — place suitcases on this level.";
                return false;
            }
        }
        else if (!DutzCollectibleProgress.IsLevel03Gameplay
            && !DutzCollectibleProgress.IsLevel00
            && GameObject.Find("DutzGoldCoins") == null)
        {
            error = "DutzGoldCoins root not found — distribute collectibles on the track in the scene.";
            return false;
        }

        var giant = DutzGiantBossNames.FindPrincessZara();
        if (giant != null && GameObject.Find(GrandmaDialogObjectName) == null)
        {
            error = "Grandma giant exists but GrandmaGiantDialog is missing.";
            return false;
        }

        var cawetan = DutzGiantBossNames.FindCawetan();
        if (cawetan != null && GameObject.Find(CawetanDialogObjectName) == null)
        {
            error = "Cawetan giant exists but CawetanGiantDialog is missing.";
            return false;
        }

        return true;
    }

    static bool ValidatePlayer(out string error)
    {
        error = null;
        var player = DutzPlayerController.Instance;
        if (player == null)
            player = Object.FindObjectOfType<DutzPlayerController>();

        if (player == null)
        {
            error = "Player (DutzPlayerController) not found.";
            return false;
        }

        if (player.GetComponent<CharacterController>() == null)
        {
            error = "Player is missing CharacterController.";
            return false;
        }

        if (player.GetComponent<DutzFallRespawn>() == null)
        {
            error = "Player is missing DutzFallRespawn (death dialog).";
            return false;
        }

        return true;
    }

    static bool ValidateCamera(out string error)
    {
        error = null;
        var cam = Camera.main;
        if (cam == null)
            cam = Object.FindObjectOfType<Camera>();

        if (cam == null)
        {
            error = "No camera found in scene.";
            return false;
        }

        return true;
    }

    static bool ValidateCollectibles(out string error)
    {
        error = null;

        if (DutzCollectibleProgress.IsLevel03Gameplay)
            return true;

        if (DutzCollectibleProgress.IsLevel00)
            return true;

        if (DutzCollectibleProgress.UsesSuitcases)
        {
            DutzSuitcaseCounter.EnsureSuitcasesAreReady();
            if (DutzSuitcaseCounter.GetSuitcases().Length == 0)
            {
                error = "No suitcases registered on Level 1 — open Dutz_Level01 and let auto-sync run.";
                return false;
            }
        }
        else
        {
            DutzGoldCoinCounter.EnsureCoinsAreReady();
            if (DutzGoldCoinCounter.GetCoins().Length == 0)
            {
                error = "No gold coins registered — distribute collectibles on the track in the scene.";
                return false;
            }
        }

        return true;
    }

    static bool ValidateSegmentPool(out string error)
    {
        error = null;
        if (DutzCollectibleProgress.IsLevel07)
            return true;

        var poolRoot = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
        if (poolRoot == null)
            return true;

        var manager = Object.FindObjectOfType<DutzSegmentHippieManager>();
        if (manager == null)
        {
            error = "DutzSegmentHippiePool exists but DutzSegmentHippieManager is missing.";
            return false;
        }

        if (!manager.enabled)
        {
            error = "DutzSegmentHippieManager is disabled — check teleport slots on pool hippies/crocs.";
            return false;
        }

        if (!HasTeleportSlotsOnPool(poolRoot.transform))
        {
            error = "Segment pool is missing DutzSegmentHippieTeleportSlots on members.";
            return false;
        }

        return true;
    }

    static bool HasTeleportSlotsOnPool(Transform poolRoot)
    {
        if (poolRoot == null)
            return true;

        var profile = poolRoot.GetComponent<DutzSegmentHippieTeleportProfile>();
        if (profile != null && profile.HasValidData)
            return true;

        var foundMember = false;
        foreach (Transform child in poolRoot)
        {
            if (!DutzSegmentHippieIdentity.IsPoolHippie(child.name))
                continue;

            foundMember = true;
            if (child.GetComponent<DutzSegmentHippieTeleportSlots>() != null)
                return true;
        }

        return !foundMember;
    }

    static bool ValidateGrandmaDialog(out string error)
    {
        error = null;
        var giant = DutzGiantBossNames.FindPrincessZara();
        if (giant == null)
            return true;

        if (GameObject.Find(GrandmaDialogObjectName) == null)
        {
            error = "Grandma giant found but GrandmaGiantDialog object is missing.";
            return false;
        }

        return true;
    }

    static bool ValidateCawetanDialog(out string error)
    {
        error = null;
        var cawetan = DutzGiantBossNames.FindCawetan();
        if (cawetan == null)
            return true;

        if (GameObject.Find(CawetanDialogObjectName) == null)
        {
            error = "Cawetan giant found but CawetanGiantDialog object is missing.";
            return false;
        }

        return true;
    }

    static bool ValidateEndGoal(out string error)
    {
        if (DutzCollectibleProgress.IsLevel03Gameplay)
        {
            error = null;
            return true;
        }

        if (DutzCollectibleProgress.IsLevel01 || DutzCollectibleProgress.IsLevel02)
            return ValidateEndHouse(out error);

        if (DutzCollectibleProgress.IsLevel00)
            return ValidateSenateBuildingMuralGoal(out error);

        return ValidateFlagPole(out error);
    }

    static bool ValidateEndGoalInHierarchy(out string error)
    {
        error = null;

        if (DutzCollectibleProgress.IsLevel01 || DutzCollectibleProgress.IsLevel02)
        {
            if (DutzEndHouseCollider.FindInScene() == null)
            {
                error = "End house not found in scene hierarchy.";
                return false;
            }

            return true;
        }

        if (DutzCollectibleProgress.IsLevel00)
        {
            if (DutzSenateBuildingMuralGoal.FindPanelObject() == null)
            {
                error = "Senate Building mural not found in scene hierarchy.";
                return false;
            }

            return true;
        }

        if (GameObject.Find(DutzFlagPoleGoal.FlagPoleName) == null)
        {
            error = "FlagPole not found in scene hierarchy.";
            return false;
        }

        return true;
    }

    static bool ValidateEndHouse(out string error)
    {
        error = null;
        DutzEndHouseCollider.EnsureFromBoot();
        var house = DutzEndHouseCollider.FindInScene();
        if (house == null)
        {
            error = "End house not found — win condition missing.";
            return false;
        }

        if (house.GetComponent<MeshCollider>() == null && house.GetComponentInChildren<MeshCollider>(true) == null)
        {
            error = "End house is missing a MeshCollider for the roof win.";
            return false;
        }

        return true;
    }

    static bool ValidateSenateBuildingMuralGoal(out string error)
    {
        error = null;
        DutzSenateBuildingMuralGoal.EnsureFromBoot();
        var panel = DutzSenateBuildingMuralGoal.FindPanelObject();
        if (panel == null)
        {
            error = "Senate Building mural not found — win condition missing.";
            return false;
        }

        if (panel.GetComponent<BoxCollider>() == null)
        {
            error = "Senate Building mural win trigger missing.";
            return false;
        }

        return true;
    }

    static bool ValidateAirplaneGoal(out string error)
    {
        error = null;
        DutzAirplaneGoal.EnsureFromBoot();
        var airplane = DutzAirplaneGoal.FindAirplaneObject();
        if (airplane == null)
        {
            error = "Dutz3dModel airplane not found — win condition missing.";
            return false;
        }

        if (airplane.GetComponentInChildren<BoxCollider>(true) == null)
        {
            error = "Airplane win zone missing — run Setup Level 0 Airplane Goal.";
            return false;
        }

        return true;
    }

    static bool ValidateFlagPole(out string error)
    {
        error = null;
        if (DutzCollectibleProgress.IsLevel03Gameplay
            || DutzCollectibleProgress.IsLevel01
            || DutzCollectibleProgress.IsLevel00)
            return true;

        if (GameObject.Find(DutzFlagPoleGoal.FlagPoleName) == null)
        {
            error = "FlagPole not found — win condition missing.";
            return false;
        }

        return true;
    }

    static bool ValidateLevelObjective(out string error)
    {
        error = null;
        DutzLevelObjective.EnsureFromBoot();
        if (Object.FindObjectOfType<DutzLevelObjective>() == null)
        {
            error = "DutzLevelObjective manager not found.";
            return false;
        }

        return true;
    }

    static bool ValidateCrossroadCrowdRespawn(out string error)
    {
        error = null;

        var hasWalkers = GameObject.Find("Level00CrowdWalkers") != null;
        var hasCitizens = GameObject.Find("Level00CrowdCitizens") != null;
        if (!hasWalkers && !hasCitizens)
            return true;

        if (Object.FindObjectOfType<DutzLevel00CrowdCrossroadRespawn>() == null)
        {
            error = "Level 00 crossroad crowd manager missing after boot.";
            return false;
        }

        var report = DutzLevel00CrowdCrossroadRespawn.BuildDiagnosticReport();
        if (!report.TrackReady)
        {
            error = "Level 00 crossroad crowd manager failed track init — see Console for details.";
            return false;
        }

        return true;
    }

    public static float DeferredBootstrapTimeout => DeferredBootstrapTimeoutSeconds;
}
