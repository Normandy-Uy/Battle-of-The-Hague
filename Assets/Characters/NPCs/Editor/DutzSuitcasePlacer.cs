using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Places suitcases on Level 1 along the highway track center.</summary>
public static class DutzSuitcasePlacer
{
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string SuitcaseFbxPath = "Assets/suitcase.fbx";
    const string SuitcasePrefabPath = "Assets/Characters/Level02/Prefabs/DutzSuitcase.prefab";
    const string SuitcaseMaterialPath = "Assets/Characters/Level02/Materials/DutzSuitcaseRed.mat";
    const string CoinsRootName = "DutzGoldCoins";
    const string CoinPrefix = "DutzGoldCoin_";
    const string SuitcasesRootName = "DutzSuitcases";
    const string SuitcasePrefix = "DutzSuitcase_";
    const float SuitcaseWorldScale = 8f;
    static readonly Vector3 SuitcaseEuler = new Vector3(270f, 0f, 0f);

    /// <summary>Batch: -executeMethod DutzSuitcasePlacer.DistributeOnLevel02</summary>
    public static void DistributeOnLevel02() => DistributeOnLevel02(log: true);

    public static void DistributeLevel1FromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 1 Suitcases", "Exit Play mode first.", "OK");
            return;
        }

        if (!DistributeLevel1Silent(log: true, showDialogs: true))
        {
            EditorUtility.DisplayDialog(
                "Level 1 Suitcases",
                "Could not redistribute suitcases on Dutz_Level01. Check the Console.",
                "OK");
        }
    }

    /// <summary>Batch: -executeMethod DutzSuitcasePlacer.DistributeLevel1Batch</summary>
    public static void DistributeLevel1Batch() => DistributeLevel1Silent(log: true, showDialogs: false);

    public static bool DistributeLevel1Silent(bool log, bool showDialogs)
    {
        if (EditorApplication.isPlaying)
            return false;

        if (!DistributeOnLevel02(log))
            return false;

        if (showDialogs)
        {
            EditorUtility.DisplayDialog(
                "Level 1 Suitcases",
                "50 suitcases were placed along the track center ahead of Player1 on Dutz_Level01.",
                "OK");
        }

        return true;
    }

    public static bool DistributeOnLevel02(bool log)
    {
        var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        var positions = DutzCollectibleTrackPlacer.BuildCollectiblePositions(out var diagnostics, log);
        if (positions.Count == 0)
        {
            Debug.LogError($"[Dutz] Could not build collectible positions for Level 1. {diagnostics}");
            return false;
        }

        var prefab = EnsureSuitcasePrefab();
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing suitcase prefab.");
            return false;
        }

        RemoveExistingGoldCoins();
        RemoveExistingSuitcases();

        var root = EnsureSuitcasesRoot();
        var placed = 0;

        for (var i = 0; i < positions.Count; i++)
        {
            var worldPos = positions[i];
            var suitcase = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(suitcase, "Place Suitcases");
            suitcase.name = $"{SuitcasePrefix}{i + 1:00}";

            suitcase.transform.SetParent(root.transform, true);
            suitcase.transform.position = worldPos;
            suitcase.transform.rotation = Quaternion.Euler(SuitcaseEuler);
            suitcase.transform.localScale = Vector3.one * SuitcaseWorldScale;

            ConfigureSuitcase(suitcase);
            placed++;
        }

        RemoveStraySceneSuitcaseTemplate();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed {placed} suitcase(s) on Level 1 along track center. {diagnostics}");
        }

        return placed == positions.Count;
    }

    static GameObject EnsureSuitcasePrefab()
    {
        EnsureSuitcaseRedMaterial();

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SuitcasePrefabPath);
        if (existing != null)
        {
            ApplySuitcaseMaterialToPrefabAsset();
            return AssetDatabase.LoadAssetAtPath<GameObject>(SuitcasePrefabPath);
        }

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(SuitcaseFbxPath);
        if (fbx == null)
        {
            Debug.LogError("[Dutz] Missing suitcase model: " + SuitcaseFbxPath);
            return null;
        }

        EnsureAssetFolder("Assets/Characters/Level02/Prefabs");

        var temp = Object.Instantiate(fbx);
        temp.name = "DutzSuitcase";
        temp.transform.localScale = Vector3.one * SuitcaseWorldScale;
        temp.transform.rotation = Quaternion.Euler(SuitcaseEuler);
        if (temp.GetComponent<DutzSuitcase>() == null)
            temp.AddComponent<DutzSuitcase>();

        ApplySuitcaseMaterial(temp);

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, SuitcasePrefabPath);
        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();

        return prefab;
    }

    static void EnsureSuitcaseRedMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(SuitcaseMaterialPath) != null)
            return;

        EnsureAssetFolder("Assets/Characters/Level02/Materials");

        var material = new Material(Shader.Find("Standard"))
        {
            name = "DutzSuitcaseRed",
            color = new Color(0.82f, 0.08f, 0.08f, 1f),
        };
        material.SetFloat("_Glossiness", 0.35f);

        AssetDatabase.CreateAsset(material, SuitcaseMaterialPath);
        AssetDatabase.SaveAssets();
    }

    static void ApplySuitcaseMaterialToPrefabAsset()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(SuitcaseMaterialPath);
        if (material == null)
            return;

        var root = PrefabUtility.LoadPrefabContents(SuitcasePrefabPath);
        if (root == null)
            return;

        ApplySuitcaseMaterial(root);
        PrefabUtility.SaveAsPrefabAsset(root, SuitcasePrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }

    static void ApplySuitcaseMaterial(GameObject root)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(SuitcaseMaterialPath);
        if (material == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var sharedMaterials = renderer.sharedMaterials;
            for (var i = 0; i < sharedMaterials.Length; i++)
                sharedMaterials[i] = material;
            renderer.sharedMaterials = sharedMaterials;
        }
    }

    static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static GameObject EnsureSuitcasesRoot()
    {
        var root = GameObject.Find(SuitcasesRootName);
        if (root != null)
            return root;

        root = new GameObject(SuitcasesRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Suitcases Root");
        return root;
    }

    static void RemoveExistingGoldCoins()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectByPrefix(root, CoinPrefix, toRemove);

        var coinsRoot = GameObject.Find(CoinsRootName);
        if (coinsRoot != null)
            toRemove.Add(coinsRoot);

        foreach (var go in toRemove.Distinct())
            Undo.DestroyObjectImmediate(go);
    }

    static void RemoveExistingSuitcases()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectByPrefix(root, SuitcasePrefix, toRemove);

        var suitcasesRoot = GameObject.Find(SuitcasesRootName);
        if (suitcasesRoot != null)
            toRemove.Add(suitcasesRoot);

        foreach (var go in toRemove.Distinct())
            Undo.DestroyObjectImmediate(go);
    }

    static void CollectByPrefix(GameObject go, string prefix, List<GameObject> list)
    {
        if (go.name.StartsWith(prefix, System.StringComparison.Ordinal))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectByPrefix(child.gameObject, prefix, list);
    }

    static void RemoveStraySceneSuitcaseTemplate()
    {
        var stray = GameObject.Find("suitcase");
        if (stray != null && stray.transform.parent == null)
            Undo.DestroyObjectImmediate(stray);
    }

    static void ConfigureSuitcase(GameObject go)
    {
        if (go.GetComponent<DutzSuitcase>() == null)
            Undo.AddComponent<DutzSuitcase>(go);

        DutzCollectibleTrackPlacer.WriteSpawnPose(go.GetComponent<DutzSuitcase>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
    }
}
