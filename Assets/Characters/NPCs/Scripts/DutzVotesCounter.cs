using UnityEngine;

/// <summary>
/// Vote tally for Level 07 only — each giant kill adds 1 vote.
/// HUD draws beside the suitcase counter.
/// </summary>
[DisallowMultipleComponent]
public class DutzVotesCounter : MonoBehaviour
{
    const string ManagerName = "DutzVotesCounter";

    static int votes;

    public static int Votes => votes;

    public static bool ShouldShow =>
        DutzCollectibleProgress.IsLevel07;

    public static void EnsureFromBoot()
    {
        if (!ShouldShow)
            return;

        if (Object.FindObjectOfType<DutzVotesCounter>() != null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzVotesCounter>();
    }

    public static void RegisterGiantKill(GameObject giant)
    {
        if (!ShouldShow || giant == null)
            return;

        if (!IsVoteGiant(giant))
            return;

        AddVotes(1);
    }

    public static void AddVotes(int amount)
    {
        if (amount == 0)
            return;

        votes = Mathf.Max(0, votes + amount);
        TryNotifyLevel07ImpeachmentWin();
    }

    static void TryNotifyLevel07ImpeachmentWin()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        if (votes < DutzSenateVotesOffer.VotesToImpeach)
            return;

        // Win only after Boy Idol is down and votes hit 16 (Senate buy or kills).
        if (!DutzLevel07BoyIdolGate.IsBoyIdolDefeated)
            return;

        DutzLevelObjective.NotifyLevel07ImpeachmentWon();
    }

    public static void ResetOnPlayerRespawn() => votes = 0;

    public static void ResetForSceneLoad() => votes = 0;

    public static bool IsVoteGiant(GameObject target)
    {
        if (target == null)
            return false;

        if (target.GetComponent<SimpleCitizensGiantHippieHunter>() != null)
            return true;

        if (DutzGiantBossNames.IsAnyGiantBoss(target.name))
            return true;

        if (DutzCollectibleProgress.IsLevel03Giant(target.name))
            return true;

        return false;
    }

    void OnGUI()
    {
        if (!ShouldShow)
            return;

        var suitcaseCollected = 0;
        var suitcaseTotal = 0;
        var showSuitcases = DutzCollectibleProgress.UsesSuitcases
            && DutzSuitcaseCounter.TryGetHudCounts(out suitcaseCollected, out suitcaseTotal)
            && suitcaseTotal > 0;

        if (showSuitcases)
            DutzCollectibleHudDraw.DrawVotesBesideSuitcases(votes, suitcaseCollected, suitcaseTotal);
        else
            DutzCollectibleHudDraw.DrawVotes(votes);
    }
}
