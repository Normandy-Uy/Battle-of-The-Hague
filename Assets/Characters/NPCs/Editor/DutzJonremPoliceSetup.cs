using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes Jonrem Police escorts on Level 01: same march-forward + chase behavior as JONREM.
/// </summary>
public static class DutzJonremPoliceSetup
{
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string MountiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Mountie_Brown.prefab";
    const string PoliceBlueMaterialPath = "Assets/Characters/NPCs/Materials/SimpleCitizens_Mountie_PoliceBlue.mat";

    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Jonrem Police", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyOnLevel01(log: true))
        {
            EditorUtility.DisplayDialog(
                "Jonrem Police",
                "Could not set up Jonrem Police.\n\nEnsure JONREM and mountie police exist on Dutz_Level01.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Jonrem Police",
                "Jonrem Police now march and chase like JONREM.",
                "OK");
        }
    }

    /// <summary>Batch: -executeMethod DutzJonremPoliceSetup.ApplyOnLevel01Batch</summary>
    public static void ApplyOnLevel01Batch() => ApplyOnLevel01(log: true);

    public static bool ApplyOnLevel01(bool log)
    {
        if (!File.Exists(Level01ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level01.unity not found.");
            return false;
        }

        DutzMountiePoliceBlueBuilder.BuildPoliceBlueAssets();

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var jonrem = DutzGiantBossNames.FindJonrem();
        if (jonrem == null)
        {
            Debug.LogError("[Dutz] JONREM not found in Dutz_Level01.");
            return false;
        }

        Undo.RecordObject(jonrem, "Setup Jonrem Police");

        DutzJonremEscortSpawnLock.RestoreAllOnLevel01();

        var police = EnsurePoliceNearJonrem(jonrem);
        if (police.Count == 0)
        {
            Debug.LogError("[Dutz] No Jonrem Police found on Level 01.");
            return false;
        }

        for (var i = 0; i < police.Count; i++)
        {
            var officer = police[i];
            Undo.RecordObject(officer, "Setup Jonrem Police");
            officer.name = $"{DutzGiantBossNames.JonremPolicePrefix} {i + 1}";
            DutzJonremEscortSpawnLock.RestoreEscort(officer);
            DutzJonremPoliceBehavior.ApplyFromJonrem(officer, jonrem, DutzJonremEscortPlacement.TravelForward);
            ApplyPoliceBlueMaterial(officer);
            EditorUtility.SetDirty(officer);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log($"[Dutz] Applied JONREM behavior to {police.Count} Jonrem Police officer(s).");

        return true;
    }

    static System.Collections.Generic.List<GameObject> EnsurePoliceNearJonrem(GameObject jonrem)
    {
        var police = new System.Collections.Generic.List<GameObject>(
            DutzJonremPoliceBehavior.FindJonremPolice());

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MountiePrefabPath);
        if (prefab == null)
            return police;

        while (police.Count < DutzJonremPoliceBehavior.PoliceCount)
        {
            var index = police.Count;
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                break;

            Undo.RegisterCreatedObjectUndo(instance, "Create Jonrem Police");
            instance.name = $"{DutzGiantBossNames.JonremPolicePrefix} {index + 1}";
            instance.transform.localScale = Vector3.one * DutzJonremPoliceBehavior.PoliceScale;
            police.Add(instance);
        }

        return police;
    }

    static void ApplyPoliceBlueMaterial(GameObject root)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(PoliceBlueMaterialPath);
        if (material == null || root == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer == null || renderer.gameObject.name != "SC_Mountie")
                continue;

            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
        }
    }
}
