using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Walkable road/deck height — same highest-hit ray logic as DutzPlayerController.
/// </summary>
public static class DutzRoadGround
{
    static int lastSyncTransformsFrame = -1;
    static readonly RaycastHit[] RaycastBuffer = new RaycastHit[64];
    static readonly Dictionary<string, HighwayDeckCache> HighwayCaches =
        new Dictionary<string, HighwayDeckCache>(16);

    struct HighwayDeckCache
    {
        public Transform Road;
        public MeshCollider Collider;
        public Bounds LocalBounds;
        public bool Valid;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHighwayCaches()
    {
        lastSyncTransformsFrame = -1;
        HighwayCaches.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterHighwayCacheInvalidation()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedInvalidateHighwayCaches;
        SceneManager.sceneLoaded += OnSceneLoadedInvalidateHighwayCaches;
    }

    static void OnSceneLoadedInvalidateHighwayCaches(Scene scene, LoadSceneMode mode) =>
        HighwayCaches.Clear();

    public static void InvalidateHighwayCache() => HighwayCaches.Clear();

    static bool TryGetHighwayCache(string highwayObjectName, out HighwayDeckCache cache)
    {
        cache = default;
        if (string.IsNullOrEmpty(highwayObjectName))
            return false;

        if (HighwayCaches.TryGetValue(highwayObjectName, out cache)
            && cache.Valid
            && cache.Road != null
            && cache.Collider != null)
        {
            return true;
        }

        var highway = GameObject.Find(highwayObjectName);
        if (highway == null)
        {
            HighwayCaches[highwayObjectName] = default;
            return false;
        }

        var col = highway.GetComponent<MeshCollider>();
        if (col == null)
        {
            HighwayCaches[highwayObjectName] = default;
            return false;
        }

        var mesh = col.sharedMesh;
        cache = new HighwayDeckCache
        {
            Road = highway.transform,
            Collider = col,
            LocalBounds = mesh != null ? mesh.bounds : new Bounds(Vector3.zero, Vector3.one),
            Valid = true
        };
        HighwayCaches[highwayObjectName] = cache;
        return true;
    }

    static int RaycastNonAlloc(Vector3 origin, Vector3 direction, float maxDistance) =>
        Physics.RaycastNonAlloc(
            origin,
            direction,
            RaycastBuffer,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

    public static void SyncTransformsIfNeeded()
    {
        var frame = Time.frameCount;
        if (lastSyncTransformsFrame == frame)
            return;

        lastSyncTransformsFrame = frame;
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Cheap flat proximity to a named highway AABB — no raycast. Used for chase leashes.
    /// </summary>
    public static bool IsNearHighwayAabb(string highwayObjectName, Vector3 worldPosition, float maxFlatMeters)
    {
        if (!TryGetHighwayCache(highwayObjectName, out var cache))
            return false;

        var closest = cache.Collider.bounds.ClosestPoint(worldPosition);
        var flat = worldPosition - closest;
        flat.y = 0f;
        if (flat.sqrMagnitude > maxFlatMeters * maxFlatMeters)
            return false;

        return Mathf.Abs(worldPosition.y - closest.y) <= maxFlatMeters;
    }

    public static bool TrySampleWalkSurface(Vector3 worldPosition, Collider exclude, out float surfaceY)
    {
        surfaceY = worldPosition.y;
        SyncTransformsIfNeeded();

        var scale = 1f;
        if (exclude != null)
            scale = Mathf.Max(exclude.transform.lossyScale.y, 1f);

        var origin = worldPosition + Vector3.up * (20f * scale);
        var hitCount = RaycastNonAlloc(origin, Vector3.down, 50f * scale);
        if (hitCount == 0)
            return false;

        var bestY = float.NegativeInfinity;
        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (exclude != null && IsSelfCollider(hit.collider, exclude))
                continue;

            if (IsNpcWalkSurfaceCollider(hit.collider))
                continue;

            if (hit.point.y > bestY)
                bestY = hit.point.y;
        }

        if (bestY <= float.NegativeInfinity)
            return false;

        surfaceY = bestY;
        return true;
    }

    public static bool TrySampleSurfaceY(Vector3 worldPosition, Collider exclude, out float surfaceY) =>
        TrySampleWalkSurface(worldPosition, exclude, out surfaceY);

    public static bool TrySampleGroundBelow(Vector3 worldPosition, Collider exclude, out float surfaceY) =>
        TrySampleWalkSurface(worldPosition, exclude, out surfaceY);

    /// <summary>Lowest highway/bridge deck under a point (avoids tall bridge shells used for coin height).</summary>
    public static bool TrySampleRoadDeckY(Vector3 worldPosition, float hintY, Collider exclude, out float surfaceY)
    {
        surfaceY = worldPosition.y;
        SyncTransformsIfNeeded();

        var origin = new Vector3(worldPosition.x, hintY + 35f, worldPosition.z);
        var hitCount = RaycastNonAlloc(origin, Vector3.down, 90f);
        if (hitCount == 0)
            return false;

        var minY = float.PositiveInfinity;
        var minRoadY = float.PositiveInfinity;
        var floor = hintY - 18f;
        var ceiling = hintY + 6f;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (exclude != null && IsSelfCollider(hit.collider, exclude))
                continue;

            if (IsNpcWalkSurfaceCollider(hit.collider))
                continue;

            var y = hit.point.y;
            if (y < floor || y > ceiling)
                continue;

            if (y < minY)
                minY = y;

            if (IsRoadCollider(hit.collider) && y < minRoadY)
                minRoadY = y;
        }

        if (minRoadY < float.PositiveInfinity)
        {
            surfaceY = minRoadY;
            return true;
        }

        if (minY < float.PositiveInfinity)
        {
            surfaceY = minY;
            return true;
        }

        return false;
    }

    /// <summary>True when a highway deck is under the point near foot height (not a distant hit).</summary>
    public static bool IsStandingOnRoadDeck(Vector3 worldPosition, float feetY)
    {
        if (!TrySampleWalkableRoadDeckY(worldPosition, feetY, null, out var deckY))
            return false;

        return Mathf.Abs(deckY - feetY) <= 2.5f;
    }

    /// <summary>Topmost highway/bridge deck under the feet (for walking / standing checks).</summary>
    public static bool TrySampleWalkableRoadDeckY(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        out float surfaceY) =>
        TrySampleRoadDeckNearFeet(worldPosition, feetY, exclude, feetBelow: 5f, feetAbove: 2.5f, out surfaceY);

    /// <summary>
    /// Level07 Highway Straight 2 is a steep pitched slab (euler X ~336°, scale Y 12).
    /// Do NOT use MeshFilter.sharedMesh.bounds at runtime — static batching replaces the mesh
    /// with world-sized bounds and TransformPoint then launches NPCs thousands of units away.
    /// Do NOT use MeshCollider.ClosestPoint — Straight 2 is non-convex, so ClosestPoint is a
    /// no-op and "deckPoint = input + road.up * feet" every FixedUpdate launches addicts.
    /// Clamp with MeshCollider.sharedMesh local bounds, then ray along -road.up onto the top face.
    /// </summary>
    public static bool TrySampleLevel07Straight2DeckPoint(Vector3 worldPosition, out Vector3 deckPoint) =>
        TrySampleLevel07Straight2DeckPoint(worldPosition, out deckPoint, out _);

    public static bool TrySampleLevel07Straight2DeckPoint(
        Vector3 worldPosition,
        out Vector3 deckPoint,
        out Vector3 deckUp) =>
        TrySampleLevel07NamedPitchedHighwayDeckPoint("Highway Straight 2", worldPosition, out deckPoint, out deckUp);

    public static bool TrySampleLevel07Straight3DeckPoint(Vector3 worldPosition, out Vector3 deckPoint) =>
        TrySampleLevel07Straight3DeckPoint(worldPosition, out deckPoint, out _);

    public static bool TrySampleLevel07Straight3DeckPoint(
        Vector3 worldPosition,
        out Vector3 deckPoint,
        out Vector3 deckUp) =>
        TrySampleLevel07NamedPitchedHighwayDeckPoint("Highway Straight 3", worldPosition, out deckPoint, out deckUp);

    /// <summary>
    /// Level07 pitched highway slabs (Straight 2 / Straight 3): clamp with MeshCollider local bounds,
    /// then ray along -road.up onto the top face.
    /// </summary>
    public static bool TrySampleLevel07NamedPitchedHighwayDeckPoint(
        string highwayObjectName,
        Vector3 worldPosition,
        out Vector3 deckPoint,
        out Vector3 deckUp)
    {
        deckPoint = worldPosition;
        deckUp = Vector3.up;

        if (!TryGetHighwayCache(highwayObjectName, out var cache))
            return false;

        var road = cache.Road;
        var col = cache.Collider;
        var localBounds = cache.LocalBounds;

        SyncTransformsIfNeeded();

        deckUp = road.up.normalized;
        if (deckUp.y < 0f)
            deckUp = -deckUp;

        // Physics mesh local bounds stay valid even when the renderer is statically batched.
        const float inset = 0.02f;
        var minX = localBounds.min.x + inset;
        var maxX = localBounds.max.x - inset;
        var minZ = localBounds.min.z + inset;
        var maxZ = localBounds.max.z - inset;
        if (minX > maxX)
        {
            minX = localBounds.center.x;
            maxX = localBounds.center.x;
        }

        if (minZ > maxZ)
        {
            minZ = localBounds.center.z;
            maxZ = localBounds.center.z;
        }

        var local = road.InverseTransformPoint(worldPosition);
        local.x = Mathf.Clamp(local.x, minX, maxX);
        local.z = Mathf.Clamp(local.z, minZ, maxZ);
        local.y = localBounds.max.y; // seed on local AABB top; ray finds the pitched face
        var onSlab = road.TransformPoint(local);

        // Cast along -deckUp through the pitched top face.
        var castDist = Mathf.Max(40f, localBounds.size.y * road.lossyScale.y + 20f);
        var origin = onSlab + deckUp * castDist;
        var hitCount = RaycastNonAlloc(origin, -deckUp, castDist * 2f);

        var bestScore = float.PositiveInfinity;
        var found = false;
        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider != col)
                continue;

            // Prefer upward-facing faces (top of steep slab), then nearest to on-slab seed.
            var facing = Vector3.Dot(hit.normal.normalized, deckUp);
            if (facing < 0.25f)
                continue;

            var score = (1f - facing) * 100f + (hit.point - onSlab).sqrMagnitude;
            if (score >= bestScore)
                continue;

            bestScore = score;
            deckPoint = hit.point;
            found = true;
        }

        if (!found)
        {
            // Stay on the authored slab top — never reuse a runaway world position.
            deckPoint = onSlab;
        }

        return true;
    }

    /// <summary>
    /// Keeps a world point on Straight 2's pitched top (pulls XZ/Y onto the collider).
    /// </summary>
    public static bool TryClampOntoLevel07Straight2Deck(ref Vector3 worldPosition, float pivotToFeet)
    {
        if (!TrySampleLevel07Straight2DeckPoint(worldPosition, out var deckPoint, out var deckUp))
            return false;

        // Cap feet offset — corrupt/huge pivotToFeet must never yeet NPCs into the sky.
        var feet = Mathf.Clamp(pivotToFeet, 0.05f, 3.5f);
        worldPosition = deckPoint + deckUp * feet;
        return true;
    }

    /// <summary>
    /// Keeps a world point on Straight 3's pitched top (pulls XZ/Y onto the collider).
    /// </summary>
    public static bool TryClampOntoLevel07Straight3Deck(ref Vector3 worldPosition, float pivotToFeet)
    {
        if (!TrySampleLevel07Straight3DeckPoint(worldPosition, out var deckPoint, out var deckUp))
            return false;

        var feet = Mathf.Clamp(pivotToFeet, 0.05f, 3.5f);
        worldPosition = deckPoint + deckUp * feet;
        return true;
    }

    /// <summary>
    /// Level07 Highway 8 is a long sloping mesh (world Y rises toward −X).
    /// Always take the highest upward-facing Highway 8 hit at this XZ — never a lower/underside face.
    /// </summary>
    public static bool TrySampleLevel07Highway8DeckPoint(
        Vector3 worldPosition,
        out Vector3 deckPoint,
        out Vector3 deckUp) =>
        TrySampleLevel07NamedHighwayDeckPoint("Highway 8", worldPosition, out deckPoint, out deckUp);

    /// <summary>
    /// Level07 Highway 7 — same top-deck raycast as Highway 8.
    /// Slope runs opposite Highway 8 (world Y rises toward +X).
    /// </summary>
    public static bool TrySampleLevel07Highway7DeckPoint(
        Vector3 worldPosition,
        out Vector3 deckPoint,
        out Vector3 deckUp) =>
        TrySampleLevel07NamedHighwayDeckPoint("Highway 7", worldPosition, out deckPoint, out deckUp);

    public static bool TrySampleLevel07NamedHighwayDeckPoint(
        string highwayObjectName,
        Vector3 worldPosition,
        out Vector3 deckPoint,
        out Vector3 deckUp)
    {
        deckPoint = worldPosition;
        deckUp = Vector3.up;

        if (!TryGetHighwayCache(highwayObjectName, out var cache))
            return false;

        var road = cache.Road;
        var col = cache.Collider;
        var localBounds = cache.LocalBounds;

        SyncTransformsIfNeeded();

        const float inset = 0.02f;
        var minX = localBounds.min.x + inset;
        var maxX = localBounds.max.x - inset;
        var minZ = localBounds.min.z + inset;
        var maxZ = localBounds.max.z - inset;
        if (minX > maxX)
        {
            minX = localBounds.center.x;
            maxX = localBounds.center.x;
        }

        if (minZ > maxZ)
        {
            minZ = localBounds.center.z;
            maxZ = localBounds.center.z;
        }

        var local = road.InverseTransformPoint(worldPosition);
        local.x = Mathf.Clamp(local.x, minX, maxX);
        local.z = Mathf.Clamp(local.z, minZ, maxZ);
        local.y = localBounds.center.y;
        var onSlab = road.TransformPoint(local);

        // Ray from above the whole AABB so slope top is always found from above.
        var worldAabb = col.bounds;
        var castTop = worldAabb.max.y + 25f;
        var castDist = castTop - (worldAabb.min.y - 25f);
        var origin = new Vector3(onSlab.x, castTop, onSlab.z);
        var hitCount = RaycastNonAlloc(origin, Vector3.down, castDist);

        // Bridges have stacked decks/beams — prefer the surface nearest the NPC's current Y.
        // Sloping highways (7/8) still want the highest walkable face.
        var preferNearestY = highwayObjectName.StartsWith("Highway Bridge", System.StringComparison.Ordinal);
        var bestY = preferNearestY ? float.PositiveInfinity : float.NegativeInfinity;
        var bestDelta = float.PositiveInfinity;
        var found = false;
        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider != col)
                continue;

            var normal = hit.normal.normalized;
            // Walkable top / slope face — reject underside and near-vertical walls.
            if (Vector3.Dot(normal, Vector3.up) < 0.35f)
                continue;

            if (preferNearestY)
            {
                var delta = Mathf.Abs(hit.point.y - worldPosition.y);
                if (delta > bestDelta)
                    continue;

                bestDelta = delta;
                bestY = hit.point.y;
            }
            else if (hit.point.y <= bestY)
            {
                continue;
            }
            else
            {
                bestY = hit.point.y;
            }

            deckPoint = hit.point;
            deckUp = normal;
            found = true;
        }

        if (!found)
        {
            // Fallback: keep near current height on bridges; sloping highways use AABB top.
            if (preferNearestY)
            {
                var hintLocal = road.InverseTransformPoint(worldPosition);
                local.y = Mathf.Clamp(hintLocal.y, localBounds.min.y, localBounds.max.y);
            }
            else
                local.y = localBounds.max.y;

            deckPoint = road.TransformPoint(local);
            deckUp = Vector3.up;
        }

        if (deckUp.y < 0.35f)
            deckUp = Vector3.up;
        else
            deckUp.Normalize();

        return true;
    }

    public static bool TryClampOntoLevel07Highway8Deck(ref Vector3 worldPosition, float pivotToFeet) =>
        TryClampOntoLevel07NamedHighwayDeck("Highway 8", ref worldPosition, pivotToFeet);

    public static bool TryClampOntoLevel07Highway7Deck(ref Vector3 worldPosition, float pivotToFeet) =>
        TryClampOntoLevel07NamedHighwayDeck("Highway 7", ref worldPosition, pivotToFeet);

    public static bool TryClampOntoLevel07NamedHighwayDeck(
        string highwayObjectName,
        ref Vector3 worldPosition,
        float pivotToFeet)
    {
        if (!TrySampleLevel07NamedHighwayDeckPoint(
                highwayObjectName, worldPosition, out var deckPoint, out var deckUp))
            return false;

        var feet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);
        // Keep pivot above the sloping top along surface normal.
        worldPosition = deckPoint + deckUp * feet;
        return true;
    }

    /// <summary>
    /// Top walkable road deck at X/Z for NPC spawn/teleport — wide vertical search so authored Y hints
    /// do not leave crocs under bridge shells or on terrain below the deck.
    /// </summary>
    public static bool TrySampleRoadDeckForPlacement(
        Vector3 worldPosition,
        float hintY,
        Collider exclude,
        out float surfaceY)
    {
        surfaceY = worldPosition.y;
        SyncTransformsIfNeeded();

        var origin = new Vector3(worldPosition.x, hintY + 40f, worldPosition.z);
        var hitCount = RaycastNonAlloc(origin, Vector3.down, 100f);
        if (hitCount == 0)
            return false;

        var minY = hintY - 25f;
        var maxY = hintY + 35f;
        var bestY = float.NegativeInfinity;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (exclude != null && IsSelfCollider(hit.collider, exclude))
                continue;

            if (!IsRoadCollider(hit.collider))
                continue;

            var y = hit.point.y;
            if (y < minY || y > maxY)
                continue;

            if (y > bestY)
                bestY = y;
        }

        if (bestY <= float.NegativeInfinity)
            return false;

        surfaceY = bestY;
        return true;
    }

    /// <summary>Deck directly under the feet — ignores lips/upper shells (for fall detection while jumping).</summary>
    public static bool TrySampleSupportDeckBelowFeet(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        out float surfaceY) =>
        TrySampleRoadDeckNearFeet(
            worldPosition,
            feetY,
            exclude,
            feetBelow: 10f,
            feetAbove: 0.4f,
            includeEndGoalHouse: true,
            out surfaceY);

    /// <summary>
    /// Top walkable road deck straight below the feet within a vertical drop range.
    /// Used for force-field multi-deck drops — ignores off-edge geometry that is not road at this X/Z.
    /// </summary>
    public static bool TrySampleRoadDeckBelowForShieldedDrop(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        float maxDropMeters,
        out float surfaceY) =>
        TrySampleRoadDeckNearFeet(
            worldPosition,
            feetY,
            exclude,
            feetBelow: maxDropMeters,
            feetAbove: 0.5f,
            includeEndGoalHouse: true,
            out surfaceY);

    /// <summary>Crocodiles: stay on current deck; do not snap up to a bridge deck far above feet.</summary>
    public const float CrocMaxDeckFeetAbove = 0.45f;

    public static bool TrySampleCrocodileRoadDeckY(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        out float surfaceY) =>
        TrySampleRoadDeckNearFeet(
            worldPosition,
            feetY,
            exclude,
            feetBelow: 14f,
            feetAbove: CrocMaxDeckFeetAbove,
            out surfaceY);

    /// <summary>
    /// Level 3 giants: highest walkable deck near the feet (like crocs), with bridge lane-shell correction.
    /// </summary>
    public const float GiantMaxDeckFeetAbove = 0.45f;
    const float GiantDeckFeetBelow = 8f;
    const float BridgeLaneShellDropMin = 1f;
    const float BridgeLaneShellDropMax = 3.5f;

    public static bool TrySampleGiantRoadDeckY(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        out float surfaceY)
    {
        if (TrySampleGiantRoadDeckNearFeet(worldPosition, feetY, exclude, out surfaceY))
            return true;

        // Sunk below deck — search upward from current feet.
        if (TrySampleRoadDeckNearFeet(
                worldPosition,
                feetY + 6f,
                exclude,
                feetBelow: 12f,
                feetAbove: 2.5f,
                out surfaceY))
        {
            return true;
        }

        return TrySampleWalkableRoadDeckY(worldPosition, feetY, exclude, out surfaceY);
    }

    static bool TrySampleGiantRoadDeckNearFeet(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        out float surfaceY)
    {
        if (!TrySampleRoadDeckNearFeet(
                worldPosition,
                feetY,
                exclude,
                feetBelow: GiantDeckFeetBelow,
                feetAbove: GiantMaxDeckFeetAbove,
                out surfaceY))
        {
            return false;
        }

        var segment = DutzHighwayDirection.FindNearestTrackSegment(worldPosition);
        if (segment == null
            || segment.name.IndexOf("Bridge", System.StringComparison.OrdinalIgnoreCase) < 0
            || Mathf.Abs(worldPosition.z) <= 1f)
        {
            return true;
        }

        var center = new Vector3(worldPosition.x, feetY, 0f);
        if (!TrySampleRoadDeckNearFeet(
                center,
                feetY,
                exclude,
                feetBelow: GiantDeckFeetBelow,
                feetAbove: GiantMaxDeckFeetAbove,
                out var centerDeckY))
        {
            return true;
        }

        var laneAboveCenter = surfaceY - centerDeckY;
        if (laneAboveCenter > BridgeLaneShellDropMin && laneAboveCenter < BridgeLaneShellDropMax)
            surfaceY = centerDeckY;

        return true;
    }

    static bool TrySampleRoadDeckNearFeet(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        float feetBelow,
        float feetAbove,
        out float surfaceY) =>
        TrySampleRoadDeckNearFeet(
            worldPosition,
            feetY,
            exclude,
            feetBelow,
            feetAbove,
            includeEndGoalHouse: false,
            out surfaceY);

    static bool TrySampleRoadDeckNearFeet(
        Vector3 worldPosition,
        float feetY,
        Collider exclude,
        float feetBelow,
        float feetAbove,
        bool includeEndGoalHouse,
        out float surfaceY)
    {
        surfaceY = feetY;
        SyncTransformsIfNeeded();

        var origin = new Vector3(worldPosition.x, feetY + 8f, worldPosition.z);
        var hitCount = RaycastNonAlloc(origin, Vector3.down, feetBelow + feetAbove + 10f);
        if (hitCount == 0)
            return false;

        var bestY = float.NegativeInfinity;
        var minY = feetY - feetBelow;
        var maxY = feetY + feetAbove;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (exclude != null && IsSelfCollider(hit.collider, exclude))
                continue;

            if (!IsRoadCollider(hit.collider)
                && !(includeEndGoalHouse && DutzEndHouseCollider.IsHouseCollider(hit.collider)))
                continue;

            var y = hit.point.y;
            if (y < minY || y > maxY)
                continue;

            if (y > bestY)
                bestY = y;
        }

        if (bestY <= float.NegativeInfinity)
            return false;

        surfaceY = bestY;
        return true;
    }

    /// <summary>Moves horizontal position onto the nearest highway/bridge deck (Y unchanged — use SnapFeetToGround after).</summary>
    public static bool TrySnapPositionToNearestRoadDeck(ref Vector3 worldPosition, float hintY, Collider exclude)
    {
        var nearest = DutzHighwayDirection.FindNearestTrackSegment(worldPosition);
        if (nearest == null)
            return false;

        var closest = DutzHighwayDirection.GetClosestPointOnSegment(nearest, worldPosition);
        var sample = new Vector3(closest.x, hintY, closest.z);
        if (!TrySampleWalkableRoadDeckY(sample, hintY, exclude, out _))
            return false;

        worldPosition.x = closest.x;
        worldPosition.z = closest.z;
        return true;
    }

    public static bool IsHighwayRoadCollider(Collider hit) => IsRoadCollider(hit);

    static bool IsRoadCollider(Collider hit)
    {
        var t = hit.transform;
        while (t != null)
        {
            var name = t.name;
            // Must be a highway segment — NOT pickups like DutzParachutePickup_Bridge5.
            if (name.StartsWith("Highway", System.StringComparison.Ordinal))
                return true;

            t = t.parent;
        }

        return false;
    }

    static bool IsSelfCollider(Collider hit, Collider exclude)
    {
        if (hit == exclude)
            return true;

        if (exclude == null)
            return false;

        return hit.transform == exclude.transform || hit.transform.IsChildOf(exclude.transform);
    }

    static bool IsNpcWalkSurfaceCollider(Collider hit)
    {
        if (hit == null)
            return false;

        if (DutzGiantHeadTopCollider.IsGiantHeadCollider(hit))
            return true;

        var root = hit.transform.root;
        if (root != null)
        {
            var rootName = root.name;
            if (!string.IsNullOrEmpty(rootName)
                && rootName.StartsWith("SimpleCitizens_", System.StringComparison.Ordinal))
            {
                return true;
            }

            if (root.GetComponent<DutzLevel00CrowdWalkerPhysics>() != null)
                return true;
        }

        var t = hit.transform;
        while (t != null)
        {
            if (t.GetComponent<SimpleCitizensNpcPhysics>() != null)
                return true;

            t = t.parent;
        }

        return false;
    }

    /// <summary>Player is on or beside bridge decks, beams, or suspension geometry (top/middle/cables).</summary>
    public static bool IsNearBridgeStructure(Vector3 position, CharacterController controller)
    {
        var probe = position + Vector3.up * 0.35f;
        const float rayLength = 70f;
        var hitCount = RaycastNonAlloc(probe, Vector3.down, rayLength);
        var onBridgeCollider = false;
        var highestBridgeY = float.NegativeInfinity;
        for (var i = 0; i < hitCount; i++)
        {
            var hit = RaycastBuffer[i];
            if (hit.collider == null || !IsBridgeStructureCollider(hit.collider))
                continue;

            onBridgeCollider = true;
            if (hit.point.y > highestBridgeY)
                highestBridgeY = hit.point.y;
        }

        if (onBridgeCollider && position.y >= highestBridgeY - 3f)
            return true;

        if (TrySampleWalkableRoadDeckY(position, position.y, controller, out var deckY))
        {
            var heightAboveWalkable = position.y - deckY;
            if (heightAboveWalkable >= -2f && heightAboveWalkable <= 58f && onBridgeCollider)
                return true;
        }

        // Brief ungrounded steps on narrow beams/cables between bridge decks.
        var scale = controller != null ? Mathf.Max(1f, controller.transform.lossyScale.y) : 1f;
        var radius = controller != null
            ? Mathf.Max(1.2f, controller.radius * controller.transform.lossyScale.x + 0.8f)
            : 1.5f;
        var nearby = Physics.OverlapSphere(
            position + Vector3.up * (0.5f * scale),
            radius * scale,
            ~0,
            QueryTriggerInteraction.Ignore);
        for (var i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] != null && IsBridgeStructureCollider(nearby[i]))
                return true;
        }

        return false;
    }

    static bool IsBridgeStructureCollider(Collider col)
    {
        var t = col != null ? col.transform : null;
        while (t != null)
        {
            var name = t.name;
            if (name.Contains("Bridge") || name.Contains("Highway Bridge"))
                return true;

            t = t.parent;
        }

        return false;
    }
}

/// <summary>
/// Track forward from the spawn segment (Highway Bridge 1) — sign reference for spawn/camera facing on curves.
/// </summary>
public static class DutzHighwayDirection
{
    const string SpawnSegmentName = "Highway Bridge 1";

    static Vector3 cachedReferenceForward = Vector3.zero;
    static bool hasCachedReference;
    static GameObject[] cachedTrackSegments = System.Array.Empty<GameObject>();
    static bool trackSegmentCacheBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache()
    {
        hasCachedReference = false;
        InvalidateTrackSegmentCache();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneCacheInvalidation()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedInvalidateTrackCache;
        SceneManager.sceneLoaded += OnSceneLoadedInvalidateTrackCache;
    }

    static void OnSceneLoadedInvalidateTrackCache(Scene scene, LoadSceneMode mode) =>
        InvalidateTrackSegmentCache();

    public static void InvalidateReferenceCache() => hasCachedReference = false;

    public static void InvalidateTrackSegmentCache()
    {
        trackSegmentCacheBuilt = false;
        cachedTrackSegments = System.Array.Empty<GameObject>();
    }

    static void EnsureTrackSegmentCache()
    {
        if (trackSegmentCacheBuilt)
            return;

        var list = new List<GameObject>(32);
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && IsTrackSegmentName(transform.name))
                list.Add(transform.gameObject);
        }

        cachedTrackSegments = list.ToArray();
        trackSegmentCacheBuilt = true;
    }

    const float TrackStartInsetMeters = 10f;

    /// <summary>World position at the beginning of the spawn segment (Highway Bridge 1), inset onto the deck.</summary>
    public static bool TryGetTrackStartSpawnPosition(out Vector3 position, out Vector3 travelForward)
    {
        InvalidateReferenceCache();
        var first = FindSpawnSegment();
        if (first == null)
        {
            position = Vector3.zero;
            travelForward = Vector3.zero;
            return false;
        }

        travelForward = Flatten(first.transform.forward);
        if (travelForward.sqrMagnitude < 0.0001f)
            travelForward = GetSegmentTravelForward(first.transform);
        if (travelForward.sqrMagnitude > 0.0001f)
            travelForward.Normalize();
        else
            travelForward = GetReferenceForward();

        var segmentTransform = first.transform;
        position = GetTrackStartEdgeWorldPoint(first, travelForward);
        position += travelForward * TrackStartInsetMeters;

        var axis = GetSegmentTravelForward(segmentTransform);
        if (axis.sqrMagnitude < 0.0001f)
            axis = Flatten(segmentTransform.forward);
        if (Vector3.Dot(axis, travelForward) < 0f)
            axis = -axis;

        var segPos = segmentTransform.position;
        var along = Vector3.Dot(position - segPos, axis);
        position = segPos + axis * along;

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(position, position.y, null, out var deckY))
            position.y = deckY;
        else if (TryGetRendererBounds(first, out var bounds))
            position.y = bounds.max.y;
        else
            position.y = first.transform.position.y + 12f;

        return true;
    }

    static Vector3 GetTrackStartEdgeWorldPoint(GameObject segment, Vector3 travelForward)
    {
        var t = segment.transform;
        var axis = GetSegmentTravelForward(t);
        if (axis.sqrMagnitude < 0.0001f)
            axis = Flatten(t.forward);

        if (Vector3.Dot(axis, travelForward) < 0f)
            axis = -axis;

        if (!TryGetRendererBounds(segment, out var bounds))
            return t.position;

        var center = bounds.center;
        var bestAlong = float.PositiveInfinity;
        var extents = bounds.extents;

        for (var xi = -1; xi <= 1; xi += 2)
        for (var yi = -1; yi <= 1; yi += 2)
        for (var zi = -1; zi <= 1; zi += 2)
        {
            var corner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
            var along = Vector3.Dot(corner - center, axis);
            if (along < bestAlong)
                bestAlong = along;
        }

        if (bestAlong < float.PositiveInfinity)
            return center + axis * bestAlong;

        return t.position;
    }

    const string TrackSecondSegmentName = "Highway Straight 2";

    /// <summary>
    /// Authoritative down-track direction from Highway Bridge 1 toward Highway Straight 2.
    /// Bridge mesh axes often point sideways; this matches actual level progression.
    /// </summary>
    public static bool TryGetTrackProgressForward(out Vector3 forward)
    {
        var bridge = FindSpawnSegment();
        if (bridge == null)
        {
            forward = Vector3.zero;
            return false;
        }

        EnsureTrackSegmentCache();
        GameObject nextSegment = null;
        for (var i = 0; i < cachedTrackSegments.Length; i++)
        {
            var segment = cachedTrackSegments[i];
            if (segment != null && segment.name == TrackSecondSegmentName)
            {
                nextSegment = segment;
                break;
            }
        }

        if (nextSegment == null)
        {
            forward = Vector3.zero;
            return false;
        }

        forward = nextSegment.transform.position - bridge.transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.zero;
            return false;
        }

        forward.Normalize();
        return true;
    }

    /// <summary>Flat forward of the spawn segment (which way "down the track" flows from the start).</summary>
    public static Vector3 GetReferenceForward()
    {
        if (hasCachedReference && cachedReferenceForward.sqrMagnitude > 0.0001f)
            return cachedReferenceForward;

        var first = FindSpawnSegment();
        if (first != null)
        {
            var flat = Flatten(first.transform.forward);
            if (flat.sqrMagnitude > 0.0001f)
            {
                cachedReferenceForward = flat;
                hasCachedReference = true;
                return cachedReferenceForward;
            }
        }

        cachedReferenceForward = Vector3.right;
        hasCachedReference = true;
        return cachedReferenceForward;
    }

    /// <summary>Road tangent at spawn (signed by spawn segment). Off-deck spawn uses reference only.</summary>
    public static Vector3 GetSpawnForwardAt(Vector3 worldPosition)
    {
        var reference = GetReferenceForward();

        if (!DutzRoadGround.IsStandingOnRoadDeck(worldPosition, worldPosition.y))
            return reference;

        var local = GetLocalTrackForwardAt(worldPosition);
        if (local.sqrMagnitude < 0.0001f)
            return reference;

        if (Vector3.Dot(local, reference) < 0f)
            local = -local;

        return local;
    }

    static Vector3 GetLocalTrackForwardAt(Vector3 worldPosition)
    {
        var segment = FindNearestTrackSegment(worldPosition);
        if (segment == null)
            return Vector3.zero;

        return GetSegmentTravelForward(segment.transform);
    }

    /// <summary>Flat direction along the piece's long deck axis (not a short mesh normal).</summary>
    static Vector3 GetSegmentTravelForward(Transform segment)
    {
        var forward = Flatten(segment.forward);
        var right = Flatten(segment.right);

        if (!TryGetRendererBounds(segment.gameObject, out var bounds))
            return forward.sqrMagnitude > 0.0001f ? forward : right;

        var extentAlongForward = ProjectBoundsOntoAxis(bounds, segment, segment.forward);
        var extentAlongRight = ProjectBoundsOntoAxis(bounds, segment, segment.right);

        if (extentAlongRight > extentAlongForward * 1.05f)
            return right.sqrMagnitude > 0.0001f ? right : forward;

        return forward.sqrMagnitude > 0.0001f ? forward : right;
    }

    static float ProjectBoundsOntoAxis(Bounds bounds, Transform segment, Vector3 axis)
    {
        if (axis.sqrMagnitude < 0.0001f)
            return 0f;

        axis.Normalize();
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        var center = bounds.center;
        var extents = bounds.extents;

        for (var xi = -1; xi <= 1; xi += 2)
        for (var yi = -1; yi <= 1; yi += 2)
        for (var zi = -1; zi <= 1; zi += 2)
        {
            var corner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
            var proj = Vector3.Dot(corner - segment.position, axis);
            if (proj < min)
                min = proj;
            if (proj > max)
                max = proj;
        }

        return max > min ? max - min : 0f;
    }

    static GameObject FindSpawnSegment()
    {
        EnsureTrackSegmentCache();
        for (var i = 0; i < cachedTrackSegments.Length; i++)
        {
            var segment = cachedTrackSegments[i];
            if (segment != null && segment.name == SpawnSegmentName)
                return segment;
        }

        return null;
    }

    public static GameObject FindNearestTrackSegment(Vector3 worldPosition)
    {
        EnsureTrackSegmentCache();

        GameObject best = null;
        var bestDistSq = float.PositiveInfinity;

        for (var i = 0; i < cachedTrackSegments.Length; i++)
        {
            var segment = cachedTrackSegments[i];
            if (segment == null)
                continue;

            var closest = GetClosestPointOnSegment(segment, worldPosition);
            var delta = worldPosition - closest;
            delta.y = 0f;
            var distSq = delta.sqrMagnitude;
            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            best = segment;
        }

        return best;
    }

    public static Vector3 GetClosestPointOnSegment(GameObject segment, Vector3 worldPosition)
    {
        if (TryGetRendererBounds(segment, out var bounds))
            return bounds.ClosestPoint(worldPosition);

        return segment.transform.position;
    }

    static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
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

    static bool IsTrackSegmentName(string objectName) =>
        !string.IsNullOrEmpty(objectName)
        && (objectName.Contains("Highway") || objectName.Contains("Bridge"));

    static Vector3 Flatten(Vector3 vector)
    {
        vector.y = 0f;
        return vector.sqrMagnitude > 0.0001f ? vector.normalized : Vector3.zero;
    }
}
