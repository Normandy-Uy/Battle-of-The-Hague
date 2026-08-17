using UnityEngine;

/// <summary>
/// Play-mode diagnostics for Level 00 Highway Cross Road crowd duplicate spawn.
/// Batch: -executeMethod DutzLevel00CrossroadDiagnostics.DiagnosePlayModeBatch
/// </summary>
public static class DutzLevel00CrossroadDiagnostics
{
    /// <summary>MCP / batch entry — call while Play mode is active on Dutz_Level00.</summary>
    public static void DiagnosePlayModeBatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Dutz] Crossroad diagnostics require Play mode on Dutz_Level00.");
            return;
        }

        if (!DutzCollectibleProgress.IsLevel00)
        {
            Debug.LogWarning("[Dutz] Crossroad diagnostics: active scene is not Level 00.");
            return;
        }

        var report = DutzLevel00CrowdCrossroadRespawn.BuildDiagnosticReport();
        Debug.Log(report.Summary);

        if (!report.ManagerExists)
            Debug.LogError("[Dutz] Crossroad diagnostics FAILED — manager missing.");
        else if (!report.TrackReady)
            Debug.LogError("[Dutz] Crossroad diagnostics FAILED — track not ready.");
        else if (report.SnapshotCount == 0)
            Debug.LogError("[Dutz] Crossroad diagnostics FAILED — no bridge crowd sources.");
        else if (report.SpawnSlotCount == 0)
            Debug.LogError("[Dutz] Crossroad diagnostics FAILED — no scene spawn slots under Level00CrossroadChaseSpawns.");
    }
}
