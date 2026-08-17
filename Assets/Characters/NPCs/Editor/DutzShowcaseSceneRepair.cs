using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Repairs dangling scene serialization (orphaned prefab/coin fragments) in Dutz_Level02.
/// </summary>
public static class DutzShowcaseSceneRepair
{
    public const string Level02ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string CoinsRootName = "DutzGoldCoins";
    static readonly string[] StrayRootNames = { "GoldCoin", "GoldCoins" };

    public static void RepairFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Repair Showcase", "Exit Play mode first.", "OK");
            return;
        }

        if (!Repair(redistributeCoins: false, log: true))
            EditorUtility.DisplayDialog("Repair Showcase", "Scene repair failed. Check the Console.", "OK");
    }

    public static void EnsureEndHouseColliderFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("End House Collider", "Exit Play mode first.", "OK");
            return;
        }

        if (!EnsureEndHouseCollider(log: true))
            EditorUtility.DisplayDialog("End House Collider", "Could not add collider. Check the Console.", "OK");
    }

    public static void FixHierarchyFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Fix Hierarchy", "Exit Play mode first.", "OK");
            return;
        }

        FixHierarchy(log: true);
    }

    /// <summary>Batch: -executeMethod DutzShowcaseSceneRepair.RepairOnShowcase</summary>
    public static void RepairOnShowcase() => Repair(redistributeCoins: false, log: true);

    /// <summary>Batch: -executeMethod DutzShowcaseSceneRepair.FixHierarchyOnShowcase</summary>
    public static void FixHierarchyOnShowcase() => FixHierarchy(log: true);

    /// <summary>Batch: -executeMethod DutzShowcaseSceneRepair.EnsureEndHouseColliderOnShowcase</summary>
    public static void EnsureEndHouseColliderOnShowcase() => EnsureEndHouseCollider(log: true);

    public static bool Repair(bool redistributeCoins, bool log)
    {
        var scene = EditorSceneManager.OpenScene(Level02ScenePath, OpenSceneMode.Single);
        var removed = CleanupOrphanRootObjects();
        EnsureEndHouseCollider(log: false);
        DutzSceneMissingScriptRepair.RepairScene(Level02ScenePath, log: false);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Saved Dutz_Level02 after removing {removed} orphan root object(s).");

        if (!redistributeCoins)
            return true;

        if (!DutzGoldCoinPlacer.DistributeOnShowcase(log))
        {
            if (log)
                Debug.LogWarning("[Dutz] Scene saved, but gold coin redistribution failed.");
            return false;
        }

        if (log)
            Debug.Log("[Dutz] Showcase scene repair complete (hierarchy cleaned + coins redistributed).");

        return true;
    }

    public static bool FixHierarchy(bool log)
    {
        var scene = EditorSceneManager.OpenScene(Level02ScenePath, OpenSceneMode.Single);
        var removed = CleanupOrphanRootObjects();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Showcase hierarchy fixed: removed {removed} orphan root object(s).");

        return true;
    }

    public static int CleanupOrphanRootObjects()
    {
        var coinsRoot = GameObject.Find(CoinsRootName);
        var toRemove = new List<GameObject>();

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (ShouldRemoveOrphanRoot(root, coinsRoot))
                toRemove.Add(root);
        }

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);

        return toRemove.Count;
    }

    static bool ShouldRemoveOrphanRoot(GameObject go, GameObject coinsRoot)
    {
        if (go == null)
            return false;

        foreach (var strayName in StrayRootNames)
        {
            if (go.name == strayName)
                return true;
        }

        if (go.GetComponent<DutzGoldCoin>() != null && go.transform.parent == null)
        {
            if (coinsRoot == null || go.transform != coinsRoot.transform)
                return true;
        }

        return string.IsNullOrWhiteSpace(go.name);
    }

    public static bool EnsureEndHouseCollider(bool log) =>
        EnsureEndHouseColliderOnScene(Level02ScenePath, log);

    public static bool EnsureEndHouseColliderOnLevel02(bool log) =>
        EnsureEndHouseColliderOnScene(DutzShowcaseSceneRepair.Level02ScenePath, log);

    /// <summary>Batch: -executeMethod DutzShowcaseSceneRepair.EnsureEndHouseColliderOnLevel02</summary>
    public static void EnsureEndHouseColliderOnLevel02Batch() => EnsureEndHouseColliderOnLevel02(log: true);

    public static bool EnsureEndHouseColliderOnScene(string scenePath, bool log)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != scenePath)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var house = GameObject.Find(DutzEndHouseCollider.HouseName);
        if (house == null)
        {
            if (log)
                Debug.LogWarning($"[Dutz] {DutzEndHouseCollider.HouseName} not found in scene.");
            return false;
        }

        DutzEndHouseCollider.EnsureMeshCollider(house);

        if (house.GetComponent<DutzEndHouseCollider>() == null)
            house.AddComponent<DutzEndHouseCollider>();

        var marker = house.GetComponent<DutzEndHouseCollider>();
        marker?.RefreshRoofZoneFromHierarchy();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] MeshCollider ensured on {DutzEndHouseCollider.HouseName}.");

        return house.GetComponent<MeshCollider>() != null;
    }
}

/// <summary>Single editor entry point for the force field suit pickup.</summary>
public static class DutzForceFieldSuitPlacer
{
    const string BridgeSegmentName = "Highway Bridge 1";
    const string PickupsRootName = "DutzPickups";

    public static void SetupFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Force Field Suit", "Exit Play mode first.", "OK");
            return;
        }

        if (!SetupOnAllLevels(log: true))
            EditorUtility.DisplayDialog("Force Field Suit", "Could not set up suit on all levels. Check the Console.", "OK");
        else
            EditorUtility.DisplayDialog(
                "Force Field Suit",
                "Force field suit FBX applied on Dutz_Level01, Dutz_Level02, and Dutz_Level03.",
                "OK");
    }

    /// <summary>Batch: -executeMethod DutzForceFieldSuitPlacer.SetupOnAllLevelsBatch</summary>
    public static void SetupOnAllLevelsBatch() => SetupOnAllLevels(log: true);

    public static bool SetupOnAllLevels(bool log)
    {
        if (!DutzForceFieldSuitModelBuilder.SyncAndBuildVisualPrefab(log))
            return false;

        var ok = true;
        ok &= SetupOnScene(DutzLevel02Setup.Level01ScenePath, log);
        ok &= SetupOnScene(DutzLevel02Setup.Level02ScenePath, log);
        ok &= SetupOnScene(DutzLevel02Setup.Level03ScenePath, log);
        return ok;
    }

    static bool SetupOnActiveOrLevel01(bool log)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path == DutzLevel02Setup.Level01ScenePath || scene.path == DutzShowcaseSceneRepair.Level02ScenePath)
            return SetupOnScene(scene.path, log);

        return SetupOnLevel02(log);
    }

    static bool SelectSuitInHierarchy(bool log)
    {
        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        if (suit == null)
            return false;

        Selection.activeGameObject = suit;
        EditorGUIUtility.PingObject(suit);

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        if (log)
            Debug.Log($"[Dutz] Selected {suit.name} under {GetHierarchyPath(suit.transform)}.");

        return true;
    }

    static string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return string.Empty;

        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    /// <summary>Batch: -executeMethod DutzForceFieldSuitPlacer.SetupOnShowcase</summary>
    public static void SetupOnShowcase() => SetupOnShowcase(log: true);

    public static bool SetupOnShowcase(bool log) => SetupOnScene(DutzShowcaseSceneRepair.Level02ScenePath, log);

    /// <summary>Batch: -executeMethod DutzForceFieldSuitPlacer.SetupOnLevel02</summary>
    public static void SetupOnLevel02() => SetupOnLevel02(log: true);

    public static bool SetupOnLevel02(bool log) => SetupOnScene(DutzLevel02Setup.Level01ScenePath, log);

    /// <summary>Batch: -executeMethod DutzForceFieldSuitPlacer.SetupOnLevel03</summary>
    public static void SetupOnLevel03() => SetupOnLevel03(log: true);

    public static bool SetupOnLevel03(bool log) => SetupOnScene(DutzLevel02Setup.Level03ScenePath, log);

    static Vector3? GetExistingSuitAnchorHint()
    {
        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        return suit != null ? suit.transform.position : (Vector3?)null;
    }

    static bool SetupOnScene(string scenePath, bool log)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        var bridge = GameObject.Find(BridgeSegmentName);
        if (bridge == null)
        {
            Debug.LogError("[Dutz] " + BridgeSegmentName + " not found in scene.");
            return false;
        }

        if (!DutzForceFieldSuitPlacement.TryGetTopBarWorldPosition(out var position, GetExistingSuitAnchorHint()))
        {
            Debug.LogError("[Dutz] Could not find top bar position on " + BridgeSegmentName);
            return false;
        }

        var pickupsRoot = EnsurePickupsRoot();
        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        if (suit == null)
        {
            suit = new GameObject(DutzForceFieldSuitPickup.PickupObjectName);
            Undo.RegisterCreatedObjectUndo(suit, "Create Force Field Suit");
        }

        Undo.RecordObject(suit.transform, "Setup Force Field Suit");
        suit.transform.position = position;
        suit.transform.SetParent(pickupsRoot.transform, true);
        suit.transform.localScale = Vector3.one * DutzForceFieldSuitPlacement.SuitWorldScale;

        var vest = suit.transform.Find("VestVisual");
        if (vest != null)
            Undo.DestroyObjectImmediate(vest.gameObject);

        RepairSuitScripts(suit);
        DutzForceFieldSuitSetup.Apply(suit);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SelectSuitInHierarchy(log: false);

        if (log)
            Debug.Log($"[Dutz] Force field suit ready at {suit.transform.position} (scene root, uniform scale).");

        return true;
    }

    static void RepairSuitScripts(GameObject suit) => RepairSuitScriptsPublic(suit);

    public static void RepairSuitScriptsPublic(GameObject suit)
    {
        Undo.RecordObject(suit, "Repair Force Field Suit Scripts");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(suit);

        var wrongObjective = suit.GetComponent<DutzLevelObjective>();
        if (wrongObjective != null)
            Undo.DestroyObjectImmediate(wrongObjective);

        var behaviours = suit.GetComponents<MonoBehaviour>();
        for (var i = behaviours.Length - 1; i >= 0; i--)
        {
            if (behaviours[i] != null)
                Undo.DestroyObjectImmediate(behaviours[i]);
        }

        suit.AddComponent<DutzForceFieldSuitPickup>();
    }

    static GameObject EnsurePickupsRoot()
    {
        var root = GameObject.Find(PickupsRootName);
        if (root != null)
            return root;

        root = new GameObject(PickupsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create DutzPickups");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root;
    }
}

/// <summary>Syncs public/forcefieldsuitpickup.fbx and builds the runtime visual prefab.</summary>
public static class DutzForceFieldSuitModelBuilder
{
    const string SourceFileName = "forcefieldsuitpickup.fbx";
    const string AssetFbxPath = "Assets/Characters/NPCs/Models/forcefieldsuitpickup.fbx";
    const string VisualPrefabPath = "Assets/Resources/DutzForceFieldSuitVisual.prefab";

    /// <summary>Batch: -executeMethod DutzForceFieldSuitModelBuilder.BuildVisualPrefabBatch</summary>
    public static void BuildVisualPrefabBatch() => SyncAndBuildVisualPrefab(log: true);

    public static bool SyncAndBuildVisualPrefab(bool log)
    {
        if (!SyncSourceFbx(log))
            return false;

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AssetFbxPath);
        if (fbx == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing imported force field suit FBX: " + AssetFbxPath);
            return false;
        }

        Directory.CreateDirectory("Assets/Resources");

        var temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        temp.name = "DutzForceFieldSuitVisual";
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        foreach (var col in temp.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, VisualPrefabPath);
        Object.DestroyImmediate(temp);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (prefab == null)
        {
            if (log)
                Debug.LogError("[Dutz] Failed to save force field suit visual prefab.");
            return false;
        }

        if (log)
            Debug.Log("[Dutz] Force field suit visual prefab saved from public/" + SourceFileName);

        return true;
    }

    static bool SyncSourceFbx(bool log)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve project root.");
            return false;
        }

        var source = Path.Combine(projectRoot, "public", SourceFileName);
        if (!File.Exists(source))
        {
            if (log)
                Debug.LogError("[Dutz] Missing public/" + SourceFileName + " — add the FBX and run again.");
            return false;
        }

        Directory.CreateDirectory("Assets/Characters/NPCs/Models");
        File.Copy(source, AssetFbxPath, overwrite: true);
        AssetDatabase.ImportAsset(AssetFbxPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log("[Dutz] Synced public/" + SourceFileName + " -> " + AssetFbxPath);

        return true;
    }
}
