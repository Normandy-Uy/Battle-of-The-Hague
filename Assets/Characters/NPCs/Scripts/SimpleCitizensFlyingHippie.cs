using UnityEngine;

/// <summary>Small hippie flying horizontally through the air (Superman pose) along the highway.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[DefaultExecutionOrder(100)]
public class SimpleCitizensFlyingHippie : MonoBehaviour
{
    public const string NamePrefix = "SimpleCitizens_Hippie_Flying_";
    public const float PatrolHeightAboveRoad = 10f;
    public const float PatrolCruiseSpeed = 9f;
    public const float PatrolAnimSpeed = 1f;

    static readonly int SpeedId = Animator.StringToHash("Speed_f");
    static readonly int GroundedId = Animator.StringToHash("Grounded_b");

    [SerializeField] string flightAnimState = "Running";
    [SerializeField] float flightBobAmplitude = 0.35f;
    [SerializeField] float flightBobFrequency = 1.1f;
    [SerializeField] Vector3 shoulderLeftEuler = new Vector3(12f, 0f, -72f);
    [SerializeField] Vector3 shoulderRightEuler = new Vector3(12f, 0f, 72f);
    [SerializeField] Vector3 armLeftEuler = new Vector3(8f, 0f, 0f);
    [SerializeField] Vector3 armRightEuler = new Vector3(8f, 0f, 0f);

    SimpleCitizensNpcPhysics npcPhysics;
    Animator animator;
    Transform shoulderLeft;
    Transform shoulderRight;
    Transform armLeft;
    Transform armRight;
    Quaternion shoulderLeftBase;
    Quaternion shoulderRightBase;
    Quaternion armLeftBase;
    Quaternion armRightBase;
    float bobPhase;
    bool poseCached;

    public static bool IsFlyingHippieName(string objectName) =>
        !string.IsNullOrEmpty(objectName)
        && objectName.StartsWith(NamePrefix, System.StringComparison.Ordinal);

    void Awake()
    {
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        CacheBones();
        ApplyFlightPhysics();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        ApplyFlightPhysics();
        EnterFlightAnimation();
    }

    void Start()
    {
        ApplySupermanWorldRotation(Vector3.right);
        EnterFlightAnimation();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        UpdateFlightAnimator();
        ApplySupermanArmPose();
    }

    public void ApplyFlightPhysics()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        animator = GetComponent<Animator>();
        if (npcPhysics == null)
            return;

        npcPhysics.ConfigureForSupermanFlight(PatrolCruiseSpeed, PatrolAnimSpeed);
    }

    public void ApplyFlightBob(ref Vector3 worldPosition)
    {
        worldPosition.y += Mathf.Sin(Time.time * flightBobFrequency + bobPhase) * flightBobAmplitude;
    }

    public static float GetPatrolWorldY(Vector3 xzOnRoad)
    {
        if (DutzRoadGround.TrySampleSurfaceY(xzOnRoad, null, out var roadY))
            return roadY + PatrolHeightAboveRoad;

        return xzOnRoad.y + PatrolHeightAboveRoad;
    }

    public static Quaternion SupermanRotation(Vector3 flightDirection)
    {
        var dir = flightDirection;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.right;
        else
            dir.Normalize();

        return Quaternion.LookRotation(dir, Vector3.down);
    }

    public void ApplySupermanWorldRotation(Vector3 flightDirection)
    {
        var rot = SupermanRotation(flightDirection);
        transform.rotation = rot;
        if (npcPhysics != null && npcPhysics.TryGetRigidbody(out var rb) && rb != null)
            rb.rotation = rot;
    }

    void EnterFlightAnimation()
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;
        animator.SetBool(GroundedId, false);
        animator.SetFloat(SpeedId, PatrolAnimSpeed);
        if (!string.IsNullOrEmpty(flightAnimState))
            animator.CrossFade(flightAnimState, 0.15f, 0);
    }

    void UpdateFlightAnimator()
    {
        if (animator == null || npcPhysics == null)
            return;

        animator.SetBool(GroundedId, false);
        animator.SetFloat(SpeedId, npcPhysics.IsFlyingMoving ? PatrolAnimSpeed : PatrolAnimSpeed * 0.35f);
    }

    void CacheBones()
    {
        poseCached = false;
        foreach (var bone in GetComponentsInChildren<Transform>(true))
        {
            switch (bone.name)
            {
                case "Shoulder_Left_jnt":
                    shoulderLeft = bone;
                    shoulderLeftBase = bone.localRotation;
                    break;
                case "Shoulder_Right_jnt":
                    shoulderRight = bone;
                    shoulderRightBase = bone.localRotation;
                    break;
                case "Arm_Left_jnt":
                    armLeft = bone;
                    armLeftBase = bone.localRotation;
                    break;
                case "Arm_Right_jnt":
                    armRight = bone;
                    armRightBase = bone.localRotation;
                    break;
            }
        }

        poseCached = shoulderLeft != null && shoulderRight != null;
    }

    void ApplySupermanArmPose()
    {
        if (!poseCached)
            CacheBones();

        if (!poseCached)
            return;

        shoulderLeft.localRotation = shoulderLeftBase * Quaternion.Euler(shoulderLeftEuler);
        shoulderRight.localRotation = shoulderRightBase * Quaternion.Euler(shoulderRightEuler);
        armLeft.localRotation = armLeftBase * Quaternion.Euler(armLeftEuler);
        armRight.localRotation = armRightBase * Quaternion.Euler(armRightEuler);
    }
}
