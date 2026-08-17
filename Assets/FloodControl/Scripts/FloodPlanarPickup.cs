using UnityEngine;

/// <summary>
/// Shared helpers for Flood Control pickups in the Z-locked side-scroller plane.
/// </summary>
public static class FloodPlanarPickup
{
    public const float LockZ = 0f;
    public const float DefaultPlanarRadius = 2.75f;
    public const float DefaultXyPadding = 0.85f;
    public const float DefaultZHalfExtent = 6f;

    static FloodPlayerHealth cachedPlayerHealth;

    public static void SnapToPlayPlane(Transform root)
    {
        if (root == null)
            return;

        Vector3 position = root.position;
        if (Mathf.Approximately(position.z, LockZ))
            return;

        position.z = LockZ;
        root.position = position;
    }

    /// <summary>
    /// Moves the root to the visual's current world XY (at lock Z) and zeros the
    /// visual local offset so a scaled/offset mesh stays on the play plane.
    /// </summary>
    public static void RecenterRootOnVisual(Transform root, Transform visual)
    {
        if (root == null || visual == null || visual == root)
            return;

        Vector3 visualWorld = visual.position;
        visual.localPosition = Vector3.zero;
        root.position = new Vector3(visualWorld.x, visualWorld.y, LockZ);
    }

    public static void EnsureKinematicBody(GameObject root)
    {
        if (root == null)
            return;

        Rigidbody body = root.GetComponent<Rigidbody>();
        if (body == null)
            body = root.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public static BoxCollider EnsureDeepTrigger(
        GameObject root,
        float xyPadding = DefaultXyPadding,
        float zHalfExtent = DefaultZHalfExtent)
    {
        if (root == null)
            return null;

        SphereCollider[] spheres = root.GetComponents<SphereCollider>();
        for (int i = 0; i < spheres.Length; i++)
        {
            if (spheres[i] == null)
                continue;

            if (Application.isPlaying)
                Object.Destroy(spheres[i]);
            else
                Object.DestroyImmediate(spheres[i]);
        }

        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box == null)
            box = root.AddComponent<BoxCollider>();

        box.isTrigger = true;

        Bounds bounds;
        if (!TryGetRendererBounds(root, out bounds))
        {
            box.center = Vector3.zero;
            box.size = new Vector3(2.5f + xyPadding * 2f, 2.5f + xyPadding * 2f, zHalfExtent * 2f);
            return box;
        }

        bounds.Expand(xyPadding);
        Vector3 lossy = root.transform.lossyScale;
        float sx = Mathf.Max(0.001f, Mathf.Abs(lossy.x));
        float sy = Mathf.Max(0.001f, Mathf.Abs(lossy.y));
        float sz = Mathf.Max(0.001f, Mathf.Abs(lossy.z));

        // Keep the full local center (including Z) so a scaled child mesh that sits
        // off the root still gets a covering trigger.
        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(
            Mathf.Max(1.5f, bounds.size.x / sx),
            Mathf.Max(1.5f, bounds.size.y / sy),
            Mathf.Max((zHalfExtent * 2f) / sz, bounds.size.z / sz + 1f));
        return box;
    }

    public static bool IsPlayerInPlanarRange(
        Transform pickup,
        float planarRadius,
        out FloodPlayerHealth health)
    {
        health = null;
        if (pickup == null)
            return false;

        if (cachedPlayerHealth == null || !cachedPlayerHealth.isActiveAndEnabled)
            cachedPlayerHealth = Object.FindObjectOfType<FloodPlayerHealth>();

        health = cachedPlayerHealth;
        if (health == null || health.IsDead)
            return false;

        Vector3 center = pickup.position;
        float radius = Mathf.Max(0.5f, planarRadius);

        // Collect against the visible mesh, not just the root — large scaled visuals
        // are often offset far from the pickup transform.
        if (TryGetRendererBounds(pickup.gameObject, out Bounds bounds))
        {
            center = bounds.center;
            float halfXy = 0.5f * Mathf.Max(bounds.size.x, bounds.size.y);
            radius = Mathf.Max(radius, halfXy + DefaultXyPadding);
        }

        Vector3 delta = health.transform.position - center;
        delta.z = 0f;
        return delta.sqrMagnitude <= radius * radius;
    }

    static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }
}
