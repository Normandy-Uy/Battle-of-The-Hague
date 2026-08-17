using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Restores Level07 Highway Bridge 1 health potions (Seg01_*) from the Level03 recovery JSON.
/// </summary>
public static class DutzLevel07Bridge1HealthPotionRestorer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string PotionsRootName = "DutzHealthPotions";
    const string PotionPrefabPath = "Assets/Characters/Level03/Prefabs/DutzHealthPotion.prefab";
    const string RecoveryJsonPath = "Assets/Characters/Level03/DutzHealthPotionRecovery.json";
    const string Bridge1Name = "Highway Bridge 1";
    const string Seg01Prefix = "DutzHealthPotion_Seg01_";
    static readonly Vector3 Level07PotionScale = new Vector3(10f, 10f, 10f);

    [MenuItem("Assets/Dutz Authoring/Restore Level07 Bridge1 Health Potions")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Restore Level07 Bridge1 Health Potions requires Edit Mode.");
            return;
        }

        if (!RestoreSilent(log: true))
            Debug.LogError("[Dutz] Failed to restore Level07 Bridge 1 health potions.");
    }

    public static bool RestoreSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (!File.Exists(Path.GetFullPath(RecoveryJsonPath)))
        {
            Debug.LogError("[Dutz] Missing recovery JSON: " + RecoveryJsonPath);
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing potion prefab: " + PotionPrefabPath);
            return false;
        }

        var json = File.ReadAllText(Path.GetFullPath(RecoveryJsonPath));
        var recovery = JsonUtility.FromJson<DutzHealthPotionRecovery.RecoveryFile>(json);
        if (recovery?.potions == null || recovery.potions.Length == 0)
        {
            Debug.LogError("[Dutz] Recovery JSON has no potion entries.");
            return false;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var bridge1 = GameObject.Find(Bridge1Name);
        if (bridge1 == null)
        {
            Debug.LogError($"[Dutz] '{Bridge1Name}' not found in Level07.");
            return false;
        }

        var root = EnsurePotionsRoot();
        RemoveExistingSeg01(root);

        var placed = 0;
        foreach (var entry in recovery.potions)
        {
            if (string.IsNullOrEmpty(entry.name)
                || !entry.name.StartsWith(Seg01Prefix, System.StringComparison.Ordinal))
                continue;

            var potion = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(potion, "Restore Bridge1 Health Potions");
            PrefabUtility.UnpackPrefabInstance(
                potion,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);

            potion.name = entry.name;
            potion.transform.SetParent(root.transform, true);

            var pos = entry.position;
            // Snap onto Bridge 1 deck so Level07 slope/height matches.
            var probe = pos;
            probe.y = Mathf.Max(pos.y, bridge1.transform.position.y) + 40f;
            if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, pos.y, null, out var deckY)
                || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
                pos.y = deckY + 1.2f;

            potion.transform.SetPositionAndRotation(pos, Quaternion.Euler(entry.eulerAngles));
            potion.transform.localScale = Level07PotionScale;

            var component = potion.GetComponent<DutzHealthPotion>();
            if (component == null)
                component = potion.AddComponent<DutzHealthPotion>();
            component.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(component);

            DutzHealthPotionSetup.ApplyGreenVisual(potion);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Restored {placed} Bridge 1 (Seg01) health potion(s) on Level07.");

        return placed > 0;
    }

    static GameObject EnsurePotionsRoot()
    {
        var root = GameObject.Find(PotionsRootName);
        if (root != null)
            return root;

        root = new GameObject(PotionsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Health Potions Root");
        return root;
    }

    static void RemoveExistingSeg01(GameObject root)
    {
        var toRemove = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in root.transform)
        {
            if (child != null
                && child.name.StartsWith(Seg01Prefix, System.StringComparison.Ordinal))
                toRemove.Add(child.gameObject);
        }

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);
    }
}
