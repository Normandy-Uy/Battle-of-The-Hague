using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures Player1 and the Senate Building win mural exist in Dutz_Level00 on disk.
/// Called from editor auto-sync only when those objects are missing — not on every open.
/// </summary>
public static class DutzLevel00EssentialsSetup
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string DutzPrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";

    /// <summary>Batch: -executeMethod DutzLevel00EssentialsSetup.EnsureEssentialsBatch</summary>
    public static void EnsureEssentialsBatch() => EnsureEssentials(log: true);

    public static bool EnsureEssentials(bool log)
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

        var changed = RemoveDuplicatePlayers(log);
        changed |= EnsurePlayer1(log);
        if (GameObject.Find(DutzSenateBuildingMural.RootName) == null)
            changed |= DutzSenateBuildingMuralPlacer.PlaceOnLevel00(log);

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log && changed)
            Debug.Log("[Dutz] Level 00 essentials ready — Player1 + Senate Building mural saved.");

        return changed;
    }

    public static bool RemoveDuplicatePlayersIfAny(bool log) => RemoveDuplicatePlayers(log);

    static bool RemoveDuplicatePlayers(bool log)
    {
        var players = Object.FindObjectsOfType<DutzPlayerController>();
        if (players.Length <= 1)
            return false;

        for (var i = 1; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            Undo.DestroyObjectImmediate(players[i].gameObject);
            if (log)
                Debug.Log("[Dutz] Removed duplicate Player1 from Level 00.");
        }

        return true;
    }

    public static bool EnsurePlayer1(bool log)
    {
        if (HasPlayerInScene())
            return false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DutzPrefabPath);
        if (prefab == null)
        {
            if (log)
                Debug.LogError("[Dutz] Dutz.prefab missing — cannot place Player1 on Level 00.");
            return false;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = DutzPlayerController.PlayerObjectName;
        Undo.RegisterCreatedObjectUndo(go, "Place Player1 on Level 00");

        if (log)
            Debug.Log("[Dutz] Placed Player1 on Level 00.");

        DutzHighwayDirection.InvalidateReferenceCache();
        Physics.SyncTransforms();
        DutzSpawnSetup.SnapSpawnFieldsToBridgeStart(logErrors: log);
        return true;
    }

    public static bool HasPlayerInScene()
    {
        if (GameObject.Find(DutzPlayerController.PlayerObjectName) != null)
            return true;

        return Object.FindObjectOfType<DutzPlayerController>() != null;
    }
}
