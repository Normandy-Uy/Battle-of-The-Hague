using UnityEngine;

/// <summary>
/// Level 00 crowd march only — no chase target, no hippie/giant bootstrap hooks.
/// Do not use SimpleCitizensNpcPhysics on ambient walkers; that script is shared with chasers.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class DutzLevel00CrowdWalkerPhysics : MonoBehaviour
{
    [SerializeField] float walkSpeed = DutzLevel00CrowdWalker.WalkSpeed;
    [SerializeField] float animatorWalkSpeed = DutzLevel00CrowdWalker.AnimatorWalkSpeed;
    [SerializeField] float groundCheckDistance = 0.6f;
    [SerializeField] Vector3 marchDirection = Vector3.left;

    static readonly int SpeedId = Animator.StringToHash("Speed_f");
    static readonly int GroundedId = Animator.StringToHash("Grounded_b");

    Rigidbody rb;
    Animator animator;
    Collider bodyCollider;
    Transform leftFootBone;
    Transform rightFootBone;

    void Awake() => Apply();

    public void Apply()
    {
        if (GetComponent<DutzPlayerController>() != null)
            return;

        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
        }

        var solidBox = GetSolidCollider() as BoxCollider;
        if (solidBox == null)
            solidBox = gameObject.AddComponent<BoxCollider>();

        DutzHippieBiteCollider.ApplyHumanoidSolidCollider(solidBox);
        bodyCollider = solidBox;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.mass = 50f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        CacheFootBones();
    }

    void CacheFootBones()
    {
        leftFootBone = null;
        rightFootBone = null;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Foot_Left_jnt")
                leftFootBone = t;
            else if (t.name == "Foot_Right_jnt")
                rightFootBone = t;
        }
    }

    float GetFeetY()
    {
        var best = float.PositiveInfinity;
        if (leftFootBone != null && leftFootBone.position.y < best)
            best = leftFootBone.position.y;
        if (rightFootBone != null && rightFootBone.position.y < best)
            best = rightFootBone.position.y;

        if (best < float.PositiveInfinity)
            return best;

        return DutzNpcFeet.GetLowestWorldY(gameObject);
    }

    float GetPivotToFeetOffset()
    {
        return transform.position.y - GetFeetY();
    }

    public void Configure(float speed, float animSpeed, Vector3 direction)
    {
        walkSpeed = speed;
        animatorWalkSpeed = animSpeed;
        marchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.left;
        marchDirection.y = 0f;
        Apply();
    }

    public float GetWalkSpeed() => walkSpeed;

    void FixedUpdate()
    {
        if (rb == null)
            return;

        var pos = rb.position;
        var dir = marchDirection;
        var moveDir = dir;
        pos += dir * (walkSpeed * Time.fixedDeltaTime);

        var look = Quaternion.LookRotation(moveDir, Vector3.up);
        transform.rotation = look;
        rb.rotation = look;

        var grounded = StickFeetToGround(ref pos);
        rb.MovePosition(pos);
        transform.SetPositionAndRotation(pos, rb.rotation);

        UpdateWalkAnimator(grounded, true);
    }

    bool StickFeetToGround(ref Vector3 worldPosition)
    {
        bodyCollider = GetSolidCollider();
        if (bodyCollider == null)
            return false;

        DutzRoadGround.SyncTransformsIfNeeded();
        var pivotToFeet = GetPivotToFeetOffset();
        var feetY = GetFeetY();

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(worldPosition, feetY, bodyCollider, out var deckSurfaceY))
        {
            worldPosition.y = deckSurfaceY + pivotToFeet;
            return true;
        }

        return IsGrounded();
    }

    bool IsGrounded()
    {
        var feetY = GetFeetY();
        var origin = new Vector3(transform.position.x, feetY + 0.15f, transform.position.z);
        var scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z, 1f);
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance * scale, ~0, QueryTriggerInteraction.Ignore);
    }

    void UpdateWalkAnimator(bool grounded, bool isMoving)
    {
        if (animator == null)
            return;

        animator.SetBool(GroundedId, grounded);
        animator.SetFloat(SpeedId, grounded && isMoving ? animatorWalkSpeed : 0f);
    }

    Collider GetSolidCollider()
    {
        foreach (var col in GetComponents<Collider>())
        {
            if (col != null && !col.isTrigger)
                return col;
        }

        return null;
    }
}
