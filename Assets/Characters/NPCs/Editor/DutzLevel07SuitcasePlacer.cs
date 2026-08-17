using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places Level01 suitcase prefabs on Level07 Highway Straight 2, Straight 3, Bridge 4, and Bridge 5 —
/// evenly along each segment, lifted within min-jump reach.
/// </summary>
public static class DutzLevel07SuitcasePlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string SuitcasePrefabPath = "Assets/Characters/Level02/Prefabs/DutzSuitcase.prefab";
    const string Straight2Name = "Highway Straight 2";
    const string Straight3Name = "Highway Straight 3";
    const string Bridge4Name = "Highway Bridge 4";
    const string Bridge5Name = "Highway Bridge 5";
    const string SuitcasesRootName = "DutzSuitcases";
    const string SuitcasePrefix = "DutzSuitcase_";
    const float SuitcaseWorldScale = 8f;
    /// <summary>Suitcases per highway segment (2 / 3 / 4 / 5).</summary>
    const int SuitcasesPerHighway = 12;
    /// <summary>Inset from local Z ends so pitched-slab suitcases stay on the walkable deck.</summary>
    const float LocalZInset = 0.42f;
    static readonly Vector3 SuitcaseEuler = new Vector3(270f, 0f, 0f);

    static readonly string[] TargetHighways =
    {
        Straight2Name,
        Straight3Name,
        Bridge4Name,
        Bridge5Name
    };

    [MenuItem("Assets/Dutz Authoring/Place Suitcases On Level07 Highways 2 3 4 5")]
    public static void PlaceSuitcasesOnLevel07Highways2345()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "[Dutz] Place Suitcases On Level07 Highways 2 3 4 5 requires Edit Mode — stop Play first.");
            return;
        }

        if (!PlaceSilent(log: true))
            Debug.LogError("[Dutz] Failed to place Level07 suitcases on Highways 2/3/4/5. Check the Console.");
    }

    /// <summary>Legacy menus — same placement as Highways 2/3/4/5.</summary>
    [MenuItem("Assets/Dutz Authoring/Place Suitcases On Level07 Highways 2 3 4")]
    public static void PlaceSuitcasesOnLevel07Highways234() => PlaceSuitcasesOnLevel07Highways2345();

    [MenuItem("Assets/Dutz Authoring/Place Suitcases On Level07 Straight2")]
    public static void PlaceSuitcasesOnLevel07Straight2() => PlaceSuitcasesOnLevel07Highways2345();

    public static bool PlaceSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();
        DutzHighwayDirection.InvalidateTrackSegmentCache();
        DutzHighwayDirection.InvalidateReferenceCache();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuitcasePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing suitcase prefab: " + SuitcasePrefabPath);
            return false;
        }

        var jumpMax = GetMaxJumpHeightAboveDeck();
        var lift = Mathf.Min(DutzCollectibleTrackPlacer.HeightAboveDeckMeters, jumpMax);
        var spawn = GetPlayerSpawn();
        var travelForward = GetTravelForward(spawn);

        var allPositions = new List<Vector3>(SuitcasesPerHighway * TargetHighways.Length);
        foreach (var highwayName in TargetHighways)
        {
            var highway = GameObject.Find(highwayName);
            if (highway == null)
            {
                Debug.LogError($"[Dutz] '{highwayName}' not found in Level07.");
                return false;
            }

            List<Vector3> positions;
            if (highwayName.StartsWith("Highway Bridge", System.StringComparison.Ordinal))
                positions = BuildBridgePathPositions(highway, spawn, travelForward, lift);
            else
                positions = BuildPitchedSlabPositions(highway.transform, lift);

            if (positions.Count == 0)
            {
                Debug.LogError($"[Dutz] Could not sample deck for suitcase placement on {highwayName}.");
                return false;
            }

            allPositions.AddRange(positions);
            if (log)
                Debug.Log($"[Dutz] Sampled {positions.Count} suitcase pose(s) on {highwayName}.");
        }

        RemoveExistingSuitcases();

        var root = EnsureSuitcasesRoot();
        var placed = 0;
        for (var i = 0; i < allPositions.Count; i++)
        {
            var suitcase = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(suitcase, "Place Level07 Suitcases");
            suitcase.name = $"{SuitcasePrefix}{i + 1:00}";
            suitcase.transform.SetParent(root.transform, true);
            suitcase.transform.position = allPositions[i];
            suitcase.transform.rotation = Quaternion.Euler(SuitcaseEuler);
            suitcase.transform.localScale = Vector3.one * SuitcaseWorldScale;

            if (suitcase.GetComponent<DutzSuitcase>() == null)
                Undo.AddComponent<DutzSuitcase>(suitcase);

            DutzCollectibleTrackPlacer.WriteSpawnPose(suitcase.GetComponent<DutzSuitcase>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(suitcase);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed {placed} suitcase(s) on Level07 Highways 2/3/4/5 " +
                $"({SuitcasesPerHighway} each on {Straight2Name}, {Straight3Name}, {Bridge4Name}, {Bridge5Name}; " +
                $"lift={lift:F1}m, jumpMax={jumpMax:F1}m).");
        }

        return placed == SuitcasesPerHighway * TargetHighways.Length;
    }

    static List<Vector3> BuildPitchedSlabPositions(Transform road, float lift)
    {
        var positions = new List<Vector3>(SuitcasesPerHighway);
        var col = road.GetComponent<MeshCollider>();
        if (col == null)
        {
            Debug.LogError($"[Dutz] '{road.name}' has no MeshCollider.");
            return positions;
        }

        var deckUp = road.up.normalized;
        if (deckUp.y < 0f)
            deckUp = -deckUp;

        var mesh = col.sharedMesh;
        float minZ;
        float maxZ;
        float localX;
        float localY;
        if (mesh != null)
        {
            var b = mesh.bounds;
            const float inset = 0.04f;
            minZ = b.min.z + inset * b.size.z;
            maxZ = b.max.z - inset * b.size.z;
            if (minZ > maxZ)
            {
                minZ = b.center.z;
                maxZ = b.center.z;
            }

            localX = b.center.x;
            localY = b.max.y;
        }
        else
        {
            minZ = -LocalZInset;
            maxZ = LocalZInset;
            localX = 0f;
            localY = 0.5f;
        }

        Physics.SyncTransforms();
        for (var i = 0; i < SuitcasesPerHighway; i++)
        {
            var t = SuitcasesPerHighway <= 1 ? 0.5f : i / (float)(SuitcasesPerHighway - 1);
            var localZ = Mathf.Lerp(minZ, maxZ, t);
            var seed = road.TransformPoint(new Vector3(localX, localY, localZ));
            if (!TryRaycastPitchedDeck(col, seed, deckUp, out var deckPoint))
            {
                Debug.LogWarning(
                    $"[Dutz] Suitcase sample {i + 1} missed {road.name} deck at localZ={localZ:F2}.");
                continue;
            }

            positions.Add(new Vector3(deckPoint.x, deckPoint.y + lift, deckPoint.z));
        }

        return positions;
    }

    static List<Vector3> BuildBridgePathPositions(
        GameObject bridge,
        Vector3 spawn,
        Vector3 travelForward,
        float lift)
    {
        var positions = new List<Vector3>(SuitcasesPerHighway);
        var path = DutzHighwayDeckSampler.BuildSegmentPath(bridge, bridge.name, spawn, travelForward);
        if (path.Samples == null || path.Samples.Count == 0)
            return positions;

        // Bridge 4 mesh has stacked decks; path samples often sit on the AABB top shell.
        // Hint near the walkable ribbon (renderer center / mid-station height ~301–309).
        var walkableHintY = bridge.transform.position.y;
        if (bridge.TryGetComponent<Renderer>(out var renderer))
            walkableHintY = renderer.bounds.center.y;
        else if (bridge.TryGetComponent<Collider>(out var col))
            walkableHintY = col.bounds.center.y;

        Physics.SyncTransforms();
        for (var i = 0; i < SuitcasesPerHighway; i++)
        {
            // Keep off the extreme ends of the bridge ribbon.
            var t = SuitcasesPerHighway <= 1
                ? 0.5f
                : Mathf.Lerp(0.08f, 0.92f, i / (float)(SuitcasesPerHighway - 1));

            if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, t, out var sample))
            {
                Debug.LogWarning($"[Dutz] Suitcase sample {i + 1} missed {bridge.name} path at t={t:F2}.");
                continue;
            }

            var deck = new Vector3(sample.Position.x, walkableHintY, sample.Position.z);
            if (!DutzRoadGround.TrySampleLevel07NamedHighwayDeckPoint(
                    bridge.name, deck, out var deckPoint, out _))
            {
                var probe = deck + Vector3.up * 40f;
                if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, walkableHintY, null, out var deckY)
                    || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
                    deck.y = deckY;
                else
                    continue;

                deckPoint = deck;
            }

            // Reject top-bar / shell snaps far above the walkable hint.
            if (deckPoint.y > walkableHintY + 40f)
            {
                Debug.LogWarning(
                    $"[Dutz] Suitcase sample {i + 1} on {bridge.name} hit high shell Y={deckPoint.y:F1} " +
                    $"(hint={walkableHintY:F1}) — skipped.");
                continue;
            }

            positions.Add(new Vector3(deckPoint.x, deckPoint.y + lift, deckPoint.z));
        }

        return positions;
    }

    static bool TryRaycastPitchedDeck(
        MeshCollider col,
        Vector3 seed,
        Vector3 deckUp,
        out Vector3 deckPoint)
    {
        deckPoint = seed;
        const float castDist = 60f;
        var origin = seed + deckUp * castDist;
        var hits = Physics.RaycastAll(origin, -deckUp, castDist * 2f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        var bestScore = float.PositiveInfinity;
        var found = false;
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider != col)
                continue;

            var facing = Vector3.Dot(hit.normal.normalized, deckUp);
            if (facing < 0.25f)
                continue;

            var score = (1f - facing) * 100f + (hit.point - seed).sqrMagnitude;
            if (score >= bestScore)
                continue;

            bestScore = score;
            deckPoint = hit.point;
            found = true;
        }

        return found;
    }

    static float GetMaxJumpHeightAboveDeck()
    {
        var jumpForce = 14f;
        var gravity = -20f;

        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            jumpForce = so.FindProperty("jumpForce").floatValue;
            gravity = so.FindProperty("gravity").floatValue;
            break;
        }

        var gravityMag = Mathf.Max(0.01f, Mathf.Abs(gravity));
        return jumpForce * jumpForce / (2f * gravityMag) - DutzCollectibleTrackPlacer.JumpHeightSafetyMargin;
    }

    static Vector3 GetPlayerSpawn()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var position, out _))
            return position;

        return new Vector3(-1002f, 7.4f, -9.1f);
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progress)
            && progress.sqrMagnitude > 0.0001f)
            return progress.normalized;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        return forward.normalized;
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
}
