using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>End goal building — roof win zone derived from hierarchy transform + mesh bounds.</summary>
[DisallowMultipleComponent]
public class DutzEndHouseCollider : MonoBehaviour
{
    public const string HouseName = "Building_House_04_color02";

    const float RoofSurfaceDepthMeters = 6f;
    const float RoofHorizontalMarginMeters = 2.5f;
    const float RoofTopMarginMeters = 2f;
    const float RoofVerticalToleranceMeters = 2.5f;

    static DutzEndHouseCollider cached;

    [SerializeField] Bounds meshLocalBounds;
    [SerializeField] float roofLocalMinY;
    [SerializeField] float roofLocalMaxY;
    [SerializeField] float xzMarginLocal;
    [SerializeField] float yToleranceLocal;
    [SerializeField] Vector3 hierarchyPosition;
    [SerializeField] Vector3 hierarchyEulerAngles;
    [SerializeField] Vector3 hierarchyScale;
    [SerializeField] Bounds worldRendererBounds;
    [SerializeField] float worldRoofMinY;
    [SerializeField] float worldRoofMaxY;

    public static bool UsesHouseRoofWin =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level01SceneName
        || SceneManager.GetActiveScene().name == DutzMobileRuntime.Level02SceneName;

    public static bool UsesFlagPoleWin => false;

    public static void EnsureFromBoot()
    {
        var house = FindHouseObject();
        if (house == null)
            return;

        if (!house.activeSelf)
            house.SetActive(true);

        var wrongObjective = house.GetComponent<DutzLevelObjective>();
        if (wrongObjective != null)
            Object.Destroy(wrongObjective);

        EnsureMeshCollider(house);

        var marker = house.GetComponent<DutzEndHouseCollider>();
        if (marker == null)
            marker = house.AddComponent<DutzEndHouseCollider>();

        marker.RefreshRoofZoneFromHierarchy();
        cached = marker;
    }

    void Awake()
    {
        cached = this;
        RefreshRoofZoneFromHierarchy();
    }

    void OnEnable()
    {
        cached = this;
        RefreshRoofZoneFromHierarchy();
    }

    void OnDisable()
    {
        if (cached == this)
            cached = null;
    }

    public void RefreshRoofZoneFromHierarchy()
    {
        var houseTransform = transform;
        hierarchyPosition = houseTransform.position;
        hierarchyEulerAngles = houseTransform.eulerAngles;
        hierarchyScale = houseTransform.lossyScale;

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            meshLocalBounds = renderer.localBounds;
            worldRendererBounds = renderer.bounds;
        }
        else
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                meshLocalBounds = meshFilter.sharedMesh.bounds;
        }

        var scaleY = Mathf.Max(hierarchyScale.y, 0.01f);
        var scaleXZ = Mathf.Max(Mathf.Max(hierarchyScale.x, hierarchyScale.z), 0.01f);

        roofLocalMinY = meshLocalBounds.max.y - RoofSurfaceDepthMeters / scaleY;
        roofLocalMaxY = meshLocalBounds.max.y + RoofTopMarginMeters / scaleY;
        xzMarginLocal = RoofHorizontalMarginMeters / scaleXZ;
        yToleranceLocal = RoofVerticalToleranceMeters / scaleY;

        worldRoofMaxY = worldRendererBounds.max.y + RoofTopMarginMeters;
        worldRoofMinY = worldRendererBounds.max.y - RoofSurfaceDepthMeters - RoofVerticalToleranceMeters;
    }

    public static bool IsPlayerOnRoof(CharacterController cc)
    {
        var house = GetCached();
        return house != null && house.ContainsPlayerOnRoof(cc);
    }

    public static bool IsRoofContact(Vector3 contactPoint, float contactNormalY)
    {
        if (contactNormalY < 0.35f)
            return false;

        var house = GetCached();
        return house != null && house.IsWorldPointOnRoof(contactPoint);
    }

    public bool ContainsPlayerOnRoof(CharacterController cc)
    {
        if (cc == null || !UsesHouseRoofWin)
            return false;

        RefreshRoofZoneFromHierarchy();
        return AnyPlayerSampleOnRoof(cc);
    }

    public bool IsWorldPointOnRoof(Vector3 worldPoint)
    {
        RefreshRoofZoneFromHierarchy();
        return IsLocalPointOnRoof(transform.InverseTransformPoint(worldPoint));
    }

    bool AnyPlayerSampleOnRoof(CharacterController cc)
    {
        RefreshRoofZoneFromHierarchy();

        if (TryGroundedHouseRoofHit(cc, out var hitPoint))
            return IsWorldPointOnRoof(hitPoint);

        var feet = GetFeetPosition(cc);
        if (IsLocalPointOnRoof(transform.InverseTransformPoint(feet)))
            return true;

        var center = cc.transform.TransformPoint(cc.center);
        if (IsLocalPointOnRoof(transform.InverseTransformPoint(center)))
            return true;

        var radius = cc.radius * 0.9f;
        if (IsLocalPointOnRoof(transform.InverseTransformPoint(feet + cc.transform.right * radius)))
            return true;
        if (IsLocalPointOnRoof(transform.InverseTransformPoint(feet - cc.transform.right * radius)))
            return true;
        if (IsLocalPointOnRoof(transform.InverseTransformPoint(feet + cc.transform.forward * radius)))
            return true;
        if (IsLocalPointOnRoof(transform.InverseTransformPoint(feet - cc.transform.forward * radius)))
            return true;

        return IsWithinWorldRoofHeightBand(feet)
            && IsWithinWorldRoofFootprint(feet);
    }

    bool TryGroundedHouseRoofHit(CharacterController cc, out Vector3 hitPoint)
    {
        hitPoint = default;
        if (cc == null || !cc.isGrounded)
            return false;

        var feet = GetFeetPosition(cc);
        var origin = feet + Vector3.up * 0.15f;
        var distance = cc.height + 1.5f;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (!IsHouseCollider(hit.collider) || hit.normal.y < 0.35f)
            return false;

        hitPoint = hit.point;
        return true;
    }

    bool IsLocalPointOnRoof(Vector3 localPoint)
    {
        if (localPoint.x < meshLocalBounds.min.x - xzMarginLocal
            || localPoint.x > meshLocalBounds.max.x + xzMarginLocal
            || localPoint.z < meshLocalBounds.min.z - xzMarginLocal
            || localPoint.z > meshLocalBounds.max.z + xzMarginLocal)
        {
            return false;
        }

        return localPoint.y >= roofLocalMinY - yToleranceLocal
            && localPoint.y <= roofLocalMaxY + yToleranceLocal;
    }

    bool IsWithinWorldRoofHeightBand(Vector3 worldPoint) =>
        worldPoint.y >= worldRoofMinY && worldPoint.y <= worldRoofMaxY;

    bool IsWithinWorldRoofFootprint(Vector3 worldPoint)
    {
        var local = transform.InverseTransformPoint(worldPoint);
        return local.x >= meshLocalBounds.min.x - xzMarginLocal
            && local.x <= meshLocalBounds.max.x + xzMarginLocal
            && local.z >= meshLocalBounds.min.z - xzMarginLocal
            && local.z <= meshLocalBounds.max.z + xzMarginLocal;
    }

    static DutzEndHouseCollider GetCached()
    {
        if (cached != null)
            return cached;

        var house = FindHouseObject();
        if (house == null)
            return null;

        cached = house.GetComponent<DutzEndHouseCollider>();
        if (cached == null)
            return null;

        cached.RefreshRoofZoneFromHierarchy();
        return cached;
    }

    public static GameObject FindInScene() => FindHouseObject();

    static GameObject FindHouseObject()
    {
        var house = GameObject.Find(HouseName);
        if (house != null)
            return house;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == HouseName)
                    return child.gameObject;
            }
        }

        return null;
    }

    static Vector3 GetFeetPosition(CharacterController cc)
    {
        var center = cc.transform.TransformPoint(cc.center);
        return center - Vector3.up * (cc.height * 0.5f - cc.radius);
    }

    public static bool TryGetWorldBounds(GameObject house, out Bounds bounds)
    {
        bounds = default;
        if (house == null)
            return false;

        var marker = house.GetComponent<DutzEndHouseCollider>();
        if (marker != null)
        {
            marker.RefreshRoofZoneFromHierarchy();
            bounds = marker.worldRendererBounds;
            return bounds.size.sqrMagnitude > 0.0001f;
        }

        var renderer = house.GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        var hasBounds = false;
        foreach (var childRenderer in house.GetComponentsInChildren<Renderer>(true))
        {
            if (childRenderer == null || !childRenderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = childRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(childRenderer.bounds);
            }
        }

        return hasBounds;
    }

    public static void EnsureMeshCollider(GameObject house)
    {
        if (house == null)
            return;

        foreach (var col in house.GetComponentsInChildren<Collider>())
        {
            if (col != null && !col.isTrigger)
                return;
        }

        var meshFilter = house.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        var meshCol = house.GetComponent<MeshCollider>();
        if (meshCol == null)
            meshCol = house.AddComponent<MeshCollider>();

        meshCol.sharedMesh = meshFilter.sharedMesh;
        meshCol.convex = false;
        meshCol.isTrigger = false;
    }

    public static bool IsHouseCollider(Collider col)
    {
        if (col == null)
            return false;

        for (var t = col.transform; t != null; t = t.parent)
        {
            if (t.name == HouseName)
                return true;
        }

        return false;
    }
}
