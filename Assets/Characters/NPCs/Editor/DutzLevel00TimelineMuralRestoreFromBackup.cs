using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores DutzLevel00TimelineMurals from Back Up New Unity Project (textures + hierarchy).
/// </summary>
public static class DutzLevel00TimelineMuralRestoreFromBackup
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string TimelineRootName = "DutzLevel00TimelineMurals";
    const string BackupFolderName = "Back Up New Unity Project";

    /// <summary>Batch: -executeMethod DutzLevel00TimelineMuralRestoreFromBackup.RestoreBatch</summary>
    public static void RestoreBatch() => RestoreFromBackup(log: true);

    [MenuItem("Assets/Dutz Authoring/Restore Timeline Murals From Backup")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Restore Timeline Murals", "Exit Play mode first.", "OK");
            return;
        }

        RestoreFromBackup(log: true);
    }

    public static bool RestoreFromBackup(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var backupRoot = GetBackupProjectRoot();
        if (string.IsNullOrEmpty(backupRoot) || !Directory.Exists(backupRoot))
        {
            if (log)
                Debug.LogError("[Dutz] Backup project not found. Expected sibling folder: " + BackupFolderName);
            return false;
        }

        var copiedTextures = CopyTimelineTexturesFromBackup(backupRoot, log);
        var backupSceneAssetPath = CopyBackupSceneIntoProject(backupRoot, log);
        if (string.IsNullOrEmpty(backupSceneAssetPath))
            return false;

        DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth++;
        try
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.path != Level00ScenePath)
                activeScene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);

            Physics.SyncTransforms();

            var existing = GameObject.Find(TimelineRootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var backupScene = EditorSceneManager.OpenScene(backupSceneAssetPath, OpenSceneMode.Additive);
            GameObject backupTimelineRoot = null;
            foreach (var root in backupScene.GetRootGameObjects())
            {
                if (root != null && root.name == TimelineRootName)
                {
                    backupTimelineRoot = root;
                    break;
                }
            }

            if (backupTimelineRoot == null)
            {
                EditorSceneManager.CloseScene(backupScene, true);
                if (log)
                    Debug.LogError("[Dutz] " + TimelineRootName + " not found in backup scene.");
                return false;
            }

            var clone = Object.Instantiate(backupTimelineRoot);
            clone.name = TimelineRootName;
            SceneManager.MoveGameObjectToScene(clone, activeScene);

            EditorSceneManager.CloseScene(backupScene, true);

            RebindTimelineMaterials(clone);
            DutzMuralBumpMessage.EnsureLevel00MuralsInScene(log);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene, Level00ScenePath);
            AssetDatabase.SaveAssets();

            if (log)
            {
                var muralCount = 0;
                foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
                {
                    if (transform != null && transform.name.StartsWith("TimelineMural_", System.StringComparison.Ordinal))
                        muralCount++;
                }

                Debug.Log(
                    $"[Dutz] Restored {TimelineRootName} from backup " +
                    $"({muralCount} mural(s), textures copied: {copiedTextures}).");
            }

            return true;
        }
        finally
        {
            DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth =
                Mathf.Max(0, DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth - 1);
        }
    }

    static string GetBackupProjectRoot()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        var sibling = Path.GetFullPath(Path.Combine(projectRoot, "..", BackupFolderName, "New Unity Project"));
        return Directory.Exists(sibling) ? sibling : null;
    }

    static int CopyTimelineTexturesFromBackup(string backupRoot, bool log)
    {
        var sourceDir = Path.Combine(
            backupRoot,
            "Assets",
            "Characters",
            "HighwayBillboards",
            "Textures",
            "Level00Timeline");

        if (!Directory.Exists(sourceDir))
            return 0;

        var destDir = Path.Combine(
            Application.dataPath,
            "Characters",
            "HighwayBillboards",
            "Textures",
            "Level00Timeline");

        Directory.CreateDirectory(destDir);

        var copied = 0;
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            if (string.IsNullOrEmpty(name)
                || (!name.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)))
                continue;

            File.Copy(file, Path.Combine(destDir, name), overwrite: true);
            copied++;
        }

        if (copied > 0)
            AssetDatabase.Refresh();

        if (log && copied > 0)
            Debug.Log($"[Dutz] Copied {copied} timeline texture file(s) from backup.");

        return copied;
    }

    static string CopyBackupSceneIntoProject(string backupRoot, bool log)
    {
        var sourceScene = Path.Combine(backupRoot, "Assets", "Scenes", "Dutz_Level00.unity");
        if (!File.Exists(sourceScene))
        {
            if (log)
                Debug.LogError("[Dutz] Backup scene not found: " + sourceScene);
            return null;
        }

        var tempDir = Path.Combine(Application.dataPath, "Editor", "TempBackupScenes");
        Directory.CreateDirectory(tempDir);

        var destScene = Path.Combine(tempDir, "Dutz_Level00_BackupReference.unity");
        File.Copy(sourceScene, destScene, overwrite: true);

        var sourceMeta = sourceScene + ".meta";
        var destMeta = destScene + ".meta";
        if (File.Exists(sourceMeta))
            File.Copy(sourceMeta, destMeta, overwrite: true);

        AssetDatabase.Refresh();
        return "Assets/Editor/TempBackupScenes/Dutz_Level00_BackupReference.unity";
    }

    static void RebindTimelineMaterials(GameObject timelineRoot)
    {
        if (timelineRoot == null)
            return;

        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (materialTemplate == null)
            return;

        var textures = DutzLevel00TimelineMuralBuilder.LoadSyncedTextures();
        if (textures.Count == 0)
            return;

        foreach (var transform in timelineRoot.GetComponentsInChildren<Transform>(true))
        {
            if (transform == null || !transform.name.StartsWith("TimelineMural_", System.StringComparison.Ordinal))
                continue;

            if (!TryParseMuralIndex(transform.name, out var muralIndex))
                continue;

            var textureIndex = muralIndex - 1;
            if (textureIndex < 0 || textureIndex >= textures.Count)
                continue;

            var texture = textures[textureIndex];
            if (texture == null)
                continue;

            var renderer = transform.GetComponent<MeshRenderer>();
            if (renderer == null)
                continue;

            var material = new Material(materialTemplate);
            material.name = $"Level00Timeline_{texture.name}";
            material.mainTexture = texture;
            renderer.sharedMaterial = material;
        }
    }

    static bool TryParseMuralIndex(string objectName, out int muralIndex)
    {
        muralIndex = 0;
        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("TimelineMural_", System.StringComparison.Ordinal))
            return false;

        var underscore = objectName.IndexOf('_', "TimelineMural_".Length);
        if (underscore < 0)
            return false;

        var indexText = objectName.Substring("TimelineMural_".Length, underscore - "TimelineMural_".Length);
        return int.TryParse(indexText, out muralIndex);
    }
}
