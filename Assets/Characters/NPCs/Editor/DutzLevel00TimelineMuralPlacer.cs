using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places one timeline mural per highway segment (1–7) in the center of the road on Dutz_Level00.
/// Murals are 2× Player1 height and face oncoming traffic.
/// </summary>
public static class DutzLevel00TimelineMuralPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string RootName = "DutzLevel00TimelineMurals";
    const float MuralHeightPlayerMultiplier = 2f;
    const float FallbackPlayerHeightMeters = 3.7f;
    const float DeckClearanceMeters = 0.08f;

    static readonly string[] SegmentNames =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        "Highway Straight 6",
        "Highway Straight 7",
    };

    /// <summary>Batch: -executeMethod DutzLevel00TimelineMuralPlacer.PlaceOnLevel00Batch</summary>
    public static void PlaceOnLevel00Batch() => PlaceOnLevel00(log: true);

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var changed = false;
        if (NeedsTimelineMuralsRepublish())
            changed = PlaceOnLevel00(log);

        return changed;
    }

    public static bool PlaceOnLevel00(bool log)
    {
        if (!File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        DutzLevel00TimelineMuralBuilder.SyncPhotos(log: false, force: false);
        var textures = DutzLevel00TimelineMuralBuilder.LoadSyncedTextures();
        if (textures.Count < SegmentNames.Length)
        {
            if (log)
            {
                Debug.LogError(
                    "[Dutz] Need all 7 timeline textures (DUTZ2016–DUTERTE2022). " +
                    "Add sources under public/ or public/DUTERTE MURALS/.");
            }

            return false;
        }

        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (materialTemplate == null)
            return false;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        if (!TryGetTrackReference(out var spawn, out var travelForward))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve Level 00 track reference for timeline murals.");
            return false;
        }

        ClearExistingMurals();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Level 00 Timeline Murals");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var segmentPaths = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(SegmentNames, spawn, travelForward);
        if (segmentPaths.Count < SegmentNames.Length)
        {
            if (log)
                Debug.LogError("[Dutz] Missing highway segments for Level 00 timeline murals.");
            Object.DestroyImmediate(root);
            return false;
        }

        var panelHeight = GetPlayerReferenceHeightMeters() * MuralHeightPlayerMultiplier;
        var placed = 0;

        for (var i = 0; i < SegmentNames.Length; i++)
        {
            var path = segmentPaths[i];
            if (path.Samples == null || path.Samples.Count == 0)
                continue;

            if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, 0.5f, out var centerSample))
                continue;

            centerSample.Position = DutzHighwayDeckSampler.PlaceOnLane(
                centerSample,
                DutzHighwayDeckSampler.CenterLaneZ,
                spawn);

            var texture = textures[Mathf.Min(i, textures.Count - 1)];
            var segmentRoot = new GameObject($"TimelineMurals ({SegmentNames[i]})");
            Undo.RegisterCreatedObjectUndo(segmentRoot, "Place Level 00 Timeline Murals");
            segmentRoot.transform.SetParent(root.transform, false);
            segmentRoot.transform.localPosition = Vector3.zero;
            segmentRoot.transform.localRotation = Quaternion.identity;
            segmentRoot.transform.localScale = Vector3.one;

            if (CreateCenterRoadMural(
                    segmentRoot.transform,
                    texture,
                    materialTemplate,
                    centerSample,
                    panelHeight,
                    i + 1,
                    SegmentNames[i]))
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
                $"[Dutz] Placed {placed} Level 00 timeline cut-out mural(s) — single panels at " +
                $"{MuralHeightPlayerMultiplier:0.#}× Player1 height ({panelHeight:0.#}m tall).");
        }

        return placed == SegmentNames.Length;
    }

    static bool CreateCenterRoadMural(
        Transform parent,
        Texture2D texture,
        Material materialTemplate,
        DutzHighwayDeckSampler.DeckSample sample,
        float panelHeight,
        int muralIndex,
        string segmentName)
    {
        if (texture == null)
            return false;

        var faceDir = -sample.Forward;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = Vector3.forward;
        faceDir.Normalize();

        var boardCenter = sample.Position;
        boardCenter.y += panelHeight * 0.5f + DeckClearanceMeters;

        var rootName = $"TimelineMural_{muralIndex:00}_{segmentName}";
        return DutzLevel00CutoutMuralPanels.CreatePanel(
            parent,
            rootName,
            texture,
            materialTemplate,
            boardCenter,
            faceDir,
            panelHeight,
            "Place Level 00 Timeline Murals") != null;
    }

    public static bool EnsureBumpInteraction(bool log) =>
        DutzMuralBumpMessage.EnsureLevel00MuralsInScene(log);

    static bool NeedsTimelineMuralsRepublish()
    {
        if (!HasCompleteTimelineMurals())
            return true;

        var root = GameObject.Find(RootName);
        if (root == null)
            return true;

        // Republish when murals still use the old Front+Back double-panel layout.
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform == null || !transform.name.StartsWith("TimelineMural_", System.StringComparison.Ordinal))
                continue;

            if (transform.Find("Back") != null)
                return true;

            if (transform.Find("Panel") == null && transform.Find("Front") == null)
                return true;
        }

        return false;
    }

    static float GetPlayerReferenceHeightMeters()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            if (player == null)
                continue;

            var renderers = player.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].enabled)
                        bounds.Encapsulate(renderers[i].bounds);
                }

                if (bounds.size.y > 0.5f)
                    return bounds.size.y;
            }

            var cc = player.GetComponent<CharacterController>();
            if (cc != null && cc.height > 0.1f)
                return cc.height;
        }

        return FallbackPlayerHeightMeters;
    }

    static bool HasCompleteTimelineMurals()
    {
        var root = GameObject.Find(RootName);
        if (root == null)
            return false;

        var count = 0;
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform != null && transform.name.StartsWith("TimelineMural_"))
                count++;
        }

        return count >= SegmentNames.Length;
    }

    static void ClearExistingMurals()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
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
            var segment = GameObject.Find(segmentName);
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

    static float GetPlayerHeightMeters() => GetPlayerReferenceHeightMeters();
}
