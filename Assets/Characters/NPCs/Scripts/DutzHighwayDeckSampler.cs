using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime deck sampling along highway segments (mirrors DutzEarlyHighwayContentPlacer geometry).</summary>
public static class DutzHighwayDeckSampler
{
    public const float LeftLaneZ = 11f;
    public const float RightLaneZ = -11f;
    public const float CenterLaneZ = 4.5f;

    public static readonly float[] SevenLaneZ =
    {
        LeftLaneZ, RightLaneZ, CenterLaneZ, -CenterLaneZ, LeftLaneZ, RightLaneZ, -CenterLaneZ
    };

    const int DeckSamplesPerSegment = 40;

    public struct DeckSample
    {
        public Vector3 Position;
        public Vector3 Forward;
        public float PathDistance;
    }

    public struct SegmentPath
    {
        public int ProfileIndex;
        public GameObject Segment;
        public string SegmentName;
        public float StartAlong;
        public float EndAlong;
        public List<DeckSample> Samples;
    }

    public static List<SegmentPath> BuildOrderedSegmentPaths(
        IReadOnlyList<string> segmentNames,
        Vector3 spawnRef,
        Vector3 travelForward)
    {
        var paths = new List<SegmentPath>(segmentNames.Count);
        for (var profileIndex = 0; profileIndex < segmentNames.Count; profileIndex++)
        {
            var segmentName = segmentNames[profileIndex];
            var segment = FindHighwaySegment(segmentName);
            if (segment == null)
            {
                Debug.LogWarning($"[Dutz] Highway segment not found: {segmentName}");
                continue;
            }

            var path = BuildSegmentPath(segment, segmentName, spawnRef, travelForward);
            path.ProfileIndex = profileIndex;
            paths.Add(path);
        }

        return paths;
    }

    public static SegmentPath BuildSegmentPath(
        GameObject segment,
        string segmentName,
        Vector3 spawnRef,
        Vector3 travelForward)
    {
        var samples = CollectSegmentDeckSamples(segment, spawnRef, travelForward);
        var startAlong = float.PositiveInfinity;
        var endAlong = float.NegativeInfinity;

        foreach (var sample in samples)
        {
            if (sample.PathDistance < startAlong)
                startAlong = sample.PathDistance;
            if (sample.PathDistance > endAlong)
                endAlong = sample.PathDistance;
        }

        if (float.IsPositiveInfinity(startAlong))
        {
            startAlong = 0f;
            endAlong = 0f;
        }

        return new SegmentPath
        {
            Segment = segment,
            SegmentName = segmentName,
            StartAlong = startAlong,
            EndAlong = endAlong,
            Samples = samples
        };
    }

    public static bool TrySampleOnPath(List<DeckSample> path, float t01, out DeckSample sample)
    {
        sample = default;
        if (path == null || path.Count == 0)
            return false;

        var index = Mathf.Clamp01(t01) * (path.Count - 1);
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

    public static bool TrySampleMinAheadOnPath(
        List<DeckSample> path,
        float minAlong,
        out DeckSample sample)
    {
        sample = default;
        if (path == null || path.Count == 0)
            return false;

        for (var i = 0; i < path.Count; i++)
        {
            if (path[i].PathDistance >= minAlong - 0.5f)
            {
                sample = path[i];
                return true;
            }
        }

        sample = path[path.Count - 1];
        return true;
    }

    public static bool TrySampleNearAlongOnPath(
        List<DeckSample> path,
        float targetAlong,
        out DeckSample sample)
    {
        sample = default;
        if (path == null || path.Count == 0)
            return false;

        var bestIndex = 0;
        var bestDelta = float.PositiveInfinity;
        for (var i = 0; i < path.Count; i++)
        {
            var delta = Mathf.Abs(path[i].PathDistance - targetAlong);
            if (delta >= bestDelta)
                continue;

            bestDelta = delta;
            bestIndex = i;
        }

        sample = path[bestIndex];
        return true;
    }

    public static Vector3 PlaceOnLane(DeckSample sample, float laneZ, Vector3 spawnRef)
    {
        var targetXZ = new Vector3(sample.Position.x, 0f, laneZ);
        if (TryGetDeckPosition(targetXZ, spawnRef, out var world))
            return world;

        return new Vector3(targetXZ.x, sample.Position.y, targetXZ.z);
    }

    public static float AlongTrackAhead(Vector3 spawn, Vector3 point, Vector3 travelForward)
    {
        var delta = point - spawn;
        delta.y = 0f;
        return Vector3.Dot(delta, travelForward);
    }

    static List<DeckSample> CollectSegmentDeckSamples(GameObject segment, Vector3 spawnRef, Vector3 travelForward)
    {
        var points = new List<Vector3>();
        CollectSegmentDeckPoints(segment, spawnRef, points);

        var path = new List<DeckSample>(points.Count);
        foreach (var point in points)
        {
            path.Add(new DeckSample
            {
                Position = point,
                Forward = travelForward,
                PathDistance = AlongTrackAhead(spawnRef, point, travelForward)
            });
        }

        path.Sort((a, b) => a.PathDistance.CompareTo(b.PathDistance));
        return path;
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
            AddBoundsFallbackSamples(bounds, roadAxis, roadSpan, points);
    }

    static void AddBoundsFallbackSamples(
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

    static GameObject FindHighwaySegment(string segmentName)
    {
        if (string.IsNullOrEmpty(segmentName))
            return null;

        var direct = GameObject.Find(segmentName);
        if (direct != null)
            return direct;

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name == segmentName)
                return transform.gameObject;
        }

        return null;
    }
}
