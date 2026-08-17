using UnityEditor;
using UnityEngine;

/// <summary>One-shot: republish Level 00 Duter cut-out murals as single panels.</summary>
public static class DutzLevel00DuterCutoutMuralRefresh
{
    [MenuItem("Assets/Dutz Authoring/Refresh Level00 Duter Cutout Murals")]
    public static void RefreshFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Refresh Level00 Duter Cutout Murals requires Edit Mode.");
            return;
        }

        RefreshAll(log: true);
    }

    /// <summary>Batch: -executeMethod DutzLevel00DuterCutoutMuralRefresh.RefreshAllBatch</summary>
    public static void RefreshAllBatch() => RefreshAll(log: true);

    public static void RefreshAll(bool log)
    {
        // Layout-only republish — cut-out textures are already synced.
        var timelineOk = DutzLevel00TimelineMuralPlacer.PlaceOnLevel00(log);
        var hagueOk = DutzLevel00DuterHagueMuralPlacer.PlaceOnLevel00(log);
        var tengotOk = DutzLevel00DuterTengotMuralPlacer.PlaceOnLevel00(log);
        DutzMuralBumpMessage.EnsureLevel00MuralsInScene(log);

        if (log)
        {
            Debug.Log(
                "[Dutz] Level00 Duter cut-out refresh — " +
                $"timeline={(timelineOk ? "ok" : "FAIL")}, " +
                $"hague={(hagueOk ? "ok" : "FAIL")}, " +
                $"tengot={(tengotOk ? "ok" : "FAIL")}.");
        }
    }
}
