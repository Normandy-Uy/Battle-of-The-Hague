using UnityEngine;

/// <summary>
/// Procedural head bob and foot spread for SimpleCitizens player — layers on Animator in LateUpdate.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DutzPlayerController))]
[DefaultExecutionOrder(32100)]
public class DutzSimpleCitizensSecondaryMotion : MonoBehaviour
{
    [SerializeField] float walkCycleSpeed = 9f;
    [SerializeField] float walkHeadBob = 0.015f;
    [SerializeField] float runHeadBob = 0.03f;
    [SerializeField] float headPitchDegrees = 3f;
    [SerializeField] float legSpreadDegrees = 5f;
    [SerializeField] float hipSpreadDegrees = 4f;
    [SerializeField] float punchFootPlantDegrees = 6f;
    [SerializeField] float blendSpeed = 12f;

    DutzPlayerController player;
    DutzPlayerPunch punch;
    CharacterController characterController;

    Transform head;
    Transform hips;
    Transform legLeft;
    Transform legRight;
    Transform footLeft;

    Vector3 headRestLocalPos;
    Quaternion headRestLocalRot;
    Quaternion hipsRestLocalRot;
    Quaternion legLeftRestLocalRot;
    Quaternion legRightRestLocalRot;
    Quaternion footLeftRestLocalRot;

    float phase;
    float moveBlend;

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        punch = GetComponent<DutzPlayerPunch>();
        characterController = GetComponent<CharacterController>();
        CacheBones();
    }

    void CacheBones()
    {
        head = FindBone("Head_jnt");
        hips = FindBone("Hips_jnt", "Hip_jnt");
        legLeft = FindBone("UpperLeg_Left_jnt", "Leg_Left_jnt");
        legRight = FindBone("UpperLeg_Right_jnt", "Leg_Right_jnt");
        footLeft = FindBone("Foot_Left_jnt");

        if (head != null)
        {
            headRestLocalPos = head.localPosition;
            headRestLocalRot = head.localRotation;
        }

        if (hips != null)
            hipsRestLocalRot = hips.localRotation;

        if (legLeft != null)
            legLeftRestLocalRot = legLeft.localRotation;

        if (legRight != null)
            legRightRestLocalRot = legRight.localRotation;

        if (footLeft != null)
            footLeftRestLocalRot = footLeft.localRotation;
    }

    Transform FindBone(params string[] names)
    {
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (child.name == names[i])
                    return child;
            }
        }

        return null;
    }

    void LateUpdate()
    {
        if (head == null && hips == null && legLeft == null)
            CacheBones();

        var targetBlend = ShouldAnimate() ? 1f : 0f;
        moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, blendSpeed * Time.deltaTime);

        if (moveBlend > 0.001f && player != null && player.IsMoving)
        {
            var cycleMul = player.IsRunning ? 1.4f : 1f;
            phase += Time.deltaTime * walkCycleSpeed * cycleMul * moveBlend;
        }

        ApplyHeadBob();
        ApplyFootSpread();
        ApplyPunchFootPlant();
    }

    bool ShouldAnimate()
    {
        if (player == null || player.ControlsLocked)
            return false;

        if (characterController != null && !characterController.isGrounded)
            return false;

        if (punch != null && punch.IsPunchingVisual)
            return false;

        return player.IsMoving;
    }

    void ApplyHeadBob()
    {
        if (head == null)
            return;

        if (moveBlend < 0.001f)
        {
            head.localPosition = headRestLocalPos;
            head.localRotation = headRestLocalRot;
            return;
        }

        var bobAmp = player != null && player.IsRunning ? runHeadBob : walkHeadBob;
        var scale = Mathf.Max(1f, transform.lossyScale.y);
        var bobY = Mathf.Sin(phase * 2f) * bobAmp * scale * moveBlend;
        var pitch = Mathf.Sin(phase * 2f) * headPitchDegrees * moveBlend;

        head.localPosition = headRestLocalPos + Vector3.up * bobY;
        head.localRotation = headRestLocalRot * Quaternion.Euler(pitch, 0f, 0f);
    }

    void ApplyFootSpread()
    {
        var spread = legSpreadDegrees * moveBlend;
        var hipSpread = hipSpreadDegrees * moveBlend;

        if (hips != null)
            hips.localRotation = hipsRestLocalRot * Quaternion.Euler(0f, hipSpread, 0f);

        if (legLeft != null)
            legLeft.localRotation = legLeftRestLocalRot * Quaternion.Euler(spread, 0f, 0f);

        if (legRight != null)
            legRight.localRotation = legRightRestLocalRot * Quaternion.Euler(-spread, 0f, 0f);
    }

    void ApplyPunchFootPlant()
    {
        if (footLeft == null || punch == null || !punch.IsPunchingVisual)
            return;

        var punchPhase = punch.PunchVisualPhase;
        if (punchPhase > 0.35f)
            return;

        var plant = Mathf.SmoothStep(0f, 1f, 1f - punchPhase / 0.35f) * punchFootPlantDegrees;
        footLeft.localRotation = footLeftRestLocalRot * Quaternion.Euler(0f, plant, 0f);
    }
}
