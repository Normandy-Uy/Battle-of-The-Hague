using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Places the ForceField suit on the top beam of Level07 Highway Bridge 1.</summary>
public static class DutzLevel07ForceFieldSuitBridge1Placer
{
    const string Bridge1Name = "Highway Bridge 1";
    const float SuitLiftAboveBeamMeters = 2.2f;
    const float BeamTopToleranceMeters = 1.5f;
    const int GridSteps = 48;

    [MenuItem("Assets/Dutz Authoring/Place ForceField Suit On Bridge 1")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place ForceField Suit On Bridge 1 requires Edit Mode.");
            return;
        }

        if (!Place(log: true))
            Debug.LogError("[Dutz] Failed to place ForceField suit on Highway Bridge 1.");
    }

    public static bool Place(bool log)
    {
        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        if (suit == null)
        {
            Debug.LogError("[Dutz] DutzForceFieldSuit not found in the open scene.");
            return false;
        }

        var bridge = GameObject.Find(Bridge1Name);
        if (bridge == null)
        {
            Debug.LogError($"[Dutz] '{Bridge1Name}' not found in the open scene.");
            return false;
        }

        Physics.SyncTransforms();

        if (!TryGetBridgeBounds(bridge, out var bounds))
        {
            Debug.LogError($"[Dutz] '{Bridge1Name}' has no renderers/colliders to measure.");
            return false;
        }

        if (!TryFindTopBeamPoint(bridge, bounds, out var beamPoint))
        {
            Debug.LogError($"[Dutz] Could not raycast any surface on '{Bridge1Name}'.");
            return false;
        }

        var position = beamPoint + Vector3.up * SuitLiftAboveBeamMeters;
        Undo.RecordObject(suit.transform, "Place ForceField Suit On Bridge 1");
        suit.transform.position = position;

        var scene = suit.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] ForceField suit placed on {Bridge1Name} beam at {position} (beam top {beamPoint}).");

        return true;
    }

    /// <summary>Grid-raycasts the bridge from above and returns the highest surface point,
    /// preferring the point closest to the bridge centre among the top hits (the beam,
    /// not a tower tip at one end).</summary>
    static bool TryFindTopBeamPoint(GameObject bridge, Bounds bounds, out Vector3 beamPoint)
    {
        beamPoint = default;
        var found = false;
        var bestY = float.NegativeInfinity;
        var hitsAtTop = new System.Collections.Generic.List<Vector3>();

        var rayStartY = bounds.max.y + 40f;
        var rayLength = bounds.size.y + 80f;

        for (var ix = 0; ix <= GridSteps; ix++)
        {
            for (var iz = 0; iz <= GridSteps; iz++)
            {
                var x = Mathf.Lerp(bounds.min.x, bounds.max.x, ix / (float)GridSteps);
                var z = Mathf.Lerp(bounds.min.z, bounds.max.z, iz / (float)GridSteps);
                var hits = Physics.RaycastAll(
                    new Vector3(x, rayStartY, z),
                    Vector3.down,
                    rayLength,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                foreach (var hit in hits)
                {
                    if (!IsBridgeCollider(hit.collider, bridge))
                        continue;

                    found = true;
                    if (hit.point.y > bestY)
                        bestY = hit.point.y;

                    hitsAtTop.Add(hit.point);
                }
            }
        }

        if (!found)
            return false;

        // Among the highest hits, pick the one nearest the bridge centre so the suit
        // sits mid-beam instead of on a tower tip at one end.
        var center = bounds.center;
        var bestDistance = float.PositiveInfinity;
        foreach (var point in hitsAtTop)
        {
            if (point.y < bestY - BeamTopToleranceMeters)
                continue;

            var dx = point.x - center.x;
            var dz = point.z - center.z;
            var distance = dx * dx + dz * dz;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                beamPoint = point;
            }
        }

        return true;
    }

    static bool TryGetBridgeBounds(GameObject bridge, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var collider in bridge.GetComponentsInChildren<Collider>(true))
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

        if (hasBounds)
            return true;

        foreach (var renderer in bridge.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
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

    static bool IsBridgeCollider(Collider collider, GameObject bridge)
    {
        if (collider == null || bridge == null)
            return false;

        return collider.transform == bridge.transform || collider.transform.IsChildOf(bridge.transform);
    }
}
