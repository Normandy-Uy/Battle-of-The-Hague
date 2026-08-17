using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level 00 — 7×6 crossroad chaser spawn grid covering highway width (Senate end).
/// Batch: -executeMethod DutzLevel00CrossroadChaseSpawnsPlacer.BuildOnLevel00Batch
/// </summary>
public static class DutzLevel00CrossroadChaseSpawnsPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string RootName = "Level00CrossroadChaseSpawns";
    public const int FormationColumns = 7;
    public const int FormationRows = 6;
    public const int SlotCount = FormationColumns * FormationRows;
    const float SenateEndInsetMeters = 8f;
    const float RowSpacingMeters = 3.5f;

    /// <summary>Batch entry — rebuild default 7×6 grid (overwrites slot transforms).</summary>
    public static void BuildOnLevel00Batch() => BuildDefaultFormation(log: true, force: true);

    /// <summary>Batch entry — re-sample road deck Y on existing slots (keeps XZ).</summary>
    public static void ResnapOnLevel00Batch() => ResnapExistingSlotsToDeck(log: true);

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var root = GameObject.Find(RootName);
        if (root != null && root.transform.childCount == SlotCount)
        {
            if (NeedsDeckResnap(root.transform))
                return ResnapExistingSlotsToDeck(log);
            return false;
        }

        if (root != null && root.transform.childCount > 0)
        {
            var changed = false;
            if (NeedsDeckResnap(root.transform))
                changed |= ResnapExistingSlotsToDeck(log);

            if (root.transform.childCount < SlotCount)
                changed |= RepairMissingSpawnSlots(log);

            if (root.transform.childCount > SlotCount)
            {
                if (log)
                    Debug.LogWarning(
                        $"[Dutz] {RootName} has {root.transform.childCount} slot(s), expected {SlotCount}. " +
                        "Remove extras manually or run BuildOnLevel00Batch to rebuild the 7×6 grid.");
            }

            return changed;
        }

        return BuildDefaultFormation(log, force: true);
    }

    /// <summary>Batch entry — add missing R/C slots only (keeps your manual positions).</summary>
    public static void RepairOnLevel00Batch() => RepairMissingSpawnSlots(log: true);

    /// <summary>Adds missing grid slots without deleting or moving existing ones.</summary>
    public static bool RepairMissingSpawnSlots(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        if (!TryGetSenateEndDeckCenter(out var anchor, out var faceBridge, out var deckHintY))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve Senate end deck to repair crossroad spawn grid.");
            return false;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
            return BuildDefaultFormation(log, force: true);

        var existing = CollectExistingGridIndices(root.transform);
        if (existing.Count >= SlotCount)
            return false;

        var laneZs = DutzHighwayDeckSampler.SevenLaneZ;
        var created = 0;

        for (var row = 0; row < FormationRows; row++)
        {
            for (var col = 0; col < FormationColumns; col++)
            {
                var key = (row, col);
                if (existing.Contains(key))
                    continue;

                var alongOffset = row * RowSpacingMeters;
                var world = anchor + faceBridge * alongOffset;
                world.z = laneZs[col % laneZs.Length];
                world.y = deckHintY;

                if (!TrySampleDeckY(world, deckHintY, out world.y))
                    world.y = deckHintY;

                var slotGo = new GameObject($"CrossroadSpawn_R{row}_C{col}");
                Undo.RegisterCreatedObjectUndo(slotGo, "Repair crossroad spawn slot");
                slotGo.transform.SetParent(root.transform, false);
                slotGo.transform.position = world;
                slotGo.transform.rotation = Quaternion.LookRotation(faceBridge, Vector3.up);

                var slot = Undo.AddComponent<DutzLevel00CrossroadSpawnSlot>(slotGo);
                slot.SetGridIndex(row, col);
                existing.Add(key);
                created++;
            }
        }

        if (created <= 0)
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        if (log)
            Debug.Log(
                $"[Dutz] {RootName}: repaired grid — added {created} missing slot(s) " +
                $"(now {root.transform.childCount}/{SlotCount}). Existing positions were kept.");

        return true;
    }

    static System.Collections.Generic.HashSet<(int row, int col)> CollectExistingGridIndices(Transform root)
    {
        var set = new System.Collections.Generic.HashSet<(int, int)>();
        foreach (Transform child in root)
        {
            if (child == null)
                continue;

            var slot = child.GetComponent<DutzLevel00CrossroadSpawnSlot>();
            if (slot != null)
            {
                set.Add((slot.Row, slot.Column));
                continue;
            }

            if (TryParseSlotName(child.name, out var parsedRow, out var parsedCol))
                set.Add((parsedRow, parsedCol));
        }

        return set;
    }

    static bool TryParseSlotName(string name, out int row, out int col)
    {
        row = 0;
        col = 0;
        if (string.IsNullOrEmpty(name) || !name.StartsWith("CrossroadSpawn_R"))
            return false;

        var parts = name.Split('_');
        if (parts.Length < 3)
            return false;

        if (!int.TryParse(parts[1].TrimStart('R'), out row))
            return false;

        if (!int.TryParse(parts[2].TrimStart('C'), out col))
            return false;

        return true;
    }

    static bool NeedsDeckResnap(Transform root)
    {
        foreach (Transform child in root)
        {
            if (child != null && child.position.y > 15f)
                return true;
        }

        return false;
    }

    public static bool ResnapExistingSlotsToDeck(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        var root = GameObject.Find(RootName);
        if (root == null || root.transform.childCount == 0)
            return false;

        if (!TryGetDeckHintY(out var deckHintY))
            deckHintY = 5.85f;

        var changed = 0;
        foreach (Transform child in root.transform)
        {
            if (child == null)
                continue;

            var world = child.position;
            if (!TrySampleDeckY(world, deckHintY, out var deckY))
                continue;

            if (Mathf.Abs(world.y - deckY) < 0.05f)
                continue;

            Undo.RecordObject(child, "Resnap crossroad spawn slot to deck");
            world.y = deckY;
            child.position = world;
            changed++;
        }

        if (changed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (log)
                Debug.Log($"[Dutz] {RootName}: re-snapped {changed} spawn slot(s) onto the road deck.");
        }

        return changed > 0;
    }

    public static bool BuildDefaultFormation(bool log, bool force)
    {
        if (!System.IO.File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        if (!TryGetSenateEndDeckCenter(out var anchor, out var faceBridge, out var deckHintY))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve Senate end deck for crossroad spawn grid.");
            return false;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create crossroad chaser spawn root");
        }
        else if (force)
        {
            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i);
                if (child != null)
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
        else if (root.transform.childCount > 0)
        {
            return false;
        }

        var laneZs = DutzHighwayDeckSampler.SevenLaneZ;
        var created = 0;

        for (var row = 0; row < FormationRows; row++)
        {
            for (var col = 0; col < FormationColumns; col++)
            {
                var alongOffset = row * RowSpacingMeters;
                var world = anchor + faceBridge * alongOffset;
                world.z = laneZs[col % laneZs.Length];
                world.y = deckHintY;

                if (!TrySampleDeckY(world, deckHintY, out world.y))
                    world.y = deckHintY;

                var slotGo = new GameObject($"CrossroadSpawn_R{row}_C{col}");
                Undo.RegisterCreatedObjectUndo(slotGo, "Create crossroad spawn slot");
                slotGo.transform.SetParent(root.transform, false);
                slotGo.transform.position = world;
                slotGo.transform.rotation = Quaternion.LookRotation(faceBridge, Vector3.up);

                var slot = Undo.AddComponent<DutzLevel00CrossroadSpawnSlot>(slotGo);
                slot.SetGridIndex(row, col);
                created++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (log)
            Debug.Log(
                $"[Dutz] {RootName}: created {created} spawn slot(s) in {FormationColumns}×{FormationRows} formation " +
                $"(anchor={anchor}, faceBridge={faceBridge}). Adjust positions in scene as needed.");

        return created > 0;
    }

    static bool TryGetSenateEndDeckCenter(out Vector3 deckCenter, out Vector3 faceBridge, out float deckHintY)
    {
        deckCenter = Vector3.zero;
        faceBridge = Vector3.right;
        deckHintY = 5.85f;

        if (!TryGetDeckHintY(out deckHintY))
            deckHintY = 5.85f;

        var spawnRef = Vector3.zero;
        var player = DutzEditorHelpers.FindPrimaryDutzPlayer();
        if (player != null)
        {
            var so = new SerializedObject(player);
            spawnRef = so.FindProperty("spawnPosition").vector3Value;
        }

        if (spawnRef.sqrMagnitude < 0.0001f)
            spawnRef = new Vector3(-1042f, deckHintY, -9.1f);

        var travelForward = DutzHighwayDirection.GetSpawnForwardAt(spawnRef);
        travelForward.y = 0f;
        if (travelForward.sqrMagnitude < 0.0001f)
            travelForward = Vector3.right;
        travelForward.Normalize();

        faceBridge = -travelForward;
        var trackRight = Vector3.Cross(Vector3.up, travelForward);
        if (trackRight.sqrMagnitude < 0.0001f)
            return false;
        trackRight.Normalize();

        var crossroad = GameObject.Find("Highway Cross Road");
        if (crossroad == null)
            return false;

        var renderer = crossroad.GetComponentInChildren<Renderer>();
        var bounds = renderer != null ? renderer.bounds : new Bounds(crossroad.transform.position, Vector3.one * 40f);
        var maxAlong = GetBoundsMaxAlong(bounds, spawnRef, travelForward);
        var lateral = Vector3.Dot(bounds.center - spawnRef, trackRight);

        deckCenter = spawnRef + travelForward * (maxAlong - SenateEndInsetMeters) + trackRight * lateral;
        deckCenter.y = deckHintY;

        if (!TrySampleDeckY(deckCenter, deckHintY, out var sampledY))
            return false;

        deckCenter.y = sampledY;
        return true;
    }

    static bool TryGetDeckHintY(out float deckHintY)
    {
        deckHintY = 5.85f;
        var player = DutzEditorHelpers.FindPrimaryDutzPlayer();
        if (player == null)
            return false;

        var so = new SerializedObject(player);
        deckHintY = so.FindProperty("spawnPosition").vector3Value.y;
        return true;
    }

    static float GetBoundsMaxAlong(Bounds bounds, Vector3 spawnRef, Vector3 travelForward)
    {
        var center = bounds.center;
        var extents = bounds.extents;
        var corners = new[]
        {
            new Vector3(center.x + extents.x, center.y, center.z + extents.z),
            new Vector3(center.x + extents.x, center.y, center.z - extents.z),
            new Vector3(center.x - extents.x, center.y, center.z + extents.z),
            new Vector3(center.x - extents.x, center.y, center.z - extents.z)
        };

        var maxAlong = float.MinValue;
        foreach (var corner in corners)
            maxAlong = Mathf.Max(maxAlong, DutzHighwayDeckSampler.AlongTrackAhead(spawnRef, corner, travelForward));

        return maxAlong;
    }

    static bool TrySampleDeckY(Vector3 world, float deckHintY, out float deckY)
    {
        deckY = deckHintY;
        var hintY = deckHintY > 0.1f ? deckHintY : 5.85f;

        if (DutzRoadGround.TrySampleRoadDeckForPlacement(world, hintY, null, out deckY)
            || DutzRoadGround.TrySampleWalkableRoadDeckY(world, hintY, null, out deckY))
            return true;

        return false;
    }
}
