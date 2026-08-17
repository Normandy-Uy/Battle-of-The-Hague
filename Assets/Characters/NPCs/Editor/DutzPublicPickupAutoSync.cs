using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps FBX pickup assets synced from public/ and upgrades open scenes silently.
/// No Tools/Dutz menu required — runs automatically when the Unity editor loads or a level scene opens.
/// </summary>
[InitializeOnLoad]
public static class DutzPublicPickupAutoSync
{
    const string Level00Path = "Assets/Scenes/Dutz_Level00.unity";
    const string Level01Path = "Assets/Scenes/Dutz_Level01.unity";
    const string Level02Path = "Assets/Scenes/Dutz_Level02.unity";
    const string Level03Path = "Assets/Scenes/Dutz_Level03.unity";
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";

    static bool assetsSyncedThisSession;

    static DutzPublicPickupAutoSync()
    {
        EditorApplication.delayCall += SyncPermanentPickupAssetsOnce;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    static string pickupSyncScenePath;

    static void OnAfterAssemblyReload()
    {
        pickupSyncScenePath = null;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (DutzHighwayPhotoBillboardPlacer.ShouldSkipSceneOpenedHandler)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                return;

            TrySnapLevel03TrackGiantsInOpenScene(scene);

            if (pickupSyncScenePath == scene.path)
                return;

            pickupSyncScenePath = scene.path;

            try
            {
                UpgradePickupsInOpenScene(scene, log: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Dutz] Pickup auto-sync after reload failed: " + ex.Message);
            }
        };
    }

    static void TrySnapLevel03TrackGiantsInOpenScene(Scene scene)
    {
        if (scene.path != Level03Path)
            return;

        if (!DutzLevel03Setup.EnsureTrackGiantsSnappedOnOpenScene(log: false))
            return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SyncPermanentPickupAssetsOnce()
    {
        if (assetsSyncedThisSession || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        assetsSyncedThisSession = true;
        SyncPermanentPickupAssets(log: false);
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (DutzHighwayPhotoBillboardPlacer.ShouldSkipSceneOpenedHandler)
            return;

        pickupSyncScenePath = null;
        var scenePath = scene.path;
        EditorApplication.delayCall += () => UpgradePickupsInOpenSceneDeferred(scenePath);
    }

    static void UpgradePickupsInOpenSceneDeferred(string scenePath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != scenePath)
            return;

        try
        {
            if (scenePath == Level00Path
                || scenePath == Level01Path
                || scenePath == Level02Path
                || scenePath == Level03Path
                || scenePath == Level07Path)
            {
                DutzGameMusicSetup.SyncVictoryVideoFile();
                DutzGameMusicSetup.SyncVictorySelfieTemplate();
            }

            UpgradePickupsInOpenScene(scene, log: false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Dutz] Pickup auto-sync failed for " + scenePath + ": " + ex.Message);
        }
    }

    /// <summary>Batch: -executeMethod DutzPublicPickupAutoSync.SyncPermanentPickupAssetsBatch</summary>
    public static void SyncPermanentPickupAssetsBatch() => SyncPermanentPickupAssets(log: true);

    public static void SyncPermanentPickupAssets(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var any = false;
        any |= DutzForceFieldSuitModelBuilder.SyncAndBuildVisualPrefab(log);
        any |= DutzHealthPotionModelBuilder.SyncAndBuildSharedAssets(log);
        any |= DutzSuperPunchModelBuilder.SyncAndBuildSharedAssets(log);
        any |= DutzSuperJumpModelBuilder.SyncSharedAssets(log);
        any |= DutzLevel07SuperJumpPlacer.SyncKangarooAssets(log);
        if (DutzLevel00TimelineMuralBuilder.SyncPhotos(log: false) > 0)
            any = true;
        if (DutzLevel00EdsaMuralBuilder.NeedsTextureResync()
            && DutzLevel00EdsaMuralBuilder.ResyncTextures(log: false, force: false) > 0)
            any = true;

        if (log && any)
            Debug.Log("[Dutz] Permanent pickup FBX assets synced from public/.");
    }

    public static void UpgradePickupsInOpenScene(Scene scene, bool log)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            return;

        // Level 00 is hand-authored (EDSA crowd, crossroad spawns, murals). Never auto-alter on open/recompile.
        if (scene.path == Level00Path)
            return;

        var changed = false;

        if (scene.path == Level00Path || scene.path == Level01Path || scene.path == Level02Path || scene.path == Level03Path)
        {
            changed |= DutzSceneMissingScriptRepair.RepairOpenLevelScene(scene, log: log);
            changed |= UpgradeForceFieldSuitInScene(log);
        }

        if (scene.path == Level02Path)
            changed |= EnsureLevel02EndGoalContent(log);

        if (scene.path == Level00Path || scene.path == Level03Path)
        {
            changed |= UpgradeLevel03HealthPotionsInScene(log);
            changed |= UpgradeSuperPunchInScene(log);
        }

        if (scene.path == Level03Path)
            changed |= DutzLevel03Setup.EnsureTrackGiantsSnappedOnOpenScene(log);

        if (scene.path == Level00Path)
        {
            changed |= UpgradeSuperJumpInScene(log);
            if (DutzLevel00EdsaMuralBuilder.NeedsTextureResync())
                changed |= DutzLevel00EdsaMuralBuilder.ResyncTextures(log, force: false) > 0;
            changed |= DutzLevel00CrowdWalkerPlacer.EnsureOnOpenScene(log);
            changed |= DutzLevel00CrowdCitizensPlacer.EnsureOnOpenScene(log);
            changed |= DutzLevel00CrossroadChaseSpawnsPlacer.EnsureOnOpenScene(log);
            changed |= DutzLevel00RallyPlacardPlacer.EnsureOnOpenScene(log);
            changed |= DutzLevel00StaticCrowdColliders.EnsureInOpenScene(log);
        }
        else if (scene.path == Level07Path)
        {
            // Super Jump / Force Field Suit: never auto-reposition or force scale —
            // authored scene transforms must stick after Save.
            changed |= DutzLevel07SuperJumpPlacer.EnsureOnOpenScene(log);
        }
        else if (scene.path == Level01Path || scene.path == Level02Path || scene.path == Level03Path)
            changed |= DutzSuperJumpPlacer.RemoveFromSceneIfPresent(scene, log);

        if (scene.path == Level01Path)
            changed |= DutzRobinCarMuralPlacer.EnsureOnOpenScene(log);

        if (!changed)
            return;

        EditorSceneManager.MarkSceneDirty(scene);

        // Level 00 has heavy manual hierarchy work (crossroad spawns, murals, crowd).
        // Never silently SaveScene here — script recompiles were overwriting author edits on disk.
        if (scene.path == Level00Path)
        {
            if (log)
                Debug.Log("[Dutz] Level 00 pickup sync applied — review Hierarchy and save with Ctrl+S when ready.");
            return;
        }

        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Saved permanent pickup visuals in " + scene.path);
    }

    static bool HasTimelineMuralsRoot() => GameObject.Find(DutzLevel00TimelineMuralPlacer.RootName) != null;

    static bool UpgradeForceFieldSuitInScene(bool log)
    {
        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        if (suit == null)
            return false;

        var changed = false;
        if (suit.GetComponent<DutzForceFieldSuitPickup>() == null
            || suit.GetComponent<DutzLevelObjective>() != null)
        {
            DutzForceFieldSuitPlacer.RepairSuitScriptsPublic(suit);
            changed = true;
        }

        var hadLegacy = suit.transform.Find("VestVisual") != null;
        var hadModel = suit.transform.Find(DutzForceFieldSuitSetup.SuitModelVisualName) != null;
        if (hadLegacy || !hadModel)
        {
            DutzForceFieldSuitSetup.Apply(suit);
            changed = true;
        }

        if (suit.GetComponent<DutzForceField>() == null)
        {
            Undo.AddComponent<DutzForceField>(suit);
            changed = true;
        }

        DutzForceField.StripFromPlayers();
        return changed;
    }

    static bool UpgradeLevel03HealthPotionsInScene(bool log) =>
        DutzHealthPotionModelBuilder.EnsureOnOpenLevel03(log);

    static bool UpgradeSuperPunchInScene(bool log) =>
        DutzSuperPunchModelBuilder.EnsureOnOpenLevel03(log);

    static bool UpgradeSuperJumpInScene(bool log) =>
        DutzSuperJumpPlacer.EnsureOnOpenScene(log);

    static bool EnsureLevel02EndGoalContent(bool log)
    {
        var changed = false;
        var pole = GameObject.Find(DutzFlagPoleGoal.FlagPoleName);
        if (pole != null)
        {
            Undo.DestroyObjectImmediate(pole);
            changed = true;
        }

        if (DutzRobinCarMuralPlacer.RemoveFromLevel02IfPresent(SceneManager.GetActiveScene(), log: false))
            changed = true;

        if (DutzShowcaseSceneRepair.EnsureEndHouseColliderOnScene(Level02Path, log: false))
            changed = true;

        if (log && changed)
            Debug.Log("[Dutz] Level 2 end goal restored: roof win house, no flagpole; Robin Car mural removed (Level 1 only).");

        return changed;
    }
}

/// <summary>
/// Picks 7 SimpleCitizens in Level 00 and gives them highway march at speed 3.
/// </summary>
public static class DutzLevel00CrowdWalkerPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string RootName = "Level00CrowdWalkers";
    public const int WalkerCount = 17;
    const int RandomSeed = 20250703;

    /// <summary>Batch: -executeMethod DutzLevel00CrowdWalkerPlacer.AssignOnLevel00Batch</summary>
    public static void AssignOnLevel00Batch() => RepairCrowdWalkers(log: true, force: true);

    /// <summary>Batch: -executeMethod DutzLevel00CrowdWalkerPlacer.RepairOnLevel00Batch</summary>
    public static void RepairOnLevel00Batch()
    {
        RepairCrowdWalkers(log: true, force: true);
    }

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var changed = false;
        if (RepairCrowdWalkersIfNeeded(log))
            changed = true;
        else
            changed |= ResyncWalkSettingsIfNeeded(log);

        changed |= DutzLevel00StaticCrowdColliders.EnsureInOpenScene(log);
        return changed;
    }

    public static bool RepairCrowdWalkersIfNeeded(bool log)
    {
        if (!NeedsRepair())
            return false;

        return RepairCrowdWalkers(log, force: false);
    }

    static bool NeedsRepair()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var expected = new System.Collections.Generic.HashSet<string>(ExpectedWalkerNames, System.StringComparer.Ordinal);
        var root = GameObject.Find(RootName);

        if (root == null || root.transform.childCount != WalkerCount)
            return true;

        foreach (Transform child in root.transform)
        {
            if (!expected.Contains(child.name))
                return true;
        }

        foreach (var name in ExpectedWalkerNames)
        {
            var go = FindSimpleCitizenByName(name);
            if (go == null)
                return true;

            if (go.GetComponent<DutzLevel00CrowdWalker>() == null)
                return true;

            if (go.GetComponent<DutzLevel00CrowdWalkerPhysics>() == null)
                return true;

            if (go.GetComponent<SimpleCitizensNpcPhysics>() != null)
                return true;

            if (go.transform.parent != root.transform)
                return true;
        }

        foreach (var walker in UnityEngine.Object.FindObjectsByType<DutzLevel00CrowdWalker>(FindObjectsSortMode.None))
        {
            if (!expected.Contains(walker.gameObject.name))
                return true;
        }

        return false;
    }

    public static bool RepairCrowdWalkers(bool log, bool force = false)
    {
        if (!force && !NeedsRepair())
            return false;

        if (!System.IO.File.Exists(Level00ScenePath))
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

        var expected = new System.Collections.Generic.HashSet<string>(ExpectedWalkerNames, System.StringComparer.Ordinal);
        var root = EnsureConfigRoot();

        foreach (var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            var go = animator.gameObject;
            if (go == null || !go.name.StartsWith("SimpleCitizens_", System.StringComparison.Ordinal))
                continue;

            if (!expected.Contains(go.name))
                StripWalkerComponents(go);
        }

        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (expected.Contains(child.name))
                continue;

            Undo.SetTransformParent(child, null, "Repair Level 00 Crowd Walkers");
            StripWalkerComponents(child.gameObject);
        }

        var repaired = 0;
        foreach (var name in ExpectedWalkerNames)
        {
            var go = FindSimpleCitizenByName(name);
            if (go == null)
            {
                if (log)
                    Debug.LogError("[Dutz] Missing Level 00 crowd walker candidate: " + name);
                continue;
            }

            Undo.RegisterCompleteObjectUndo(go, "Repair Level 00 Crowd Walkers");
            StripWalkerComponents(go);

            if (go.GetComponent<DutzLevel00CrowdWalker>() == null)
                Undo.AddComponent<DutzLevel00CrowdWalker>(go);

            go.GetComponent<DutzLevel00CrowdWalker>().ApplyWalkSettings();
            StripHippieAi(go);

            if (go.transform.parent != root)
                Undo.SetTransformParent(go.transform, root, "Repair Level 00 Crowd Walkers");

            repaired++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        SaveLevel00SceneIfAllowed(scene, log);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Repaired {repaired}/{WalkerCount} Level 00 crowd walker(s) under {RootName}: " +
                string.Join(", ", ExpectedWalkerNames));
        }

        return repaired == WalkerCount;
    }

    static System.Collections.Generic.HashSet<string> BuildExpectedWalkerSet(Scene scene)
    {
        return new System.Collections.Generic.HashSet<string>(ExpectedWalkerNames, System.StringComparer.Ordinal);
    }

    static GameObject FindSimpleCitizenByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        foreach (var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            var go = animator.gameObject;
            if (go != null && go.name == objectName && IsCrowdCandidate(go))
                return go;
        }

        return null;
    }

    public static bool OrganizeWalkersIfNeeded(bool log)
    {
        if (!NeedsRepair())
            return false;

        return RepairCrowdWalkers(log);
    }

    static bool PruneNonWalkersFromRoot(
        Transform root,
        System.Collections.Generic.HashSet<string> expectedNames,
        bool log)
    {
        var changed = false;

        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (expectedNames.Contains(child.name))
                continue;

            Undo.SetTransformParent(child, null, "Prune non-walkers from Level 00 Crowd Walkers");
            StripWalkerComponents(child.gameObject);
            changed = true;

            if (log)
                Debug.LogWarning($"[Dutz] Removed non-walker '{child.name}' from {RootName}.");
        }

        return changed;
    }

    static bool ResyncWalkSettingsIfNeeded(bool log)
    {
        var walkers = UnityEngine.Object.FindObjectsByType<DutzLevel00CrowdWalker>(FindObjectsSortMode.None);
        if (walkers.Length != WalkerCount)
            return false;

        var needsResync = false;
        foreach (var walker in walkers)
        {
            if (DutzLevel00CrowdWalker.NeedsSettingsResync(walker))
                needsResync = true;
        }

        if (!needsResync)
            return false;

        foreach (var walker in walkers)
        {
            Undo.RegisterCompleteObjectUndo(walker.gameObject, "Resync Level 00 Crowd Walkers");
            walker.ApplyWalkSettings();
        }

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        SaveLevel00SceneIfAllowed(scene, log);

        if (log)
            Debug.Log("[Dutz] Resynced Level 00 crowd walker settings from each walker's scene orientation.");

        return true;
    }

    static void SaveLevel00SceneIfAllowed(Scene scene, bool log)
    {
        if (!scene.IsValid() || scene.path != Level00ScenePath)
        {
            EditorSceneManager.SaveScene(scene);
            return;
        }

        if (log)
            Debug.Log("[Dutz] Level 00 crowd walker changes applied — save with Ctrl+S when ready.");
    }

    public static bool NeedsAssignment()
    {
        var walkers = UnityEngine.Object.FindObjectsByType<DutzLevel00CrowdWalker>(FindObjectsSortMode.None);
        if (walkers.Length != WalkerCount)
            return true;

        foreach (var walker in walkers)
        {
            var physics = walker.GetComponent<DutzLevel00CrowdWalkerPhysics>();
            if (physics == null
                || !Mathf.Approximately(
                    physics.GetWalkSpeed(),
                    DutzLevel00CrowdWalker.GetExpectedWalkSpeed(walker.gameObject.name)))
                return true;

            if (walker.GetComponent<SimpleCitizensNpcPhysics>() != null)
                return true;

            if (DutzLevel00CrowdWalker.NeedsSettingsResync(walker))
                return true;
        }

        return false;
    }

    public static bool AssignWalkers(bool log) => RepairCrowdWalkers(log, force: true);

    static Transform EnsureConfigRoot()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            return existing.transform;

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Assign Level 00 Crowd Walkers");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    static System.Collections.Generic.List<GameObject> FindCrowdCandidates(Scene scene)
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
            CollectSimpleCitizens(root.transform, list);

        list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return list;
    }

    static void CollectSimpleCitizens(Transform transform, System.Collections.Generic.List<GameObject> list)
    {
        var go = transform.gameObject;
        if (IsCrowdCandidate(go))
            list.Add(go);

        for (var i = 0; i < transform.childCount; i++)
            CollectSimpleCitizens(transform.GetChild(i), list);
    }

    static bool IsCrowdCandidate(GameObject go)
    {
        if (go == null || go.GetComponent<DutzPlayerController>() != null)
            return false;

        var name = go.name;
        if (string.IsNullOrEmpty(name) || !name.StartsWith("SimpleCitizens_", System.StringComparison.Ordinal))
            return false;

        return go.GetComponent<Animator>() != null;
    }

    static void StripWalkerComponents(GameObject go)
    {
        foreach (var walker in go.GetComponents<DutzLevel00CrowdWalker>())
            Undo.DestroyObjectImmediate(walker);

        foreach (var march in go.GetComponents<DutzLevel00CrowdWalkerPhysics>())
            Undo.DestroyObjectImmediate(march);

        var chasePhysics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (chasePhysics != null)
            Undo.DestroyObjectImmediate(chasePhysics);

        StripHippieAi(go);
    }

    static readonly string[] ExpectedWalkerNames =
    {
        "SimpleCitizens_Clown_Black",
        "SimpleCitizens_Clown_Brown",
        "SimpleCitizens_Emo_White",
        "SimpleCitizens_Luchador_Brown",
        "SimpleCitizens_Mountie_White",
        "SimpleCitizens_Runner_White",
        "SimpleCitizens_Tourist_Brown",
        "SimpleCitizens_Biker_Black",
        "SimpleCitizens_Cheerleader_White",
        "SimpleCitizens_Footballer_Black",
        "SimpleCitizens_Hip_Black",
        "SimpleCitizens_Nerd_White",
        "SimpleCitizens_Prisoner_Brown",
        "SimpleCitizens_Racer_White",
        "SimpleCitizens_Runner_Black",
        "SimpleCitizens_ShopKeeper_White",
        "SimpleCitizens_Tourist_White",
    };

    static void ClearExistingWalkers(System.Collections.Generic.List<GameObject> candidates)
    {
        foreach (var go in candidates)
        {
            if (go.GetComponent<DutzLevel00CrowdWalker>() == null
                && !SimpleCitizensNpcPhysics.IsLevel00CrowdWalker(go))
            {
                continue;
            }

            StripWalkerComponents(go);
        }
    }

    static void StripHippieAi(GameObject go)
    {
        foreach (var hunter in go.GetComponents<SimpleCitizensHippieHunter>())
            Undo.DestroyObjectImmediate(hunter);

        foreach (var biter in go.GetComponents<SimpleCitizensHippieBiter>())
            Undo.DestroyObjectImmediate(biter);
    }

    static System.Collections.Generic.List<GameObject> PickRandomCandidates(
        System.Collections.Generic.List<GameObject> candidates,
        int count,
        int seed)
    {
        var shuffled = new System.Collections.Generic.List<GameObject>(candidates);
        var rng = new System.Random(seed);

        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled.GetRange(0, count);
    }
}

/// <summary>Parents ambient Level 00 SimpleCitizens under one folder; walkers stay in Level00CrowdWalkers.</summary>
public static class DutzLevel00CrowdCitizensPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string RootName = "Level00CrowdCitizens";

    /// <summary>Batch: -executeMethod DutzLevel00CrowdCitizensPlacer.OrganizeOnLevel00Batch</summary>
    public static void OrganizeOnLevel00Batch() => OrganizeCitizens(log: true, force: true);

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        return OrganizeCitizens(log, force: false);
    }

    public static bool OrganizeCitizens(bool log, bool force = false)
    {
        if (!force && !NeedsOrganize())
            return false;

        if (!System.IO.File.Exists(Level00ScenePath))
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

        var root = EnsureRoot();
        var walkersRoot = GameObject.Find(DutzLevel00CrowdWalkerPlacer.RootName)?.transform;
        var changed = false;
        var count = 0;

        foreach (var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            var go = animator.gameObject;
            if (!IsCitizenForFolder(go, walkersRoot))
                continue;

            if (go.transform.parent == root)
                continue;

            Undo.RegisterCompleteObjectUndo(go.transform, "Organize Level 00 crowd citizens");
            go.transform.SetParent(root, true);
            changed = true;
            count++;
        }

        if (log && changed)
            Debug.Log($"[Dutz] Level 00 crowd citizens: {count} under {RootName}.");

        return changed;
    }

    static bool NeedsOrganize()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var root = GameObject.Find(RootName);
        var walkersRoot = GameObject.Find(DutzLevel00CrowdWalkerPlacer.RootName)?.transform;

        foreach (var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            var go = animator.gameObject;
            if (!IsCitizenForFolder(go, walkersRoot))
                continue;

            if (root == null || go.transform.parent != root.transform)
                return true;
        }

        return false;
    }

    static bool IsCitizenForFolder(GameObject go, Transform walkersRoot)
    {
        if (go == null || go.GetComponent<DutzPlayerController>() != null)
            return false;

        if (go.GetComponent<DutzLevel00CrowdWalker>() != null)
            return false;

        var name = go.name;
        if (string.IsNullOrEmpty(name) || !name.StartsWith("SimpleCitizens_", System.StringComparison.Ordinal))
            return false;

        if (go.GetComponent<Animator>() == null)
            return false;

        if (walkersRoot != null && go.transform.IsChildOf(walkersRoot))
            return false;

        return true;
    }

    static Transform EnsureRoot()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            return existing.transform;

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Level 00 crowd citizens folder");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        return root.transform;
    }
}

/// <summary>Places tiny rally placards among Level 00 SimpleCitizens (scaled to each holder).</summary>
public static class DutzLevel00RallyPlacardPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string RootName = "Level00RallyPlacards";
    public const int PlacardCount = 6;

    const float PlacardHeightNpcFactor = 0.14f;
    const float PlacardWidthHeightRatio = 1.7f;
    const float PlacardMaxHeightMeters = 0.65f;
    const float PlacardMinHeightMeters = 0.18f;
    const float PlacardScaleMultiplier = 5f;
    const float HeadClearanceMeters = 0.1f;
    const float ForwardOffsetMeters = 0f;
    const float LateralOffsetMeters = 0f;

    struct PlacardSpec
    {
        public readonly string HolderName;
        public readonly string Text;
        public readonly float LateralSign;

        public PlacardSpec(string holderName, string text, float lateralSign)
        {
            HolderName = holderName;
            Text = text;
            LateralSign = lateralSign;
        }
    }

    static readonly PlacardSpec[] Specs =
    {
        new("SimpleCitizens_Runner_Black",
            "Bakit si Marcolea and ikukulong eh kayo ang MandaramBong?", -1f),
        new("SimpleCitizens_Nerd_White", "Dapat managot ang mga kurakot.", 1f),
        new("SimpleCitizens_Tourist_Black",
            "Bakit nilulunod ang pag-iimbestiga sa flood control scam?", -1f),
        new("SimpleCitizens_Biker_White", "Don't bend the law.", 1f),
        new("SimpleCitizens_Clown_Brown",
            "Ang korap kinakanlong, ang nagbunyag kinukulong!", -1f),
        new("SimpleCitizens_Runner_White", "Transparency for a better democracy.", 1f),
    };

    /// <summary>Batch: -executeMethod DutzLevel00RallyPlacardPlacer.PlaceOnLevel00Batch</summary>
    public static void PlaceOnLevel00Batch() => PlacePlacards(log: true, force: true);

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        return PlacePlacards(log, force: false);
    }

    public static bool PlacePlacards(bool log, bool force = false)
    {
        if (!force && !NeedsPlacement())
            return false;

        if (!System.IO.File.Exists(Level00ScenePath))
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

        ClearExistingRoot();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Level 00 rally placards");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var placed = 0;
        for (var i = 0; i < Specs.Length; i++)
        {
            var spec = Specs[i];
            var holder = GameObject.Find(spec.HolderName);
            if (holder == null)
            {
                if (log)
                    Debug.LogWarning("[Dutz] Rally placard holder missing: " + spec.HolderName);
                continue;
            }

            if (CreatePlacard(root.transform, holder.transform, spec, i + 1))
                placed++;
        }

        if (log)
            Debug.Log($"[Dutz] Level 00 rally placards: {placed}/{PlacardCount} among crowd.");

        return placed > 0;
    }

    static bool CreatePlacard(Transform root, Transform holder, PlacardSpec spec, int index)
    {
        if (holder == null)
            return false;

        if (!TryGetNpcMetrics(holder, out var npcHeight))
            npcHeight = 3.6f;

        // Base board size only — wrap + glyph shrink + height growth happen in FitTextToBoard.
        var placardHeight = Mathf.Clamp(npcHeight * PlacardHeightNpcFactor, PlacardMinHeightMeters, PlacardMaxHeightMeters)
                            * PlacardScaleMultiplier;
        var placardWidth = placardHeight * PlacardWidthHeightRatio;
        var characterSize = placardHeight / 16f;

        var placardGo = new GameObject($"RallyPlacard_{index:00}");
        Undo.RegisterCreatedObjectUndo(placardGo, "Place Level 00 rally placards");
        placardGo.transform.SetParent(root.transform, false);

        var placard = placardGo.AddComponent<DutzLevel00RallyPlacard>();
        placard.Configure(
            holder,
            spec.Text,
            placardWidth,
            placardHeight,
            characterSize,
            ForwardOffsetMeters,
            LateralOffsetMeters * spec.LateralSign,
            HeadClearanceMeters);

        return true;
    }

    static bool TryGetNpcMetrics(Transform holder, out float npcHeight)
    {
        npcHeight = 0f;
        if (!TryGetRendererBounds(holder, out var bounds))
            return false;

        npcHeight = bounds.size.y;
        return npcHeight > 0.2f;
    }

    static bool TryGetRendererBounds(Transform holder, out Bounds bounds)
    {
        bounds = default;
        var renderers = holder.GetComponentsInChildren<Renderer>();
        var found = false;

        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static bool NeedsPlacement()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var root = GameObject.Find(RootName);
        if (root == null)
            return true;

        if (root.transform.childCount != PlacardCount)
            return true;

        foreach (var placard in root.GetComponentsInChildren<DutzLevel00RallyPlacard>(true))
        {
            if (placard == null)
                continue;

            if (placard.LayoutVersion != DutzLevel00RallyPlacard.CurrentLayoutVersion)
                return true;
        }

        var found = 0;
        foreach (var spec in Specs)
        {
            var matched = false;
            foreach (var placard in root.GetComponentsInChildren<DutzLevel00RallyPlacard>(true))
            {
                if (placard == null)
                    continue;

                if (!TextsMatch(placard.PlacardText, spec.Text))
                    continue;

                if (placard.transform.parent != root.transform)
                    return true;

                matched = true;
                break;
            }

            if (!matched)
                return true;

            found++;
        }

        return found != PlacardCount;
    }

    static bool TextsMatch(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
            return false;

        return string.Equals(
            actual.Replace("\n", " ").Trim(),
            expected.Trim(),
            System.StringComparison.Ordinal);
    }

    static void ClearExistingRoot()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }
}
