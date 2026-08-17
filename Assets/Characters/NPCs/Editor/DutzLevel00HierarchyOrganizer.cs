using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Groups scattered Level 00 hierarchy nodes into their authoring folders (no auto-save on open).
/// Batch: -executeMethod DutzLevel00HierarchyOrganizer.OrganizeBatch
/// </summary>
public static class DutzLevel00HierarchyOrganizer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";

    public const string PickupsRootName = "DutzPickups";
    public const string SpecialMuralsRootName = "Level00SpecialMurals";

    static readonly string[] PickupNames =
    {
        "DutzForceFieldSuit",
        "DutzSuperJumpPickup",
        "DutzSuperPunchPickup",
        "DutzSuperJumpArrow",
    };

    static readonly string[] SpecialMuralRootNames =
    {
        "DutzSenateBuildingMural",
        "DutzSenateBuildingMural_Spawn",
        "DutzJailMural",
        "DutzJailMural_End",
        "DutzLevel00DuterHagueMural",
        "DutzLevel00DuterTengotMural",
        "Level00_DUTERTEHAGUE",
        "Level00_DUTERTENGOT",
    };

    /// <summary>Batch entry for MCP / CI.</summary>
    public static void OrganizeBatch() => OrganizeHierarchy(log: true);

    [MenuItem("Assets/Dutz Authoring/Organize Level 00 Hierarchy Folders")]
    public static void OrganizeFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Organize Level 00", "Exit Play mode first.", "OK");
            return;
        }

        OrganizeHierarchy(log: true);
    }

    public static bool OrganizeHierarchy(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);

        Physics.SyncTransforms();

        DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth++;
        var moved = 0;

        try
        {
            moved += DutzLevel00CrowdWalkerPlacer.RepairCrowdWalkers(log: false, force: true) ? 1 : 0;
            moved += DutzLevel00CrowdCitizensPlacer.OrganizeCitizens(log: false, force: true) ? 1 : 0;

            var timelineRoot = EnsureRoot(DutzLevel00TimelineMuralPlacer.RootName, ref moved);
            ConsolidateNamedFolders("TimelineMurals (", timelineRoot, ref moved);

            var edsaRoot = EnsureRoot(DutzLevel00EdsaMuralPlacer.RootName, ref moved);
            ConsolidateNamedFolders("EdsaMurals (", edsaRoot, ref moved);
            ParentLooseEdsaPanels(edsaRoot, ref moved);

            var pickupsRoot = EnsureRoot(PickupsRootName, ref moved);
            moved += ParentLooseObjects(PickupNames, pickupsRoot);

            var specialRoot = EnsureRoot(SpecialMuralsRootName, ref moved);
            moved += ParentLooseObjects(SpecialMuralRootNames, specialRoot);

            var rallyRoot = GameObject.Find(DutzLevel00RallyPlacardPlacer.RootName)?.transform;
            if (rallyRoot != null)
                moved += ParentLooseByPrefix("RallyPlacard_", rallyRoot);

            RemoveEmptyDuplicateRoots(ref moved);
        }
        finally
        {
            DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth =
                Mathf.Max(0, DutzHighwayPhotoBillboardPlacer.SceneSyncSuppressDepth - 1);
        }

        if (moved <= 0)
        {
            if (log)
                Debug.Log("[Dutz] Level 00 hierarchy already organized.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);

        if (log)
            Debug.Log($"[Dutz] Organized Level 00 hierarchy ({moved} change group(s)). Save with Ctrl+S when ready.");

        return true;
    }

    static Transform EnsureRoot(string rootName, ref int moved)
    {
        var existing = GameObject.Find(rootName);
        if (existing != null)
            return existing.transform;

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Organize Level 00 hierarchy");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        moved++;
        return root.transform;
    }

    static void ConsolidateNamedFolders(string namePrefix, Transform canonicalRoot, ref int moved)
    {
        if (canonicalRoot == null)
            return;

        var matches = new List<Transform>();
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform == null || !transform.name.StartsWith(namePrefix, System.StringComparison.Ordinal))
                continue;

            matches.Add(transform);
        }

        var byName = new Dictionary<string, List<Transform>>(System.StringComparer.Ordinal);
        foreach (var transform in matches)
        {
            if (!byName.TryGetValue(transform.name, out var list))
            {
                list = new List<Transform>();
                byName[transform.name] = list;
            }

            list.Add(transform);
        }

        foreach (var pair in byName)
        {
            Transform keeper = null;
            foreach (var transform in pair.Value)
            {
                if (transform.parent == canonicalRoot)
                {
                    keeper = transform;
                    break;
                }
            }

            keeper ??= pair.Value[0];

            if (keeper.parent != canonicalRoot)
            {
                Undo.SetTransformParent(keeper, canonicalRoot, "Organize Level 00 hierarchy");
                moved++;
            }

            foreach (var transform in pair.Value)
            {
                if (transform == keeper)
                    continue;

                MoveAllChildren(transform, keeper, ref moved);

                if (transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(transform.gameObject);
                    moved++;
                }
            }
        }
    }

    static void ParentLooseEdsaPanels(Transform edsaRoot, ref int moved)
    {
        if (edsaRoot == null)
            return;

        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform == null || transform.parent != null)
                continue;

            var name = transform.name;
            if (!name.StartsWith("Mural_Highway ", System.StringComparison.Ordinal)
                && !name.StartsWith("EdsaMural_", System.StringComparison.Ordinal))
            {
                continue;
            }

            var segmentName = TryParseHighwaySegmentFromMuralName(name);
            if (string.IsNullOrEmpty(segmentName))
                continue;

            var segmentFolder = FindOrCreateChild(edsaRoot, $"EdsaMurals ({segmentName})", ref moved);
            Undo.SetTransformParent(transform, segmentFolder, "Organize Level 00 hierarchy");
            moved++;
        }
    }

    static string TryParseHighwaySegmentFromMuralName(string muralName)
    {
        if (string.IsNullOrEmpty(muralName))
            return null;

        if (muralName.StartsWith("EdsaMural_", System.StringComparison.Ordinal))
        {
            // Panels are grouped by segment folder already; skip unless orphaned at root.
            return null;
        }

        const string prefix = "Mural_";
        if (!muralName.StartsWith(prefix, System.StringComparison.Ordinal))
            return null;

        var remainder = muralName.Substring(prefix.Length);
        foreach (var segment in DutzLevel00EdsaMuralPlacer.SegmentNames)
        {
            if (remainder.StartsWith(segment, System.StringComparison.Ordinal))
                return segment;
        }

        return null;
    }

    static int ParentLooseObjects(IEnumerable<string> objectNames, Transform parent)
    {
        if (parent == null)
            return 0;

        var moved = 0;
        foreach (var objectName in objectNames)
        {
            var go = GameObject.Find(objectName);
            if (go == null || go.transform.parent == parent)
                continue;

            Undo.SetTransformParent(go.transform, parent, "Organize Level 00 hierarchy");
            moved++;
        }

        return moved;
    }

    static int ParentLooseByPrefix(string prefix, Transform parent)
    {
        if (parent == null)
            return 0;

        var moved = 0;
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform == null || transform.parent == parent)
                continue;

            if (!transform.name.StartsWith(prefix, System.StringComparison.Ordinal))
                continue;

            if (transform.parent != null && transform.parent.name == DutzLevel00RallyPlacardPlacer.RootName)
                continue;

            Undo.SetTransformParent(transform, parent, "Organize Level 00 hierarchy");
            moved++;
        }

        return moved;
    }

    static Transform FindOrCreateChild(Transform parent, string childName, ref int moved)
    {
        foreach (Transform child in parent)
        {
            if (child != null && child.name == childName)
                return child;
        }

        var go = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(go, "Organize Level 00 hierarchy");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        moved++;
        return go.transform;
    }

    static void MoveAllChildren(Transform from, Transform to, ref int moved)
    {
        while (from.childCount > 0)
        {
            var child = from.GetChild(0);
            Undo.SetTransformParent(child, to, "Organize Level 00 hierarchy");
            moved++;
        }
    }

    static void RemoveEmptyDuplicateRoots(ref int moved)
    {
        foreach (var rootName in new[]
                 {
                     DutzLevel00TimelineMuralPlacer.RootName,
                     DutzLevel00EdsaMuralPlacer.RootName,
                     PickupsRootName,
                     SpecialMuralsRootName,
                     DutzLevel00RallyPlacardPlacer.RootName,
                     DutzLevel00CrowdWalkerPlacer.RootName,
                     DutzLevel00CrowdCitizensPlacer.RootName,
                     DutzLevel00CrossroadChaseSpawnsPlacer.RootName,
                 })
        {
            var all = new List<GameObject>();
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (transform != null && transform.name == rootName && transform.parent == null)
                    all.Add(transform.gameObject);
            }

            if (all.Count <= 1)
                continue;

            GameObject keeper = all[0];
            var maxChildren = keeper.transform.childCount;
            foreach (var go in all)
            {
                if (go.transform.childCount > maxChildren)
                {
                    keeper = go;
                    maxChildren = go.transform.childCount;
                }
            }

            foreach (var go in all)
            {
                if (go == keeper)
                    continue;

                MoveAllChildren(go.transform, keeper.transform, ref moved);
                if (go.transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(go);
                    moved++;
                }
            }
        }
    }
}
