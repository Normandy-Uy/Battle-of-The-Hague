using UnityEngine;

/// <summary>
/// CharacterController occasionally tunnels through scaled non-convex MeshColliders on
/// Highway Bridge 4/5. Adds one static BoxCollider ribbon at the true lower road-deck
/// height (AABB center is inflated by upper beams). MeshColliders stay enabled.
/// Boot-only work — no per-frame cost.
/// </summary>
public static class DutzHighwayBridgeStandableDecks
{
    const string ChildName = "DutzStandableDeck";
    const float DeckThicknessMeters = 3f;
    const float HorizontalInset = 0.06f;
    const float DefaultDeckHintY = 8f;
    const float MaxDeckAboveBoundsMin = 22f;

    static readonly string[] TargetBridgeNames =
    {
        "Highway Bridge 4",
        "Highway Bridge 5",
    };

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03 && !DutzCollectibleProgress.IsLevel07)
            return;

        DutzRoadGround.SyncTransformsIfNeeded();

        for (var i = 0; i < TargetBridgeNames.Length; i++)
            EnsureBridgeDeck(TargetBridgeNames[i]);
    }

    static void EnsureBridgeDeck(string bridgeName)
    {
        // Remove legacy world-space sibling decks from earlier experiments.
        var sibling = GameObject.Find(bridgeName + " StandableDeck");
        if (sibling != null)
            Object.Destroy(sibling);

        var bridge = GameObject.Find(bridgeName);
        if (bridge == null)
            return;

        foreach (var meshCol in bridge.GetComponents<MeshCollider>())
        {
            if (meshCol != null)
                meshCol.enabled = true;
        }

        var existing = bridge.transform.Find(ChildName);
        if (existing != null)
            Object.Destroy(existing.gameObject);

        var meshCollider = bridge.GetComponent<MeshCollider>();
        if (meshCollider == null)
            return;

        var bounds = meshCollider.bounds;
        if (bounds.size.sqrMagnitude < 0.01f)
            return;

        if (!TrySampleRoadDeckY(bridge, meshCollider, bounds, out var deckY))
            deckY = Mathf.Clamp(DefaultDeckHintY, bounds.min.y + 0.5f, bounds.min.y + MaxDeckAboveBoundsMin);

        // Top of box = deck surface so CharacterController rests on a solid face.
        var thickness = DeckThicknessMeters;
        var worldCenter = new Vector3(bounds.center.x, deckY - thickness * 0.5f, bounds.center.z);
        var worldSize = new Vector3(
            Mathf.Max(4f, bounds.size.x * (1f - HorizontalInset)),
            thickness,
            Mathf.Max(4f, bounds.size.z * (1f - HorizontalInset)));

        var child = new GameObject(ChildName);
        child.transform.SetParent(bridge.transform, false);
        child.layer = bridge.layer;

        var box = child.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.enabled = true;

        var lossy = bridge.transform.lossyScale;
        var invScale = new Vector3(
            ApproxInv(lossy.x),
            ApproxInv(lossy.y),
            ApproxInv(lossy.z));

        box.center = bridge.transform.InverseTransformPoint(worldCenter);
        box.size = new Vector3(
            worldSize.x * invScale.x,
            worldSize.y * invScale.y,
            worldSize.z * invScale.z);

        Debug.Log(
            "[Dutz] Standable deck on " + bridgeName +
            " at Y=" + deckY.ToString("0.00") +
            " (box top; MeshCollider kept).");
    }

    static float ApproxInv(float scale) =>
        Mathf.Abs(scale) < 0.0001f ? 1f : 1f / scale;

    /// <summary>
    /// Prefer the lowest upward-facing MeshCollider hit cluster (road deck), not beams.
    /// </summary>
    static bool TrySampleRoadDeckY(
        GameObject bridge,
        MeshCollider meshCollider,
        Bounds bounds,
        out float deckY)
    {
        deckY = 0f;
        var hits = 0;
        var sumY = 0f;
        var minAccepted = float.PositiveInfinity;

        // Hint near typical walk height so stacked-deck bridges pick the road, not cables.
        var hintY = Mathf.Clamp(
            DefaultDeckHintY,
            bounds.min.y + 1f,
            bounds.min.y + MaxDeckAboveBoundsMin);

        var spanX = bounds.size.x;
        var spanZ = bounds.size.z;
        var alongX = spanX >= spanZ;
        var halfAlong = (alongX ? spanX : spanZ) * 0.5f * (1f - HorizontalInset);
        var halfAcross = (alongX ? spanZ : spanX) * 0.15f;

        // 5 samples along the span × 3 across the lane.
        for (var i = -2; i <= 2; i++)
        {
            var along = halfAlong * (i / 2f);
            for (var j = -1; j <= 1; j++)
            {
                var across = halfAcross * j;
                var x = bounds.center.x + (alongX ? along : across);
                var z = bounds.center.z + (alongX ? across : along);
                var origin = new Vector3(x, bounds.max.y + 40f, z);
                var castDist = bounds.size.y + 80f;
                var count = Physics.RaycastNonAlloc(
                    origin,
                    Vector3.down,
                    RayBuffer,
                    castDist,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                var bestY = float.PositiveInfinity;
                var found = false;
                for (var h = 0; h < count; h++)
                {
                    var hit = RayBuffer[h];
                    if (hit.collider != meshCollider)
                        continue;

                    if (Vector3.Dot(hit.normal.normalized, Vector3.up) < 0.45f)
                        continue;

                    // Reject upper beams/cables far above the road hint.
                    if (hit.point.y > hintY + 12f)
                        continue;

                    if (hit.point.y < bestY)
                    {
                        bestY = hit.point.y;
                        found = true;
                    }
                }

                if (!found)
                    continue;

                sumY += bestY;
                hits++;
                if (bestY < minAccepted)
                    minAccepted = bestY;
            }
        }

        if (hits <= 0)
        {
            // Fallback: named-highway sampler with a low hint position.
            var hintPos = new Vector3(bounds.center.x, hintY, bounds.center.z);
            if (DutzRoadGround.TrySampleLevel07NamedHighwayDeckPoint(
                    bridge.name,
                    hintPos,
                    out var deckPoint,
                    out _))
            {
                deckY = deckPoint.y;
                return true;
            }

            return false;
        }

        // Blend mean with lowest sample so one high outlier can't lift the box into beams.
        deckY = Mathf.Lerp(minAccepted, sumY / hits, 0.35f);
        return true;
    }

    static readonly RaycastHit[] RayBuffer = new RaycastHit[32];
}
