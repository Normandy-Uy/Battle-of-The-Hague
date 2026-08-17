using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Extracts and restores Level 3 health potions from recovery JSON — Unity Editor API only.</summary>
public static class DutzHealthPotionRecovery
{
    const string PotionsRootName = "DutzHealthPotions";
    const string PotionPrefabPath = "Assets/Characters/Level03/Prefabs/DutzHealthPotion.prefab";
    const string RecoveryJsonPath = "Assets/Characters/Level03/DutzHealthPotionRecovery.json";

    [Serializable]
    public class RecoveryFile
    {
        public RecoveryEntry[] potions = Array.Empty<RecoveryEntry>();
    }

    [Serializable]
    public class RecoveryEntry
    {
        public string name;
        public Vector3 position;
        public Vector3 eulerAngles;
        public Vector3 localScale = Vector3.one;
    }

    /// <summary>Batch: -executeMethod DutzHealthPotionRecovery.ExtractRecoveryJsonBatch</summary>
    public static void ExtractRecoveryJsonBatch() => ExtractRecoveryJsonFromScene(log: true);

    /// <summary>Batch: -executeMethod DutzHealthPotionRecovery.RestoreOnLevel03Batch</summary>
    public static void RestoreOnLevel03Batch() => RestoreOnLevel03(log: true);

    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Restore Health Potions", "Exit Play mode first.", "OK");
            return;
        }

        if (!RestoreOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Restore Health Potions",
                "Could not restore Level 3 health potions. Check the Console.",
                "OK");
        }
    }

    public static bool ExtractRecoveryJsonFromScene(bool log)
    {
        var scenePath = Path.GetFullPath(DutzLevel02Setup.Level03ScenePath);
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level03.unity not found for recovery extract.");
            return false;
        }

        var yaml = File.ReadAllText(scenePath);
        var entries = ParsePotionEntriesFromSceneYaml(yaml);
        if (entries.Count == 0)
        {
            if (log)
                Debug.LogWarning("[Dutz] No health potion PrefabInstance data found in scene YAML.");
            return false;
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        var recovery = new RecoveryFile { potions = entries.ToArray() };
        var json = JsonUtility.ToJson(recovery, true);

        var jsonFullPath = Path.GetFullPath(RecoveryJsonPath);
        Directory.CreateDirectory(Path.GetDirectoryName(jsonFullPath) ?? string.Empty);
        File.WriteAllText(jsonFullPath, json);
        AssetDatabase.Refresh();

        if (log)
            Debug.Log($"[Dutz] Extracted {entries.Count} health potion pose(s) to {RecoveryJsonPath}.");

        return true;
    }

    public static bool RestoreOnLevel03(bool log)
    {
        if (EditorApplication.isPlaying)
            return false;

        if (!File.Exists(Path.GetFullPath(RecoveryJsonPath)))
        {
            if (!ExtractRecoveryJsonFromScene(log))
                return false;
        }

        var recovery = LoadRecoveryFile();
        if (recovery.potions == null || recovery.potions.Length == 0)
        {
            if (log)
                Debug.LogError("[Dutz] Recovery JSON has no potion entries.");
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionPrefabPath);
        if (prefab == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing health potion prefab: " + PotionPrefabPath);
            return false;
        }

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        RemoveExistingPotions();
        var root = EnsurePotionsRoot();
        var placed = 0;

        foreach (var entry in recovery.potions)
        {
            if (string.IsNullOrEmpty(entry.name))
                continue;

            var potion = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(potion, "Restore Health Potions");
            potion.name = entry.name;
            potion.transform.SetParent(root.transform, false);
            potion.transform.localPosition = entry.position;
            potion.transform.localRotation = Quaternion.Euler(entry.eulerAngles);
            potion.transform.localScale = entry.localScale == Vector3.zero ? Vector3.one : entry.localScale;

            PrefabUtility.UnpackPrefabInstance(potion, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var component = potion.GetComponent<DutzHealthPotion>();
            if (component == null)
                component = potion.AddComponent<DutzHealthPotion>();

            component.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(component);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Restored {placed} health potion(s) on Level 3 from {RecoveryJsonPath}.");

        return placed == recovery.potions.Length;
    }

    static RecoveryFile LoadRecoveryFile()
    {
        var jsonFullPath = Path.GetFullPath(RecoveryJsonPath);
        if (!File.Exists(jsonFullPath))
            return new RecoveryFile();

        var json = File.ReadAllText(jsonFullPath);
        return JsonUtility.FromJson<RecoveryFile>(json) ?? new RecoveryFile();
    }

    static List<RecoveryEntry> ParsePotionEntriesFromSceneYaml(string yaml)
    {
        var entries = new List<RecoveryEntry>();
        var blocks = yaml.Split(new[] { "--- !u!1001 &" }, StringSplitOptions.None);

        foreach (var block in blocks)
        {
            var name = ReadStringProperty(block, "m_Name");
            if (string.IsNullOrEmpty(name) || !name.StartsWith(DutzHealthPotion.PotionPrefix, StringComparison.Ordinal))
                continue;

            var entry = new RecoveryEntry
            {
                name = name,
                position = ReadVector3(block, "spawnPose.position", "m_LocalPosition"),
                eulerAngles = ReadEuler(block),
                localScale = ReadVector3(block, "spawnPose.localScale", "m_LocalScale")
            };

            if (entry.localScale == Vector3.zero)
                entry.localScale = Vector3.one;

            entries.Add(entry);
        }

        return entries;
    }

    static Vector3 ReadVector3(string block, string primaryPrefix, string fallbackPrefix)
    {
        float x = ReadFloat(block, primaryPrefix + ".x", fallbackPrefix + ".x");
        float y = ReadFloat(block, primaryPrefix + ".y", fallbackPrefix + ".y");
        float z = ReadFloat(block, primaryPrefix + ".z", fallbackPrefix + ".z");
        return new Vector3(x, y, z);
    }

    static Vector3 ReadEuler(string block)
    {
        if (TryReadFloat(block, "spawnPose.eulerAngles.y", out _))
        {
            return new Vector3(
                ReadFloat(block, "spawnPose.eulerAngles.x", null),
                ReadFloat(block, "spawnPose.eulerAngles.y", null),
                ReadFloat(block, "spawnPose.eulerAngles.z", null));
        }

        var rx = ReadFloat(block, "m_LocalRotation.x", null);
        var ry = ReadFloat(block, "m_LocalRotation.y", null);
        var rz = ReadFloat(block, "m_LocalRotation.z", null);
        var rw = ReadFloat(block, "m_LocalRotation.w", null);
        if (Mathf.Approximately(rw, 0f) && Mathf.Approximately(rx, 0f) && Mathf.Approximately(ry, 0f) && Mathf.Approximately(rz, 0f))
            rw = 1f;

        return Quaternion.Normalize(new Quaternion(rx, ry, rz, rw)).eulerAngles;
    }

    static float ReadFloat(string block, string primaryProp, string fallbackProp)
    {
        if (TryReadFloat(block, primaryProp, out var value))
            return value;

        if (!string.IsNullOrEmpty(fallbackProp) && TryReadFloat(block, fallbackProp, out value))
            return value;

        return 0f;
    }

    static bool TryReadFloat(string block, string propertyPath, out float value)
    {
        value = 0f;
        if (string.IsNullOrEmpty(propertyPath))
            return false;

        var pattern = "propertyPath: " + Regex.Escape(propertyPath) + @"\r?\n\s+value: ([^\r\n]+)";
        var match = Regex.Match(block, pattern);
        if (!match.Success)
            return false;

        var raw = match.Groups[1].Value.Trim();
        if (raw == "-0")
            raw = "0";

        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    static string ReadStringProperty(string block, string propertyPath)
    {
        var pattern = "propertyPath: " + Regex.Escape(propertyPath) + @"\r?\n\s+value: ([^\r\n]+)";
        var match = Regex.Match(block, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    static void RemoveExistingPotions()
    {
        foreach (var potion in UnityEngine.Object.FindObjectsOfType<DutzHealthPotion>(true))
        {
            if (potion == null)
                continue;

            Undo.DestroyObjectImmediate(potion.gameObject);
        }

        var root = GameObject.Find(PotionsRootName);
        if (root != null)
            Undo.DestroyObjectImmediate(root);
    }

    static GameObject EnsurePotionsRoot()
    {
        var root = new GameObject(PotionsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Restore Health Potions");
        return root;
    }
}
