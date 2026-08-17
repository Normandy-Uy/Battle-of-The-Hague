using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Places coins/suitcases along the highway centerline ahead of Player1.</summary>
public static class DutzCollectibleTrackPlacer
{
    public const int TotalCollectibles = 50;
    public const float MinAheadMeters = 15f;
    public const float EndInsetMeters = 25f;
    public const float HeightAboveDeckMeters = 2f;
    public const float JumpHeightSafetyMargin = 0.75f;

    public static List<Vector3> BuildCollectiblePositions(out string diagnostics, bool log = false)
    {
        diagnostics = string.Empty;
        Physics.SyncTransforms();

        var spawn = GetPlayer1Spawn();
        var travelForward = GetPlayerTravelForward(spawn);
        var path = BuildMergedTrackPath(spawn, travelForward);

        if (path.Count < 2)
        {
            diagnostics = "Track path too short; using synthetic forward samples.";
            path = BuildSyntheticPath(spawn, travelForward);
        }

        path = FilterForwardPath(path, MinAheadMeters);
        if (path.Count < 2)
            path = BuildSyntheticPath(spawn, travelForward);

        if (path.Count < 2)
        {
            diagnostics = "Could not build any track path.";
            if (log)
                Debug.LogWarning($"[Dutz] {diagnostics}");
            return new List<Vector3>();
        }

        var minAlong = Mathf.Max(MinAheadMeters, path[0].PathDistance);
        var maxAlong = path[path.Count - 1].PathDistance - EndInsetMeters;
        if (maxAlong <= minAlong + 10f)
            maxAlong = minAlong + 120f;

        var maxReachAboveDeck = GetMaxJumpHeightAboveDeck();
        var lift = Mathf.Min(HeightAboveDeckMeters, maxReachAboveDeck);
        var positions = new List<Vector3>(TotalCollectibles);

        for (var i = 0; i < TotalCollectibles; i++)
        {
            var t = TotalCollectibles <= 1 ? 0.5f : i / (float)(TotalCollectibles - 1);
            var along = Mathf.Lerp(minAlong, maxAlong, t);
            var deck = SamplePathAtDistance(path, along);
            deck = SnapDeckHeight(deck, spawn);
            positions.Add(new Vector3(deck.x, deck.y + lift, deck.z));
        }

        diagnostics =
            $"spawn=({spawn.x:F1},{spawn.y:F1},{spawn.z:F1}) along={minAlong:F0}–{maxAlong:F0}m " +
            $"lift={lift:F1}m jumpMax={maxReachAboveDeck:F1}m pathPts={path.Count}";

        if (log && positions.Count > 0)
        {
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            foreach (var p in positions)
            {
                if (p.y < minY)
                    minY = p.y;
                if (p.y > maxY)
                    maxY = p.y;
            }

            Debug.Log(
                $"[Dutz] Built {positions.Count} collectible position(s) on track center. " +
                $"Y {minY:F1}–{maxY:F1}. {diagnostics}");
        }

        return positions;
    }

    public static void WriteSpawnPose(Component collectible)
    {
        if (collectible == null)
            return;

        Undo.RecordObject(collectible, "Write Collectible Spawn Pose");

        if (collectible is DutzGoldCoin coin)
            coin.CaptureSpawnPoseFromTransform();
        else if (collectible is DutzSuitcase suitcase)
            suitcase.CaptureSpawnPoseFromTransform();
        else if (collectible is DutzHealthPotion potion)
            potion.CaptureSpawnPoseFromTransform(force: true);

        EditorUtility.SetDirty(collectible);
    }

    static List<DutzHighwayDeckSampler.DeckSample> FilterForwardPath(
        List<DutzHighwayDeckSampler.DeckSample> path,
        float minAheadMeters)
    {
        var filtered = new List<DutzHighwayDeckSampler.DeckSample>();
        foreach (var sample in path)
        {
            if (sample.PathDistance >= minAheadMeters - 0.5f)
                filtered.Add(sample);
        }

        return filtered.Count >= 2 ? filtered : path;
    }

    static Vector3 SnapDeckHeight(Vector3 deck, Vector3 spawn)
    {
        var probe = new Vector3(deck.x, spawn.y + 40f, deck.z);
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, spawn.y, null, out var deckY)
            || DutzRoadGround.TrySampleRoadDeckY(probe, spawn.y, null, out deckY))
        {
            return new Vector3(deck.x, deckY, deck.z);
        }

        return deck;
    }

    static List<DutzHighwayDeckSampler.DeckSample> BuildMergedTrackPath(Vector3 spawn, Vector3 travelForward)
    {
        var merged = new List<DutzHighwayDeckSampler.DeckSample>();
        foreach (var segment in FindTrackSegments())
        {
            var segmentPath = DutzHighwayDeckSampler.BuildSegmentPath(segment, segment.name, spawn, travelForward);
            if (segmentPath.Samples == null || segmentPath.Samples.Count == 0)
                continue;

            merged.AddRange(segmentPath.Samples);
        }

        merged.Sort((a, b) => a.PathDistance.CompareTo(b.PathDistance));
        return DedupePath(merged, 3f);
    }

    static List<DutzHighwayDeckSampler.DeckSample> BuildSyntheticPath(Vector3 spawn, Vector3 travelForward)
    {
        var path = new List<DutzHighwayDeckSampler.DeckSample>(48);
        var endAhead = Mathf.Max(400f, GetTrackEndAhead(spawn, travelForward));

        for (var i = 0; i < 48; i++)
        {
            var t = i / 47f;
            var along = Mathf.Lerp(MinAheadMeters, endAhead, t);
            var xz = spawn + travelForward * along;
            var deck = SampleDeckAt(spawn, xz);
            path.Add(new DutzHighwayDeckSampler.DeckSample
            {
                Position = deck,
                Forward = travelForward,
                PathDistance = along
            });
        }

        return path;
    }

    static float GetTrackEndAhead(Vector3 spawn, Vector3 travelForward)
    {
        var best = float.NegativeInfinity;
        foreach (var segment in FindTrackSegments())
        {
            if (!TryGetSegmentBounds(segment, out var bounds))
                continue;

            foreach (var corner in BoundsCorners(bounds))
            {
                var along = DutzHighwayDeckSampler.AlongTrackAhead(spawn, corner, travelForward);
                if (along > best)
                    best = along;
            }
        }

        return best > float.NegativeInfinity ? best - EndInsetMeters : 780f;
    }

    static Vector3 SamplePathAtDistance(List<DutzHighwayDeckSampler.DeckSample> path, float along)
    {
        if (path.Count == 0)
            return Vector3.zero;

        if (along <= path[0].PathDistance)
            return path[0].Position;

        for (var i = 1; i < path.Count; i++)
        {
            var previous = path[i - 1];
            var current = path[i];
            if (along > current.PathDistance)
                continue;

            var span = Mathf.Max(0.001f, current.PathDistance - previous.PathDistance);
            var t = Mathf.Clamp01((along - previous.PathDistance) / span);
            return Vector3.Lerp(previous.Position, current.Position, t);
        }

        return path[path.Count - 1].Position;
    }

    static Vector3 SampleDeckAt(Vector3 spawn, Vector3 xz)
    {
        var probe = new Vector3(xz.x, spawn.y + 40f, xz.z);
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, spawn.y, null, out var deckY)
            || DutzRoadGround.TrySampleRoadDeckY(probe, spawn.y, null, out deckY))
        {
            return new Vector3(xz.x, deckY, xz.z);
        }

        return new Vector3(xz.x, spawn.y, xz.z);
    }

    static List<DutzHighwayDeckSampler.DeckSample> DedupePath(
        List<DutzHighwayDeckSampler.DeckSample> path,
        float minSeparation)
    {
        if (path.Count <= 1)
            return path;

        var deduped = new List<DutzHighwayDeckSampler.DeckSample>(path.Count) { path[0] };
        for (var i = 1; i < path.Count; i++)
        {
            var last = deduped[deduped.Count - 1].Position;
            if (Vector3.Distance(last, path[i].Position) >= minSeparation)
                deduped.Add(path[i]);
        }

        return deduped;
    }

    static List<GameObject> FindTrackSegments()
    {
        var segments = new List<GameObject>();
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null)
                continue;

            var name = transform.name;
            if (!name.Contains("Highway") && !name.Contains("Bridge"))
                continue;

            if (transform.parent != null
                && (transform.parent.name.Contains("Highway") || transform.parent.name.Contains("Bridge")))
                continue;

            segments.Add(transform.gameObject);
        }

        segments.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return segments;
    }

    static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
    {
        var c = bounds.center;
        var e = bounds.extents;
        for (var xi = -1; xi <= 1; xi += 2)
        for (var yi = -1; yi <= 1; yi += 2)
        for (var zi = -1; zi <= 1; zi += 2)
            yield return c + Vector3.Scale(e, new Vector3(xi, yi, zi));
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

        return hasBounds;
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
        return jumpForce * jumpForce / (2f * gravityMag) - JumpHeightSafetyMargin;
    }

    static Vector3 GetPlayer1Spawn()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            if (player == null || !player.name.Contains("Player1"))
                continue;

            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackSpawn, out _))
            return trackSpawn;

        return new Vector3(-390f, 18f, 1f);
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
}
