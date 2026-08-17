using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places an extra DUTERTENGOT center-road mural on Highway Bridge 1 in Dutz_Level00.
/// </summary>
public static class DutzLevel00DuterTengotMuralPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string SegmentName = "Highway Bridge 1";
    const string MuralObjectName = "DuterTengotMural_Highway Bridge 1";
    const float PathPosition = 0.35f;
    const float MuralHeightPlayerMultiplier = 2f;
    const float FallbackPlayerHeightMeters = 3.7f;
    const float DeckClearanceMeters = 0.08f;

    public const string RootName = "DutzLevel00DuterTengotMural";

    /// <summary>Batch: -executeMethod DutzLevel00DuterTengotMuralPlacer.PlaceOnLevel00Batch</summary>
    public static void PlaceOnLevel00Batch() => PlaceOnLevel00(log: true);

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        if (HasMural())
            return false;

        return PlaceOnLevel00(log);
    }

    public static bool PlaceOnLevel00(bool log)
    {
        if (!File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        if (!DutzLevel00DuterTengotMuralBuilder.SyncPhoto(log: false, force: false))
        {
            if (log)
                Debug.LogError("[Dutz] Could not sync DUTERTENGOT.png from public/DUTERTE MURALS/.");
            return false;
        }

        var texture = DutzLevel00DuterTengotMuralBuilder.LoadSyncedTexture();
        if (texture == null)
        {
            if (log)
                Debug.LogError("[Dutz] DUTERTENGOT texture missing after sync.");
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
                Debug.LogError("[Dutz] Could not resolve Level 00 track reference for DUTERTENGOT mural.");
            return false;
        }

        var segmentPaths = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(
            new[] { SegmentName }, spawn, travelForward);
        if (segmentPaths.Count == 0 || segmentPaths[0].Samples == null || segmentPaths[0].Samples.Count == 0)
        {
            if (log)
                Debug.LogError("[Dutz] Highway Bridge 1 not found for DUTERTENGOT mural.");
            return false;
        }

        ClearExistingMural();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place DUTERTENGOT Mural");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var path = segmentPaths[0];
        if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, PathPosition, out var centerSample))
        {
            Object.DestroyImmediate(root);
            if (log)
                Debug.LogError("[Dutz] Could not sample deck on Highway Bridge 1.");
            return false;
        }

        centerSample.Position = DutzHighwayDeckSampler.PlaceOnLane(
            centerSample,
            DutzHighwayDeckSampler.CenterLaneZ,
            spawn);

        var panelHeight = GetPlayerReferenceHeightMeters() * MuralHeightPlayerMultiplier;
        if (!CreateCenterRoadMural(root.transform, texture, materialTemplate, centerSample, panelHeight))
        {
            Object.DestroyImmediate(root);
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Level00ScenePath);
        AssetDatabase.SaveAssets();

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed DUTERTENGOT cut-out mural (single panel) on {SegmentName}.");
        }

        return true;
    }

    static bool CreateCenterRoadMural(
        Transform parent,
        Texture2D texture,
        Material materialTemplate,
        DutzHighwayDeckSampler.DeckSample sample,
        float panelHeight)
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

        return DutzLevel00CutoutMuralPanels.CreatePanel(
            parent,
            MuralObjectName,
            texture,
            materialTemplate,
            boardCenter,
            faceDir,
            panelHeight,
            "Place DUTERTENGOT Mural",
            "MY GOD, I HATE DRUGS.") != null;
    }

    public static bool EnsureBumpInteraction(bool log) =>
        DutzMuralBumpMessage.EnsureLevel00MuralsInScene(log);

    static bool HasMural()
    {
        var mural = GameObject.Find(MuralObjectName);
        if (mural == null)
            return false;

        // Old double-panel layout must be replaced.
        if (mural.transform.Find("Back") != null)
            return false;

        return mural.transform.Find("Panel") != null || mural.transform.Find("Front") != null;
    }

    static void ClearExistingMural()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
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

        var segment = GameObject.Find(SegmentName);
        if (segment == null)
            return false;

        var forward = segment.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            return false;

        travelForward = forward.normalized;
        return true;
    }
}
