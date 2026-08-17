using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Level 1 and Level 7 use suitcases; Level 2 uses gold coins — shared shop, HUD, and win score.</summary>
public static class DutzCollectibleProgress
{
    public static bool UsesSuitcases =>
        IsLevel01 || IsLevel07;

    public static bool IsLevel01 =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level01SceneName;

    public static bool IsLevel02 =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level02SceneName;

    public static bool IsLevel00 =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level00SceneName;

    public static bool IsLevel03 =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level03SceneName;

    public static bool IsLevel07 =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level07SceneName;

    /// <summary>Hague-style combat scenes (Level 3 and Level 7) — giants, HP, parachute, finale combat.</summary>
    public static bool IsLevel03Gameplay =>
        IsLevel03 || IsLevel07;

    public const float Level03GiantChaseSpeed = 27f;
    public const float Level03TrackGiantChaseSpeed = 25f;

    public const float Level03EndBossScale = 4.5f;

    const float Level03GiantChaseBaseSpeed = 22f;
    const float Level03GiantChaseBaseAnimSpeed = 1.5f;

    public static float GetLevel03GiantChaseAnimSpeed() =>
        Level03GiantChaseBaseAnimSpeed * (Level03GiantChaseSpeed / Level03GiantChaseBaseSpeed);

    public static float GetLevel03TrackGiantChaseAnimSpeed() =>
        Level03GiantChaseBaseAnimSpeed * (Level03TrackGiantChaseSpeed / Level03GiantChaseBaseSpeed);

    public static Vector3 GetLevel03EndBossScale() => Vector3.one * Level03EndBossScale;

    public static void ApplyLevel03EndBossScale(Transform transform)
    {
        if (transform == null)
            return;

        transform.localScale = GetLevel03EndBossScale();
    }

    public static Vector3 GetLevel03EndEtOlScale() => GetLevel03EndBossScale();

    public static void ApplyLevel03EndEtOlScale(Transform transform) => ApplyLevel03EndBossScale(transform);

    public static bool IsLevel03TrackEtOl(string objectName) =>
        DutzLevel03TrackGiantFaces.IsAnyTrackGiant(objectName);

    public static bool IsLevel03BonusGiant(string objectName) =>
        DutzGiantBossNames.IsHontavirus(objectName)
        || DutzGiantBossNames.IsLengLengLugaw(objectName);

    /// <summary>Level07 chase giants that show nearby HP and take punch combat like Level03 (not crocs).</summary>
    public static bool IsLevel07CombatGiant(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (DutzGiantBossNames.IsGongBong(objectName))
            return true;

        if (DutzGiantBossNames.IsAnyGiantBoss(objectName))
            return true;

        if (string.Equals(objectName, "RAPTOR", System.StringComparison.Ordinal))
            return true;

        if (string.Equals(objectName, "KIKAY P", System.StringComparison.Ordinal))
            return true;

        if (string.Equals(objectName, "Lie Fivex", System.StringComparison.Ordinal))
            return true;

        return false;
    }

    public static bool ShowsProximityHitPoints(string objectName) =>
        IsLevel03TrackEtOl(objectName)
        || IsLevel03BonusGiant(objectName)
        || (IsLevel07 && IsLevel07CombatGiant(objectName));

    /// <summary>Giants that accept Level03-style punch probe + stun (Level03 roster + Level07 combat giants).</summary>
    public static bool IsPunchCombatGiant(string objectName) =>
        IsLevel03Giant(objectName) || (IsLevel07 && IsLevel07CombatGiant(objectName));

    public static bool IsLevel03Giant(string objectName) =>
        DutzGiantBossNames.IsLevel03EndBoss(objectName)
        || IsLevel03TrackEtOl(objectName)
        || IsLevel03BonusGiant(objectName);

    /// <summary>Giants that walk the highway deck on Level 3 (not flying on bridge shells).</summary>
    public static bool UsesLevel03GiantRoadFooting(string objectName) =>
        IsLevel03TrackEtOl(objectName)
        || IsLevel03BonusGiant(objectName)
        || (IsLevel03Gameplay && DutzGiantBossNames.IsLevel03EndBoss(objectName));

    /// <summary>Level 3 giants use route-locked chase (no per-tick deck raycasts).</summary>
    public static bool UsesLevel03RouteLockedGiants(string objectName) =>
        IsLevel03Gameplay && UsesLevel03GiantRoadFooting(objectName);

    public static int CollectedCount =>
        UsesSuitcases ? DutzSuitcaseCounter.CollectedCount : DutzGoldCoinCounter.CollectedCount;

    public static bool TrySpend(int amount) =>
        UsesSuitcases
            ? DutzSuitcaseCounter.TrySpend(amount)
            : DutzGoldCoinCounter.TrySpend(amount);

    public static void ResetOnPlayerRespawn()
    {
        if (UsesSuitcases)
        {
            DutzSuitcaseCounter.ResetOnPlayerRespawn();
            if (IsLevel07)
                DutzVotesCounter.ResetOnPlayerRespawn();
        }
        else
            DutzGoldCoinCounter.ResetOnPlayerRespawn();

        if (IsLevel03Gameplay)
            DutzHealthPotionRegistry.ResetOnPlayerRespawn();

        DutzSuperPunchPickup.ResetOnPlayerRespawn();
        DutzSuperJumpPickup.ResetOnPlayerRespawn();
    }

    public static string BonusNoun => UsesSuitcases ? "Suitcase" : "Coin";

    public static string BonusNounPlural => UsesSuitcases ? "Suitcases" : "Coins";

    public static string SpendNounPlural => UsesSuitcases ? "suitcases" : "coins";
}
