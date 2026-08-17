using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>/// Level 00 ambient crowd walker — adds march-only physics, strips chase physics if present.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DutzLevel00CrowdWalkerPhysics))]
public class DutzLevel00CrowdWalker : MonoBehaviour
{
    public const float WalkSpeed = 1f;
    public const float WalkSpeedFast = 2f;
    public const float AnimatorWalkSpeed = 0.2f;
    public const float AnimatorWalkSpeedFast = 0.4f;
    const int CurrentWalkSettingsVersion = 5;

    static readonly System.Collections.Generic.HashSet<string> FastWalkerNames =
        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal)
        {
            "SimpleCitizens_Biker_Black",
            "SimpleCitizens_Cheerleader_White",
            "SimpleCitizens_Footballer_Black",
            "SimpleCitizens_Hip_Black",
            "SimpleCitizens_Nerd_White",
            "SimpleCitizens_Prisoner_Brown",
            "SimpleCitizens_Racer_White",
            "SimpleCitizens_Runner_Black",
            "SimpleCitizens_ShopKeeper_White",
            "SimpleCitizens_Tourist_White",
        };

    [SerializeField] int walkSettingsVersion;

    public static bool IsCrowdWalker(GameObject go) =>
        go != null && go.GetComponent<DutzLevel00CrowdWalker>() != null;

    public static bool NeedsSettingsResync(DutzLevel00CrowdWalker walker) =>
        walker != null && walker.walkSettingsVersion != CurrentWalkSettingsVersion;

    public static bool IsFastWalkerName(string objectName) =>
        !string.IsNullOrEmpty(objectName) && FastWalkerNames.Contains(objectName);

    public static float GetExpectedWalkSpeed(string objectName) =>
        IsFastWalkerName(objectName) ? WalkSpeedFast : WalkSpeed;

    public static float GetExpectedAnimatorWalkSpeed(string objectName) =>
        IsFastWalkerName(objectName) ? AnimatorWalkSpeedFast : AnimatorWalkSpeed;

    void Awake()
    {
        ApplyWalkSettings();
    }

#if UNITY_EDITOR
    void OnEnable()
    {
        if (!Application.isPlaying && GetComponents<DutzLevel00CrowdWalker>().Length == 1)
            ApplyWalkSettings();
    }
#endif

    public void ApplyWalkSettings()
    {
        if (GetComponent<DutzPlayerController>() != null)
            return;

        RemoveChasePhysicsIfPresent();

        var physics = GetComponent<DutzLevel00CrowdWalkerPhysics>();
        if (physics == null)
            physics = gameObject.AddComponent<DutzLevel00CrowdWalkerPhysics>();

        physics.Configure(
            GetExpectedWalkSpeed(name),
            GetExpectedAnimatorWalkSpeed(name),
            GetMarchDirectionFromOrientation());
        walkSettingsVersion = CurrentWalkSettingsVersion;
    }

    Vector3 GetMarchDirectionFromOrientation()
    {
        var forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    void RemoveChasePhysicsIfPresent()
    {
        var chasePhysics = GetComponent<SimpleCitizensNpcPhysics>();
        if (chasePhysics == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.DestroyObjectImmediate(chasePhysics);
            return;
        }
#endif
        Destroy(chasePhysics);
    }

}

/// <summary>
/// Level 00 only — static crowd uses collider-only (CharacterController blocks);
/// walkers stay kinematic and nudge the player on contact.
/// </summary>
public static class DutzLevel00StaticCrowdColliders
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";

    public static bool EnsureInOpenScene(bool log)
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return false;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        if (scene.name != DutzMobileRuntime.Level00SceneName && scene.path != Level00ScenePath)
            return false;

        return ApplyToAllStaticCrowd(log);
    }

    public static bool ApplyToAllStaticCrowd(bool log)
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return false;

        var changed = false;
        var count = 0;

        foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            var go = animator.gameObject;
            if (!IsStaticCrowdNpc(go))
                continue;

            if (ApplyColliderOnly(go))
            {
                changed = true;
                count++;
            }
        }

        if (log && changed)
            Debug.Log($"[Dutz] Level 00 static crowd colliders (no Rigidbody) on {count} SimpleCitizen(s).");

        return changed;
    }

    public static bool IsStaticCrowdNpc(GameObject go)
    {
        if (go == null || go.GetComponent<DutzPlayerController>() != null)
            return false;

        var name = go.name;
        if (string.IsNullOrEmpty(name) || !name.StartsWith("SimpleCitizens_", System.StringComparison.Ordinal))
            return false;

        if (go.GetComponent<DutzLevel00CrowdWalker>() != null)
            return false;

        if (go.GetComponent<SimpleCitizensNpcPhysics>() != null)
            return false;

        if (SimpleCitizensHippieBiter.IsSmallAddictName(name))
            return false;

        if (name.IndexOf("Giant", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        if (DutzGiantBossNames.IsAnyGiantBoss(name))
            return false;

        return go.GetComponent<Animator>() != null;
    }

    public static bool ApplyColliderOnly(GameObject go)
    {
        if (go == null)
            return false;

        var changed = false;

        var box = go.GetComponent<BoxCollider>();
        if (box == null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                box = Undo.AddComponent<BoxCollider>(go);
            else
#endif
                box = go.AddComponent<BoxCollider>();
            changed = true;
        }

        if (box != null && box.isTrigger)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(box, "Level 00 static crowd collider");
#endif
            box.isTrigger = false;
            changed = true;
        }

        if (box != null && !box.isTrigger && NeedsCrowdSolidColliderFix(box))
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(box, "Level 00 static crowd collider");
#endif
            var beforeCenter = box.center;
            var beforeSize = box.size;
            DutzHippieBiteCollider.ApplyHumanoidSolidCollider(box);
            if ((beforeCenter - box.center).sqrMagnitude > 0.0001f
                || (beforeSize - box.size).sqrMagnitude > 0.0001f)
                changed = true;
        }

        foreach (var rb in go.GetComponents<Rigidbody>())
        {
            if (rb == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(rb);
            else
#endif
                Object.Destroy(rb);
            changed = true;
        }

        return changed;
    }

    static bool NeedsCrowdSolidColliderFix(BoxCollider solid)
    {
        if (solid == null || solid.isTrigger)
            return true;

        if (DutzHippieBiteCollider.NeedsSolidColliderFix(solid))
            return true;

        const float tol = 0.12f;
        var expectedSize = DutzHippieBiteCollider.ClampSolidSize(DutzHippieBiteCollider.SolidSize);
        return Vector3.Distance(solid.center, DutzHippieBiteCollider.SolidCenter) > tol
            || Vector3.Distance(solid.size, expectedSize) > tol;
    }
}

/// <summary>Level 00 — depenetration when CharacterController hits marching walkers or crossroad chasers.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(95)]
public class DutzLevel00PlayerCrowdPushback : MonoBehaviour
{
    const float WalkerPushMeters = 0.1f;
    const float CitizenChasePushMeters = 0.24f * DutzLevel00CrossroadCitizenChaser.ChaserPushMultiplier;
    const float EdgePushBias = 0.7f;

    CharacterController cc;

    void Awake() => cc = GetComponent<CharacterController>();

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!DutzCollectibleProgress.IsLevel00 || cc == null || hit.collider == null)
            return;

        if (hit.collider.isTrigger)
            return;

        var root = hit.collider.transform.root;
        if (root == null)
            return;

        var isWalker = root.GetComponent<DutzLevel00CrowdWalker>() != null;
        var isChaser = DutzLevel00CrossroadCitizenChaser.IsCrossroadChasingCitizen(root.gameObject);
        if (!isWalker && !isChaser)
            return;

        var push = hit.normal;
        push.y = 0f;
        if (push.sqrMagnitude < 0.0001f)
            return;

        if (isChaser)
            push = BlendTowardNearestRoadEdge(transform.position, push);

        var amount = isChaser ? CitizenChasePushMeters : WalkerPushMeters;
        cc.Move(push.normalized * amount);
    }

    static Vector3 BlendTowardNearestRoadEdge(Vector3 playerPosition, Vector3 contactNormal)
    {
        var toEdge = GetNearestRoadEdgeDirection(playerPosition);
        if (toEdge.sqrMagnitude < 0.0001f)
            return contactNormal;

        contactNormal.y = 0f;
        if (contactNormal.sqrMagnitude < 0.0001f)
            return toEdge;

        contactNormal.Normalize();
        var blended = Vector3.Lerp(contactNormal, toEdge, EdgePushBias);
        blended.y = 0f;
        return blended.sqrMagnitude > 0.0001f ? blended.normalized : contactNormal;
    }

    static Vector3 GetNearestRoadEdgeDirection(Vector3 worldPosition)
    {
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var forward))
        {
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                var right = Vector3.Cross(Vector3.up, forward);
                if (right.sqrMagnitude > 0.0001f)
                {
                    right.Normalize();
                    var distToPositiveEdge = Mathf.Abs(
                        worldPosition.z - DutzHighwayDeckSampler.LeftLaneZ);
                    var distToNegativeEdge = Mathf.Abs(
                        worldPosition.z - DutzHighwayDeckSampler.RightLaneZ);
                    return distToPositiveEdge <= distToNegativeEdge ? right : -right;
                }
            }
        }

        var leftDist = Mathf.Abs(worldPosition.z - DutzHighwayDeckSampler.LeftLaneZ);
        var rightDist = Mathf.Abs(worldPosition.z - DutzHighwayDeckSampler.RightLaneZ);
        return leftDist <= rightDist ? Vector3.forward : Vector3.back;
    }
}
