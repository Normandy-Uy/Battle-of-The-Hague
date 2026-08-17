using UnityEngine;

/// <summary>
/// Jonrem Police capture trigger — touching the player ends the run with the police capture dialog.
/// Tune on each Jonrem Police object: Dutz Jonrem Police Capture component in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class DutzJonremPoliceCapture : MonoBehaviour
{
    const float RetriggerCooldown = 1.5f;
    const float ShieldedCaptureReachPad = 2.5f;
    const float SolidBodyCaptureReachPad = 0.15f;
    /// <summary>Vault gate: feet within 0.3 m of police solid head (or above) skips capture.</summary>
    const float ChaseClosureVaultGraceMeters = 0.3f;
    /// <summary>Fallback pivot Δy when no solid body collider is present.</summary>
    const float ChaseClosureVaultFallbackDeltaY = 3f;

    [Header("Capture distance (Inspector)")]
    [Tooltip("Max gap between the police capture trigger and the player body before capture.")]
    [SerializeField] float captureReachMeters = 0.35f;

    [Header("Capture trigger volume (local space)")]
    [SerializeField] Vector3 captureTriggerCenter = new(0f, 0.5f, 0.32f);
    [SerializeField] Vector3 captureTriggerSize = new(0.5f, 0.5f, 0.6f);

    float lastCaptureTime = -999f;

    public float CaptureReachMeters => captureReachMeters;

    public static void EnsureOnPolice(GameObject police)
    {
        if (police == null || !DutzJonremPoliceBehavior.IsPoliceCandidate(police.name))
            return;

        var capture = police.GetComponent<DutzJonremPoliceCapture>();
        if (capture == null)
            capture = police.AddComponent<DutzJonremPoliceCapture>();

        DutzHippieBiteCollider.EnsureSmallHippieColliders(police);
        capture.ApplyCaptureTrigger();
        capture.ApplyLevel01CaptureTuning();
    }

    public void ApplyLevel01CaptureTuning()
    {
        if (captureReachMeters < DutzJonremPoliceBehavior.PoliceCaptureReachMeters * 0.5f)
            captureReachMeters = DutzJonremPoliceBehavior.PoliceCaptureReachMeters;

        if (captureTriggerSize.sqrMagnitude < 0.4f)
            captureTriggerSize = new Vector3(0.65f, 0.85f, 0.75f);

        ApplyCaptureTrigger();
    }

    void Awake() => ApplyCaptureTrigger();

    void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyCaptureTrigger();
    }

    void ApplyCaptureTrigger()
    {
        BoxCollider captureTrigger = null;
        foreach (var col in GetComponents<BoxCollider>())
        {
            if (col != null && col.isTrigger)
            {
                captureTrigger = col;
                break;
            }
        }

        if (captureTrigger == null)
        {
            captureTrigger = gameObject.AddComponent<BoxCollider>();
            captureTrigger.isTrigger = true;
        }

        captureTrigger.isTrigger = true;
        captureTrigger.center = captureTriggerCenter;
        captureTrigger.size = captureTriggerSize;
    }

    void OnTriggerStay(Collider other) => TryCaptureFromCollider(other);

    void OnTriggerEnter(Collider other) => TryCaptureFromCollider(other);

    void FixedUpdate() => TryCaptureNearbyPlayer();

    void TryCaptureFromCollider(Collider other)
    {
        if (other == null || Time.time - lastCaptureTime < RetriggerCooldown)
            return;

        if (!DutzCollectibleProgress.IsLevel01)
            return;

        var player = other.GetComponentInParent<DutzPlayerController>();
        if (player == null)
            return;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return;

        TryCapturePlayer(player, cc);
    }

    void TryCaptureNearbyPlayer()
    {
        if (Time.time - lastCaptureTime < RetriggerCooldown)
            return;

        if (!DutzCollectibleProgress.IsLevel01)
            return;

        var player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
        if (player == null)
            return;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return;

        TryCapturePlayer(player, cc);
    }

    void TryCapturePlayer(DutzPlayerController player, CharacterController cc)
    {
        if (DutzDifficulty.IsSeniorCitizenMode())
            return;

        // SuperJump / high vault: do not capture via touch OR chase-closure while clearing the body.
        // (Solid body is ~5.6 m tall — without this, mid-ascent still registers as contact.)
        if (IsVaultingPastPolice(player, cc))
            return;

        if (!IsTouchingPlayerForCapture(cc, player) && !IsWithinChaseClosureCapture(cc))
            return;

        if (!DutzPoliceCaptureDialog.TryCapture(player))
            return;

        lastCaptureTime = Time.time;
    }

    bool IsTouchingPlayerForCapture(CharacterController cc, DutzPlayerController player)
    {
        if (DutzForceField.IsPlayerShielded(player))
            return IsWithinShieldedCaptureRange(cc, player);

        var slop = Mathf.Max(0f, captureReachMeters);
        var solidSlop = slop + SolidBodyCaptureReachPad;

        foreach (var col in GetComponents<BoxCollider>())
        {
            if (col == null || !col.enabled)
                continue;

            if (col.isTrigger)
            {
                if (DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, cc, 0f, slop))
                    return true;
            }
            else if (DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, cc, 0f, solidSlop))
            {
                return true;
            }
        }

        return false;
    }

    bool IsWithinChaseClosureCapture(CharacterController cc)
    {
        var hunter = GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter == null || !hunter.HasAwakened || hunter.IsPunchStunned)
            return false;

        // Escape path 1: far enough horizontally (XZ).
        var flat = cc.transform.position - transform.position;
        flat.y = 0f;

        var playerScale = Mathf.Max(0.01f, cc.transform.lossyScale.y);
        var playerRadius = cc.radius * playerScale;
        var allowedDistance = DutzJonremPoliceBehavior.PoliceChaseStopDistanceMeters
            + captureReachMeters
            + playerRadius;

        if (flat.sqrMagnitude > allowedDistance * allowedDistance)
            return false;

        return true;
    }

    /// <summary>
    /// True when the player is clearing the officer vertically (0.3 m grace below head).
    /// SuperJump mid-air also counts once feet pass mid-body so ascent through the XZ bubble
    /// is not captured before peak.
    /// </summary>
    bool IsVaultingPastPolice(DutzPlayerController player, CharacterController cc)
    {
        var playerBottom = cc.transform.position.y;

        if (!TryGetPoliceSolidBodyTop(out var policeTop))
            return (playerBottom - transform.position.y) >= ChaseClosureVaultFallbackDeltaY;

        // Feet at/above head minus 0.3 m — full clear / peak vault.
        if (playerBottom >= policeTop - ChaseClosureVaultGraceMeters)
            return true;

        // SuperJump airborne: once above mid-body, treat as vault so tall solid + chase-closure
        // do not catch during the rise (grace still 0.3 m below mid).
        if (player != null && player.HasSuperJumpActive && !cc.isGrounded)
        {
            var policeBottom = transform.position.y;
            var mid = (policeBottom + policeTop) * 0.5f;
            if (playerBottom >= mid - ChaseClosureVaultGraceMeters)
                return true;
        }

        return false;
    }

    bool TryGetPoliceSolidBodyTop(out float policeTop)
    {
        policeTop = 0f;
        var found = false;
        foreach (var col in GetComponents<BoxCollider>())
        {
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            var top = col.bounds.max.y;
            if (!found || top > policeTop)
            {
                policeTop = top;
                found = true;
            }
        }

        return found;
    }

    bool IsWithinShieldedCaptureRange(CharacterController cc, DutzPlayerController player)
    {
        var playerCenter = cc.transform.position + cc.center;
        playerCenter.y = cc.transform.position.y + cc.height * 0.5f;

        var policeCenter = transform.position + Vector3.up * (cc.height * 0.5f);
        var delta = playerCenter - policeCenter;
        delta.y = 0f;

        var field = DutzForceField.FindForPlayer(player);
        var shieldRadius = field != null ? field.GetShieldWorldRadius() : cc.radius * 1.35f;
        var policeReach = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 1.1f;
        var allowedGap = shieldRadius + cc.radius + policeReach + ShieldedCaptureReachPad;

        return delta.sqrMagnitude <= allowedGap * allowedGap;
    }
}
