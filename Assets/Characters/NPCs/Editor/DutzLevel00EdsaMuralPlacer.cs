using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places EDSA side-wall murals (L001–L018 left, R001–R018 right) on Dutz_Level00 highway segments 1–6.
/// Panels are spaced uniformly along the full track so left/right walls read as one continuous mural strip.
/// </summary>
public static class DutzLevel00EdsaMuralPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string RootName = "HighwayPhotoMurals";
    public const int ExpectedPanelCount = 36;

    public static readonly string[] SegmentNames =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        "Highway Straight 6",
    };

    /// <summary>Batch: -executeMethod DutzLevel00EdsaMuralPlacer.PlaceOnLevel00Batch</summary>
    public static void PlaceOnLevel00Batch() => PlaceOnLevel00(log: true);

    [MenuItem("Assets/Dutz Authoring/Rebuild Level 00 EDSA Highway Murals")]
    public static void RebuildFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("EDSA Murals", "Exit Play mode first.", "OK");
            return;
        }

        PlaceOnLevel00(log: true);
    }

    public static bool EnsureOnOpenScene(bool log)
    {
        // Never auto-rebuild. PlaceOnLevel00 destroys HighwayPhotoMurals and recreates panels.
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        if (log && NeedsEdsaMuralsRepublish())
        {
            Debug.LogWarning(
                "[Dutz] EDSA murals look incomplete. Use Assets/Dutz Authoring/Rebuild Level 00 EDSA Highway Murals only if you intend to wipe HighwayPhotoMurals.");
        }

        return false;
    }

    public static bool PlaceOnLevel00(bool log)
    {
        if (!File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        DutzLevel00EdsaMuralBuilder.ResyncTextures(log: false, force: false);
        if (!DutzLevel00EdsaMuralBuilder.HasAllSyncedTextures())
        {
            if (log)
            {
                Debug.LogError(
                    "[Dutz] Need all 36 EDSA mural PNGs in public/EDSA_murals_level00/ before placing murals.");
            }

            return false;
        }

        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (materialTemplate == null)
            return false;

        var settings = DutzHighwayPhotoBillboardPlacer.LoadBillboardSettings();
        var panelsPerSide = Mathf.Max(1, settings.panelsPerRoadSide);
        var totalPanelsPerSide = SegmentNames.Length * panelsPerSide;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        if (!TryGetTrackReference(out var spawn, out var travelForward))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve Level 00 track reference for EDSA murals.");
            return false;
        }

        var segmentPaths = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(SegmentNames, spawn, travelForward);
        if (segmentPaths.Count < SegmentNames.Length)
        {
            if (log)
                Debug.LogError("[Dutz] Missing highway segments for Level 00 EDSA murals.");
            return false;
        }

        var trackSpans = BuildCumulativeTrackSpans(segmentPaths);
        if (trackSpans.Count == 0)
        {
            if (log)
                Debug.LogError("[Dutz] Could not measure Level 00 highway length for EDSA murals.");
            return false;
        }

        var trackLength = trackSpans[trackSpans.Count - 1].EndDistance;
        var overlap = Mathf.Max(0f, settings.elevatedPanelOverlap);
        var panelStride = trackLength / totalPanelsPerSide;
        var panelWidth = panelStride + overlap;
        var panelHeight = ResolveUniformPanelHeight(segmentPaths, settings);

        DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth++;
        try
        {
            DutzHighwayPhotoBillboardPlacer.ClearBillboardPlacementCache();
            DutzHighwayPhotoBillboardPlacer.ClearSideWallMuralsRoot();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Place Level 00 EDSA Murals");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var segmentRoots = new Dictionary<string, Transform>(System.StringComparer.Ordinal);
            foreach (var segmentName in SegmentNames)
            {
                var segmentRoot = new GameObject($"EdsaMurals ({segmentName})");
                Undo.RegisterCreatedObjectUndo(segmentRoot, "Place Level 00 EDSA Murals");
                segmentRoot.transform.SetParent(root.transform, false);
                segmentRoot.transform.localPosition = Vector3.zero;
                segmentRoot.transform.localRotation = Quaternion.identity;
                segmentRoot.transform.localScale = Vector3.one;
                segmentRoots[segmentName] = segmentRoot.transform;
            }

            var placed = 0;
            for (var panelIndex = 0; panelIndex < totalPanelsPerSide; panelIndex++)
            {
                var muralNumber = panelIndex + 1;
                var targetDistance = (panelIndex + 0.5f) * panelStride;

                if (!TryFindTrackSpanAtDistance(trackSpans, targetDistance, out var trackSpan))
                {
                    if (log)
                        Debug.LogWarning($"[Dutz] No highway segment for EDSA mural distance {targetDistance:F1}m.");
                    continue;
                }

                var localT = Mathf.InverseLerp(trackSpan.StartDistance, trackSpan.EndDistance, targetDistance);
                if (!TrySampleSegmentDeck(trackSpan.Path, localT, out var deckSample))
                    continue;

                if (!segmentRoots.TryGetValue(trackSpan.Path.SegmentName, out var parent) || trackSpan.Path.Segment == null)
                    continue;

                var leftTexture = DutzLevel00EdsaMuralBuilder.LoadTexture('L', muralNumber);
                if (leftTexture != null
                    && DutzHighwayPhotoBillboardPlacer.PlaceEdsaTrackPanel(
                        trackSpan.Path.Segment,
                        parent,
                        deckSample.Position,
                        isLeftSide: true,
                        panelWidth,
                        panelHeight,
                        leftTexture,
                        materialTemplate,
                        settings,
                        $"EdsaMural_Edsa_L{muralNumber:000}",
                        $"Mural_{trackSpan.Path.SegmentName}_L_{muralNumber:00}"))
                {
                    placed++;
                }

                var rightTexture = DutzLevel00EdsaMuralBuilder.LoadTexture('R', muralNumber);
                if (rightTexture != null
                    && DutzHighwayPhotoBillboardPlacer.PlaceEdsaTrackPanel(
                        trackSpan.Path.Segment,
                        parent,
                        deckSample.Position,
                        isLeftSide: false,
                        panelWidth,
                        panelHeight,
                        rightTexture,
                        materialTemplate,
                        settings,
                        $"EdsaMural_Edsa_R{muralNumber:000}",
                        $"Mural_{trackSpan.Path.SegmentName}_R_{muralNumber:00}"))
                {
                    placed++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Level00ScenePath);
            AssetDatabase.SaveAssets();

            if (log)
            {
                Debug.Log(
                    $"[Dutz] Rebuilt {placed}/{ExpectedPanelCount} Level 00 EDSA mural(s) — " +
                    $"continuous {totalPanelsPerSide} panels per side " +
                    $"(stride {panelStride:F1}m, width {panelWidth:F1}m, height {panelHeight:F1}m).");
            }

            return placed == ExpectedPanelCount;
        }
        finally
        {
            DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth =
                Mathf.Max(0, DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth - 1);
        }
    }

    static float ResolveUniformPanelHeight(
        IReadOnlyList<DutzHighwayDeckSampler.SegmentPath> segmentPaths,
        DutzHighwayPhotoBillboardSettings settings)
    {
        var maxHeight = 0f;
        foreach (var path in segmentPaths)
        {
            if (path.Segment == null)
                continue;

            var collider = path.Segment.GetComponent<MeshCollider>();
            var renderer = path.Segment.GetComponent<Renderer>();
            if (collider == null && renderer == null)
                continue;

            var bounds = collider != null ? collider.bounds : renderer.bounds;
            var heightSpan = bounds.max.y - bounds.min.y;
            maxHeight = Mathf.Max(maxHeight, heightSpan * settings.tallWallHeightCoverage);
        }

        return maxHeight > 1f ? maxHeight : 90f;
    }

    struct TrackSpan
    {
        public DutzHighwayDeckSampler.SegmentPath Path;
        public float StartDistance;
        public float EndDistance;
    }

    static List<TrackSpan> BuildCumulativeTrackSpans(IReadOnlyList<DutzHighwayDeckSampler.SegmentPath> segmentPaths)
    {
        var spans = new List<TrackSpan>(segmentPaths.Count);
        var distance = 0f;

        foreach (var path in segmentPaths)
        {
            if (path.Segment == null)
                continue;

            var length = GetSegmentRoadLengthMeters(path.Segment);
            if (length < 1f)
                length = Mathf.Max(1f, path.EndAlong - path.StartAlong);

            spans.Add(new TrackSpan
            {
                Path = path,
                StartDistance = distance,
                EndDistance = distance + length
            });
            distance += length;
        }

        return spans;
    }

    static float GetSegmentRoadLengthMeters(GameObject segment)
    {
        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return 0f;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        DutzHighwayPhotoBillboardPlacer.GetSegmentTrackAxesPublic(segment, bounds, out var roadAxis, out _);
        var roadSpan = ProjectSpanOnAxis(bounds, roadAxis);
        return Mathf.Max(0f, roadSpan.max - roadSpan.min);
    }

    static (float min, float max) ProjectSpanOnAxis(Bounds bounds, Vector3 axis)
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
            var projection = Vector3.Dot(corner, axis);
            if (projection < min)
                min = projection;
            if (projection > max)
                max = projection;
        }

        return (min, max);
    }

    static bool TryFindTrackSpanAtDistance(IReadOnlyList<TrackSpan> spans, float distance, out TrackSpan result)
    {
        result = default;
        foreach (var span in spans)
        {
            if (distance < span.StartDistance - 0.01f || distance > span.EndDistance + 0.01f)
                continue;

            result = span;
            return true;
        }

        if (spans.Count == 0)
            return false;

        result = distance <= spans[0].StartDistance ? spans[0] : spans[spans.Count - 1];
        return result.Path.Segment != null;
    }

    static bool TrySampleSegmentDeck(DutzHighwayDeckSampler.SegmentPath path, float localT01, out DutzHighwayDeckSampler.DeckSample sample)
    {
        sample = default;
        if (path.Samples != null && path.Samples.Count > 0)
            return DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, localT01, out sample);

        if (path.Segment == null)
            return false;

        var collider = path.Segment.GetComponent<MeshCollider>();
        var renderer = path.Segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return false;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        DutzHighwayPhotoBillboardPlacer.GetSegmentTrackAxesPublic(path.Segment, bounds, out var roadAxis, out _);
        var roadSpan = ProjectSpanOnAxis(bounds, roadAxis);
        var roadT = Mathf.Lerp(roadSpan.min, roadSpan.max, Mathf.Clamp01(localT01));
        var centerLine = bounds.center + roadAxis * (roadT - Vector3.Dot(bounds.center, roadAxis));
        centerLine.y = bounds.min.y;

        sample = new DutzHighwayDeckSampler.DeckSample
        {
            Position = centerLine,
            Forward = path.Segment.transform.forward,
            PathDistance = roadT
        };
        return true;
    }

    static bool TryGetTrackReference(out Vector3 spawn, out Vector3 travelForward)
    {
        spawn = DutzHealthPotionPlacer.GetPlayer1SpawnForAuthoring();
        travelForward = Vector3.right;

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackSpawn, out var trackForward)
            && trackForward.sqrMagnitude > 0.001f)
        {
            spawn = trackSpawn;
            travelForward = trackForward.normalized;
            return true;
        }

        foreach (var segmentName in SegmentNames)
        {
            var segment = DutzHighwayPhotoBillboardPlacer.FindHighwaySegmentByName(segmentName);
            if (segment == null)
                continue;

            var forward = segment.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
            {
                travelForward = forward.normalized;
                return true;
            }
        }

        return false;
    }

    static bool NeedsEdsaMuralsRepublish()
    {
        var root = GameObject.Find(RootName);
        if (root == null)
            return true;

        var muralCount = 0;
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform != null && transform.name.StartsWith("Mural_"))
                muralCount++;
        }

        if (muralCount != ExpectedPanelCount)
            return true;

        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform == null || !transform.name.StartsWith("Mural_"))
                continue;

            var renderer = transform.GetComponent<MeshRenderer>();
            var material = renderer != null ? renderer.sharedMaterial : null;
            var texture = material != null ? material.mainTexture : null;
            if (texture == null || !texture.name.StartsWith("Edsa_"))
                return true;
        }

        return false;
    }
}
