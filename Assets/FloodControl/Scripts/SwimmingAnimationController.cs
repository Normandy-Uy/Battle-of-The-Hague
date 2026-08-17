using UnityEngine;

/// <summary>
/// Lightweight procedural swimming animation for the SimpleCitizens humanoid rig.
/// The existing Animator supplies a stable airborne base pose; this component adds
/// treading, arm strokes, leg kicks, body pitch, and velocity-responsive cadence.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(500)]
public sealed class SwimmingAnimationController : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed_f");
    static readonly int GroundedId = Animator.StringToHash("Grounded_b");
    static readonly int JumpId = Animator.StringToHash("Jump_b");

    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody body;
    [SerializeField] GameManager gameManager;

    [Header("Base Pose")]
    [SerializeField] string airborneState = "Falling";
    [SerializeField] float bodyPitch = -55f;
    [SerializeField] float bodyRoll = 4f;
    [SerializeField] float verticalPitchInfluence = 4f;

    [Header("Stroke")]
    [SerializeField] float treadFrequency = 0.8f;
    [SerializeField] float swimFrequency = 1.8f;
    [SerializeField] float movingSpeedThreshold = 0.2f;
    [SerializeField] float fullStrokeSpeed = 4f;
    [SerializeField] float shoulderSweep = 58f;
    [SerializeField] float upperArmDrive = 18f;
    [SerializeField] float elbowBend = 32f;

    [Header("Kick")]
    [SerializeField] float hipKick = 24f;
    [SerializeField] float kneeBend = 28f;
    [SerializeField] float treadKickScale = 0.4f;

    [Header("Smoothing")]
    [SerializeField] float motionBlendSpeed = 3f;

    Transform hips;
    Transform chest;
    Transform leftUpperArm;
    Transform rightUpperArm;
    Transform leftLowerArm;
    Transform rightLowerArm;
    Transform leftUpperLeg;
    Transform rightUpperLeg;
    Transform leftLowerLeg;
    Transform rightLowerLeg;

    float phase;
    float motionBlend;
    bool bonesReady;

    /// <summary>Current swim stroke phase in radians (for splash SFX sync).</summary>
    public float SwimPhase => phase;

    /// <summary>0 = treading, 1 = full stroke.</summary>
    public float MotionBlend => motionBlend;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (body == null)
            body = GetComponent<Rigidbody>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        CacheBones();
    }

    void Start()
    {
        EnterSwimmingPose();
    }

    void Update()
    {
        if (animator == null)
            return;

        // Keep the controller in its looping airborne pose. Procedural offsets are
        // applied after Animator evaluation in LateUpdate.
        animator.SetBool(GroundedId, false);
        animator.SetBool(JumpId, false);
        animator.SetFloat(SpeedId, 0f);
    }

    void LateUpdate()
    {
        if (!bonesReady)
        {
            CacheBones();
            if (!bonesReady)
                return;
        }

        Vector3 velocity = body != null ? body.velocity : Vector3.zero;
        float planarSpeed = new Vector2(velocity.x, velocity.y).magnitude;
        bool gameplayActive = gameManager == null || gameManager.IsGameplayEnabled;
        float targetBlend = gameplayActive
            ? Mathf.InverseLerp(movingSpeedThreshold, fullStrokeSpeed, planarSpeed)
            : 0f;

        motionBlend = Mathf.MoveTowards(
            motionBlend,
            targetBlend,
            motionBlendSpeed * Time.deltaTime);

        float frequency = Mathf.Lerp(treadFrequency, swimFrequency, motionBlend);
        phase = Mathf.Repeat(phase + frequency * Mathf.PI * 2f * Time.deltaTime, Mathf.PI * 2f);

        ApplySwimPose(velocity);
    }

    public void EnterSwimmingPose()
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;
        animator.SetBool(GroundedId, false);
        animator.SetBool(JumpId, false);
        animator.SetFloat(SpeedId, 0f);

        if (!string.IsNullOrEmpty(airborneState))
            animator.CrossFade(airborneState, 0.1f, 0);
    }

    void CacheBones()
    {
        if (animator == null || !animator.isHuman)
        {
            bonesReady = false;
            return;
        }

        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        bonesReady = hips != null
            && leftUpperArm != null
            && rightUpperArm != null
            && leftUpperLeg != null
            && rightUpperLeg != null;
    }

    void ApplySwimPose(Vector3 velocity)
    {
        float stroke = Mathf.Sin(phase);
        float recovery = Mathf.Cos(phase);
        float kick = Mathf.Sin(phase * 2f);
        float strokeWeight = Mathf.Lerp(0.35f, 1f, motionBlend);
        float kickWeight = Mathf.Lerp(treadKickScale, 1f, motionBlend);

        // Tilt the whole humanoid through the hips while leaving Rigidbody rotation fixed.
        float verticalPitch = Mathf.Clamp(
            velocity.y * verticalPitchInfluence,
            -12f,
            12f);
        ApplyOffset(hips, new Vector3(0f, 0f, bodyPitch + verticalPitch));

        if (chest != null)
            ApplyOffset(chest, new Vector3(0f, 0f, recovery * bodyRoll));

        // Mirrored breaststroke-like arm sweep.
        float sweep = (30f + stroke * shoulderSweep) * strokeWeight;
        float drive = recovery * upperArmDrive * strokeWeight;
        ApplyOffset(leftUpperArm, new Vector3(drive, 0f, -sweep));
        ApplyOffset(rightUpperArm, new Vector3(drive, 0f, sweep));

        float bend = (0.5f + 0.5f * recovery) * elbowBend * strokeWeight;
        ApplyOffset(leftLowerArm, new Vector3(0f, bend, -bend));
        ApplyOffset(rightLowerArm, new Vector3(0f, -bend, bend));

        // Alternating flutter kick, subdued while treading.
        float legAngle = kick * hipKick * kickWeight;
        ApplyOffset(leftUpperLeg, new Vector3(legAngle, 0f, 0f));
        ApplyOffset(rightUpperLeg, new Vector3(-legAngle, 0f, 0f));

        float leftKnee = Mathf.Max(0f, -kick) * kneeBend * kickWeight;
        float rightKnee = Mathf.Max(0f, kick) * kneeBend * kickWeight;
        ApplyOffset(leftLowerLeg, new Vector3(leftKnee, 0f, 0f));
        ApplyOffset(rightLowerLeg, new Vector3(rightKnee, 0f, 0f));
    }

    static void ApplyOffset(Transform bone, Vector3 eulerOffset)
    {
        if (bone != null)
            bone.localRotation *= Quaternion.Euler(eulerOffset);
    }

    void OnValidate()
    {
        treadFrequency = Mathf.Max(0f, treadFrequency);
        swimFrequency = Mathf.Max(treadFrequency, swimFrequency);
        movingSpeedThreshold = Mathf.Max(0f, movingSpeedThreshold);
        fullStrokeSpeed = Mathf.Max(movingSpeedThreshold + 0.01f, fullStrokeSpeed);
        shoulderSweep = Mathf.Max(0f, shoulderSweep);
        upperArmDrive = Mathf.Max(0f, upperArmDrive);
        elbowBend = Mathf.Max(0f, elbowBend);
        hipKick = Mathf.Max(0f, hipKick);
        kneeBend = Mathf.Max(0f, kneeBend);
        treadKickScale = Mathf.Clamp01(treadKickScale);
        motionBlendSpeed = Mathf.Max(0f, motionBlendSpeed);
    }
}
