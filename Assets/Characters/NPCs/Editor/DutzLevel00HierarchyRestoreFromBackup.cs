using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores missing Level 00 hierarchy nodes from backup scenes (full project backup + scene snapshots).
/// Batch: -executeMethod DutzLevel00HierarchyRestoreFromBackup.RestoreBatch
/// </summary>
public static class DutzLevel00HierarchyRestoreFromBackup
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string SnapshotScenePath = "_SceneSnapshots_2026-08-15_pre-backup-restore/Dutz_Level00.unity";
    const string FullBackupFolderName = "Back Up New Unity Project";

    static readonly HashSet<string> SkipRestoreNames = new(System.StringComparer.Ordinal)
    {
        "Main Camera",
        "Directional Light",
    };

    /// <summary>Batch entry for MCP / CI.</summary>
    public static void RestoreBatch() => RestoreMissingHierarchies(log: true);

    [MenuItem("Assets/Dutz Authoring/Restore Level 00 Missing Hierarchies From Backup")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Restore Level 00", "Exit Play mode first.", "OK");
            return;
        }

        RestoreMissingHierarchies(log: true);
    }

    public static bool RestoreMissingHierarchies(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var sources = ResolveBackupSceneAssetPaths(log);
        if (sources.Count == 0)
        {
            if (log)
                Debug.LogError("[Dutz] No Level 00 backup scenes found to scavenge.");
            return false;
        }

        DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth++;
        try
        {
            var targetScene = SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || targetScene.path != Level00ScenePath)
                targetScene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);

            Physics.SyncTransforms();

            var knownPaths = CollectScenePaths(targetScene);
            var restoredPaths = new List<string>();

            foreach (var backupAssetPath in sources)
            {
                if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, backupAssetPath)))
                    continue;

                if (log)
                    Debug.Log("[Dutz] Scavenging backup scene: " + backupAssetPath);

                var backupScene = EditorSceneManager.OpenScene(backupAssetPath, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in backupScene.GetRootGameObjects())
                    {
                        if (root == null)
                            continue;

                        RestoreSubtreeIfMissing(root.transform, string.Empty, targetScene, knownPaths, restoredPaths);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(backupScene, true);
                }
            }

            if (restoredPaths.Count == 0)
            {
                if (log)
                    Debug.Log("[Dutz] Level 00 hierarchy restore — nothing missing in backups.");
                return false;
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene, Level00ScenePath);

            if (log)
            {
                Debug.Log($"[Dutz] Restored {restoredPaths.Count} missing Level 00 hierarchy node(s) from backup:");
                foreach (var path in restoredPaths)
                    Debug.Log("[Dutz]   + " + path);
            }

            return true;
        }
        finally
        {
            DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth =
                Mathf.Max(0, DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth - 1);
        }
    }

    static List<string> ResolveBackupSceneAssetPaths(bool log)
    {
        var list = new List<string>();

        var fullBackupScene = CopyFullBackupSceneIntoProject(log);
        if (!string.IsNullOrEmpty(fullBackupScene))
            list.Add(fullBackupScene);

        var snapshotScene = CopySnapshotSceneIntoProject(log);
        if (!string.IsNullOrEmpty(snapshotScene))
            list.Add(snapshotScene);

        return list;
    }

    static string CopySnapshotSceneIntoProject(bool log)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        var sourceScene = Path.GetFullPath(Path.Combine(projectRoot, SnapshotScenePath));
        if (!File.Exists(sourceScene))
        {
            if (log)
                Debug.LogWarning("[Dutz] Snapshot scene not found: " + sourceScene);
            return null;
        }

        var tempDir = Path.Combine(Application.dataPath, "Editor", "TempBackupScenes");
        Directory.CreateDirectory(tempDir);

        var destScene = Path.Combine(tempDir, "Dutz_Level00_SnapshotReference.unity");
        File.Copy(sourceScene, destScene, overwrite: true);

        var sourceMeta = sourceScene + ".meta";
        var destMeta = destScene + ".meta";
        if (File.Exists(sourceMeta))
            File.Copy(sourceMeta, destMeta, overwrite: true);

        AssetDatabase.Refresh();
        return "Assets/Editor/TempBackupScenes/Dutz_Level00_SnapshotReference.unity";
    }

    static string CopyFullBackupSceneIntoProject(bool log)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        var sourceScene = Path.GetFullPath(Path.Combine(
            projectRoot,
            "..",
            FullBackupFolderName,
            "New Unity Project",
            "Assets",
            "Scenes",
            "Dutz_Level00.unity"));

        if (!File.Exists(sourceScene))
        {
            if (log)
                Debug.LogWarning("[Dutz] Full backup scene not found: " + sourceScene);
            return null;
        }

        var tempDir = Path.Combine(Application.dataPath, "Editor", "TempBackupScenes");
        Directory.CreateDirectory(tempDir);

        var destScene = Path.Combine(tempDir, "Dutz_Level00_FullBackupReference.unity");
        File.Copy(sourceScene, destScene, overwrite: true);

        var sourceMeta = sourceScene + ".meta";
        var destMeta = destScene + ".meta";
        if (File.Exists(sourceMeta))
            File.Copy(sourceMeta, destMeta, overwrite: true);

        AssetDatabase.Refresh();
        return "Assets/Editor/TempBackupScenes/Dutz_Level00_FullBackupReference.unity";
    }

    static HashSet<string> CollectScenePaths(Scene scene)
    {
        var paths = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            CollectPathsRecursive(root.transform, string.Empty, paths);
        }

        return paths;
    }

    static void CollectPathsRecursive(Transform transform, string parentPath, HashSet<string> paths)
    {
        var path = CombinePath(parentPath, transform.name);
        paths.Add(path);

        for (var i = 0; i < transform.childCount; i++)
            CollectPathsRecursive(transform.GetChild(i), path, paths);
    }

    static void RestoreSubtreeIfMissing(
        Transform source,
        string parentPath,
        Scene targetScene,
        HashSet<string> knownPaths,
        List<string> restoredPaths)
    {
        if (source == null)
            return;

        var path = CombinePath(parentPath, source.name);

        if (knownPaths.Contains(path))
        {
            for (var i = 0; i < source.childCount; i++)
                RestoreSubtreeIfMissing(source.GetChild(i), path, targetScene, knownPaths, restoredPaths);
            return;
        }

        if (SkipRestoreNames.Contains(source.name))
            return;

        if (ShouldSkipByPrefix(source.name))
        {
            for (var i = 0; i < source.childCount; i++)
                RestoreSubtreeIfMissing(source.GetChild(i), path, targetScene, knownPaths, restoredPaths);
            return;
        }

        var clone = Object.Instantiate(source.gameObject);
        clone.name = source.name;
        Undo.RegisterCreatedObjectUndo(clone, "Restore Level 00 hierarchy from backup");
        SceneManager.MoveGameObjectToScene(clone, targetScene);

        var parent = FindTransformByPath(targetScene, parentPath);
        if (parent != null)
            clone.transform.SetParent(parent, true);

        RegisterPathsRecursive(clone.transform, parentPath, knownPaths);
        restoredPaths.Add(path);
    }

    static bool ShouldSkipByPrefix(string name)
    {
        // Never replace the authored crossroad grid — only fill gaps elsewhere.
        return name.StartsWith("CrossroadSpawn_", System.StringComparison.Ordinal);
    }

    static void RegisterPathsRecursive(Transform transform, string parentPath, HashSet<string> knownPaths)
    {
        var path = CombinePath(parentPath, transform.name);
        knownPaths.Add(path);

        for (var i = 0; i < transform.childCount; i++)
            RegisterPathsRecursive(transform.GetChild(i), path, knownPaths);
    }

    static Transform FindTransformByPath(Scene scene, string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var parts = path.Split('/');
        if (parts.Length == 0)
            return null;

        GameObject current = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && root.name == parts[0])
            {
                current = root;
                break;
            }
        }

        if (current == null)
            return null;

        for (var i = 1; i < parts.Length; i++)
        {
            var child = current.transform.Find(parts[i]);
            if (child == null)
                return null;

            current = child.gameObject;
        }

        return current.transform;
    }

    static string CombinePath(string parentPath, string name) =>
        string.IsNullOrEmpty(parentPath) ? name : parentPath + "/" + name;
}
