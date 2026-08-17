using UnityEngine;

/// <summary>
/// Procedural walk cycle for blocky avatar — legs, arms, body; optional feet when present.
/// </summary>
public class DutzWalkAnimation : MonoBehaviour
{
    [SerializeField] float walkCycleSpeed = 9f;
    [SerializeField] float legSwing = 38f;
    [SerializeField] float armSwing = 22f;
    [SerializeField] float bodyBob = 0.05f;
    [SerializeField] float sandalStride = 0.1f;
    [SerializeField] float blendSpeed = 12f;

    DutzPlayerController player;
    Transform body;
    Transform legL;
    Transform legR;
    Transform armL;
    Transform armR;
    Transform sandalL;
    Transform sandalR;

    LimbPose bodyBase;
    LimbPose legLBase;
    LimbPose legRBase;
    LimbPose armLBase;
    LimbPose armRBase;
    LimbPose sandalLBase;
    LimbPose sandalRBase;

    float phase;
    float moveBlend;

    public float WalkPhase => phase;
    public float MoveBlendAmount => moveBlend;

    struct LimbPose
    {
        public Vector3 localPosition;
        public Vector3 localEuler;
    }

    void Awake()
    {
        player = GetComponentInParent<DutzPlayerController>();
        if (player == null)
            player = FindObjectOfType<DutzPlayerController>();
        body = transform.Find("Body");
        legL = transform.Find("Leg_L");
        legR = transform.Find("Leg_R");
        armL = transform.Find("Arm_L");
        armR = transform.Find("Arm_R");
        sandalL = transform.Find("Sandal_L");
        if (sandalL == null)
            sandalL = transform.Find("Foot_L");
        sandalR = transform.Find("Sandal_R");
        if (sandalR == null)
            sandalR = transform.Find("Foot_R");

        bodyBase = Capture(body);
        legLBase = Capture(legL);
        legRBase = Capture(legR);
        armLBase = Capture(armL);
        armRBase = Capture(armR);
        sandalLBase = Capture(sandalL);
        sandalRBase = Capture(sandalR);
    }

    static LimbPose Capture(Transform t)
    {
        if (t == null)
            return default;

        return new LimbPose
        {
            localPosition = t.localPosition,
            localEuler = t.localEulerAngles
        };
    }

    void LateUpdate()
    {
        var targetBlend = player != null && player.IsMoving ? 1f : 0f;
        moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, blendSpeed * Time.deltaTime);

        if (moveBlend > 0.001f)
        {
            var cycleMul = player != null && player.IsRunning ? 1.4f : 1f;
            phase += Time.deltaTime * walkCycleSpeed * cycleMul * moveBlend;
        }

        var sin = Mathf.Sin(phase);
        var cos = Mathf.Cos(phase);
        var doubleSin = Mathf.Sin(phase * 2f);

        Apply(body, bodyBase, bodyBase.localPosition + Vector3.up * (Mathf.Abs(doubleSin) * bodyBob * moveBlend), bodyBase.localEuler);
        Apply(legL, legLBase, legLBase.localPosition, legLBase.localEuler + new Vector3(sin * legSwing * moveBlend, 0f, 0f));
        Apply(legR, legRBase, legRBase.localPosition, legRBase.localEuler + new Vector3(-sin * legSwing * moveBlend, 0f, 0f));
        Apply(armL, armLBase, armLBase.localPosition, armLBase.localEuler + new Vector3(0f, 0f, -cos * armSwing * moveBlend));
        Apply(armR, armRBase, armRBase.localPosition, armRBase.localEuler + new Vector3(0f, 0f, cos * armSwing * moveBlend));
        Apply(sandalL, sandalLBase,
            sandalLBase.localPosition + new Vector3(0f, Mathf.Abs(sin) * 0.02f * moveBlend, sin * sandalStride * moveBlend),
            sandalLBase.localEuler);
        Apply(sandalR, sandalRBase,
            sandalRBase.localPosition + new Vector3(0f, Mathf.Abs(-sin) * 0.02f * moveBlend, -sin * sandalStride * moveBlend),
            sandalRBase.localEuler);
    }

    static void Apply(Transform t, LimbPose targetPose, Vector3 pos, Vector3 euler)
    {
        if (t == null)
            return;

        t.localPosition = pos;
        t.localEulerAngles = euler;
    }
}
