using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Syncs public/DUTZJAIL.png and places a finish-line mural at the highway end on Dutz_Level03.
/// Menu: Tools / Dutz / Level Setup / Place Dutz Jail Mural (Level 03)
/// </summary>
public static class DutzJailMuralPlacer
{
    const string Level03ScenePath = "Assets/Scenes/Dutz_Level03.unity";
    const string SourceFileName = "DUTZJAIL.png";
    const string TextureAssetPath = "Assets/Characters/HighwayBillboards/Textures/DutzJail.png";
    const string RootName = "DutzJailMural";
    public const string SceneRootName = RootName;
    const string PanelName = "DutzJailMural_End";
    const string LastSegmentName = "Highway Straight 6";
    const float EndInsetMeters = 8f;
    const float PanelWidthMeters = 40f;

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Dutz Jail Mural", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Dutz Jail Mural",
                "Could not place mural.\n\nAdd public/DUTZJAIL.png and ensure Dutz_Level03 has highway segments.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog("Dutz Jail Mural", "DUTZJAIL mural placed at the end of the track.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzJailMuralPlacer.PlaceOnLevel03Batch</summary>
    public static void PlaceOnLevel03Batch() => PlaceOnLevel03(log: true);

    public static bool PlaceOnLevel03(bool log)
    {
        if (!File.Exists(Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        if (!SyncTexture())
        {
            Debug.LogError("[Dutz] Missing public/DUTZJAIL.png â€” add the image and run again.");
            return false;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (texture == null || materialTemplate == null)
            return false;

        var settings = DutzHaguePhotoBillboardBuilder.EnsureSettings();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level03ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level03ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        if (!TryGetTrackEndMuralPose(out var boardCenter, out var faceDir, out var deckY))
        {
            Debug.LogError("[Dutz] Could not resolve track end for DUTZJAIL mural.");
            return false;
        }

        ClearExisting();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Dutz Jail Mural");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var aspect = texture.width / (float)Mathf.Max(1, texture.height);
        var panelWidth = PanelWidthMeters;
        var panelHeight = panelWidth / Mathf.Max(0.25f, aspect);
        boardCenter.y = deckY + settings.elevatedHeightAboveDeck + panelHeight * 0.5f;

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, "Place Dutz Jail Mural");
        panel.name = PanelName;
        panel.transform.SetParent(root.transform, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        var muralMaterial = new Material(materialTemplate) { mainTexture = texture };
        muralMaterial.name = "DutzJailMural";
        panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;
        DutzMuralBumpMessage.Apply(panel);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed DUTZJAIL mural at {boardCenter} facing {faceDir} " +
                $"({panelWidth:0.#}m wide on {LastSegmentName} end).");
        }

        return true;
    }

    public static bool SyncTexture()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return false;

        var sourcePath = Path.Combine(projectRoot, "public", SourceFileName);
        if (!File.Exists(sourcePath))
            return false;

        DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        var settings = DutzHaguePhotoBillboardBuilder.EnsureSettings();

        var destFullPath = Path.Combine(
            projectRoot, TextureAssetPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destFullPath) ?? projectRoot);

        if (File.Exists(destFullPath)
            && File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(destFullPath))
        {
            return true;
        }

        var bytes = File.ReadAllBytes(sourcePath);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            return false;
        }

        var maxEdge = Mathf.Max(settings.maxTextureEdge, 2048);
        var resized = ResizeTexture(loaded, maxEdge);
        Object.DestroyImmediate(loaded);

        var png = resized.EncodeToPNG();
        Object.DestroyImmediate(resized);
        File.WriteAllBytes(destFullPath, png);

        AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(TextureAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = maxEdge;
            importer.SaveAndReimport();
        }

        return true;
    }

    static Texture2D ResizeTexture(Texture2D source, int maxEdge)
    {
        var width = source.width;
        var height = source.height;
        var longest = Mathf.Max(width, height);
        if (longest <= maxEdge)
            return Object.Instantiate(source);

        var scale = maxEdge / (float)longest;
        var targetWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
        var targetHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    static void ClearExisting()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static bool TryGetTrackEndMuralPose(out Vector3 boardCenter, out Vector3 faceDir, out float deckY)
    {
        boardCenter = Vector3.zero;
        faceDir = Vector3.right;
        deckY = 0f;

        if (!DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var spawn, out var travelForward)
            || travelForward.sqrMagnitude < 0.0001f)
        {
            travelForward = Vector3.right;
        }
        else
        {
            travelForward.y = 0f;
            travelForward.Normalize();
        }

        faceDir = -travelForward;

        var segment = FindSegmentByName(LastSegmentName) ?? FindFarthestSegment(spawn, travelForward);
        if (segment == null || !TryGetSegmentLayout(segment, out var bounds, out var roadAxis, out var wallAxis, out var roadSpan, out var wallSpan))
            return false;

        if (Vector3.Dot(roadAxis, travelForward) < 0f)
            roadAxis = -roadAxis;

        var roadEnd = roadSpan.max - EndInsetMeters;

        var roadPoint = PointOnAxis(bounds.center, roadAxis, roadEnd);
        var wallCenter = (wallSpan.min + wallSpan.max) * 0.5f;
        boardCenter = PointOnAxis(roadPoint, wallAxis, wallCenter);
        deckY = bounds.center.y;
        return true;
    }

    static GameObject FindFarthestSegment(Vector3 spawn, Vector3 travelForward)
    {
        GameObject best = null;
        var bestAlong = float.NegativeInfinity;

        foreach (var segment in FindAllHighwaySegments())
        {
            if (!TryGetRendererBounds(segment, out var bounds))
                continue;

            foreach (var corner in BoundsCorners(bounds))
            {
                var along = Vector3.Dot(corner - spawn, travelForward);
                if (along <= bestAlong)
                    continue;

                bestAlong = along;
                best = segment;
            }
        }

        return best;
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

    static List<GameObject> FindAllHighwaySegments()
    {
        var segments = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectHighwaySegment(root.transform, segments);

        segments.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return segments;
    }

    static void CollectHighwaySegment(Transform node, List<GameObject> segments)
    {
        var name = node.name;
        if (name == RootName || name.StartsWith("HagueMurals (") || name.Contains("Murals") || name.Contains("Billboard"))
            return;

        if ((name.Contains("Highway Straight") || name.Contains("Highway Bridge"))
            && (node.parent == null
                || (!node.parent.name.Contains("Highway") && !node.parent.name.Contains("Bridge"))))
        {
            segments.Add(node.gameObject);
        }

        for (var i = 0; i < node.childCount; i++)
            CollectHighwaySegment(node.GetChild(i), segments);
    }

    static bool TryGetSegmentLayout(
        GameObject segment,
        out Bounds bounds,
        out Vector3 roadAxis,
        out Vector3 wallAxis,
        out (float min, float max) roadSpan,
        out (float min, float max) wallSpan)
    {
        bounds = default;
        roadAxis = Vector3.right;
        wallAxis = Vector3.forward;
        roadSpan = default;
        wallSpan = default;

        if (!TryGetRendererBounds(segment, out bounds))
            return false;

        GetRoadAndWallAxes(bounds, out roadAxis, out wallAxis);
        roadSpan = ProjectSpan(bounds, roadAxis);
        wallSpan = ProjectSpan(bounds, wallAxis);
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

    static bool TryGetRendererBounds(GameObject segment, out Bounds bounds)
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
}

/// <summary>
/// Syncs public/SENATE BUILDING.png and places a single cheap plane mural on Level 1 at spawn.
/// </summary>
public static class DutzSenateBuildingMuralPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string SourceFileName = "SENATE BUILDING.png";
    const string TextureAssetPath = "Assets/Characters/HighwayBillboards/Textures/SenateBuilding.png";
    const string RootName = "DutzSenateBuildingMural";
    public const string SceneRootName = RootName;
    const string PanelName = "DutzSenateBuildingMural_Spawn";
    const float LookAheadMeters = 20f;
    const float PanelWidthMeters = 26f;
    const int MaxTextureEdge = 512;

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Senate Building Mural", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceOnLevel01(log: true))
        {
            EditorUtility.DisplayDialog(
                "Senate Building Mural",
                "Could not place mural.\n\nAdd public/SENATE BUILDING.png and ensure Dutz_Level01 has Highway Bridge 1.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Senate Building Mural",
            "Senate Building mural placed at Level 1 spawn (faces the player).",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzSenateBuildingMuralPlacer.PlaceOnLevel01Batch</summary>
    public static void PlaceOnLevel01Batch() => PlaceOnLevel01(log: true);

    /// <summary>Batch: -executeMethod DutzSenateBuildingMuralPlacer.PlaceOnLevel00Batch</summary>
    public static void PlaceOnLevel00Batch() => PlaceOnLevel00(log: true);

    public static bool PlaceOnLevel01(bool log) => PlaceOnScene(Level01ScenePath, EnsureLevel01PlayerAtSpawn, log);

    public static bool PlaceOnLevel00(bool log) => PlaceOnScene(Level00ScenePath, EnsureLevel00PlayerAtSpawn, log);

    static bool PlaceOnScene(string scenePath, System.Action ensurePlayer, bool log)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogError("[Dutz] Scene not found: " + scenePath);
            return false;
        }

        if (!SyncTexture())
        {
            Debug.LogError("[Dutz] Missing public/SENATE BUILDING.png â€” add the image and run again.");
            return false;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (texture == null || materialTemplate == null)
            return false;

        var settings = DutzHaguePhotoBillboardBuilder.EnsureSettings();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        DutzHighwayDirection.InvalidateReferenceCache();
        ensurePlayer?.Invoke();
        if (!TryGetSpawnMuralPose(out var boardCenter, out var faceDir, out var deckY))
        {
            Debug.LogError("[Dutz] Could not resolve spawn pose for Senate Building mural.");
            return false;
        }

        ClearExisting();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Senate Building Mural");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var aspect = texture.width / (float)Mathf.Max(1, texture.height);
        var panelWidth = PanelWidthMeters;
        var panelHeight = panelWidth / Mathf.Max(0.25f, aspect);
        boardCenter.y = deckY + settings.elevatedHeightAboveDeck + panelHeight * 0.5f;

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, "Place Senate Building Mural");
        panel.name = PanelName;
        panel.transform.SetParent(root.transform, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        var muralMaterial = AssetDatabase.LoadAssetAtPath<Material>(DutzSenateBuildingMural.MaterialAssetPath);
        if (muralMaterial == null)
        {
            muralMaterial = new Material(materialTemplate) { mainTexture = texture };
            muralMaterial.name = "SenateBuildingMural";
        }

        panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed Senate Building mural at {boardCenter} facing {faceDir} " +
                $"({panelWidth:0.#}m wide, {MaxTextureEdge}px texture).");
        }

        return true;
    }

    public static bool SyncTexture()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return false;

        var sourcePath = FindSourcePhoto(projectRoot);
        if (string.IsNullOrEmpty(sourcePath))
            return false;

        DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        var destFullPath = Path.Combine(
            projectRoot, TextureAssetPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destFullPath) ?? projectRoot);

        if (File.Exists(destFullPath)
            && File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(destFullPath))
        {
            return true;
        }

        var bytes = File.ReadAllBytes(sourcePath);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            return false;
        }

        var resized = ResizeTexture(loaded, MaxTextureEdge);
        Object.DestroyImmediate(loaded);

        File.WriteAllBytes(destFullPath, resized.EncodeToPNG());
        Object.DestroyImmediate(resized);

        AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(TextureAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = MaxTextureEdge;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        return true;
    }

    static string FindSourcePhoto(string projectRoot)
    {
        var exact = Path.Combine(projectRoot, "public", SourceFileName);
        if (File.Exists(exact))
            return exact;

        var publicDir = Path.Combine(projectRoot, "public");
        if (!Directory.Exists(publicDir))
            return null;

        foreach (var path in Directory.GetFiles(publicDir))
        {
            var name = Path.GetFileName(path);
            if (string.Equals(name, SourceFileName, System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    static Texture2D ResizeTexture(Texture2D source, int maxEdge)
    {
        var width = source.width;
        var height = source.height;
        var longest = Mathf.Max(width, height);
        if (longest <= maxEdge)
            return Object.Instantiate(source);

        var scale = maxEdge / (float)longest;
        var targetWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
        var targetHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    static void ClearExisting()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static void EnsureLevel01PlayerAtSpawn()
    {
        var player = GameObject.Find("Player1")?.GetComponent<DutzPlayerController>();
        if (player == null)
            return;

        var so = new SerializedObject(player);
        var spawn = so.FindProperty("spawnPosition").vector3Value;
        if (spawn.y > 20f || Mathf.Abs(spawn.x + 1002f) > 250f)
        {
            spawn = new Vector3(-1002f, 7.4f, -9.1f);
            so.FindProperty("spawnPosition").vector3Value = spawn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        DutzSpawnSetup.ApplyInspectorSpawnToDutz();
    }

    static void EnsureLevel00PlayerAtSpawn()
    {
        DutzLevel00EssentialsSetup.EnsurePlayer1(log: false);
    }

    static bool TryGetSpawnMuralPose(out Vector3 boardCenter, out Vector3 faceDir, out float deckY)
    {
        boardCenter = Vector3.zero;
        faceDir = Vector3.right;
        deckY = 0f;

        var player = DutzEditorHelpers.FindPrimaryDutzPlayer();
        if (player == null)
            return false;

        var so = new SerializedObject(player);
        var spawn = so.FindProperty("spawnPosition").vector3Value;
        var travelForward = player.transform.forward;
        travelForward.y = 0f;
        if (travelForward.sqrMagnitude < 0.0001f)
        {
            travelForward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
            if (so.FindProperty("invertSpawnFacing").boolValue)
                travelForward = -travelForward;
        }
        else
        {
            travelForward.Normalize();
        }

        faceDir = -travelForward;

        var lateral = Vector3.Cross(Vector3.up, travelForward);
        if (lateral.sqrMagnitude < 0.0001f)
            lateral = Vector3.right;
        else
            lateral.Normalize();

        boardCenter = spawn + travelForward * LookAheadMeters + lateral * 11f;
        deckY = spawn.y;

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(boardCenter, spawn.y, null, out var sampledDeckY))
            deckY = sampledDeckY;
        else if (DutzRoadGround.TrySampleRoadDeckY(boardCenter, spawn.y, null, out sampledDeckY))
            deckY = sampledDeckY;

        return true;
    }
}

/// <summary>
/// Syncs public/ROBINCAR.png and places a cheap plane mural at the Level 1 track end.
/// </summary>
public static class DutzRobinCarMuralPlacer
{
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string SourceFileName = "ROBINCAR.png";
    const string TextureAssetPath = "Assets/Characters/HighwayBillboards/Textures/RobinCar.png";
    const string RootName = "DutzRobinCarMural";
    public const string SceneRootName = RootName;
    const string PanelName = "DutzRobinCarMural_End";
    const string LastSegmentName = "Highway Straight 6";
    const float EndInsetMeters = 8f;
    const float PanelWidthMeters = 32f;
    const int MaxTextureEdge = 512;

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Robin Car Mural", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceOnLevel01(log: true))
        {
            EditorUtility.DisplayDialog(
                "Robin Car Mural",
                "Could not place mural.\n\nAdd public/ROBINCAR.png and ensure Dutz_Level01 has highway segments.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Robin Car Mural",
            "Robin Car mural placed at the end of Level 1 (faces the approaching player).",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzRobinCarMuralPlacer.PlaceOnLevel01Batch</summary>
    public static void PlaceOnLevel01Batch() => PlaceOnLevel01(log: true);

    [MenuItem("Assets/Dutz Authoring/Sync Robin Car Texture")]
    public static void SyncTextureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Sync Robin Car Texture requires Edit Mode.");
            return;
        }

        if (!SyncTexture(force: true))
        {
            Debug.LogError("[Dutz] Missing public/ROBINCAR.png — add the image and run again.");
            return;
        }

        RefreshMuralMaterialInOpenScene();
        Debug.Log("[Dutz] Robin Car texture synced from public/ROBINCAR.png → " + TextureAssetPath);
    }

    public static bool PlaceOnLevel01(bool log) => PlaceOnScene(Level01ScenePath, log);

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level01ScenePath)
            return false;

        // Sync when public/ROBINCAR.png is newer; ignore "already current" as a scene change.
        SyncTexture(force: false);
        if (GameObject.Find(RootName) == null)
            return PlaceOnLevel01(log);

        return RefreshMuralMaterialInOpenScene();
    }

    /// <summary>Robin Car mural is Level 1 only — strip it from other level scenes.</summary>
    public static bool RemoveFromLevel02IfPresent(Scene scene, bool log)
    {
        if (!scene.IsValid() || scene.path != DutzShowcaseSceneRepair.Level02ScenePath)
            return false;

        var root = GameObject.Find(RootName);
        if (root == null)
            return false;

        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(scene);

        if (log)
            Debug.Log("[Dutz] Removed Robin Car mural from Level 2 (Level 1 only).");

        return true;
    }

    /// <summary>Batch: -executeMethod DutzRobinCarMuralPlacer.RemoveFromLevel02Batch</summary>
    public static void RemoveFromLevel02Batch()
    {
        var scene = EditorSceneManager.OpenScene(DutzShowcaseSceneRepair.Level02ScenePath, OpenSceneMode.Single);
        if (RemoveFromLevel02IfPresent(scene, log: true))
            EditorSceneManager.SaveScene(scene);
    }

    static bool PlaceOnScene(string scenePath, bool log)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogError($"[Dutz] Scene not found: {scenePath}");
            return false;
        }

        if (!SyncTexture())
        {
            Debug.LogError("[Dutz] Missing public/ROBINCAR.png â€” add the image and run again.");
            return false;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (texture == null || materialTemplate == null)
            return false;

        var settings = DutzHaguePhotoBillboardBuilder.EnsureSettings();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        DutzHighwayDirection.InvalidateReferenceCache();
        if (!TryGetTrackEndMuralPose(out var boardCenter, out var faceDir, out var deckY))
        {
            Debug.LogError($"[Dutz] Could not resolve track end for Robin Car mural in {scenePath}.");
            return false;
        }

        ClearExisting();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Robin Car Mural");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var aspect = texture.width / (float)Mathf.Max(1, texture.height);
        var panelWidth = PanelWidthMeters;
        var panelHeight = panelWidth / Mathf.Max(0.25f, aspect);
        boardCenter.y = deckY + settings.elevatedHeightAboveDeck + panelHeight * 0.5f;

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, "Place Robin Car Mural");
        panel.name = PanelName;
        panel.transform.SetParent(root.transform, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        var muralMaterial = new Material(materialTemplate) { mainTexture = texture };
        muralMaterial.name = "RobinCarMural";
        panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed Robin Car mural at {boardCenter} facing {faceDir} " +
                $"({panelWidth:0.#}m wide on {LastSegmentName} end in {scenePath}).");
        }

        return true;
    }

    public static bool SyncTexture(bool force = false)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return false;

        var sourcePath = FindSourcePhoto(projectRoot);
        if (string.IsNullOrEmpty(sourcePath))
            return false;

        DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        var destFullPath = Path.Combine(
            projectRoot, TextureAssetPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destFullPath) ?? projectRoot);

        if (!force
            && File.Exists(destFullPath)
            && File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(destFullPath))
        {
            return true;
        }

        var bytes = File.ReadAllBytes(sourcePath);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            return false;
        }

        var resized = ResizeTexture(loaded, MaxTextureEdge);
        Object.DestroyImmediate(loaded);

        File.WriteAllBytes(destFullPath, resized.EncodeToPNG());
        Object.DestroyImmediate(resized);

        AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(TextureAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = MaxTextureEdge;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        return true;
    }

    static bool RefreshMuralMaterialInOpenScene()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        if (texture == null)
            return false;

        var panel = GameObject.Find(PanelName);
        if (panel == null)
            return false;

        var renderer = panel.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.sharedMaterial == null)
            return false;

        if (renderer.sharedMaterial.mainTexture == texture)
            return false;

        renderer.sharedMaterial.mainTexture = texture;
        return true;
    }

    static string FindSourcePhoto(string projectRoot)
    {
        var exact = Path.Combine(projectRoot, "public", SourceFileName);
        if (File.Exists(exact))
            return exact;

        var publicDir = Path.Combine(projectRoot, "public");
        if (!Directory.Exists(publicDir))
            return null;

        foreach (var path in Directory.GetFiles(publicDir))
        {
            var name = Path.GetFileName(path);
            if (string.Equals(name, SourceFileName, System.StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    static Texture2D ResizeTexture(Texture2D source, int maxEdge)
    {
        var width = source.width;
        var height = source.height;
        var longest = Mathf.Max(width, height);
        if (longest <= maxEdge)
            return Object.Instantiate(source);

        var scale = maxEdge / (float)longest;
        var targetWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
        var targetHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    static void ClearExisting()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static bool TryGetTrackEndMuralPose(out Vector3 boardCenter, out Vector3 faceDir, out float deckY)
    {
        boardCenter = Vector3.zero;
        faceDir = Vector3.left;
        deckY = 0f;

        if (!DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var spawn, out var travelForward)
            || travelForward.sqrMagnitude < 0.0001f)
        {
            travelForward = Vector3.right;
        }
        else
        {
            travelForward.y = 0f;
            travelForward.Normalize();
        }

        faceDir = -travelForward;

        var segment = GameObject.Find(LastSegmentName) ?? FindSegmentByName(LastSegmentName);
        if (segment == null)
            segment = FindFarthestSegment(spawn, travelForward);
        if (segment == null || !TryGetRendererBounds(segment, out var bounds))
            return false;

        var endPoint = bounds.center;
        var endAnchor = FindEndHouseInScene() ?? FindFlagPoleInScene();
        if (endAnchor != null)
        {
            endPoint = endAnchor.transform.position;
        }
        else
        {
            var farthestAlong = float.NegativeInfinity;
            foreach (var corner in BoundsCorners(bounds))
            {
                var along = Vector3.Dot(corner - spawn, travelForward);
                if (along <= farthestAlong)
                    continue;

                farthestAlong = along;
                endPoint = corner;
            }

            if (Mathf.Abs(Vector3.Dot(travelForward, Vector3.right)) > 0.7f)
                endPoint = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);
        }

        endPoint -= travelForward * EndInsetMeters;

        var wallAxis = Vector3.Cross(Vector3.up, travelForward);
        if (wallAxis.sqrMagnitude < 0.0001f)
            wallAxis = Vector3.forward;
        else
            wallAxis.Normalize();

        boardCenter = endPoint;
        if (endAnchor == null)
            boardCenter = PointOnAxis(boardCenter, wallAxis, Vector3.Dot(bounds.center, wallAxis));

        deckY = bounds.max.y;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(boardCenter, deckY, null, out var sampledDeckY))
            deckY = sampledDeckY;
        else if (DutzRoadGround.TrySampleRoadDeckY(boardCenter, deckY, null, out sampledDeckY))
            deckY = sampledDeckY;

        return true;
    }

    static GameObject FindEndHouseInScene()
    {
        var house = GameObject.Find(DutzEndHouseCollider.HouseName);
        if (house != null)
            return house;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == DutzEndHouseCollider.HouseName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    static GameObject FindFlagPoleInScene()
    {
        var pole = GameObject.Find(DutzFlagPoleGoal.FlagPoleName);
        if (pole != null)
            return pole;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == DutzFlagPoleGoal.FlagPoleName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    static GameObject FindFarthestSegment(Vector3 spawn, Vector3 travelForward)
    {
        GameObject best = null;
        var bestAlong = float.NegativeInfinity;

        foreach (var segment in FindAllHighwaySegments())
        {
            if (!TryGetRendererBounds(segment, out var bounds))
                continue;

            foreach (var corner in BoundsCorners(bounds))
            {
                var along = Vector3.Dot(corner - spawn, travelForward);
                if (along <= bestAlong)
                    continue;

                bestAlong = along;
                best = segment;
            }
        }

        return best;
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

    static List<GameObject> FindAllHighwaySegments()
    {
        var segments = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectHighwaySegment(root.transform, segments);

        segments.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return segments;
    }

    static void CollectHighwaySegment(Transform node, List<GameObject> segments)
    {
        var name = node.name;
        if (name == RootName
            || name == DutzSenateBuildingMuralPlacer.SceneRootName
            || name.StartsWith("HagueMurals (")
            || name.Contains("Murals")
            || name.Contains("Billboard"))
        {
            return;
        }

        if ((name.Contains("Highway Straight") || name.Contains("Highway Bridge"))
            && (node.parent == null
                || (!node.parent.name.Contains("Highway") && !node.parent.name.Contains("Bridge"))))
        {
            segments.Add(node.gameObject);
        }

        for (var i = 0; i < node.childCount; i++)
            CollectHighwaySegment(node.GetChild(i), segments);
    }

    static bool TryGetSegmentLayout(
        GameObject segment,
        out Bounds bounds,
        out Vector3 roadAxis,
        out Vector3 wallAxis,
        out (float min, float max) roadSpan,
        out (float min, float max) wallSpan)
    {
        bounds = default;
        roadAxis = Vector3.right;
        wallAxis = Vector3.forward;
        roadSpan = default;
        wallSpan = default;

        if (!TryGetRendererBounds(segment, out bounds))
            return false;

        GetRoadAndWallAxes(bounds, out roadAxis, out wallAxis);
        roadSpan = ProjectSpan(bounds, roadAxis);
        wallSpan = ProjectSpan(bounds, wallAxis);
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

    static bool TryGetRendererBounds(GameObject segment, out Bounds bounds)
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
}
