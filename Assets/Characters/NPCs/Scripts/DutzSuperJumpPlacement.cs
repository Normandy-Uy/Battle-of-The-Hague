using UnityEngine;

/// <summary>Places the Super Jump pickup on the top horizontal bar of Highway Bridge 1.</summary>
public static class DutzSuperJumpPlacement
{
    const string BridgeSegmentName = "Highway Bridge 1";
    public const float PickupWorldScale = 1f;
    const float PlacementAlongTrackMeters = 55f;

    public static bool TryGetTopBarWorldPosition(out Vector3 position) =>
        TryGetTopBarWorldPosition(out position, null);

    public static bool TryGetTopBarWorldPosition(out Vector3 position, Vector3? anchorHint)
    {
        position = Vector3.zero;

        var bridge = GameObject.Find(BridgeSegmentName);
        if (bridge == null)
            return false;

        if (!TryGetBridgeBounds(bridge, out var bounds))
            return false;

        var anchor = anchorHint ?? GetPlacementAnchor(bridge, bounds);
        var deckY = anchor.y;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(anchor, anchor.y, null, out var sampledDeckY))
            deckY = sampledDeckY;

        const float minHeightAboveWalkableMeters = 18f;
        var minSurfaceY = deckY + minHeightAboveWalkableMeters;
        var bestY = float.NegativeInfinity;
        var bestPoint = anchor;
        var found = false;

        var rayOrigin = new Vector3(anchor.x, bounds.max.y + 40f, anchor.z);
        var hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            bounds.size.y + 80f,
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (var hit in hits)
        {
            if (!IsBridgeCollider(hit.collider, bridge))
                continue;

            if (hit.point.y < minSurfaceY)
                continue;

            if (hit.point.y <= bestY)
                continue;

            bestY = hit.point.y;
            bestPoint = hit.point;
            found = true;
        }

        position = found
            ? bestPoint + Vector3.up * 2.2f
            : new Vector3(anchor.x, bounds.max.y - 1.5f, anchor.z);

        return true;
    }

    static Vector3 GetPlacementAnchor(GameObject bridge, Bounds bridgeBounds)
    {
        DutzHighwayDirection.InvalidateReferenceCache();
        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackStart, out var travelForward))
            return trackStart + travelForward * PlacementAlongTrackMeters;

        var player = Object.FindObjectOfType<DutzPlayerController>();
        if (player != null)
        {
            var forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                return player.transform.position + forward.normalized * PlacementAlongTrackMeters;
        }

        var center = bridgeBounds.center;
        if (DutzRoadGround.TrySampleRoadDeckForPlacement(center, center.y, null, out var deckY))
            center.y = deckY;

        return center;
    }

    static bool TryGetBridgeBounds(GameObject bridge, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

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

        return hasBounds;
    }

    static bool IsBridgeCollider(Collider collider, GameObject bridge)
    {
        if (collider == null || bridge == null)
            return false;

        return collider.transform == bridge.transform || collider.transform.IsChildOf(bridge.transform);
    }
}
