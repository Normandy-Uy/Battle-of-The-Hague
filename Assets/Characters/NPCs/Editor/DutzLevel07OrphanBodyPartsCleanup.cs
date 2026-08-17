using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level07: deletes orphaned body joints / outfit meshes / BossFace / PotionModelVisual
/// that got unparented to the scene root (hierarchy litter).
/// </summary>
public static class DutzLevel07OrphanBodyPartsCleanup
{
    [MenuItem("Assets/Dutz Authoring/Cleanup Level07 Orphan Body Parts At Root")]
    public static void CleanupFromMenu()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[Dutz] Cleanup Level07 Orphan Body Parts requires Edit Mode.");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.Contains("Level07"))
        {
            Debug.LogError("[Dutz] Open Dutz_Level07 before cleaning orphan body parts.");
            return;
        }

        var deleted = Cleanup(scene, log: true);
        if (deleted > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[Dutz] Removed {deleted} orphaned root object(s) from {scene.name}.");
    }

    public static int Cleanup(Scene scene, bool log)
    {
        var roots = scene.GetRootGameObjects();
        var toDelete = new List<GameObject>();

        foreach (var root in roots)
        {
            if (root == null)
                continue;

            if (!IsOrphanLitter(root.name))
                continue;

            // Never touch real characters / track giants even if somehow at root.
            if (root.GetComponent<SimpleCitizensGiantHippieHunter>() != null
                || root.GetComponent<DutzNpcHitPoints>() != null
                || DutzGiantBossNames.IsAnyGiantBoss(root.name)
                || DutzCollectibleProgress.IsLevel03Giant(root.name)
                || root.name.StartsWith("Dutz", System.StringComparison.Ordinal)
                || root.name.StartsWith("Level07_", System.StringComparison.Ordinal)
                || root.name.StartsWith("Player", System.StringComparison.Ordinal)
                || root.name.StartsWith("Highway", System.StringComparison.Ordinal))
                continue;

            toDelete.Add(root);
        }

        var count = 0;
        foreach (var go in toDelete)
        {
            if (go == null)
                continue;

            if (log)
                Debug.Log($"[Dutz] Deleting orphaned root: {go.name}");

            Undo.DestroyObjectImmediate(go);
            count++;
        }

        return count;
    }

    static bool IsOrphanLitter(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Detached skeleton joints.
        if (name.EndsWith("_jnt", System.StringComparison.Ordinal)
            || name.StartsWith("Spine_jnt", System.StringComparison.Ordinal)
            || name.Equals("Hips_jnt", System.StringComparison.Ordinal))
            return true;

        // Detached SimpleCitizens outfit meshes (normally children of a character).
        if (name.StartsWith("SC_", System.StringComparison.Ordinal))
            return true;

        // Detached boss face quad / potion visual leftovers.
        // Never delete real potion roots — only loose visual clones at scene root.
        if (name.Equals("BossFace", System.StringComparison.Ordinal))
            return true;

        if (name.Equals("PotionModelVisual", System.StringComparison.Ordinal))
            return true;

        // Explicit: never treat health potion roots as litter.
        if (name.StartsWith("DutzHealthPotion_", System.StringComparison.Ordinal))
            return false;

        return false;
    }
}
