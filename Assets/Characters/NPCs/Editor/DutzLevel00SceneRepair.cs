using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manual Level 00 mural/player repair only — use Assets/Dutz Authoring/Repair Level 00 Scene
/// or RepairOpenSceneBatch. Does not run automatically on scene open.
/// </summary>
public static class DutzLevel00SceneRepair
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";

    /// <summary>Batch: -executeMethod DutzLevel00SceneRepair.RepairOpenSceneBatch</summary>
    public static void RepairOpenSceneBatch() => RepairOpenScene(log: true, force: true);

    [MenuItem("Assets/Dutz Authoring/Repair Level 00 Scene")]
    public static void RepairFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Repair Level 00", "Exit Play mode first.", "OK");
            return;
        }

        RepairOpenScene(log: true, force: true);
    }

    public static bool RepairOpenSceneIfNeeded(bool log) => RepairOpenScene(log, force: false);

    public static bool RepairOpenScene(bool log, bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (!File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        PrepareHighwayReferences();

        var changed = false;
        if (force)
            changed |= DutzLevel00HighwaySegmentRestore.RestoreMissing(log: false);

        if (force || !DutzLevel00EssentialsSetup.HasPlayerInScene())
            changed |= DutzLevel00EssentialsSetup.EnsurePlayer1(log);

        changed |= DutzLevel00EssentialsSetup.RemoveDuplicatePlayersIfAny(log);

        if (GameObject.Find(DutzSenateBuildingMural.RootName) == null)
            changed |= DutzSenateBuildingMuralPlacer.PlaceOnLevel00(log);
        else if (force)
        {
            DutzSenateBuildingMuralPlacer.SyncTexture();
            changed |= DutzSenateBuildingMural.EnsurePanelMaterials(log);
        }

        if (force || CountTimelineMurals() < 7)
        {
            changed |= force
                ? DutzLevel00TimelineMuralPlacer.PlaceOnLevel00(log)
                : DutzLevel00TimelineMuralPlacer.EnsureOnOpenScene(log);
        }

        if (GameObject.Find(DutzLevel00EdsaMuralPlacer.RootName) == null)
            changed |= DutzLevel00EdsaMuralPlacer.PlaceOnLevel00(log);

        if (force || GameObject.Find(DutzLevel00DuterHagueMuralPlacer.RootName) == null)
        {
            changed |= force
                ? DutzLevel00DuterHagueMuralPlacer.PlaceOnLevel00(log)
                : DutzLevel00DuterHagueMuralPlacer.EnsureOnOpenScene(log);
        }

        changed |= force
            ? DutzLevel00DuterTengotMuralPlacer.PlaceOnLevel00(log)
            : DutzLevel00DuterTengotMuralPlacer.EnsureOnOpenScene(log);

        changed |= DutzMuralBumpMessage.EnsureLevel00MuralsInScene(log);

        if (!changed)
        {
            if (log)
                Debug.Log("[Dutz] Level 00 scene repair — nothing missing.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Level 00 scene repair saved.");

        return true;
    }

    static void PrepareHighwayReferences()
    {
        for (var i = 0; i < 3; i++)
        {
            DutzHighwayDirection.InvalidateReferenceCache();
            DutzHighwayDirection.InvalidateTrackSegmentCache();
            Physics.SyncTransforms();
        }
    }

    static int CountTimelineMurals()
    {
        var count = 0;
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name.StartsWith("TimelineMural_", System.StringComparison.Ordinal))
                count++;
        }

        return count;
    }

}
