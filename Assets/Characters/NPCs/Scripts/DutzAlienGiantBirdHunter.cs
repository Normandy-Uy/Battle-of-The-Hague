using UnityEngine;

/// <summary>
/// Level 7 Alien Giant Bird — aerial patrol over spawn, then chase Player1 in 3D air.
/// Chase/burn values mirrored from Level 3 RAPTOR track giant.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-180)]
public class DutzAlienGiantBirdHunter : MonoBehaviour
{
    public const string BirdObjectName = "AlienGiantBirdSubmit";

    // Mirrored from Level 3 RAPTOR
    public const float RaptorWakeDistance = 200f;
    public const float RaptorChaseSpeed = 25f;
    public const float RaptorChaseStopDistance = 5f;
    public const float RaptorBurnPerSecond = 10f;

    // Bird-specific: user requested 100 HP (RAPTOR itself has 50).
    public const int BirdHitPoints = 100;

    [Header("Chase (from RAPTOR)")]
    [SerializeField] float wakeDistance = RaptorWakeDistance;
    [SerializeField] float chaseSpeed = RaptorChaseSpeed;
    [SerializeField] float chaseStopDistance = RaptorChaseStopDistance;
    [SerializeField] float chaseAnimTurnSpeed = 220f;

    [Header("Aerial Patrol")]
    [SerializeField] float patrolSpeed = 12f;
    [SerializeField] float patrolRadius = 45f;
    [SerializeField] float patrolHeightBob = 3.5f;
    [SerializeField] float patrolBobFrequency = 0.55f;

    [Header("Burn Contact")]
    [SerializeField] float contactColliderRadius = 4.5f;

    Vector3 patrolCenter;
    float patrolAngle;
    float bobPhase;
    bool hunting;
    Rigidbody rb;
    SphereCollider contactCollider;
    DutzPlayerController player;

    public static DutzAlienGiantBirdHunter EnsureConfigured(GameObject bird)
    {
        if (bird == null)
            return null;

        FixMeshPivotOffset(bird.transform);

        var rb = bird.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bird.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var hunter = bird.GetComponent<DutzAlienGiantBirdHunter>();
        if (hunter == null)
            hunter = bird.AddComponent<DutzAlienGiantBirdHunter>();

        hunter.EnsureContactCollider();

        var heat = DutzGiantHeat.EnsureOn(bird);
        heat?.Configure(RaptorBurnPerSecond);

        var hp = bird.GetComponent<DutzNpcHitPoints>();
        if (hp == null)
            hp = DutzNpcHitPoints.EnsureOn(bird, BirdHitPoints);
        else if (!hp.IsDead)
            hp.SetMaxHitPoints(BirdHitPoints);

        DutzGiantBirdSounds.EnsureOn(bird);

        hunter.enabled = true;
        return hunter;
    }

    static void FixMeshPivotOffset(Transform root)
    {
        var armature = root.Find("Armature");
        if (armature != null)
        {
            var local = armature.localPosition;
            // FBX author left the skinned body ~47m ahead of the root pivot.
            if (Mathf.Abs(local.z) > 1f)
                armature.localPosition = new Vector3(local.x, local.y, 0f);
        }

        var sphere = root.Find("Sphere");
        if (sphere != null)
        {
            var local = sphere.localPosition;
            if (local.sqrMagnitude > 0.01f)
                sphere.localPosition = Vector3.zero;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        FixMeshPivotOffset(transform);
        EnsureContactCollider();

        patrolCenter = transform.position;
        patrolAngle = Random.Range(0f, Mathf.PI * 2f);
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        var heat = GetComponent<DutzGiantHeat>();
        if (heat == null)
            heat = DutzGiantHeat.EnsureOn(gameObject);
        heat?.Configure(RaptorBurnPerSecond);

        DutzGiantBirdSounds.EnsureOn(gameObject);
    }

    void Start()
    {
        player = DutzPlayerController.Instance;
        patrolCenter = transform.position;
    }

    void EnsureContactCollider()
    {
        contactCollider = GetComponent<SphereCollider>();
        if (contactCollider == null)
            contactCollider = gameObject.AddComponent<SphereCollider>();

        contactCollider.isTrigger = true;
        contactCollider.radius = Mathf.Max(1f, contactColliderRadius);
        contactCollider.center = new Vector3(0f, 1.5f, 0f);
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (player == null)
            player = DutzPlayerController.Instance;

        var pos = rb.position;
        Vector3 moveDir;

        if (ShouldHunt())
        {
            hunting = true;
            ApplyChase(ref pos, out moveDir);
        }
        else
        {
            hunting = false;
            ApplyPatrol(ref pos, out moveDir);
        }

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            var flat = moveDir;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                flat = transform.forward;

            flat.Normalize();
            var targetRot = Quaternion.LookRotation(flat, Vector3.up);
            var nextRot = Quaternion.RotateTowards(
                rb.rotation,
                targetRot,
                chaseAnimTurnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(nextRot);
            transform.rotation = nextRot;
        }

        rb.MovePosition(pos);
        transform.position = pos;
    }

    bool ShouldHunt()
    {
        if (player == null || player.ControlsLocked)
            return false;

        var delta = player.transform.position - transform.position;
        var maxDist = hunting ? wakeDistance * 1.15f : wakeDistance;
        return delta.sqrMagnitude <= maxDist * maxDist;
    }

    void ApplyChase(ref Vector3 pos, out Vector3 moveDir)
    {
        var target = player.transform.position;
        // Keep a slight altitude advantage while pursuing in air/ground.
        target.y = Mathf.Max(target.y + 3.5f, patrolCenter.y - 8f);

        var delta = target - pos;
        var dist = delta.magnitude;
        if (dist <= Mathf.Max(0.5f, chaseStopDistance) || dist < 0.01f)
        {
            moveDir = dist > 0.01f ? delta / dist : transform.forward;
            return;
        }

        moveDir = delta / dist;
        var step = chaseSpeed * Time.fixedDeltaTime;
        if (dist - chaseStopDistance < step)
            step = Mathf.Max(0f, dist - chaseStopDistance);

        pos += moveDir * step;
    }

    void ApplyPatrol(ref Vector3 pos, out Vector3 moveDir)
    {
        var circumference = Mathf.Max(8f, patrolRadius * Mathf.PI * 2f);
        var angular = (patrolSpeed / circumference) * Mathf.PI * 2f;
        patrolAngle += angular * Time.fixedDeltaTime;

        var bob = Mathf.Sin(Time.time * patrolBobFrequency + bobPhase) * patrolHeightBob;
        var next = patrolCenter
            + new Vector3(Mathf.Cos(patrolAngle) * patrolRadius, bob, Mathf.Sin(patrolAngle) * patrolRadius);

        moveDir = next - pos;
        if (moveDir.sqrMagnitude > 0.0001f)
            moveDir.Normalize();
        else
            moveDir = transform.forward;

        pos = next;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var center = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
        Gizmos.DrawWireSphere(center, patrolRadius);
        Gizmos.color = new Color(1f, 0.35f, 0.15f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, wakeDistance);
    }
#endif
}
