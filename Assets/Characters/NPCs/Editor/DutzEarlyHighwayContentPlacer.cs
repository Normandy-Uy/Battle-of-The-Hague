using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Legacy near-spawn addict placement on Highway Bridge 1 and Highway Straight 2 (coins removed).
/// </summary>
public static class DutzEarlyHighwayContentPlacer
{
    const string ShowcaseScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string HippiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Hippie_Black.prefab";

    const string BridgeSegmentName = "Highway Bridge 1";
    const string StraightSegmentName = "Highway Straight 2";
    const string AddictPrefix = "SimpleCitizens_Hippie_NearSpawn_";
    const string CoinPrefix = "DutzGoldCoin_NearSpawn_";
    const string CoinsRootName = "DutzGoldCoins";

    const int AddictCount = 10;
    const float AddictMinAheadMeters = 30f;
    const float ForwardPathSpanMeters = 260f;
    const float EndInsetFraction = 0.06f;
    const float LeftLaneZ = 11f;
    const float RightLaneZ = -11f;
    const float CenterLaneZ = 4.5f;
    const int DeckSamplesPerSegment = 40;
    const int PlacementVersion = 9;
    const string PlacementVersionKey = "DutzEarlyHighwayPlacementVersion";

    static readonly float[] AddictAlongFractions =
    {
        0.05f, 0.14f, 0.24f, 0.34f, 0.44f, 0.55f, 0.66f, 0.77f, 0.88f, 0.96f
    };

    static readonly float[] AddictLaneZ =
    {
        LeftLaneZ, RightLaneZ,
        CenterLaneZ, -CenterLaneZ,
        LeftLaneZ, RightLaneZ,
        RightLaneZ, LeftLaneZ,
        -CenterLaneZ, CenterLaneZ
    };

    struct DeckSample
    {
        public Vector3 Position;
        public Vector3 Forward;
        public float PathDistance;
    }

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Early Highway Content", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceOnShowcase(log: true))
            EditorUtility.DisplayDialog("Early Highway Content", "Could not place early highway addicts.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzEarlyHighwayContentPlacer.PlaceOnShowcase</summary>
    public static void PlaceOnShowcase() => PlaceOnShowcase(log: false);

    public static bool PlaceOnShowcase(bool log)
    {
        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();
        var spawn = GetPlayerSpawn();
        var travelForward = GetPlayerTravelForward(spawn);
        var basePath = BuildEarlyDeckPath(spawn, travelForward, out var pathDiag);
        var addictPath = FilterPathMinAhead(spawn, travelForward, basePath, AddictMinAheadMeters);
        if (addictPath.Count < 4)
        {
            Debug.LogError(
                $"[Dutz] Could not build ahead deck path on {BridgeSegmentName} + {StraightSegmentName}. {pathDiag}");
            return false;
        }

        var hippiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (hippiePrefab == null)
        {
            Debug.LogError("[Dutz] Missing hippie prefab.");
            return false;
        }

        var segmentPool = SimpleCitizensHippieNpcSetup.ShowcaseUsesSegmentHippiePool();
        RemoveExistingNearSpawnCoins();
        if (!segmentPool)
            RemoveExistingNearSpawnAddicts();

        var addictsPlaced = segmentPool ? 0 : PlaceAddicts(hippiePrefab, addictPath, spawn, travelForward);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorPrefs.SetInt(PlacementVersionKey, PlacementVersion);

        if (log)
        {
            var span = addictPath[addictPath.Count - 1].PathDistance - addictPath[0].PathDistance;
            Debug.Log(
                $"[Dutz] Early highway addicts: {addictsPlaced} placed (>={AddictMinAheadMeters:F0}m ahead) " +
                $"over {span:F0}m on {BridgeSegmentName} + {StraightSegmentName}.");
        }

        return segmentPool || addictsPlaced == AddictCount;
    }

    public static void RemoveNearSpawnAddictsFromShowcase()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return;

        EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        RemoveExistingNearSpawnAddicts();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static void RemoveNearSpawnCoinsFromShowcase()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return;

        EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        RemoveExistingNearSpawnCoins();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    static List<DeckSample> BuildEarlyDeckPath(Vector3 spawn, Vector3 travelForward, out string diagnostics)
    {
        diagnostics = string.Empty;

        var bridge = GameObject.Find(BridgeSegmentName);
        var straight = GameObject.Find(StraightSegmentName);

        var bridgePts = new List<Vector3>();
        var straightPts = new List<Vector3>();
        if (bridge != null)
            CollectSegmentDeckPoints(bridge, spawn, bridgePts);
        if (straight != null)
            CollectSegmentDeckPoints(straight, spawn, straightPts);

        var ordered = new List<Vector3>();
        ordered.AddRange(bridgePts);
        ordered.AddRange(straightPts);
        ordered.Sort((a, b) =>
            AlongTrackAhead(spawn, a, travelForward).CompareTo(AlongTrackAhead(spawn, b, travelForward)));
        ordered = DedupeClosePoints(ordered, 3f);

        if (ordered.Count < 4)
        {
            ordered.Clear();
            if (bridge != null && TryGetSegmentBounds(bridge, out var bridgeBounds))
            {
                GetSegmentRoadAxis(bridge, bridgeBounds, out var bridgeAxis);
                var bridgeSpan = ProjectSpan(bridgeBounds, bridgeAxis);
                AddBoundsFallbackSamples(bridge, bridgeBounds, bridgeAxis, bridgeSpan, ordered);
            }

            if (straight != null && TryGetSegmentBounds(straight, out var straightBounds))
            {
                GetSegmentRoadAxis(straight, straightBounds, out var straightAxis);
                var straightSpan = ProjectSpan(straightBounds, straightAxis);
                AddBoundsFallbackSamples(straight, straightBounds, straightAxis, straightSpan, ordered);
            }

            ordered = DedupeClosePoints(ordered, 3f);
        }

        diagnostics =
            $"bridge={(bridge != null ? "ok" : "missing")} bridgePts={bridgePts.Count} " +
            $"straight={(straight != null ? "ok" : "missing")} straightPts={straightPts.Count} " +
            $"ordered={ordered.Count}";

        if (ordered.Count < 4)
            return BuildSyntheticForwardPath(spawn, travelForward, AddictMinAheadMeters, ForwardPathSpanMeters, 48);

        var path = new List<DeckSample>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var along = AlongTrackAhead(spawn, ordered[i], travelForward);
            path.Add(new DeckSample
            {
                Position = ordered[i],
                Forward = travelForward,
                PathDistance = along
            });
        }

        path.Sort((a, b) => a.PathDistance.CompareTo(b.PathDistance));

        if (path.Count < 4)
            return BuildSyntheticForwardPath(spawn, travelForward, AddictMinAheadMeters, ForwardPathSpanMeters, 48);

        var endDist = path[path.Count - 1].PathDistance;
        var trimDist = endDist - Mathf.Max(15f, (endDist - path[0].PathDistance) * EndInsetFraction);
        path.RemoveAll(s => s.PathDistance > trimDist);
        return path;
    }

    static List<DeckSample> FilterPathMinAhead(
        Vector3 spawn,
        Vector3 travelForward,
        List<DeckSample> path,
        float minAheadMeters)
    {
        var filtered = new List<DeckSample>();
        foreach (var sample in path)
        {
            if (sample.PathDistance >= minAheadMeters - 1f)
                filtered.Add(sample);
        }

        if (filtered.Count >= 4)
            return filtered;

        var maxAhead = path.Count > 0
            ? Mathf.Max(minAheadMeters + 40f, path[path.Count - 1].PathDistance)
            : minAheadMeters + ForwardPathSpanMeters;

        return BuildSyntheticForwardPath(spawn, travelForward, minAheadMeters, maxAhead, 48);
    }

    static List<DeckSample> BuildSyntheticForwardPath(
        Vector3 spawn,
        Vector3 travelForward,
        float startAhead,
        float endAhead,
        int count)
    {
        var path = new List<DeckSample>(count);
        for (var i = 0; i < count; i++)
        {
            var t = count <= 1 ? 0.5f : i / (float)(count - 1);
            var along = Mathf.Lerp(startAhead, endAhead, t);
            var xz = spawn + travelForward * along;
            var deck = SampleForwardDeckPosition(xz, spawn, travelForward, along);
            path.Add(new DeckSample
            {
                Position = deck,
                Forward = travelForward,
                PathDistance = along
            });
        }

        return path;
    }

    static Vector3 SampleForwardDeckPosition(Vector3 xz, Vector3 spawn, Vector3 travelForward, float along)
    {
        if (TryGetDeckPosition(new Vector3(xz.x, 0f, xz.z), spawn, out var deck))
            return deck;

        var y = SampleDeckY(xz.x, xz.z, spawn, spawn.y);
        return new Vector3(xz.x, y, xz.z);
    }

    static float SampleDeckY(float worldX, float worldZ, Vector3 spawn, float pathHintY)
    {
        if (TryGetDeckPosition(new Vector3(worldX, 0f, worldZ), spawn, out var deck))
            return deck.y;

        var sample = new Vector3(worldX, pathHintY, worldZ);
        if (DutzRoadGround.TrySampleRoadDeckY(sample, spawn.y, null, out var deckY))
            return deckY;

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(sample, spawn.y, null, out deckY))
            return deckY;

        return pathHintY;
    }

    static Vector3 GetPlayerTravelForward(Vector3 spawn)
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

    static float AlongTrackAhead(Vector3 spawn, Vector3 point, Vector3 travelForward)
    {
        var delta = point - spawn;
        delta.y = 0f;
        return Vector3.Dot(delta, travelForward);
    }

    static Vector3 EnsureMinAheadOnDeck(
        Vector3 world,
        Vector3 spawn,
        Vector3 travelForward,
        float minAheadMeters)
    {
        var along = AlongTrackAhead(spawn, world, travelForward);
        if (along >= minAheadMeters - 0.5f)
            return world;

        var shifted = spawn + travelForward * minAheadMeters;
        return SampleForwardDeckPosition(shifted, spawn, travelForward, minAheadMeters);
    }

    static int SortAlongSegment(Vector3 a, Vector3 b, GameObject segment, Vector3 fallbackForward)
    {
        if (segment == null)
            return a.x.CompareTo(b.x);

        if (!TryGetSegmentBounds(segment, out var bounds))
            return a.x.CompareTo(b.x);

        GetSegmentRoadAxis(segment, bounds, out var roadAxis);
        return Vector3.Dot(a, roadAxis).CompareTo(Vector3.Dot(b, roadAxis));
    }

    static void CollectSegmentDeckPoints(GameObject segment, Vector3 spawn, List<Vector3> points)
    {
        if (!TryGetSegmentBounds(segment, out var bounds))
            return;

        GetSegmentRoadAxis(segment, bounds, out var roadAxis);
        var roadSpan = ProjectSpan(bounds, roadAxis);
        if (roadSpan.max - roadSpan.min < 8f)
            return;

        for (var i = 0; i <= DeckSamplesPerSegment; i++)
        {
            var t = Mathf.Lerp(roadSpan.min, roadSpan.max, i / (float)DeckSamplesPerSegment);
            var centerLine = PointOnAxis(bounds.center, roadAxis, t);
            if (TrySnapDeckPoint(centerLine, segment, bounds, spawn, out var deckPos))
                points.Add(deckPos);
        }

        if (points.Count == 0)
            AddBoundsFallbackSamples(segment, bounds, roadAxis, roadSpan, points);
    }

    static void AddBoundsFallbackSamples(
        GameObject segment,
        Bounds bounds,
        Vector3 roadAxis,
        (float min, float max) roadSpan,
        List<Vector3> points)
    {
        var deckY = bounds.max.y - 0.5f;
        for (var i = 0; i <= DeckSamplesPerSegment; i++)
        {
            var t = Mathf.Lerp(roadSpan.min, roadSpan.max, i / (float)DeckSamplesPerSegment);
            var p = PointOnAxis(bounds.center, roadAxis, t);
            p.y = deckY;
            points.Add(p);
        }
    }

    static bool TryGetSegmentBounds(GameObject segment, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in segment.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (var collider in segment.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || collider.isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    static bool TrySnapDeckPoint(
        Vector3 worldXZ,
        GameObject segment,
        Bounds segmentBounds,
        Vector3 spawn,
        out Vector3 deckPos)
    {
        deckPos = new Vector3(worldXZ.x, segmentBounds.max.y - 1f, worldXZ.z);
        var probe = new Vector3(worldXZ.x, spawn.y + 40f, worldXZ.z);

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, spawn.y, null, out var deckY)
            || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
        {
            deckPos.y = deckY;
        }

        var closest = segmentBounds.ClosestPoint(deckPos);
        deckPos.x = closest.x;
        deckPos.z = closest.z;
        return true;
    }

    static void GetSegmentRoadAxis(GameObject segment, Bounds bounds, out Vector3 roadAxis)
    {
        var forward = Flatten(segment.transform.forward);
        var right = Flatten(segment.transform.right);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.forward;

        var extForward = ProjectSpan(bounds, forward);
        var extRight = ProjectSpan(bounds, right);
        roadAxis = extRight.max - extRight.min > extForward.max - extForward.min ? right : forward;
    }

    static int PlaceAddicts(GameObject prefab, List<DeckSample> path, Vector3 spawn, Vector3 travelForward)
    {
        var placed = 0;
        for (var i = 0; i < AddictCount; i++)
        {
            if (!TrySampleOnPath(path, AddictAlongFractions[i], out var sample))
                continue;

            sample.Position = EnsureMinAheadOnDeck(sample.Position, spawn, travelForward, AddictMinAheadMeters);

            var targetXZ = new Vector3(sample.Position.x, 0f, AddictLaneZ[i]);
            if (!TryGetDeckPosition(targetXZ, spawn, out var world))
                world = new Vector3(targetXZ.x, sample.Position.y, targetXZ.z);
            world = EnsureMinAheadOnDeck(world, spawn, travelForward, AddictMinAheadMeters);

            var rotation = Quaternion.LookRotation(sample.Forward, Vector3.up);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Place Near-Spawn Addict");
            go.name = $"{AddictPrefix}{i + 1:00}";
            go.transform.SetPositionAndRotation(world, rotation);
            SimpleCitizensHippieNpcSetup.SetupHippie(go);
            LockPivotXZ(go, targetXZ.x, targetXZ.z, spawn);
            placed++;
        }

        return placed;
    }

    static void LockPivotXZ(GameObject go, float x, float z, Vector3 spawn)
    {
        if (!TryGetDeckPosition(new Vector3(x, 0f, z), spawn, out var deck))
            return;

        go.transform.position = new Vector3(x, deck.y, z);

        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn != null)
            respawn.RecordSpawnPoint();
    }

    static bool TryGetDeckPosition(Vector3 targetXZ, Vector3 spawn, out Vector3 world)
    {
        world = new Vector3(targetXZ.x, spawn.y, targetXZ.z);
        var probe = new Vector3(targetXZ.x, spawn.y + 40f, targetXZ.z);
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, spawn.y, null, out var deckY)
            || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
        {
            world = new Vector3(targetXZ.x, deckY, targetXZ.z);
            return true;
        }

        var nearest = DutzHighwayDirection.FindNearestTrackSegment(world);
        if (nearest != null && TryGetSegmentBounds(nearest, out var bounds))
        {
            world = new Vector3(targetXZ.x, bounds.max.y - 1f, targetXZ.z);
            return true;
        }

        return false;
    }

    static bool TrySampleOnPath(List<DeckSample> path, float t01, out DeckSample sample)
    {
        sample = default;
        if (path == null || path.Count == 0)
            return false;

        var index = t01 * (path.Count - 1);
        var i0 = Mathf.Clamp(Mathf.FloorToInt(index), 0, path.Count - 1);
        var i1 = Mathf.Clamp(i0 + 1, 0, path.Count - 1);
        var blend = index - i0;

        var a = path[i0];
        var b = path[i1];
        sample = new DeckSample
        {
            Position = Vector3.Lerp(a.Position, b.Position, blend),
            Forward = Vector3.Slerp(a.Forward, b.Forward, blend).normalized,
            PathDistance = Mathf.Lerp(a.PathDistance, b.PathDistance, blend)
        };

        return true;
    }

    static List<Vector3> DedupeClosePoints(List<Vector3> points, float minSpacing)
    {
        if (points.Count <= 1)
            return points;

        var result = new List<Vector3> { points[0] };
        for (var i = 1; i < points.Count; i++)
        {
            if (HorizontalDistance(result[result.Count - 1], points[i]) >= minSpacing)
                result.Add(points[i]);
        }

        return result;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static Vector3 PointOnAxis(Vector3 center, Vector3 axis, float axisValue)
    {
        var offset = axisValue - Vector3.Dot(center, axis);
        return center + axis * offset;
    }

    static (float min, float max) ProjectSpan(Bounds bounds, Vector3 axis)
    {
        var c = bounds.center;
        var e = bounds.extents;
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;

        for (var xi = -1; xi <= 1; xi += 2)
        for (var yi = -1; yi <= 1; yi += 2)
        for (var zi = -1; zi <= 1; zi += 2)
        {
            var corner = c + Vector3.Scale(e, new Vector3(xi, yi, zi));
            var d = Vector3.Dot(corner, axis);
            if (d < min)
                min = d;
            if (d > max)
                max = d;
        }

        return (min, max);
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }

    static void RemoveExistingNearSpawnContent()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectNearSpawnInHierarchy(root, toRemove, removeAddicts: true, removeCoins: true);

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);
    }

    static void RemoveExistingNearSpawnAddicts()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectNearSpawnInHierarchy(root, toRemove, removeAddicts: true, removeCoins: false);

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);
    }

    static void RemoveExistingNearSpawnCoins()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectNearSpawnInHierarchy(root, toRemove, removeAddicts: false, removeCoins: true);

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);

        var coinsRoot = GameObject.Find(CoinsRootName);
        if (coinsRoot != null && coinsRoot.transform.childCount == 0)
            Undo.DestroyObjectImmediate(coinsRoot);
    }

    static void CollectNearSpawnInHierarchy(
        GameObject go,
        List<GameObject> list,
        bool removeAddicts,
        bool removeCoins)
    {
        if ((removeAddicts && go.name.StartsWith(AddictPrefix, System.StringComparison.Ordinal))
            || (removeCoins && go.name.StartsWith(CoinPrefix, System.StringComparison.Ordinal)))
        {
            list.Add(go);
            return;
        }

        foreach (Transform child in go.transform)
            CollectNearSpawnInHierarchy(child.gameObject, list, removeAddicts, removeCoins);
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

    public static bool NeedsApply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return false;

        var scene = SceneManager.GetActiveScene();
        if (!scene.path.Replace('\\', '/').EndsWith("Dutz_Level02.unity"))
            return true;

        var segmentPool = SimpleCitizensHippieNpcSetup.ShowcaseUsesSegmentHippiePool();
        if (segmentPool)
            return CountNearSpawnCoins() > 0;

        return EditorPrefs.GetInt(PlacementVersionKey, 0) < PlacementVersion
               || CountNearSpawnAddicts() < AddictCount
               || CountNearSpawnCoins() > 0;
    }

    public static void TryApplyToShowcase()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || !NeedsApply())
            return;

        if (SimpleCitizensHippieNpcSetup.ShowcaseUsesSegmentHippiePool())
        {
            RemoveNearSpawnCoinsFromShowcase();
            return;
        }

        PlaceOnShowcase(log: true);
    }

    static int CountNearSpawnAddicts()
    {
        var count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CountPrefixInHierarchy(root, AddictPrefix, ref count);

        return count;
    }

    static int CountNearSpawnCoins()
    {
        var count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CountPrefixInHierarchy(root, CoinPrefix, ref count);

        return count;
    }

    static void CountPrefixInHierarchy(GameObject go, string prefix, ref int count)
    {
        if (go.name.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            count++;
            return;
        }

        foreach (Transform child in go.transform)
            CountPrefixInHierarchy(child.gameObject, prefix, ref count);
    }
}
