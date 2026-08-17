using UnityEngine;

/// <summary>Places the force field suit on the top horizontal bar of a highway bridge segment.</summary>
public static class DutzForceFieldSuitPlacement
{
    const string BridgeSegmentName = "Highway Bridge 1";
    public const float SuitWorldScale = 4f;
    const float MinTopBarHeightAboveDeckMeters = 18f;
    const float MinMiddleDeckHeightAboveWalkableMeters = 22f;
    const float MaxMiddleDeckHeightAboveWalkableMeters = 32f;
    const float PlacementAlongTrackMeters = 110f;
    const float SuitLiftAboveBarMeters = 2.2f;

    public const float TopDeckAlongDefault = 0.5f;
    public const float MiddleDeckAlongDefault = 0.5f;

    public static bool TryGetTopBarWorldPosition(out Vector3 position) =>
        TryGetTopBarWorldPosition(BridgeSegmentName, out position, null);

    public static bool TryGetTopBarWorldPosition(out Vector3 position, Vector3? anchorHint) =>
        TryGetTopBarWorldPosition(BridgeSegmentName, out position, anchorHint);

    public static bool TryGetTopBarWorldPosition(string bridgeSegmentName, out Vector3 position) =>
        TryGetTopBarWorldPosition(bridgeSegmentName, out position, null);

    public static bool TryGetTopBarWorldPosition(string bridgeSegmentName, out Vector3 position, Vector3? anchorHint) =>
        TryGetBridgeDeckWorldPosition(
            bridgeSegmentName,
            out position,
            anchorHint,
            MinTopBarHeightAboveDeckMeters,
            float.PositiveInfinity);

    public static bool TryGetMiddleDeckWorldPosition(string bridgeSegmentName, out Vector3 position, Vector3? anchorHint = null) =>
        TryGetBridgeDeckWorldPosition(
            bridgeSegmentName,
            out position,
            anchorHint,
            MinMiddleDeckHeightAboveWalkableMeters,
            MaxMiddleDeckHeightAboveWalkableMeters);

    public static bool IsLikelyTopDeckHeight(float worldY, float walkableDeckY) =>
        worldY >= walkableDeckY + MinTopBarHeightAboveDeckMeters;

    public static bool IsLikelyMiddleDeckHeight(float worldY, float walkableDeckY) =>
        worldY >= walkableDeckY + MinMiddleDeckHeightAboveWalkableMeters
        && worldY <= walkableDeckY + MaxMiddleDeckHeightAboveWalkableMeters;

    static bool TryGetBridgeDeckWorldPosition(
        string bridgeSegmentName,
        out Vector3 position,
        Vector3? anchorHint,
        float minHeightAboveWalkableMeters,
        float maxHeightAboveWalkableMeters)
    {
        position = Vector3.zero;

        if (string.IsNullOrEmpty(bridgeSegmentName))
            return false;

        var bridge = GameObject.Find(bridgeSegmentName);
        if (bridge == null)
            return false;

        if (!TryGetBridgeBounds(bridge, out var bounds))
            return false;

        var anchor = anchorHint ?? GetPlacementAnchor(bridge, bounds, bridgeSegmentName);
        var deckY = anchor.y;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(anchor, anchor.y, null, out var sampledDeckY))
            deckY = sampledDeckY;

        var minSurfaceY = deckY + minHeightAboveWalkableMeters;
        var maxSurfaceY = float.IsPositiveInfinity(maxHeightAboveWalkableMeters)
            ? float.PositiveInfinity
            : deckY + maxHeightAboveWalkableMeters;
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

            if (hit.point.y > maxSurfaceY)
                continue;

            if (hit.point.y <= bestY)
                continue;

            bestY = hit.point.y;
            bestPoint = hit.point;
            found = true;
        }

        if (!found)
        {
            var fallbackY = float.IsPositiveInfinity(maxHeightAboveWalkableMeters)
                ? bounds.max.y - 1.5f
                : deckY + (minHeightAboveWalkableMeters + maxHeightAboveWalkableMeters) * 0.5f;
            position = new Vector3(anchor.x, fallbackY, anchor.z);
        }
        else
        {
            position = bestPoint + Vector3.up * SuitLiftAboveBarMeters;
        }

        return true;
    }

    static Vector3 GetPlacementAnchor(GameObject bridge, Bounds bridgeBounds, string bridgeSegmentName)
    {
        if (bridgeSegmentName == BridgeSegmentName)
            return GetPlacementAnchor(bridge, bridgeBounds);

        return bridgeBounds.center;
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
