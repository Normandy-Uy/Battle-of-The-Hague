using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places EDSA side-wall murals (L001–L018 left, R001–R018 right) on Dutz_Level00 highway segments 1–6.
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

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        if (!NeedsEdsaMuralsRepublish())
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

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        DutzHighwayPhotoBillboardPlacer.ClearBillboardPlacementCache();
        DutzHighwayPhotoBillboardPlacer.ClearSideWallMuralsRoot();

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Level 00 EDSA Murals");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var placed = 0;
        for (var segmentIndex = 0; segmentIndex < SegmentNames.Length; segmentIndex++)
        {
            var segmentName = SegmentNames[segmentIndex];
            var segment = DutzHighwayPhotoBillboardPlacer.FindHighwaySegmentByName(segmentName);
            if (segment == null)
            {
                if (log)
                    Debug.LogWarning("[Dutz] EDSA mural segment missing in scene: " + segmentName);
                continue;
            }

            var segmentRoot = new GameObject($"EdsaMurals ({segmentName})");
            Undo.RegisterCreatedObjectUndo(segmentRoot, "Place Level 00 EDSA Murals");
            segmentRoot.transform.SetParent(root.transform, false);
            segmentRoot.transform.localPosition = Vector3.zero;
            segmentRoot.transform.localRotation = Quaternion.identity;
            segmentRoot.transform.localScale = Vector3.one;

            for (var panelIndex = 0; panelIndex < panelsPerSide; panelIndex++)
            {
                var muralNumber = segmentIndex * panelsPerSide + panelIndex + 1;

                var leftTexture = DutzLevel00EdsaMuralBuilder.LoadTexture('L', muralNumber);
                if (leftTexture != null
                    && DutzHighwayPhotoBillboardPlacer.PlaceSideWallPanel(
                        segment,
                        segmentRoot.transform,
                        isLeftSide: true,
                        panelIndex,
                        panelsPerSide,
                        leftTexture,
                        materialTemplate,
                        settings,
                        $"EdsaMural_Edsa_L{muralNumber:000}"))
                {
                    placed++;
                }

                var rightTexture = DutzLevel00EdsaMuralBuilder.LoadTexture('R', muralNumber);
                if (rightTexture != null
                    && DutzHighwayPhotoBillboardPlacer.PlaceSideWallPanel(
                        segment,
                        segmentRoot.transform,
                        isLeftSide: false,
                        panelIndex,
                        panelsPerSide,
                        rightTexture,
                        materialTemplate,
                        settings,
                        $"EdsaMural_Edsa_R{muralNumber:000}"))
                {
                    placed++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, Level00ScenePath);
        AssetDatabase.SaveAssets();

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed {placed} Level 00 EDSA side-wall mural(s) — " +
                $"{panelsPerSide} per side across {SegmentNames.Length} highway segments.");
        }

        return placed == ExpectedPanelCount;
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
