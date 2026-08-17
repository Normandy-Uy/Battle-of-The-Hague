using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Syncs the Level07 Senate group mural photo and places it mid Highway Straight 6.
/// Menu: Assets / Dutz Authoring / Place Senate Mural On Level07 Straight6
/// </summary>
public static class DutzLevel07SenateMuralPlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string SegmentName = "Highway Straight 6";
    const string TextureAssetPath = "Assets/Characters/HighwayBillboards/Textures/Level07Senate.png";
    const string MaterialAssetPath = "Assets/Characters/HighwayBillboards/Materials/Level07SenateMural.mat";
    public const string RootName = DutzSenateVotesOffer.RootName;
    const string PanelName = DutzSenateVotesOffer.PanelName;
    const string LegacyHighway8PanelName = DutzSenateVotesOffer.LegacyPanelName;
    const float PathPosition = 0.5f;
    const float PanelWidthMeters = 36f;
    const float DeckClearanceMeters = 0.12f;
    const int MaxTextureEdge = 1024;

    static readonly string[] PublicFileNames =
    {
        "SENATE.png",
        "SENATE GROUP.png",
        "SENATE_GROUP.png",
        "Senate.png",
    };

    [MenuItem("Assets/Dutz Authoring/Place Senate Mural On Level07 Straight6")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Senate Mural On Level07 Straight6 requires Edit Mode.");
            return;
        }

        if (!PlaceOnLevel07(log: true))
            Debug.LogError("[Dutz] Failed to place Senate mural on Level07 Highway Straight 6.");
    }

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path.Replace('\\', '/') != Level07Path)
            return false;

        if (GameObject.Find(RootName) != null || GameObject.Find(PanelName) != null)
            return false;

        return PlaceOnLevel07(log);
    }

    public static bool PlaceOnLevel07(bool log)
    {
        if (!File.Exists(Level07Path))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level07.unity not found.");
            return false;
        }

        if (!SyncTexture(log))
            return false;

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        var materialTemplate = DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        if (texture == null || materialTemplate == null)
        {
            if (log)
                Debug.LogError("[Dutz] Senate mural texture/material missing after sync.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
        {
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        DutzHighwayDirection.InvalidateTrackSegmentCache();
        DutzHighwayDirection.InvalidateReferenceCache();

        if (!TryGetTrackReference(out var spawn, out var travelForward))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve Level07 track reference for Senate mural.");
            return false;
        }

        var segmentPaths = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(
            new[] { SegmentName }, spawn, travelForward);
        if (segmentPaths.Count == 0 || segmentPaths[0].Samples == null || segmentPaths[0].Samples.Count == 0)
        {
            if (log)
                Debug.LogError($"[Dutz] {SegmentName} not found for Senate mural.");
            return false;
        }

        if (!DutzHighwayDeckSampler.TrySampleOnPath(segmentPaths[0].Samples, PathPosition, out var centerSample))
        {
            if (log)
                Debug.LogError($"[Dutz] Could not sample mid deck on {SegmentName}.");
            return false;
        }

        // Do not use PlaceOnLane(CenterLaneZ) — that forces world Z=4.5 and yanks Straight 6 off-road.
        var deck = centerSample.Position;
        var probe = deck;
        probe.y += 40f;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, deck.y, null, out var deckY)
            || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
            deck.y = deckY;

        var faceDir = -centerSample.Forward;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = -travelForward;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = Vector3.right;
        faceDir.Normalize();

        ClearExisting();
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Level07 Senate Mural");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        if (!CreateCenterRoadMural(root.transform, texture, materialTemplate, deck, faceDir))
        {
            Object.DestroyImmediate(root);
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed Senate mural mid {SegmentName} at {deck} " +
                $"({PanelWidthMeters:0.#}m wide, faces oncoming traffic, votes offer wired).");
        }

        return true;
    }

    static bool CreateCenterRoadMural(
        Transform parent,
        Texture2D texture,
        Material materialTemplate,
        Vector3 deckPoint,
        Vector3 faceDir)
    {
        var aspect = texture.width / (float)Mathf.Max(1, texture.height);
        var panelWidth = PanelWidthMeters;
        var panelHeight = panelWidth / Mathf.Max(0.25f, aspect);

        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = Vector3.right;
        faceDir.Normalize();

        var boardCenter = deckPoint;
        boardCenter.y += panelHeight * 0.5f + DeckClearanceMeters;

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, "Place Level07 Senate Mural");
        panel.name = PanelName;
        panel.transform.SetParent(parent, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());

        var muralMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        if (muralMaterial == null)
        {
            muralMaterial = new Material(materialTemplate)
            {
                name = "Level07SenateMural",
                mainTexture = texture
            };
            EnsureMaterialFolders();
            AssetDatabase.CreateAsset(muralMaterial, MaterialAssetPath);
        }
        else
        {
            muralMaterial.mainTexture = texture;
            EditorUtility.SetDirty(muralMaterial);
        }

        panel.GetComponent<MeshRenderer>().sharedMaterial = muralMaterial;
        DutzSenateVotesOffer.EnsureOn(panel);
        return true;
    }

    public static bool SyncTexture(bool log)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath) != null)
        {
            DutzHaguePhotoBillboardBuilder.EnsureMaterial();
            if (log)
                Debug.Log("[Dutz] Using existing Level07 Senate mural texture → " + TextureAssetPath);
            return true;
        }

        var sourcePath = FindSourcePhoto();
        if (string.IsNullOrEmpty(sourcePath))
        {
            if (log)
            {
                Debug.LogError(
                    "[Dutz] Missing Senate mural photo. Add public/SENATE.png " +
                    "(or SENATE GROUP.png), or keep the attached Gemini image in Cursor assets.");
            }

            return false;
        }

        if (!DutzHaguePhotoBillboardBuilder.SyncPosterizedPhoto(
                sourcePath, TextureAssetPath, MaxTextureEdge, forceRewrite: false))
        {
            if (log)
                Debug.LogError("[Dutz] Failed to sync Senate mural texture from " + sourcePath);
            return false;
        }

        DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        AssetDatabase.Refresh();

        if (log)
            Debug.Log("[Dutz] Synced Level07 Senate mural → " + TextureAssetPath);

        return true;
    }

    static string FindSourcePhoto()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrEmpty(projectRoot))
        {
            var publicDir = Path.Combine(projectRoot, "public");
            if (Directory.Exists(publicDir))
            {
                foreach (var fileName in PublicFileNames)
                {
                    var exact = Path.Combine(publicDir, fileName);
                    if (File.Exists(exact))
                        return exact;
                }

                foreach (var candidate in Directory.GetFiles(publicDir))
                {
                    var name = Path.GetFileName(candidate);
                    foreach (var fileName in PublicFileNames)
                    {
                        if (string.Equals(name, fileName, System.StringComparison.OrdinalIgnoreCase))
                            return candidate;
                    }
                }
            }

            var imported = Path.Combine(projectRoot, TextureAssetPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(imported))
                return imported;
        }

        return null;
    }

    static void ClearExisting()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var panel = GameObject.Find(PanelName);
        if (panel != null)
            Undo.DestroyObjectImmediate(panel);

        var legacy = GameObject.Find(LegacyHighway8PanelName);
        if (legacy != null)
        {
            var legacyRoot = legacy.transform.parent != null ? legacy.transform.parent.gameObject : legacy;
            Undo.DestroyObjectImmediate(legacyRoot);
        }
    }

    static void EnsureMaterialFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards"))
            AssetDatabase.CreateFolder("Assets/Characters", "HighwayBillboards");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards/Materials"))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards/Textures"))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards", "Textures");
    }

    static bool TryGetTrackReference(out Vector3 spawn, out Vector3 travelForward)
    {
        spawn = new Vector3(-1002f, 7.4f, -9.1f);
        travelForward = Vector3.right;

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackSpawn, out var trackForward)
            && trackForward.sqrMagnitude > 0.001f)
        {
            spawn = trackSpawn;
            travelForward = trackForward.normalized;
            return true;
        }

        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progress)
            && progress.sqrMagnitude > 0.001f)
        {
            travelForward = progress.normalized;
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
