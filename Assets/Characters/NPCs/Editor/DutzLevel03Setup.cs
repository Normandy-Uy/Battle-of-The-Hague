using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prepares Dutz_Level03 — welcome level, no crocs/suitcases, Hague photos, E-TOL giants.</summary>
public static class DutzLevel03Setup
{
    const string TrackGiantRootName = "DutzLevel03TrackGiants";
    const int EtolDuplicatesTotal = 5;
    const int EtolsPerSegment = 1;
    const float TrackEtOlAlongSegment = 0.5f;
    const int TrackEtOlHitPoints = 50;
    const float TrackGiantWidthScale = 0.5f;
    const float TrackGiantHeightScale = 0.5f;
    const float TrackGiantScaleMultiplier = 3f;

    static readonly string[] HighwaySegmentNames =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        "Highway Straight 6"
    };

    static readonly string[] RemoveGiantNames =
    {
        DutzGiantBossNames.GongBong,
        DutzGiantBossNames.Tamby,
        DutzGiantBossNames.PrincessZara,
        DutzGiantBossNames.GeneralRook,
        DutzGiantBossNames.Trililing,
        DutzGiantBossNames.LegacyGrandma,
        DutzGiantBossNames.LegacyMid,
        "GrandmaGiantDialog"
    };

    static readonly float[] LaneZ = { DutzHighwayDeckSampler.LeftLaneZ, DutzHighwayDeckSampler.RightLaneZ };

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyGiantSpeedOnLevel03Batch</summary>
    public static void ApplyGiantSpeedOnLevel03Batch() => ApplyGiantSpeedOnLevel03(log: true);

    public static void ApplyGiantSpeedFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3 Giants", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyGiantSpeedOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Level 3 Giants",
                "Could not apply giant speed on Dutz_Level03. Check Console.",
                "OK");
        }
    }

    public static bool ApplyGiantSpeedOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var adjusted = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (!IsLevel03Giant(hunter.gameObject.name))
                continue;

            Undo.RecordObject(hunter, "Apply Level 3 Giant Speed");
            ApplyGiantSpeedToHunter(hunter);
            adjusted++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 3 giant chase speed — track: {DutzCollectibleProgress.Level03TrackGiantChaseSpeed:0.#} m/s, " +
                $"end E-TOL: {DutzCollectibleProgress.Level03GiantChaseSpeed:0.#} m/s on {adjusted} giant(s).");
        }

        return adjusted > 0;
    }

    static bool IsLevel03Giant(string objectName) =>
        DutzCollectibleProgress.IsLevel03Giant(objectName);

    static void ApplyGiantSpeedToHunter(SimpleCitizensGiantHippieHunter hunter)
    {
        var isTrack = DutzCollectibleProgress.IsLevel03TrackEtOl(hunter.gameObject.name);
        var chase = isTrack
            ? DutzCollectibleProgress.Level03TrackGiantChaseSpeed
            : DutzCollectibleProgress.Level03GiantChaseSpeed;
        var anim = isTrack
            ? DutzCollectibleProgress.GetLevel03TrackGiantChaseAnimSpeed()
            : DutzCollectibleProgress.GetLevel03GiantChaseAnimSpeed();

        var hso = new SerializedObject(hunter);
        hso.FindProperty("chaseSpeed").floatValue = chase;
        hso.FindProperty("chaseAnimSpeed").floatValue = anim;
        hso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hunter);

        var physics = hunter.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            return;

        Undo.RecordObject(physics, "Apply Level 3 Giant Speed");
        var pso = new SerializedObject(physics);
        pso.FindProperty("walkSpeed").floatValue = chase;
        pso.FindProperty("animatorWalkSpeed").floatValue = anim;
        pso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(physics);
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyHalfHeightTrackGiantsBatch</summary>
    public static void ApplyHalfHeightTrackGiantsBatch() => ApplyHalfHeightTrackGiants(log: true);

    public static void ApplyHalfHeightTrackGiantsFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3 Giants", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyHalfHeightTrackGiants(log: true))
        {
            EditorUtility.DisplayDialog(
                "Level 3 Giants",
                "Could not halve track E-TOL height on Dutz_Level03. Check Console.",
                "OK");
        }
    }

    public static bool ApplyHalfHeightTrackGiants(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var adjusted = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            adjusted += ApplyHalfHeightInHierarchy(root.transform);

        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (!IsLevel03Giant(hunter.gameObject.name))
                continue;

            DutzGiantHeadTopCollider.EnsureOnGiant(hunter.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Level 3 track E-TOL width scaled to {TrackGiantWidthScale:0.##} on {adjusted} giant(s). End E-TOL unchanged.");

        return adjusted > 0;
    }

    public static void ApplyGiantsFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3 Giants", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceEtolsOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Level 3 Giants",
                "Could not place E-TOL giants on Dutz_Level03. Check Console.",
                "OK");
        }
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.PlaceEtolsOnLevel03Batch</summary>
    public static void PlaceEtolsOnLevel03Batch() => PlaceEtolsOnLevel03(log: true);

    public static bool PlaceEtolsOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var removedBosses = RemoveOtherGiants();
        var removedTrack = RemoveTrackGiants();

        var template = GameObject.Find(DutzGiantBossNames.BeybiM) ?? GameObject.Find(DutzGiantBossNames.ETol);
        if (template == null)
        {
            Debug.LogError("[Dutz] End giant BEYBI M not found in Dutz_Level03.");
            return false;
        }

        var root = GameObject.Find(TrackGiantRootName);
        if (root == null)
        {
            root = new GameObject(TrackGiantRootName);
            Undo.RegisterCreatedObjectUndo(root, "Place Level 3 E-TOL Track Giants");
        }

        var spawn = GetPlayerSpawn();
        var travelForward = GetTravelForward(spawn);
        var placed = 0;

        for (var segmentIndex = 0;
             segmentIndex < HighwaySegmentNames.Length - 1 && placed < EtolDuplicatesTotal;
             segmentIndex++)
        {
            var segmentName = HighwaySegmentNames[segmentIndex];
            var segment = GameObject.Find(segmentName);
            if (segment == null)
            {
                Debug.LogWarning($"[Dutz] Highway segment not found: {segmentName}");
                continue;
            }

            var path = DutzHighwayDeckSampler.BuildSegmentPath(segment, segmentName, spawn, travelForward);
            if (path.Samples == null || path.Samples.Count == 0)
            {
                Debug.LogWarning($"[Dutz] No deck samples for segment: {segmentName}");
                continue;
            }

            for (var slot = 0; slot < EtolsPerSegment && placed < EtolDuplicatesTotal; slot++)
            {
                if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, TrackEtOlAlongSegment, out var sample))
                    continue;

                var laneZ = LaneZ[segmentIndex % LaneZ.Length];
                var world = DutzHighwayDeckSampler.PlaceOnLane(sample, laneZ, spawn);
                var rotation = Quaternion.LookRotation(sample.Forward, Vector3.up);

                var copy = Object.Instantiate(template);
                Undo.RegisterCreatedObjectUndo(copy, "Place Level 3 Track Giant");
                copy.name = DutzLevel03TrackGiantFaces.GetDisplayName(placed);
                copy.transform.SetParent(root.transform, true);
                copy.transform.SetPositionAndRotation(world, rotation);
                copy.SetActive(true);

                if (PrefabUtility.IsPartOfAnyPrefab(copy))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                ApplyTrackGiantHalfHeightScale(copy);
                SnapTrackGiantToRoad(copy);
                var hunter = copy.GetComponent<SimpleCitizensGiantHippieHunter>();
                if (hunter != null)
                    ApplyGiantSpeedToHunter(hunter);
                DutzHippieBiteCollider.EnsureTrililingSolidCollider(copy);
                DutzGiantHeadTopCollider.EnsureOnGiant(copy);
                DutzNpcHitPoints.EnsureOn(copy, TrackEtOlHitPoints);
                ApplyGiantHeatToGiant(copy);
                placed++;
            }
        }

        var endHunter = template.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (endHunter != null)
            ApplyGiantSpeedToHunter(endHunter);

        ApplyEndEtOlHeightScale(template);
        DutzGiantHippieBossFaceBuilder.ApplyTrackGiantFacesOnLevel03(log: false);
        DutzGiantHippieBossFaceBuilder.ApplyBeybiMFaceOnLevel03(log: false);
        DutzHippieBiteCollider.EnsureTrililingSolidCollider(template);
        DutzGiantHeadTopCollider.EnsureOnGiant(template);
        DutzNpcHitPoints.EnsureOn(template, EndEtOlHitPoints);
        ApplyGiantHeatToGiant(template);

        EnsureBonusGiantsOnLevel03(log: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 3 giants — removed {removedBosses} other boss(es), " +
                $"{removedTrack} prior track giant(s), placed {placed}/{EtolDuplicatesTotal} " +
                $"track giant(s) (RAPTOR, BOYOYONG, KIKAY P, Lie Fivex, KLARING). End BEYBI M kept.");
        }

        return placed == EtolDuplicatesTotal;
    }

    static readonly string[] BonusGiantNames =
    {
        DutzGiantBossNames.Hontavirus,
        DutzGiantBossNames.LengLengLugaw,
    };

    static readonly string[] BonusGiantSegmentNames =
    {
        "Highway Bridge 5",
        "Highway Straight 6",
    };

    static bool IsBonusGiantName(string objectName)
    {
        for (var i = 0; i < BonusGiantNames.Length; i++)
        {
            if (BonusGiantNames[i] == objectName)
                return true;
        }

        return false;
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.EnsureBonusGiantsOnLevel03Batch</summary>
    public static void EnsureBonusGiantsOnLevel03Batch() => EnsureBonusGiantsOnLevel03(log: true);

    public static bool EnsureBonusGiantsOnLevel03(bool log, bool lightweightAutoApply = false)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            if (lightweightAutoApply)
                return false;

            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var changed = false;
        var trackRoot = GameObject.Find(TrackGiantRootName);
        if (trackRoot == null)
        {
            trackRoot = new GameObject(TrackGiantRootName);
            Undo.RegisterCreatedObjectUndo(trackRoot, "Create Level 3 Track Giants Root");
            changed = true;
        }

        var template = GameObject.Find(DutzGiantBossNames.BeybiM) ?? GameObject.Find(DutzGiantBossNames.ETol);
        if (template == null)
        {
            Debug.LogError("[Dutz] End giant BEYBI M not found — cannot prepare Level 3 bonus giants.");
            return false;
        }

        var placedBefore = CountBonusGiantsInScene();
        PlaceMissingBonusGiants(trackRoot.transform, template);
        if (CountBonusGiantsInScene() > placedBefore)
            changed = true;

        var prepared = 0;

        for (var i = 0; i < BonusGiantNames.Length; i++)
        {
            var giant = GameObject.Find(BonusGiantNames[i]);
            if (giant == null)
            {
                Debug.LogWarning($"[Dutz] Bonus giant not found in Level 3 scene: {BonusGiantNames[i]}");
                continue;
            }

            if (giant.transform.parent != trackRoot.transform)
            {
                Undo.SetTransformParent(giant.transform, trackRoot.transform, "Parent Level 3 Bonus Giant");
                EditorUtility.SetDirty(giant);
                changed = true;
            }

            if (lightweightAutoApply)
            {
                EnsureBonusGiantComponentsLightweight(giant);
            }
            else
            {
                if (PrefabUtility.IsPartOfAnyPrefab(giant))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    changed = true;
                }

                ApplyTrackGiantHalfHeightScale(giant);
                SnapTrackGiantToRoad(giant);

                var hunter = giant.GetComponent<SimpleCitizensGiantHippieHunter>();
                if (hunter != null)
                    ApplyGiantSpeedToHunter(hunter);

                DutzHippieBiteCollider.EnsureTrililingSolidCollider(giant);
                DutzGiantHeadTopCollider.EnsureOnGiant(giant);
                DutzNpcHitPoints.EnsureOn(giant, TrackEtOlHitPoints);
                ApplyGiantHeatToGiant(giant);
                changed = true;
            }

            prepared++;
        }

        if (!lightweightAutoApply || NeedsBonusGiantFace(DutzGiantBossNames.Hontavirus))
        {
            if (DutzGiantHippieBossFaceBuilder.EnsureHontavirusBossFaceOnOpenScene(log: false, persistScene: false))
                changed = true;
        }

        if (!lightweightAutoApply || NeedsBonusGiantFace(DutzGiantBossNames.LengLengLugaw))
        {
            if (DutzGiantHippieBossFaceBuilder.EnsureLengLengLugawBossFaceOnOpenScene(log: false, persistScene: false))
                changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 3 bonus giants prepared — {prepared}/{BonusGiantNames.Length} " +
                $"(HONTAVIRUS, LENG LENG LUGAW) snapped to road with track HP/heat.");
        }

        return prepared == BonusGiantNames.Length;
    }

    static int CountBonusGiantsInScene()
    {
        var count = 0;
        for (var i = 0; i < BonusGiantNames.Length; i++)
        {
            if (GameObject.Find(BonusGiantNames[i]) != null)
                count++;
        }

        return count;
    }

    static bool NeedsBonusGiantFace(string giantName)
    {
        var giant = GameObject.Find(giantName);
        return giant != null && giant.GetComponent<DutzGiantHippieBossFace>() == null;
    }

    static void EnsureBonusGiantComponentsLightweight(GameObject giant)
    {
        if (giant.GetComponent<SimpleCitizensGiantHippieHunter>() == null)
            giant.AddComponent<SimpleCitizensGiantHippieHunter>();
        if (giant.GetComponent<SimpleCitizensNpcPhysics>() == null)
            giant.AddComponent<SimpleCitizensNpcPhysics>();
        if (giant.GetComponent<DutzNpcHitPoints>() == null)
            DutzNpcHitPoints.EnsureOn(giant, TrackEtOlHitPoints);
        if (giant.GetComponent<DutzGiantHeat>() == null)
            ApplyGiantHeatToGiant(giant);
    }

    static void PlaceMissingBonusGiants(Transform trackRoot, GameObject template)
    {
        var spawn = GetPlayerSpawn();
        var travelForward = GetTravelForward(spawn);

        for (var i = 0; i < BonusGiantNames.Length; i++)
        {
            if (GameObject.Find(BonusGiantNames[i]) != null)
                continue;

            if (i >= BonusGiantSegmentNames.Length)
            {
                Debug.LogWarning($"[Dutz] No highway segment configured for bonus giant {BonusGiantNames[i]}.");
                continue;
            }

            var segmentName = BonusGiantSegmentNames[i];
            var segment = GameObject.Find(segmentName);
            if (segment == null)
            {
                Debug.LogWarning($"[Dutz] Highway segment not found for bonus giant {BonusGiantNames[i]}: {segmentName}");
                continue;
            }

            var path = DutzHighwayDeckSampler.BuildSegmentPath(segment, segmentName, spawn, travelForward);
            if (path.Samples == null || path.Samples.Count == 0)
            {
                Debug.LogWarning($"[Dutz] No deck samples for bonus giant segment: {segmentName}");
                continue;
            }

            if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, TrackEtOlAlongSegment, out var sample))
                continue;

            var laneZ = LaneZ[(EtolDuplicatesTotal + i) % LaneZ.Length];
            var world = DutzHighwayDeckSampler.PlaceOnLane(sample, laneZ, spawn);
            var rotation = Quaternion.LookRotation(sample.Forward, Vector3.up);

            var copy = Object.Instantiate(template);
            Undo.RegisterCreatedObjectUndo(copy, "Place Level 3 Bonus Giant");
            copy.name = BonusGiantNames[i];
            copy.transform.SetParent(trackRoot, true);
            copy.transform.SetPositionAndRotation(world, rotation);
            copy.SetActive(true);

            if (PrefabUtility.IsPartOfAnyPrefab(copy))
            {
                PrefabUtility.UnpackPrefabInstance(
                    copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }
    }

    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyLevel03Content(log: true))
            EditorUtility.DisplayDialog("Level 3", "Could not apply Level 3 content. Check Console.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyLevel03ContentBatch</summary>
    public static void ApplyLevel03ContentBatch() => ApplyLevel03Content(log: true);

    public static bool ApplyLevel03Content(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        var removedSuitcases = RemoveSuitcases();
        var removedCrocs = RemoveCrocodilesAndPool();
        // Never rebuild HighwayPhotoMurals here — PlaceOnLevel03 deletes the root and wipes hand-tuned positions.
        // Hague murals: Tools/Dutz/Reposition Hague Murals. Jail mural: Place Dutz Jail Mural (only if missing).
        var jailMuralOk = GameObject.Find(DutzJailMuralPlacer.SceneRootName) != null
            || DutzJailMuralPlacer.PlaceOnLevel03(log: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 3 content applied — removed {removedSuitcases} suitcase root(s), " +
                $"{removedCrocs} croc/pool object(s), dutzJailMural={(jailMuralOk ? "ok" : "skipped/failed")}. " +
                "Hague mural positions were left unchanged.");
        }

        return true;
    }

    static int RemoveCrocodilesAndPool()
    {
        var toDestroy = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            CollectMatching(root.transform, DutzSegmentHippieIdentity.PoolRootName, toDestroy);
            CollectMatching(root.transform, DutzSegmentHippieIdentity.ManagerObjectName, toDestroy);
            CollectMatching(root.transform, "Crocodile", toDestroy);
        }

        foreach (var go in toDestroy)
            Object.DestroyImmediate(go);

        return toDestroy.Count;
    }

    static int RemoveSuitcases()
    {
        var toDestroy = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectMatching(root.transform, "DutzSuitcases", toDestroy);

        foreach (var go in toDestroy)
            Object.DestroyImmediate(go);

        return toDestroy.Count;
    }

    static void CollectMatching(Transform node, string objectName, List<GameObject> matches)
    {
        if (node == null)
            return;

        if (node.name == objectName)
        {
            matches.Add(node.gameObject);
            return;
        }

        for (var i = 0; i < node.childCount; i++)
            CollectMatching(node.GetChild(i), objectName, matches);
    }

    static int RemoveMatching(Transform node, string objectName)
    {
        var removed = 0;
        if (node.name == objectName)
        {
            Object.DestroyImmediate(node.gameObject);
            return 1;
        }

        for (var i = node.childCount - 1; i >= 0; i--)
            removed += RemoveMatching(node.GetChild(i), objectName);

        return removed;
    }

    static int RemoveOtherGiants()
    {
        var toDestroy = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectNamedObjects(root.transform, RemoveGiantNames, toDestroy);

        foreach (var go in toDestroy)
            Object.DestroyImmediate(go);

        return toDestroy.Count;
    }

    static int RemoveTrackGiants()
    {
        var removed = 0;
        var trackRoot = GameObject.Find(TrackGiantRootName);
        if (trackRoot != null)
        {
            for (var i = trackRoot.transform.childCount - 1; i >= 0; i--)
            {
                var child = trackRoot.transform.GetChild(i).gameObject;
                if (IsBonusGiantName(child.name))
                    continue;

                Object.DestroyImmediate(child);
                removed++;
            }

            if (trackRoot.transform.childCount == 0)
                Object.DestroyImmediate(trackRoot);
        }

        var stray = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectTrackEtols(root.transform, stray);

        foreach (var go in stray)
        {
            Object.DestroyImmediate(go);
            removed++;
        }

        return removed;
    }

    static void CollectTrackEtols(Transform node, List<GameObject> matches)
    {
        if (DutzLevel03TrackGiantFaces.IsAnyTrackGiant(node.name))
        {
            matches.Add(node.gameObject);
            return;
        }

        for (var i = 0; i < node.childCount; i++)
            CollectTrackEtols(node.GetChild(i), matches);
    }

    static void CollectNamedObjects(Transform node, IReadOnlyList<string> names, List<GameObject> matches)
    {
        for (var i = 0; i < names.Count; i++)
        {
            if (node.name == names[i])
            {
                matches.Add(node.gameObject);
                return;
            }
        }

        for (var i = 0; i < node.childCount; i++)
            CollectNamedObjects(node.GetChild(i), names, matches);
    }

    static int ApplyHalfHeightInHierarchy(Transform node)
    {
        var adjusted = 0;
        if (IsTrackGiant(node.name))
        {
            Undo.RecordObject(node, "Halve Level 3 Track Giant Height");
            ApplyTrackGiantHalfHeightScale(node.gameObject);
            adjusted++;
            return adjusted;
        }

        for (var i = 0; i < node.childCount; i++)
            adjusted += ApplyHalfHeightInHierarchy(node.GetChild(i));

        return adjusted;
    }

    static bool IsTrackGiant(string objectName) =>
        DutzLevel03TrackGiantFaces.IsAnyTrackGiant(objectName);

    static Transform GetTrackGiantScaleTemplate() => DutzGiantBossNames.FindTrililing()?.transform;

    static Vector3 GetTrackGiantTargetScale(Transform endBossTemplate)
    {
        var templateWidth = endBossTemplate != null
            ? Mathf.Max(endBossTemplate.localScale.x, endBossTemplate.localScale.z)
            : DutzCollectibleProgress.Level03EndBossScale;
        var width = templateWidth * TrackGiantWidthScale * TrackGiantScaleMultiplier;
        return new Vector3(width, width * TrackGiantHeightScale, width);
    }

    static void ApplyTrackGiantHalfHeightScale(GameObject go)
    {
        Undo.RecordObject(go.transform, "Apply Level 3 Track Giant Scale");
        go.transform.localScale = GetTrackGiantTargetScale(GetTrackGiantScaleTemplate());

        Physics.SyncTransforms();
        EditorUtility.SetDirty(go);
    }

    static void ApplyEndEtOlHeightScale(GameObject go)
    {
        if (go == null || !DutzGiantBossNames.IsLevel03EndBoss(go.name))
            return;

        Undo.RecordObject(go.transform, "Apply Level 3 End Boss Scale");
        DutzCollectibleProgress.ApplyLevel03EndBossScale(go.transform);

        Physics.SyncTransforms();
        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SnapFeetToRoad();
        }

        DutzGiantHeadTopCollider.EnsureOnGiant(go);
        RecordGiantSpawnPoint(go);
        EditorUtility.SetDirty(go);
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyEndEtOlHeightScaleBatch</summary>
    public static void ApplyEndEtOlHeightScaleBatch() => ApplyEndEtOlHeightScaleOnLevel03(log: true);

    public static void ApplyEndEtOlHeightScaleFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3 Giants", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyEndEtOlHeightScaleOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Level 3 Giants",
                "Could not apply end E-TOL height scale. Check Console.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Level 3 Giants",
            $"End E-TOL height reduced by 30% (scale {DutzCollectibleProgress.GetLevel03EndEtOlScale()}).",
            "OK");
    }

    public static bool ApplyEndEtOlHeightScaleOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        var endEtOl = DutzGiantBossNames.FindTrililing();
        if (endEtOl == null)
        {
            Debug.LogError("[Dutz] End E-TOL not found in Level 3 scene.");
            return false;
        }

        ApplyEndEtOlHeightScale(endEtOl);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
            $"End BEYBI M height scale applied: {DutzCollectibleProgress.GetLevel03EndBossScale()} " +
                $"on {endEtOl.name}.");
        }

        return true;
    }

    static void RecordGiantSpawnPoint(GameObject go)
    {
        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn != null)
            respawn.RecordSpawnPoint();
    }

    /// <summary>Snap a Level 3 track giant's feet to the highway deck and refresh its respawn pose.</summary>
    public static void SnapTrackGiantToRoad(GameObject go)
    {
        if (go == null)
            return;

        Physics.SyncTransforms();

        if (TrySnapLevel03GiantDeckFromHighwayProbe(go))
        {
            DutzGiantHeadTopCollider.EnsureOnGiant(go);
            RecordGiantSpawnPoint(go);
            EditorUtility.SetDirty(go);
            return;
        }

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SnapFeetToRoad();
        }

        DutzGiantHeadTopCollider.EnsureOnGiant(go);
        RecordGiantSpawnPoint(go);
        EditorUtility.SetDirty(go);
    }

    static bool TrySnapLevel03GiantDeckFromHighwayProbe(GameObject go)
    {
        if (!DutzCollectibleProgress.UsesLevel03GiantRoadFooting(go.name))
            return false;

        Collider col = null;
        foreach (var c in go.GetComponents<Collider>())
        {
            if (c != null && !c.isTrigger)
            {
                col = c;
                break;
            }
        }

        var pos = go.transform.position;
        var feetY = DutzNpcFeet.GetLowestWorldY(go);
        if (!DutzRoadGround.TrySampleGiantRoadDeckY(pos, feetY, col, out var deckY))
            return false;

        DutzNpcFeet.PlacePivotOnSurface(go, deckY);
        return true;
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.RestoreTrackGiantScalesOnLevel03Batch</summary>
    public static void RestoreTrackGiantScalesOnLevel03Batch() => RestoreTrackGiantScalesOnLevel03(log: true);

    public static bool RestoreTrackGiantScalesOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var root = GameObject.Find(TrackGiantRootName);
        if (root == null)
        {
            Debug.LogError("[Dutz] DutzLevel03TrackGiants root not found.");
            return false;
        }

        var targetScale = GetTrackGiantTargetScale(GetTrackGiantScaleTemplate());
        var restored = 0;

        for (var i = 0; i < root.transform.childCount; i++)
        {
            var giant = root.transform.GetChild(i).gameObject;
            ApplyTrackGiantHalfHeightScale(giant);
            SnapTrackGiantToRoad(giant);
            restored++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Restored {restored} Level 3 track giant scale(s) to " +
                $"{targetScale.x:0.##} x {targetScale.y:0.##} x {targetScale.z:0.##} and snapped feet to road.");
        }

        return restored > 0;
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.SnapAllTrackGiantsOnLevel03Batch</summary>
    public static void SnapAllTrackGiantsOnLevel03Batch() => SnapAllTrackGiantsOnLevel03(log: true);

    public static bool SnapAllTrackGiantsOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var root = GameObject.Find(TrackGiantRootName);
        if (root == null)
        {
            Debug.LogError("[Dutz] DutzLevel03TrackGiants root not found.");
            return false;
        }

        var snapped = 0;
        for (var i = 0; i < root.transform.childCount; i++)
        {
            var giant = root.transform.GetChild(i).gameObject;
            SnapTrackGiantToRoad(giant);
            snapped++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Snapped {snapped} Level 3 track giant(s) to the highway deck.");

        return snapped > 0;
    }

    /// <summary>Re-snap Level 3 track E-TOL giants that are floating above or sunk below the deck.</summary>
    public static bool EnsureTrackGiantsSnappedOnOpenScene(bool log)
    {
        if (SceneManager.GetActiveScene().path != DutzLevel02Setup.Level03ScenePath)
            return false;

        var root = GameObject.Find(TrackGiantRootName);
        if (root == null)
            return false;

        var changed = false;
        for (var i = 0; i < root.transform.childCount; i++)
        {
            var giant = root.transform.GetChild(i).gameObject;
            if (!DutzCollectibleProgress.IsLevel03TrackEtOl(giant.name))
                continue;

            if (!TrackGiantMisalignedWithDeck(giant))
                continue;

            var yBefore = giant.transform.position.y;
            SnapTrackGiantToRoad(giant);
            if (Mathf.Abs(giant.transform.position.y - yBefore) > 0.02f)
                changed = true;
        }

        if (log && changed)
            Debug.Log("[Dutz] Snapped Level 3 track giant(s) onto the highway deck.");

        return changed;
    }

    const float TrackGiantDeckMisalignThreshold = 0.35f;

    static bool TrackGiantMisalignedWithDeck(GameObject giant)
    {
        Collider col = null;
        foreach (var c in giant.GetComponents<Collider>())
        {
            if (c != null && !c.isTrigger)
            {
                col = c;
                break;
            }
        }

        Physics.SyncTransforms();
        var feetY = DutzNpcFeet.GetLowestWorldY(giant);
        if (!DutzRoadGround.TrySampleGiantRoadDeckY(giant.transform.position, feetY, col, out var deckY))
            return true;

        return Mathf.Abs(feetY - deckY) > TrackGiantDeckMisalignThreshold;
    }

    static Vector3 GetPlayerSpawn()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        return new Vector3(-1002f, 7.4f, -9.1f);
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            if (so.FindProperty("invertSpawnFacing").boolValue)
                forward = -forward;
            break;
        }

        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.right;
    }

    const int EndEtOlHitPoints = DutzNpcHitPoints.EndEtOlHitPoints;

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyEndEtOlHitPointsBatch</summary>
    public static void ApplyEndEtOlHitPointsBatch() => ApplyEndEtOlHitPoints(log: true);

    public static void ApplyEndEtOlHitPointsFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("E-TOL Hit Points", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyEndEtOlHitPoints(log: true))
        {
            EditorUtility.DisplayDialog(
                "E-TOL Hit Points",
                "Could not apply HP to end E-TOL on Dutz_Level03. Check Console.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("E-TOL Hit Points", $"End E-TOL now has {DutzNpcHitPoints.EndEtOlHitPoints} hit points.", "OK");
        }
    }

    public static bool ApplyEndEtOlHitPoints(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var etol = DutzGiantBossNames.FindTrililing();
        if (etol == null)
        {
            Debug.LogError("[Dutz] End E-TOL not found in Dutz_Level03.");
            return false;
        }

        DutzNpcHitPoints.EnsureOn(etol, EndEtOlHitPoints);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] End E-TOL ({etol.name}) configured with {EndEtOlHitPoints} hit points.");

        return true;
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyTrackGiantHitPointsBatch</summary>
    public static void ApplyTrackGiantHitPointsBatch() => ApplyTrackGiantHitPoints(log: true);

    public static bool ApplyTrackGiantHitPoints(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var updated = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (!DutzCollectibleProgress.IsLevel03TrackEtOl(hunter.gameObject.name))
                continue;

            DutzNpcHitPoints.EnsureOn(hunter.gameObject, TrackEtOlHitPoints);
            updated++;
        }

        if (updated == 0)
        {
            Debug.LogError("[Dutz] No Level 3 track giants found in Dutz_Level03.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Applied {TrackEtOlHitPoints} HP to {updated} track giant(s). BEYBI M unchanged.");

        return true;
    }

    /// <summary>Batch: -executeMethod DutzLevel03Setup.ApplyGiantHeatOnLevel03Batch</summary>
    public static void ApplyGiantHeatOnLevel03Batch() => ApplyGiantHeatOnLevel03(log: true);

    public static void ApplyGiantHeatFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3 Giant Heat", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyGiantHeatOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Level 3 Giant Heat",
                "Could not apply giant heat on Dutz_Level03. Check Console.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Level 3 Giant Heat", "All Level 3 giants now radiate heat.", "OK");
        }
    }

    public static bool ApplyGiantHeatOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var adjusted = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (!IsLevel03Giant(hunter.gameObject.name))
                continue;

            Undo.RecordObject(hunter.gameObject, "Apply Level 3 Giant Heat");
            ApplyGiantHeatToGiant(hunter.gameObject);
            DutzGiantHeadTopCollider.EnsureOnGiant(hunter.gameObject);
            adjusted++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log(
                $"[Dutz] Giant heat applied to {adjusted} Level 3 giant(s) — " +
                $"track: {DutzGiantHeat.TrackBurnPerSecond:0.#} HP/s, end E-TOL: {DutzGiantHeat.EndBossBurnPerSecond:0.#} HP/s.");

        return adjusted > 0;
    }

    static void ApplyGiantHeatToGiant(GameObject go)
    {
        var heat = DutzGiantHeat.EnsureOn(go);
        if (heat == null)
            return;

        var heatSo = new SerializedObject(heat);
        heatSo.FindProperty("burnPerSecond").floatValue = DutzGiantHeat.GetBurnPerSecondForGiant(go.name);
        heatSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(heat);
    }

    /// <summary>
    /// Editor-only: bake head/body colliders into the scene file (optional).
    /// Play mode applies colliders automatically via bootstrap + giant hunter — no menu needed each run.
    /// Batch: -executeMethod DutzLevel03Setup.ApplyGiantHeadCollidersOnLevel03Batch
    /// </summary>
    public static void ApplyGiantHeadCollidersOnLevel03Batch() => ApplyGiantHeadCollidersOnLevel03(log: true);

    public static void ApplyGiantHeadCollidersFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Giant Head Colliders", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyGiantHeadCollidersOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Giant Head Colliders",
                "Could not apply giant head colliders on Dutz_Level03. Check Console.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Giant Head Colliders", "Giant head colliders applied on Level 3.", "OK");
        }
    }

    public static bool ApplyGiantHeadCollidersOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var adjusted = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (!IsLevel03Giant(hunter.gameObject.name))
                continue;

            Undo.RecordObject(hunter.gameObject, "Apply Giant Head Colliders");
            DutzGiantHeadTopCollider.EnsureOnGiant(hunter.gameObject);
            DutzHippieBiteCollider.EnsureTrililingSolidCollider(hunter.gameObject);
            adjusted++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Giant head colliders applied to {adjusted} Level 3 giant(s).");

        return adjusted > 0;
    }
}
