using UnityEngine;

/// <summary>
/// Procedurally bends the crocodile toward Player1 and opens its lower jaw.
/// The rig has no usable animation clips, so this drives the existing bones directly.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class CrocodileBiteAnimation : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] float biteDuration = 1.1f;
    [SerializeField, Range(0.05f, 0.45f)] float openPortion = 0.12f;
    [SerializeField, Range(0.1f, 0.8f)] float holdPortion = 0.6f;

    [Header("Pose")]
    [SerializeField] float maximumBendAngle = 45f;
    [SerializeField] float jawOpenAngle = 70f;
    [SerializeField] float jawDropDistance = 0.18f;
    [SerializeField] float lungeDistance = 0.8f;

    Transform visualRoot;
    Transform spine2;
    Transform neck;
    Transform neck1;
    Transform head;
    Transform lowerJaw;
    Transform biteTarget;

    Quaternion spine2Bind;
    Quaternion neckBind;
    Quaternion neck1Bind;
    Quaternion headBind;
    Quaternion lowerJawBind;
    Vector3 visualBindPosition;
    Vector3 lowerJawBindPosition;

    float elapsed;
    bool initialized;
    bool biting;

    void Awake()
    {
        CacheRig();
    }

    public void TriggerBite(Transform target)
    {
        if (!initialized)
            CacheRig();
        if (!initialized)
            return;

        biteTarget = target;
        elapsed = 0f;
        biting = true;
    }

    void LateUpdate()
    {
        if (!biting || !initialized)
            return;

        elapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsed / biteDuration);
        float poseWeight = EvaluatePoseWeight(normalizedTime);

        RestoreBindPose();

        float bendAngle = CalculateBendAngle() * poseWeight;
        ApplyBend(spine2, spine2Bind, bendAngle * 0.15f);
        ApplyBend(neck, neckBind, bendAngle * 0.25f);
        ApplyBend(neck1, neck1Bind, bendAngle * 0.25f);
        ApplyBend(head, headBind, bendAngle * 0.35f);

        lowerJaw.localRotation =
            lowerJawBind * Quaternion.AngleAxis(jawOpenAngle * poseWeight, Vector3.forward);
        lowerJaw.localPosition =
            lowerJawBindPosition + Vector3.down * (jawDropDistance * poseWeight);

        ApplyLunge(poseWeight);

        if (normalizedTime >= 1f)
        {
            RestoreBindPose();
            biting = false;
            biteTarget = null;
        }
    }

    void CacheRig()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            switch (current.name)
            {
                case "CrocVisual":
                    visualRoot = current;
                    break;
                case "Bip01 Spine2":
                    spine2 = current;
                    break;
                case "Bip01 Neck":
                    neck = current;
                    break;
                case "Bip01 Neck1":
                    neck1 = current;
                    break;
                case "Bip01 Head":
                    head = current;
                    break;
                case "Bone01":
                    lowerJaw = current;
                    break;
            }
        }

        initialized = visualRoot != null
            && spine2 != null
            && neck != null
            && neck1 != null
            && head != null
            && lowerJaw != null;
        if (!initialized)
        {
            Debug.LogError(
                $"[FloodControl] Crocodile bite rig is incomplete on {name}.",
                this);
            return;
        }

        spine2Bind = spine2.localRotation;
        neckBind = neck.localRotation;
        neck1Bind = neck1.localRotation;
        headBind = head.localRotation;
        lowerJawBind = lowerJaw.localRotation;
        visualBindPosition = visualRoot.localPosition;
        lowerJawBindPosition = lowerJaw.localPosition;
    }

    float CalculateBendAngle()
    {
        if (biteTarget == null)
            return maximumBendAngle;

        Vector3 toTarget = biteTarget.position - head.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return 0f;

        // Crocodile snout runs along the head bone's local -X axis.
        float signedAngle = Vector3.SignedAngle(
            -head.right,
            toTarget.normalized,
            head.forward);
        return Mathf.Clamp(signedAngle, -maximumBendAngle, maximumBendAngle);
    }

    float EvaluatePoseWeight(float normalizedTime)
    {
        if (normalizedTime < openPortion)
            return Smooth01(normalizedTime / openPortion);

        float closeStart = Mathf.Clamp01(openPortion + holdPortion);
        if (normalizedTime < closeStart)
            return 1f;

        float closeDuration = Mathf.Max(0.001f, 1f - closeStart);
        return 1f - Smooth01((normalizedTime - closeStart) / closeDuration);
    }

    void ApplyLunge(float poseWeight)
    {
        if (biteTarget == null)
        {
            visualRoot.localPosition = visualBindPosition;
            return;
        }

        Vector3 toTarget = biteTarget.position - head.position;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            visualRoot.localPosition = visualBindPosition;
            return;
        }

        Vector3 localDirection = transform.InverseTransformDirection(toTarget.normalized);
        visualRoot.localPosition =
            visualBindPosition + localDirection * (lungeDistance * poseWeight);
    }

    static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    static void ApplyBend(
        Transform bone,
        Quaternion bindRotation,
        float angle)
    {
        bone.localRotation =
            bindRotation * Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void RestoreBindPose()
    {
        spine2.localRotation = spine2Bind;
        neck.localRotation = neckBind;
        neck1.localRotation = neck1Bind;
        head.localRotation = headBind;
        lowerJaw.localRotation = lowerJawBind;
        lowerJaw.localPosition = lowerJawBindPosition;
        visualRoot.localPosition = visualBindPosition;
    }

    void OnDisable()
    {
        if (initialized)
            RestoreBindPose();
        biting = false;
        biteTarget = null;
    }

    void OnValidate()
    {
        biteDuration = Mathf.Max(0.1f, biteDuration);
        maximumBendAngle = Mathf.Clamp(maximumBendAngle, 0f, 60f);
        jawOpenAngle = Mathf.Clamp(jawOpenAngle, 0f, 80f);
        jawDropDistance = Mathf.Clamp(jawDropDistance, 0f, 0.5f);
        lungeDistance = Mathf.Clamp(lungeDistance, 0f, 2f);
    }
}
