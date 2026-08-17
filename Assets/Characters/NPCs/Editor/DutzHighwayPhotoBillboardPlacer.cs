using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tiles low-poly Hague photos across highway side walls in Dutz_Level03.
/// Menu: Tools / Dutz / Place Hague Highway Billboards (Level 03)
/// </summary>
public static class DutzHighwayPhotoBillboardPlacer
{
    const string Level03ScenePath = "Assets/Scenes/Dutz_Level03.unity";
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string RootName = "HighwayPhotoMurals";
    const string LegacyRootName = "HighwayPhotoBillboards";
    const string SettingsPath = "Assets/Characters/HighwayBillboards/DutzHighwayPhotoBillboardSettings.asset";
    const string ExcludedSegmentName = "Highway Straight 6";
    const float FallbackBridgePanelHeight = 90f;
    const float FallbackBridgePanelWidth = 104f;

    internal static int SceneSyncSuppressDepth;
    internal static bool ShouldSkipSceneOpenedHandler => SceneSyncSuppressDepth > 0;

    static float? _cachedBridgePanelHeight;
    static float? _cachedBridgePanelWidth;

    static readonly string[] MuralSegmentOrder =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
    };

    /// <summary>Batch: -executeMethod DutzHighwayPhotoBillboardPlacer.RestoreLevel03MuralsBatch</summary>
    public static void RestoreLevel03MuralsBatch() => RestoreLevel03Murals(log: true);

    /// <summary>Batch: -executeMethod DutzHighwayPhotoBillboardPlacer.RestoreLevel00MuralsBatch</summary>
    public static void RestoreLevel00MuralsBatch() => RestoreLevel00Murals(log: true);

    /// <summary>Batch: -executeMethod DutzHighwayPhotoBillboardPlacer.RestoreLevel00HagueFromLevel03Batch</summary>
    public static void RestoreLevel00HagueFromLevel03Batch() => CopyHagueMuralsFromLevel03ToLevel00(log: true);

    /// <summary>Copies HighwayPhotoMurals from Level 03 onto Level 00 — preserves positions, never rebuilds from placer math.</summary>
    public static bool CopyHagueMuralsFromLevel03ToLevel00(bool log)
    {
        if (ShouldSkipSceneOpenedHandler)
            return false;

        if (!System.IO.File.Exists(Level03ScenePath) || !System.IO.File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Need Dutz_Level03 and Dutz_Level00 to copy Hague murals.");
            return false;
        }

        var returnScenePath = SceneManager.GetActiveScene().path;
        SceneSyncSuppressDepth++;
        try
        {
            var snapshots = CaptureHagueMuralsFromScene(Level03ScenePath);
            if (snapshots.Count == 0)
            {
                if (log)
                    Debug.LogError("[Dutz] No Hague murals found in Dutz_Level03 to copy.");
                return false;
            }

            if (!ApplyHagueMuralsToScene(Level00ScenePath, snapshots, log))
                return false;

            if (log)
            {
                Debug.Log(
                    $"[Dutz] Copied {snapshots.Count} Hague side-wall mural(s) from Dutz_Level03 to Dutz_Level00.");
            }

            return true;
        }
        finally
        {
            SceneSyncSuppressDepth--;
            if (!string.IsNullOrEmpty(returnScenePath)
                && returnScenePath != SceneManager.GetActiveScene().path
                && System.IO.File.Exists(returnScenePath))
            {
                SceneSyncSuppressDepth++;
                try
                {
                    EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
                }
                finally
                {
                    SceneSyncSuppressDepth--;
                }
            }
        }
    }

    public static bool RestoreLevel00HagueFromLevel03IfNeeded(bool log)
    {
        // Manual / batch only — never auto-run on scene open (opens Level 03 and loops scene callbacks).
        return false;
    }

    struct HagueMuralSnapshot
    {
        public string SegmentGroupName;
        public string PanelName;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Texture MainTexture;
    }

    static List<HagueMuralSnapshot> CaptureHagueMuralsFromScene(string scenePath)
    {
        var snapshots = new List<HagueMuralSnapshot>();
        if (!System.IO.File.Exists(scenePath))
            return snapshots;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var root = GameObject.Find(RootName);
        if (root == null)
            return snapshots;

        foreach (Transform segmentGroup in root.transform)
        {
            if (segmentGroup == null)
                continue;

            foreach (Transform panel in segmentGroup)
            {
                if (panel == null || !panel.name.StartsWith("Mural_"))
                    continue;

                var renderer = panel.GetComponent<MeshRenderer>();
                snapshots.Add(new HagueMuralSnapshot
                {
                    SegmentGroupName = segmentGroup.name,
                    PanelName = panel.name,
                    Position = panel.position,
                    Rotation = panel.rotation,
                    Scale = panel.localScale,
                    MainTexture = renderer != null && renderer.sharedMaterial != null
                        ? renderer.sharedMaterial.mainTexture
                        : null
                });
            }
        }

        return snapshots;
    }

    static bool ApplyHagueMuralsToScene(string scenePath, List<HagueMuralSnapshot> snapshots, bool log)
    {
        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (materialTemplate == null)
            return false;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Restore Hague Murals");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var segmentGroups = new Dictionary<string, Transform>();
        foreach (var snapshot in snapshots)
        {
            if (!segmentGroups.TryGetValue(snapshot.SegmentGroupName, out var segmentGroup))
            {
                var groupGo = new GameObject(snapshot.SegmentGroupName);
                Undo.RegisterCreatedObjectUndo(groupGo, "Restore Hague Murals");
                groupGo.transform.SetParent(root.transform, false);
                groupGo.transform.localPosition = Vector3.zero;
                groupGo.transform.localRotation = Quaternion.identity;
                groupGo.transform.localScale = Vector3.one;
                segmentGroup = groupGo.transform;
                segmentGroups[snapshot.SegmentGroupName] = segmentGroup;
            }

            var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(panel, "Restore Hague Murals");
            panel.name = snapshot.PanelName;
            panel.transform.SetParent(segmentGroup, false);
            panel.transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);
            panel.transform.localScale = snapshot.Scale;
            Object.DestroyImmediate(panel.GetComponent<Collider>());

            var muralMaterial = new Material(materialTemplate)
            {
                mainTexture = snapshot.MainTexture,
                name = $"HagueMural_{snapshot.PanelName}"
            };
            panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        PersistScene(scene, scenePath);
        return snapshots.Count > 0;
    }

    static bool HagueMuralsDifferFromLevel03Reference()
    {
        var reference = CaptureHagueMuralsFromScene(Level03ScenePath);
        if (reference.Count == 0)
            return false;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);

        var root = GameObject.Find(RootName);
        if (root == null)
            return true;

        var currentCount = 0;
        foreach (Transform panel in root.GetComponentsInChildren<Transform>(true))
        {
            if (panel != null && panel.name.StartsWith("Mural_"))
                currentCount++;
        }

        if (currentCount != reference.Count)
            return true;

        var referenceByName = new Dictionary<string, HagueMuralSnapshot>();
        foreach (var snapshot in reference)
            referenceByName[snapshot.PanelName] = snapshot;

        foreach (Transform panel in root.GetComponentsInChildren<Transform>(true))
        {
            if (panel == null || !panel.name.StartsWith("Mural_"))
                continue;

            if (!referenceByName.TryGetValue(panel.name, out var expected))
                return true;

            if (Vector3.Distance(panel.position, expected.Position) > 0.05f)
                return true;

            if (Vector3.Distance(panel.localScale, expected.Scale) > 0.05f)
                return true;
        }

        return false;
    }

    /// <summary>Recovery — sync Hague photos and rebuild side-wall murals on Level 00 (does not touch timeline road murals).</summary>
    public static void RestoreLevel00Murals(bool log)
    {
        if (!CopyHagueMuralsFromLevel03ToLevel00(log))
            Debug.LogError("[Dutz] Restore failed — could not copy Hague murals from Dutz_Level03 to Dutz_Level00.");
    }

    public static bool EnsureLevel00HagueMurals(bool log) => RestoreLevel00HagueFromLevel03IfNeeded(log);

    public static bool PlaceOnLevel00(bool log) => CopyHagueMuralsFromLevel03ToLevel00(log);

    /// <summary>Recovery — sync Hague photos and rebuild all murals (no cancel dialog).</summary>
    public static void RestoreLevel03Murals(bool log)
    {
        if (DutzHaguePhotoBillboardBuilder.SyncPhotos(log: log) == 0)
        {
            Debug.LogError("[Dutz] Restore failed — add Hague1.jpg … HAGUE8.png to public/ and retry.");
            return;
        }

        if (!PlaceOnLevel03(log: log))
            Debug.LogError("[Dutz] Restore failed — could not place murals on Dutz_Level03.");
        else if (log)
            Debug.Log("[Dutz] Hague murals restored on Dutz_Level03.");
    }

    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Restore Murals", "Exit Play mode first.", "OK");
            return;
        }

        RestoreLevel03Murals(log: true);
        EditorUtility.DisplayDialog(
            "Restore Murals",
            "Hague murals rebuilt on Dutz_Level03.\n\nRe-tune positions with Reposition Hague Murals if needed.",
            "OK");
    }

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hague Murals", "Exit Play mode first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Hague Murals — DESTRUCTIVE",
                "This deletes all existing Hague murals and rebuilds them from scratch.\n\n" +
                "Custom positions you moved by hand will be LOST.\n\n" +
                "To nudge existing murals without rebuilding, use:\n" +
                "Tools / Dutz / Reposition Hague Murals (Level 03)\n\n" +
                "Continue with full rebuild?",
                "Rebuild All",
                "Cancel"))
        {
            return;
        }

        if (!PlaceOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Hague Murals",
                "Could not place murals on Dutz_Level03.\n\n" +
                "1. Add Hague1.jpg … Hague8.png to public/\n" +
                "2. Run Sync Hague Photos (Low Poly)\n" +
                "3. Ensure Dutz_Level03 has highway segments",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Hague Murals",
            "Hague photos now cover the highway side walls on Dutz_Level03.",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzHighwayPhotoBillboardPlacer.RepositionLevel03MuralsBatch</summary>
    public static void RepositionLevel03MuralsBatch() => RepositionOnLevel03(log: true);

    public static void RepositionFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hague Murals", "Exit Play mode first.", "OK");
            return;
        }

        if (!RepositionOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Hague Murals",
                "Could not reposition murals on Dutz_Level03.\n\nEnsure HighwayPhotoMurals exists in the scene.",
                "OK");
        }
    }

    public static bool RepositionOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var settings = AssetDatabase.LoadAssetAtPath<DutzHighwayPhotoBillboardSettings>(SettingsPath)
            ?? DutzHaguePhotoBillboardBuilder.EnsureSettings();

        ClearBridgeReferenceCache();

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogError("[Dutz] HighwayPhotoMurals root not found in Dutz_Level03.");
            return false;
        }

        var moved = 0;
        foreach (var panel in root.GetComponentsInChildren<Transform>(true))
        {
            if (!TryParseMuralName(panel.name, out var segmentName, out var isLeftSide))
                continue;

            var segment = FindSegmentByName(segmentName);
            if (segment == null)
                continue;

            if (IsExcludedSegment(segmentName))
                continue;

            if (!TryGetSegmentLayout(segment, out var bounds, out var roadAxis, out var wallAxis, out var wallSpan, out var roadSpan))
                continue;

            if (!TryParseMuralIndex(panel.name, out var panelIndex))
                continue;

            var panelsPerSide = Mathf.Max(1, settings.panelsPerRoadSide);
            var roadLength = roadSpan.max - roadSpan.min;
            ResolvePanelMetrics(segment.name, bounds, roadLength, panelsPerSide, settings,
                out var panelWidth, out var panelHeight);
            var fittedWidth = roadLength / panelsPerSide;
            var wallAxisValue = isLeftSide ? wallSpan.min : wallSpan.max;
            var roadT = roadSpan.min + fittedWidth * (panelIndex + 0.5f);
            var roadPoint = PointOnAxis(bounds.center, roadAxis, roadT);
            roadPoint.y = bounds.min.y;
            var outward = GetOutwardFromHighwayEdge(bounds, wallAxis, wallAxisValue);
            var centerY = bounds.min.y + panelHeight * 0.5f;
            var collider = segment.GetComponent<MeshCollider>();
            SnapToWallFace(
                collider, bounds, roadPoint, centerY, wallAxis, wallAxisValue, outward,
                settings.elevatedLateralOffset, out var newPos, out var faceDir);

            faceDir.y = 0f;
            if (faceDir.sqrMagnitude < 0.001f)
                faceDir = -FlattenOutward(outward);
            faceDir.Normalize();

            Undo.RecordObject(panel, "Reposition Hague Murals");
            panel.SetPositionAndRotation(
                newPos,
                Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f));
            panel.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
            EditorUtility.SetDirty(panel.gameObject);
            moved++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        PersistLevel03Scene(scene);

        if (log)
        {
            LogMuralOffsetDiagnostics(root, settings.elevatedLateralOffset);
            Debug.Log($"[Dutz] Repositioned {moved} Hague mural(s) to {settings.elevatedLateralOffset} unit(s) from highway sides.");
        }

        return moved > 0;
    }

    static void PersistLevel03Scene(Scene scene) => PersistScene(scene, Level03ScenePath);

    /// <summary>Batch: -executeMethod DutzHighwayPhotoBillboardPlacer.PlaceOnLevel03Batch</summary>
    public static void PlaceOnLevel03Batch() => PlaceOnLevel03(log: true);

    /// <summary>Batch: -executeMethod DutzHighwayPhotoBillboardPlacer.SetupLevel03BillboardsBatch</summary>
    public static void SetupLevel03BillboardsBatch()
    {
        if (DutzHaguePhotoBillboardBuilder.SyncPhotos(log: true) == 0)
            return;

        PlaceOnLevel03(log: true);
    }

    public static bool PlaceOnLevel03(bool log) => PlaceHagueMuralsOnScene(Level03ScenePath, log);

    static bool PlaceHagueMuralsOnScene(string scenePath, bool log)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError("[Dutz] Scene not found: " + scenePath);
            return false;
        }

        if (DutzHaguePhotoBillboardBuilder.SyncPhotos(log: false) == 0)
            return false;

        var textures = DutzHaguePhotoBillboardBuilder.LoadSyncedTextures();
        if (textures.Count == 0)
        {
            Debug.LogError("[Dutz] No Hague mural textures found after sync.");
            return false;
        }

        var material = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (material == null)
            return false;

        var settings = AssetDatabase.LoadAssetAtPath<DutzHighwayPhotoBillboardSettings>(SettingsPath)
            ?? DutzHaguePhotoBillboardBuilder.EnsureSettings();

        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        ClearBridgeReferenceCache();
        ClearExisting();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Hague Highway Murals");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var placed = 0;
        var textureIndex = 0;
        foreach (var segment in FindTrackSegments())
        {
            var segmentRoot = new GameObject($"HagueMurals ({segment.name})");
            Undo.RegisterCreatedObjectUndo(segmentRoot, "Place Hague Highway Murals");
            segmentRoot.transform.SetParent(root.transform, false);
            segmentRoot.transform.localPosition = Vector3.zero;
            segmentRoot.transform.localRotation = Quaternion.identity;
            segmentRoot.transform.localScale = Vector3.one;

            placed += PlaceMuralsOnSegment(segment, segmentRoot.transform, textures, material, settings, ref textureIndex);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        PersistScene(scene, scenePath);

        if (log)
        {
            var sceneLabel = scenePath.Contains("Level00") ? "Dutz_Level00" : "Dutz_Level03";
            Debug.Log(
                $"[Dutz] Placed {placed} Hague side-wall mural(s) on {sceneLabel} — " +
                $"{settings.panelsPerRoadSide} per side, segments through Highway Bridge 5 (no {ExcludedSegmentName}).");
        }

        return placed > 0;
    }

    static void PersistScene(Scene scene, string scenePath)
    {
        if (!scene.IsValid())
            scene = EditorSceneManager.GetSceneByPath(scenePath);

        if (!scene.IsValid())
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!EditorSceneManager.SaveScene(scene, scenePath))
            Debug.LogError($"[Dutz] Failed to save {scenePath}");

        AssetDatabase.SaveAssets();
    }

    static int PlaceMuralsOnSegment(
        GameObject segment,
        Transform parent,
        List<Texture2D> textures,
        Material materialTemplate,
        DutzHighwayPhotoBillboardSettings settings,
        ref int textureIndex)
    {
        if (IsExcludedSegment(segment.name))
            return 0;

        return PlaceTallMuralsOnSegment(
            segment, parent, textures, materialTemplate, settings, ref textureIndex);
    }

    static bool IsExcludedSegment(string segmentName) =>
        segmentName == ExcludedSegmentName
        || segmentName.Contains(ExcludedSegmentName);

    static int PlaceTallMuralsOnSegment(
        GameObject segment,
        Transform parent,
        List<Texture2D> textures,
        Material materialTemplate,
        DutzHighwayPhotoBillboardSettings settings,
        ref int textureIndex)
    {
        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return 0;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        GetSegmentTrackAxes(segment, bounds, out var roadAxis, out var wallAxis);
        var roadSpan = ProjectSpan(bounds, roadAxis);
        var wallSpan = ProjectSpan(bounds, wallAxis);
        var roadLength = roadSpan.max - roadSpan.min;
        if (roadLength < 4f)
            return 0;

        var placed = 0;
        var panelsPerSide = Mathf.Max(1, settings.panelsPerRoadSide);

        placed += TileTallSide(
            segment, collider, parent, segment.name, bounds, roadAxis, wallAxis, roadSpan, wallSpan,
            roadLength, isLeftSide: true, panelsPerSide, textures, materialTemplate, settings, ref textureIndex);

        placed += TileTallSide(
            segment, collider, parent, segment.name, bounds, roadAxis, wallAxis, roadSpan, wallSpan,
            roadLength, isLeftSide: false, panelsPerSide, textures, materialTemplate, settings, ref textureIndex);

        return placed;
    }

    static int TileTallSide(
        GameObject segment,
        MeshCollider collider,
        Transform parent,
        string segmentName,
        Bounds bounds,
        Vector3 roadAxis,
        Vector3 wallAxis,
        (float min, float max) roadSpan,
        (float min, float max) wallSpan,
        float roadLength,
        bool isLeftSide,
        int panelCount,
        List<Texture2D> textures,
        Material materialTemplate,
        DutzHighwayPhotoBillboardSettings settings,
        ref int textureIndex)
    {
        ResolvePanelMetrics(segmentName, bounds, roadLength, panelCount, settings,
            out var panelWidth, out var panelHeight);
        var fittedWidth = roadLength / panelCount;
        var placed = 0;
        var sideSuffix = isLeftSide ? "L" : "R";
        var wallAxisValue = isLeftSide ? wallSpan.min : wallSpan.max;
        var outward = GetOutwardFromHighwayEdge(bounds, wallAxis, wallAxisValue);

        for (var i = 0; i < panelCount; i++)
        {
            var roadT = roadSpan.min + fittedWidth * (i + 0.5f);
            var roadPoint = PointOnAxis(bounds.center, roadAxis, roadT);
            roadPoint.y = bounds.min.y;

            var texture = textures[textureIndex % textures.Count];
            textureIndex++;

            var centerY = bounds.min.y + panelHeight * 0.5f;
            SnapToWallFace(
                collider, bounds, roadPoint, centerY, wallAxis, wallAxisValue, outward,
                settings.elevatedLateralOffset, out var boardCenter, out var faceDir);

            faceDir.y = 0f;
            if (faceDir.sqrMagnitude < 0.001f)
                faceDir = -outward;
            faceDir.Normalize();

            var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(panel, "Place Hague Highway Murals");
            panel.name = $"Mural_{segmentName}_{sideSuffix}_{i + 1:00}";
            panel.transform.SetParent(parent, false);
            panel.transform.position = boardCenter;
            panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
            Object.DestroyImmediate(panel.GetComponent<Collider>());

            var muralMaterial = new Material(materialTemplate) { mainTexture = texture };
            muralMaterial.name = $"HagueMural_{texture.name}";
            panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;
            placed++;
        }

        return placed;
    }

    public static void ClearSideWallMuralsRoot() => ClearExisting();

    public static GameObject FindHighwaySegmentByName(string segmentName) => FindSegmentByName(segmentName);

    public static void GetSegmentTrackAxesPublic(
        GameObject segment,
        Bounds bounds,
        out Vector3 roadAxis,
        out Vector3 wallAxis) => GetSegmentTrackAxes(segment, bounds, out roadAxis, out wallAxis);

    public static DutzHighwayPhotoBillboardSettings LoadBillboardSettings() =>
        AssetDatabase.LoadAssetAtPath<DutzHighwayPhotoBillboardSettings>(SettingsPath)
        ?? DutzHaguePhotoBillboardBuilder.EnsureSettings();

    public static void ClearBillboardPlacementCache() => ClearBridgeReferenceCache();

    public static bool PlaceSideWallPanel(
        GameObject segment,
        Transform parent,
        bool isLeftSide,
        int panelIndexZeroBased,
        int panelsPerSide,
        Texture2D texture,
        Material materialTemplate,
        DutzHighwayPhotoBillboardSettings settings,
        string materialInstanceName)
    {
        if (segment == null || parent == null || texture == null || materialTemplate == null || settings == null)
            return false;

        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return false;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        GetSegmentTrackAxes(segment, bounds, out var roadAxis, out var wallAxis);
        var roadSpan = ProjectSpan(bounds, roadAxis);
        var wallSpan = ProjectSpan(bounds, wallAxis);
        var roadLength = roadSpan.max - roadSpan.min;
        if (roadLength < 4f || panelsPerSide < 1)
            return false;

        ResolvePanelMetrics(segment.name, bounds, roadLength, panelsPerSide, settings,
            out var panelWidth, out var panelHeight);
        var fittedWidth = roadLength / panelsPerSide;
        var sideSuffix = isLeftSide ? "L" : "R";
        var wallAxisValue = isLeftSide ? wallSpan.min : wallSpan.max;
        var outward = GetOutwardFromHighwayEdge(bounds, wallAxis, wallAxisValue);

        var roadT = roadSpan.min + fittedWidth * (panelIndexZeroBased + 0.5f);
        var roadPoint = PointOnAxis(bounds.center, roadAxis, roadT);
        roadPoint.y = bounds.min.y;

        var centerY = bounds.min.y + panelHeight * 0.5f;
        SnapToWallFace(
            collider, bounds, roadPoint, centerY, wallAxis, wallAxisValue, outward,
            settings.elevatedLateralOffset, out var boardCenter, out var faceDir);

        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = -outward;
        faceDir.Normalize();

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, "Place EDSA Highway Murals");
        panel.name = $"Mural_{segment.name}_{sideSuffix}_{panelIndexZeroBased + 1:00}";
        panel.transform.SetParent(parent, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        var muralMaterial = new Material(materialTemplate) { mainTexture = texture };
        muralMaterial.name = string.IsNullOrEmpty(materialInstanceName)
            ? $"EdsaMural_{texture.name}"
            : materialInstanceName;
        panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;
        return true;
    }

    /// <summary>
    /// Places one EDSA panel at a world road point using explicit width/height (track-continuous layout).
    /// </summary>
    public static bool PlaceEdsaTrackPanel(
        GameObject segment,
        Transform parent,
        Vector3 roadPoint,
        bool isLeftSide,
        float panelWidth,
        float panelHeight,
        Texture2D texture,
        Material materialTemplate,
        DutzHighwayPhotoBillboardSettings settings,
        string materialInstanceName,
        string panelName)
    {
        if (segment == null || parent == null || texture == null || materialTemplate == null || settings == null)
            return false;

        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return false;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        GetSegmentTrackAxes(segment, bounds, out _, out var wallAxis);
        var wallSpan = ProjectSpan(bounds, wallAxis);
        var sideSuffix = isLeftSide ? "L" : "R";
        var wallAxisValue = isLeftSide ? wallSpan.min : wallSpan.max;
        var outward = GetOutwardFromHighwayEdge(bounds, wallAxis, wallAxisValue);

        roadPoint.y = bounds.min.y;
        var centerY = bounds.min.y + panelHeight * 0.5f;
        SnapToWallFace(
            collider, bounds, roadPoint, centerY, wallAxis, wallAxisValue, outward,
            settings.elevatedLateralOffset, out var boardCenter, out var faceDir);

        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = -outward;
        faceDir.Normalize();

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, "Place EDSA Highway Murals");
        panel.name = string.IsNullOrEmpty(panelName)
            ? $"Mural_{segment.name}_{sideSuffix}_00"
            : panelName;
        panel.transform.SetParent(parent, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        var muralMaterial = new Material(materialTemplate) { mainTexture = texture };
        muralMaterial.name = string.IsNullOrEmpty(materialInstanceName)
            ? $"EdsaMural_{texture.name}"
            : materialInstanceName;
        panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;
        return true;
    }

    static void SnapToWallFace(
        MeshCollider collider,
        Bounds bounds,
        Vector3 roadPoint,
        float centerY,
        Vector3 wallAxis,
        float wallAxisValue,
        Vector3 outward,
        float lateralOffset,
        out Vector3 boardCenter,
        out Vector3 faceDir)
    {
        var flatOutward = FlattenOutward(outward);
        faceDir = -flatOutward;

        var wallEdge = PointOnAxis(roadPoint, wallAxis, wallAxisValue);
        var wallPoint = wallEdge;
        wallPoint.y = centerY;

        if (collider != null && TryGetOutermostWallPoint(collider, bounds, roadPoint, flatOutward, out var meshWall))
            wallPoint = meshWall;

        boardCenter = wallPoint + flatOutward * Mathf.Max(0f, lateralOffset);
        boardCenter.y = centerY;

        if (collider == null)
            return;

        var maxDist = Mathf.Max(bounds.extents.x, bounds.extents.z) * 2f;
        var origin = roadPoint;
        origin.y = centerY;
        var rayOrigin = origin + flatOutward * maxDist;
        if (!collider.Raycast(new Ray(rayOrigin, -flatOutward), out var hit, maxDist * 2f))
            return;

        var hitNormal = hit.normal;
        hitNormal.y = 0f;
        if (hitNormal.sqrMagnitude > 0.001f)
            faceDir = -hitNormal.normalized;
    }

    static bool TryGetOutermostWallPoint(
        MeshCollider collider,
        Bounds bounds,
        Vector3 roadPoint,
        Vector3 flatOutward,
        out Vector3 wallPoint)
    {
        wallPoint = roadPoint;
        var maxDist = Mathf.Max(bounds.extents.x, bounds.extents.z) * 2f;
        var heightSpan = ProjectSpan(bounds, Vector3.up);
        var bestDepth = float.NegativeInfinity;
        var found = false;

        foreach (var t in new[] { 0.1f, 0.35f, 0.55f, 0.75f, 0.9f })
        {
            var sample = roadPoint;
            sample.y = Mathf.Lerp(heightSpan.min, heightSpan.max, t);
            var rayOrigin = sample + flatOutward * maxDist;
            if (!collider.Raycast(new Ray(rayOrigin, -flatOutward), out var hit, maxDist * 2f))
                continue;

            var depth = Vector3.Dot(hit.point - roadPoint, flatOutward);
            if (depth <= bestDepth)
                continue;

            bestDepth = depth;
            wallPoint = hit.point;
            found = true;
        }

        return found;
    }

    static Vector3 FlattenOutward(Vector3 outward)
    {
        var flat = outward;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            return outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;
        return flat.normalized;
    }

    static void LogMuralOffsetDiagnostics(GameObject root, float targetOffset)
    {
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        foreach (var panel in root.GetComponentsInChildren<Transform>(true))
        {
            if (!TryParseMuralName(panel.name, out var segmentName, out var isLeftSide))
                continue;

            if (!TryParseMuralIndex(panel.name, out var panelIndex))
                continue;

            var segment = FindSegmentByName(segmentName);
            if (segment == null
                || !TryGetSegmentLayout(segment, out var bounds, out var roadAxis, out var wallAxis, out var wallSpan, out var roadSpan))
                continue;

            var settings = AssetDatabase.LoadAssetAtPath<DutzHighwayPhotoBillboardSettings>(SettingsPath);
            var panelsPerSide = Mathf.Max(1, settings != null ? settings.panelsPerRoadSide : 3);
            var roadLength = roadSpan.max - roadSpan.min;
            var fittedWidth = roadLength / panelsPerSide;
            var wallAxisValue = isLeftSide ? wallSpan.min : wallSpan.max;
            var roadT = roadSpan.min + fittedWidth * (panelIndex + 0.5f);
            var roadPoint = PointOnAxis(bounds.center, roadAxis, roadT);
            roadPoint.y = bounds.min.y;

            var outward = GetOutwardFromHighwayEdge(bounds, wallAxis, wallAxisValue);
            var flatOutward = FlattenOutward(outward);
            var wallPoint = PointOnAxis(roadPoint, wallAxis, wallAxisValue);
            var collider = segment.GetComponent<MeshCollider>();
            if (collider != null
                && TryGetOutermostWallPoint(collider, bounds, roadPoint, flatOutward, out var meshWall))
            {
                wallPoint = meshWall;
            }

            var gap = Vector3.Dot(panel.position - wallPoint, flatOutward);
            min = Mathf.Min(min, gap);
            max = Mathf.Max(max, gap);
            Debug.Log($"[Dutz] Mural gap {panel.name}: {gap:F2}m (target {targetOffset:F2}m)");
        }

        Debug.Log($"[Dutz] Mural lateral gap range: {min:F2}m – {max:F2}m (target {targetOffset:F2}m)");
    }

    static void ClearBridgeReferenceCache()
    {
        _cachedBridgePanelHeight = null;
        _cachedBridgePanelWidth = null;
    }

    static bool IsStraightHighwaySegment(string segmentName) =>
        segmentName.Contains("Highway Straight");

    static void EnsureBridgeReferenceMetrics(DutzHighwayPhotoBillboardSettings settings)
    {
        if (_cachedBridgePanelHeight.HasValue && _cachedBridgePanelWidth.HasValue)
            return;

        var maxHeight = 0f;
        var maxPanelWidth = 0f;
        var panelsPerSide = Mathf.Max(1, settings.panelsPerRoadSide);
        var overlap = Mathf.Max(0f, settings.elevatedPanelOverlap);

        foreach (var segmentName in MuralSegmentOrder)
        {
            if (!segmentName.Contains("Bridge"))
                continue;

            var segment = FindSegmentByName(segmentName);
            if (segment == null)
                continue;

            var collider = segment.GetComponent<MeshCollider>();
            var renderer = segment.GetComponent<Renderer>();
            if (collider == null && renderer == null)
                continue;

            var bounds = collider != null ? collider.bounds : renderer.bounds;
            GetSegmentTrackAxes(segment, bounds, out var roadAxis, out _);
            var roadSpan = ProjectSpan(bounds, roadAxis);
            var roadLength = roadSpan.max - roadSpan.min;
            var heightSpan = ProjectSpan(bounds, Vector3.up);
            var height = (heightSpan.max - heightSpan.min) * settings.tallWallHeightCoverage;
            var width = roadLength / panelsPerSide + overlap;

            maxHeight = Mathf.Max(maxHeight, height);
            maxPanelWidth = Mathf.Max(maxPanelWidth, width);
        }

        _cachedBridgePanelHeight = maxHeight > 1f ? maxHeight : FallbackBridgePanelHeight;
        _cachedBridgePanelWidth = maxPanelWidth > 1f ? maxPanelWidth : FallbackBridgePanelWidth;
    }

    static void ResolvePanelMetrics(
        string segmentName,
        Bounds bounds,
        float roadLength,
        int panelsPerSide,
        DutzHighwayPhotoBillboardSettings settings,
        out float panelWidth,
        out float panelHeight)
    {
        var overlap = Mathf.Max(0f, settings.elevatedPanelOverlap);
        var fittedWidth = roadLength / Mathf.Max(1, panelsPerSide) + overlap;
        var heightSpan = ProjectSpan(bounds, Vector3.up);
        var naturalHeight = (heightSpan.max - heightSpan.min) * settings.tallWallHeightCoverage;

        if (!IsStraightHighwaySegment(segmentName))
        {
            panelWidth = fittedWidth;
            panelHeight = naturalHeight;
            return;
        }

        EnsureBridgeReferenceMetrics(settings);
        panelWidth = _cachedBridgePanelWidth.Value;
        panelHeight = _cachedBridgePanelHeight.Value;
    }

    static void ClearExisting()
    {
        foreach (var rootName in new[] { RootName, LegacyRootName })
        {
            var existing = GameObject.Find(rootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);
        }
    }

    static List<GameObject> FindTrackSegments()
    {
        var segments = new List<GameObject>();
        foreach (var segmentName in MuralSegmentOrder)
        {
            var segment = FindSegmentByName(segmentName);
            if (segment == null)
            {
                Debug.LogWarning($"[Dutz] Mural segment missing in scene: {segmentName}");
                continue;
            }

            if (IsExcludedSegment(segment.name))
                continue;

            segments.Add(segment);
        }

        return segments;
    }

    static void GetSegmentTrackAxes(GameObject segment, Bounds bounds, out Vector3 roadAxis, out Vector3 wallAxis)
    {
        var t = segment.transform;
        roadAxis = GetSegmentTravelForward(t, bounds);
        if (roadAxis.sqrMagnitude < 0.0001f)
            roadAxis = Vector3.right;
        roadAxis.Normalize();

        wallAxis = Vector3.Cross(Vector3.up, roadAxis);
        if (wallAxis.sqrMagnitude < 0.0001f)
            wallAxis = Vector3.forward;
        wallAxis.Normalize();

        var segmentRight = Flatten(t.right);
        if (segmentRight.sqrMagnitude > 0.0001f && Vector3.Dot(wallAxis, segmentRight.normalized) < 0f)
            wallAxis = -wallAxis;
    }

    static Vector3 GetSegmentTravelForward(Transform segment, Bounds bounds)
    {
        var forward = Flatten(segment.forward);
        var right = Flatten(segment.right);

        if (forward.sqrMagnitude < 0.0001f && right.sqrMagnitude < 0.0001f)
            return Vector3.right;

        var alongForward = SpanLength(bounds, forward.sqrMagnitude > 0.0001f ? forward.normalized : right);
        var alongRight = SpanLength(bounds, right.sqrMagnitude > 0.0001f ? right.normalized : forward);
        return alongRight > alongForward * 1.05f
            ? (right.sqrMagnitude > 0.0001f ? right : forward)
            : (forward.sqrMagnitude > 0.0001f ? forward : right);
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static float SpanLength(Bounds bounds, Vector3 axis)
    {
        if (axis.sqrMagnitude < 0.0001f)
            return 0f;

        axis.Normalize();
        var span = ProjectSpan(bounds, axis);
        return span.max - span.min;
    }

    static void GetRoadAndWallAxes(Bounds bounds, out Vector3 roadAxis, out Vector3 wallAxis)
    {
        if (bounds.extents.x >= bounds.extents.z)
        {
            roadAxis = Vector3.right;
            wallAxis = Vector3.forward;
        }
        else
        {
            roadAxis = Vector3.forward;
            wallAxis = Vector3.right;
        }
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
            var projection = Vector3.Dot(corner, axis);
            if (projection < min)
                min = projection;
            if (projection > max)
                max = projection;
        }

        return (min, max);
    }

    static Vector3 GetOutwardFromHighwayEdge(Bounds bounds, Vector3 wallAxis, float wallAxisValue)
    {
        var centerOnWall = Vector3.Dot(bounds.center, wallAxis);
        return wallAxisValue < centerOnWall ? -wallAxis : wallAxis;
    }

    static bool TryGetSegmentLayout(
        GameObject segment,
        out Bounds bounds,
        out Vector3 roadAxis,
        out Vector3 wallAxis,
        out (float min, float max) wallSpan,
        out (float min, float max) roadSpan)
    {
        bounds = default;
        roadAxis = Vector3.right;
        wallAxis = Vector3.forward;
        wallSpan = default;
        roadSpan = default;

        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return false;

        bounds = collider != null ? collider.bounds : renderer.bounds;
        GetSegmentTrackAxes(segment, bounds, out roadAxis, out wallAxis);
        wallSpan = ProjectSpan(bounds, wallAxis);
        roadSpan = ProjectSpan(bounds, roadAxis);
        return true;
    }

    static bool TryGetSegmentLayout(
        GameObject segment,
        out Bounds bounds,
        out Vector3 roadAxis,
        out Vector3 wallAxis,
        out (float min, float max) wallSpan)
    {
        if (!TryGetSegmentLayout(segment, out bounds, out roadAxis, out wallAxis, out wallSpan, out _))
            return false;

        return true;
    }

    static GameObject FindSegmentByName(string segmentName)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindChildByName(root.transform, segmentName);
            if (found != null)
                return found;
        }

        return null;
    }

    static GameObject FindChildByName(Transform node, string objectName)
    {
        if (node.name == objectName)
            return node.gameObject;

        for (var i = 0; i < node.childCount; i++)
        {
            var found = FindChildByName(node.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    static bool TryParseMuralName(string muralName, out string segmentName, out bool isLeftSide)
    {
        segmentName = null;
        isLeftSide = false;

        if (!muralName.StartsWith("Mural_"))
            return false;

        var body = muralName.Substring("Mural_".Length);
        var sideIndex = body.LastIndexOf("_L_", System.StringComparison.Ordinal);
        var sideToken = "_L_";
        if (sideIndex < 0)
        {
            sideIndex = body.LastIndexOf("_R_", System.StringComparison.Ordinal);
            sideToken = "_R_";
        }

        if (sideIndex < 0)
            return false;

        segmentName = body.Substring(0, sideIndex);
        isLeftSide = sideToken == "_L_";
        return !string.IsNullOrEmpty(segmentName);
    }

    static bool TryParseMuralIndex(string muralName, out int panelIndex)
    {
        panelIndex = 0;
        if (!TryParseMuralName(muralName, out _, out _))
            return false;

        var underscore = muralName.LastIndexOf('_');
        if (underscore < 0 || underscore >= muralName.Length - 1)
            return false;

        if (!int.TryParse(muralName.Substring(underscore + 1), out var oneBased) || oneBased < 1)
            return false;

        panelIndex = oneBased - 1;
        return true;
    }
}
