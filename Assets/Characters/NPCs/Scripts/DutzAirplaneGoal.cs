using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Level 0 end goal — touching any part of the Dutz3dModel airplane wins the level.</summary>
[DisallowMultipleComponent]
public class DutzAirplaneGoal : MonoBehaviour
{
    public const string AirplaneObjectName = "Dutz3dModel";
    const string WinColliderChildName = "AirplaneWinZone";
    const float WinZonePaddingMeters = 2f;

    public const float PlayerTouchReachMeters = 2.5f;

    static DutzAirplaneGoal cached;

    public static bool UsesAirplaneWin => false;

    public static void EnsureFromBoot()
    {
        if (!UsesAirplaneWin)
            return;

        var airplane = FindAirplaneObject();
        if (airplane == null)
        {
            Debug.LogWarning("[Dutz] Dutz3dModel airplane not found in Level 0 — win goal missing.");
            return;
        }

        if (!airplane.activeSelf)
            airplane.SetActive(true);

        var marker = airplane.GetComponent<DutzAirplaneGoal>();
        if (marker == null)
            marker = airplane.AddComponent<DutzAirplaneGoal>();

        marker.EnsureTouchColliders();
        cached = marker;
    }

    void Awake()
    {
        cached = this;
        EnsureTouchColliders();
    }

    void OnEnable()
    {
        cached = this;
    }

    void OnDisable()
    {
        if (cached == this)
            cached = null;
    }

    public void EnsureTouchColliders()
    {
        RemoveLegacyMeshColliders();
        EnsureWinBoxCollider();
    }

    void RemoveLegacyMeshColliders()
    {
        var meshColliders = GetComponentsInChildren<MeshCollider>(true);
        for (var i = 0; i < meshColliders.Length; i++)
        {
            var meshCol = meshColliders[i];
            if (meshCol == null || meshCol.gameObject.name == WinColliderChildName)
                continue;

            if (Application.isPlaying)
                Destroy(meshCol);
            else
                DestroyImmediate(meshCol);
        }
    }

    void EnsureWinBoxCollider()
    {
        if (!TryGetWorldRendererBounds(out var worldBounds))
            return;

        var winZone = transform.Find(WinColliderChildName);
        if (winZone == null)
        {
            var go = new GameObject(WinColliderChildName);
            winZone = go.transform;
            winZone.SetParent(transform, false);
        }

        var box = winZone.GetComponent<BoxCollider>();
        if (box == null)
            box = winZone.gameObject.AddComponent<BoxCollider>();

        var padded = worldBounds;
        padded.Expand(WinZonePaddingMeters * 2f);

        winZone.localPosition = transform.InverseTransformPoint(padded.center);
        winZone.localRotation = Quaternion.identity;
        winZone.localScale = Vector3.one;

        var localSize = transform.InverseTransformVector(padded.size);
        box.center = Vector3.zero;
        box.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z));
        box.isTrigger = false;
    }

    bool TryGetWorldRendererBounds(out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
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

    public static bool IsAirplaneCollider(Collider col)
    {
        if (col == null)
            return false;

        for (var t = col.transform; t != null; t = t.parent)
        {
            if (t.name == WinColliderChildName)
                return true;

            if (t.name == AirplaneObjectName || t.GetComponent<DutzAirplaneGoal>() != null)
                return true;
        }

        return false;
    }

    public static bool IsPlayerTouchingAirplane(DutzPlayerController player)
    {
        if (!UsesAirplaneWin || player == null)
            return false;

        var airplane = GetCached();
        if (airplane == null)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        var winZone = airplane.transform.Find(WinColliderChildName);
        if (winZone == null)
            return false;

        var box = winZone.GetComponent<BoxCollider>();
        if (box == null || !box.enabled || box.isTrigger)
            return false;

        var playerBounds = DutzHippieBiteCollider.GetPlayerBodyBounds(cc);
        return IsWithinReach(box.bounds, playerBounds, PlayerTouchReachMeters);
    }

    static bool IsWithinReach(Bounds targetBounds, Bounds playerBounds, float reach)
    {
        var expanded = playerBounds;
        expanded.Expand(reach * 2f);
        if (!expanded.Intersects(targetBounds))
            return false;

        var closestOnTarget = targetBounds.ClosestPoint(playerBounds.center);
        var closestOnPlayer = playerBounds.ClosestPoint(closestOnTarget);
        return (closestOnTarget - closestOnPlayer).sqrMagnitude <= reach * reach;
    }

    static DutzAirplaneGoal GetCached()
    {
        if (cached != null)
            return cached;

        var airplane = FindAirplaneObject();
        if (airplane == null)
            return null;

        cached = airplane.GetComponent<DutzAirplaneGoal>();
        return cached;
    }

    public static GameObject FindAirplaneObject()
    {
        var airplane = GameObject.Find(AirplaneObjectName);
        if (airplane != null)
            return airplane;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == AirplaneObjectName)
                    return child.gameObject;
            }
        }

        return null;
    }
}
