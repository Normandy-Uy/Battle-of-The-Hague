using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Player1 punch — per-level damage is set in the Inspector on Dutz.prefab.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DutzPlayerController))]
[DefaultExecutionOrder(32500)]
public class DutzPlayerPunch : MonoBehaviour
{
    static readonly int PunchTriggerId = Animator.StringToHash("Punch_b");
    static readonly int PunchStateHash = Animator.StringToHash("Punch");

    const float PunchVisualDuration = 0.3f;
    const float AnimatorPunchStateSpeed = 1.35f;
    const float AnticipationEndPhase = 0.08f / PunchVisualDuration;
    const float StrikeEndPhase = 0.14f / PunchVisualDuration;

    // Match PlayerPunch.anim keyframes for procedural fallback.
    static readonly Vector3 ShoulderAnticipation = new Vector3(0f, -6f, 24f);
    static readonly Vector3 ShoulderStrike = new Vector3(8f, -12f, -6f);
    static readonly Vector3 ArmAnticipation = new Vector3(-90f, -8f, 20f);
    static readonly Vector3 ArmStrike = new Vector3(-160f, -8f, 78f);
    static readonly Vector3 ForearmAnticipation = new Vector3(-70f, 0f, 0f);
    static readonly Vector3 ForearmStrike = new Vector3(-10f, 0f, 0f);
    static readonly Vector3 HandAnticipation = new Vector3(8f, 0f, 0f);
    static readonly Vector3 HandStrike = new Vector3(18f, 0f, 0f);
    static readonly Vector3 ChestAnticipation = new Vector3(0f, 0f, -6f);
    static readonly Vector3 ChestStrike = new Vector3(6f, 0f, 10f);

    [SerializeField] KeyCode punchKey = KeyCode.F;
    [SerializeField] float punchReach = 2.2f;
    [SerializeField] float punchRadius = 1.35f;
    [SerializeField] float punchHeight = 1.75f;
    [SerializeField] float punchCooldownSeconds = 2f;
    [SerializeField] float damageDelaySeconds = 0.11f;
    [SerializeField] float level03ReachMultiplier = 2.75f;
    [SerializeField] float level03RadiusMultiplier = 2f;
    [SerializeField] float level03GiantFacingDot = 0.45f;

    [Header("Punch damage")]
    [SerializeField] int PUNCH_DAMAGE_LEVEL_1 = 5;
    [SerializeField] int PUNCH_DAMAGE_LEVEL_2 = 5;
    [SerializeField] int PUNCH_DAMAGE_LEVEL_3 = 10;
    [SerializeField] int PUNCH_DAMAGE_LEVEL_7 = 20;
    [SerializeField] int SUPERPUNCH_DAMAGE = 30;

    public int SuperPunchDamage => SUPERPUNCH_DAMAGE;

    static readonly int SpeedId = Animator.StringToHash("Speed_f");

    DutzPlayerController player;
    DutzMovementSounds movementSounds;
    Animator animator;
    Transform punchShoulder;
    Transform punchArm;
    Transform punchForearm;
    Transform punchHand;
    Transform punchChest;
    Vector3 shoulderRestEuler;
    Vector3 armRestEuler;
    Vector3 forearmRestEuler;
    Vector3 handRestEuler;
    Quaternion shoulderRestRotation;
    Quaternion armRestRotation;
    Quaternion forearmRestRotation;
    Quaternion handRestRotation;
    Quaternion chestRestRotation;
    float nextPunchTime;
    float punchVisualEnd = -1f;
    Coroutine punchRoutine;
    Coroutine punchVisualRoutine;
    float punchAnimatorSpeedRestore = 1f;
    bool punchAnimatorFrozen;
    bool punchAnimatorWasEnabled = true;
    bool slashVfxPlayed;
    bool animatorPunchActive;
    bool hasAnimatorPunchTrigger;
    bool superPunchActiveThisLife;
    float punchDurationSeconds = PunchVisualDuration;

    public bool HasSuperPunchActive => superPunchActiveThisLife;

    public int GetCurrentPunchDamage() => GetPunchDamage();

    public float CooldownRemaining => Mathf.Max(0f, nextPunchTime - Time.time);
    public bool IsOnCooldown => CooldownRemaining > 0f;
    public bool IsPunchingVisual => punchVisualEnd > 0f && Time.time <= punchVisualEnd;
    public float PunchVisualPhase
    {
        get
        {
            if (!IsPunchingVisual)
                return 0f;

            if (animatorPunchActive && animator != null && IsInAnimatorPunchState())
                return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            var elapsed = punchDurationSeconds - (punchVisualEnd - Time.time);
            return Mathf.Clamp01(elapsed / punchDurationSeconds);
        }
    }

    public string BuildRuntimeDiagnostics()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"punchArm={(punchArm != null ? punchArm.name : "NULL")}");
        sb.AppendLine($"punchForearm={(punchForearm != null ? punchForearm.name : "NULL")}");
        sb.AppendLine($"punchShoulder={(punchShoulder != null ? punchShoulder.name : "NULL")}");
        sb.AppendLine($"punchHand={(punchHand != null ? punchHand.name : "NULL")}");
        sb.AppendLine($"IsPunchingVisual={IsPunchingVisual} cooldown={CooldownRemaining:F2}s");
        sb.AppendLine($"superPunchActive={superPunchActiveThisLife} SUPERPUNCH_DAMAGE={SUPERPUNCH_DAMAGE} currentPunchDamage={GetPunchDamage()}");
        sb.AppendLine($"animatorPunchActive={animatorPunchActive} hasPunchTrigger={hasAnimatorPunchTrigger} duration={punchDurationSeconds:F2}s");

        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            sb.AppendLine($"animatorStateHash={state.shortNameHash} inPunch={state.IsName("Punch")} time={state.normalizedTime:F2}");
        }

        return sb.ToString();
    }

    public void DebugForcePunch()
    {
        nextPunchTime = 0f;
        CachePunchBonesIfNeeded();
        TryPunch();
    }

    public void EnsureBonesCachedForDiagnostics() => CachePunchBonesIfNeeded();

    public void EnableSuperPunchForLife()
    {
        superPunchActiveThisLife = true;
    }

    public void ClearSuperPunchForLife()
    {
        superPunchActiveThisLife = false;
    }

    public static void ResetSuperPunchForLife()
    {
        var player = DutzPlayerController.Instance ?? Object.FindObjectOfType<DutzPlayerController>();
        player?.GetComponent<DutzPlayerPunch>()?.ClearSuperPunchForLife();
    }

    public static void EnsureFromBoot()
    {
        var playerController = DutzPlayerController.Instance
            ?? Object.FindObjectOfType<DutzPlayerController>();
        if (playerController == null)
            return;

        if (playerController.GetComponent<DutzPlayerPunch>() == null)
            playerController.gameObject.AddComponent<DutzPlayerPunch>();

        DutzPunchFx.EnsureFromBoot();
        DutzPunchSlashVfx.EnsureFromBoot();
    }

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        movementSounds = GetComponent<DutzMovementSounds>();
        animator = GetComponent<Animator>();
        hasAnimatorPunchTrigger = HasAnimatorPunchTrigger();
        punchDurationSeconds = ResolvePunchDuration();
        DutzPunchFx.EnsureFromBoot();
        DutzPunchSlashVfx.EnsureFromBoot();
    }

    void Start()
    {
        CachePunchBones();
        StartCoroutine(DeferredBoneCache());
    }

    IEnumerator DeferredBoneCache()
    {
        yield return null;
        if (animator != null && !animator.isInitialized)
            animator.Rebind();
        hasAnimatorPunchTrigger = HasAnimatorPunchTrigger();
        punchDurationSeconds = ResolvePunchDuration();
        CachePunchBones();
    }

    void CachePunchBones()
    {
        punchShoulder = FindPunchBone("Shoulder_Right_jnt");
        punchArm = FindPunchBone("Arm_Right_jnt", "UpperArm_Right_jnt");
        punchForearm = FindPunchBone("Forearm_Right_jnt", "LowerArm_Right_jnt");
        punchHand = FindPunchBone("Hand_Right_jnt");
        punchChest = FindPunchBone("Chest_jnt", "Spine_jnt");

        if (punchArm != null && punchForearm != null)
            return;

        if (animator != null && animator.isHuman)
        {
            punchShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            punchArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            punchForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            punchHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
    }

    void CachePunchBonesIfNeeded()
    {
        if (animator != null && !animator.isInitialized)
            animator.Rebind();

        if (punchArm == null || punchForearm == null)
            CachePunchBones();
    }

    Transform FindPunchBone(params string[] names)
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

    void Update()
    {
        if (player == null || player.ControlsLocked)
            return;

        if (Time.time < nextPunchTime)
            return;

        if (!WasPunchPressedThisFrame())
            return;

        TryPunch();
    }

    bool WasPunchPressedThisFrame()
    {
        // Keyboard always works (desktop + mobile with a keyboard).
        if (Input.GetKeyDown(punchKey))
            return true;

        if (DutzRobloxMobileInput.IsMobileControlsActive)
            return DutzRobloxMobileInput.PunchPressedThisFrame;

        return Input.GetMouseButtonDown(0);
    }

    void TryPunch()
    {
        CachePunchBonesIfNeeded();
        punchDurationSeconds = ResolvePunchDuration();
        punchVisualEnd = Time.time + punchDurationSeconds;
        slashVfxPlayed = false;
        animatorPunchActive = TryBeginAnimatorPunch();

        if (!animatorPunchActive)
        {
            BeginPunchVisual();
            HoldProceduralPunchAnimator();
        }

        var fistPos = GetFistWorldPosition();
        DutzPunchFx.PlayWindup(fistPos);
        DutzPunchSlashVfx.PlayCharge(fistPos);
        movementSounds?.PlayPunchSwing();
        DutzCameraFollow.Instance?.PlayPunchShake(false);

        if (punchVisualRoutine != null)
            StopCoroutine(punchVisualRoutine);

        punchVisualRoutine = StartCoroutine(PunchVisualRoutine());

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);

        punchRoutine = StartCoroutine(PunchDamageRoutine());
        nextPunchTime = Time.time + punchCooldownSeconds;
    }

    bool HasAnimatorPunchTrigger()
    {
        if (animator == null)
            return false;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == PunchTriggerId && parameter.type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    float ResolvePunchDuration()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null || clip.name != "PlayerPunch")
                    continue;

                return clip.length / AnimatorPunchStateSpeed;
            }
        }

        return PunchVisualDuration;
    }

    bool TryBeginAnimatorPunch()
    {
        if (!hasAnimatorPunchTrigger || animator == null)
            return false;

        if (!animator.isActiveAndEnabled)
            animator.enabled = true;

        animator.SetFloat(SpeedId, 0f);
        animator.ResetTrigger(PunchTriggerId);
        animator.SetTrigger(PunchTriggerId);
        animator.Update(0f);
        return true;
    }

    bool IsInAnimatorPunchState()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return false;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        return state.shortNameHash == PunchStateHash || state.IsName("Punch");
    }

    void BeginPunchVisual()
    {
        if (punchShoulder != null)
        {
            shoulderRestEuler = punchShoulder.localEulerAngles;
            shoulderRestRotation = punchShoulder.localRotation;
        }

        if (punchArm != null)
        {
            armRestEuler = punchArm.localEulerAngles;
            armRestRotation = punchArm.localRotation;
        }

        if (punchForearm != null)
        {
            forearmRestEuler = punchForearm.localEulerAngles;
            forearmRestRotation = punchForearm.localRotation;
        }

        if (punchHand != null)
        {
            handRestEuler = punchHand.localEulerAngles;
            handRestRotation = punchHand.localRotation;
        }

        if (punchChest != null)
            chestRestRotation = punchChest.localRotation;
    }

    void HoldProceduralPunchAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat(SpeedId, 0f);

        if (!punchAnimatorFrozen)
        {
            punchAnimatorWasEnabled = animator.enabled;
            punchAnimatorSpeedRestore = animator.speed > 0.001f ? animator.speed : 1f;
            animator.speed = 1f;
            animator.enabled = false;
            punchAnimatorFrozen = true;
        }
    }

    void RestoreProceduralPunchAnimator()
    {
        if (animator == null || !punchAnimatorFrozen)
            return;

        animator.enabled = punchAnimatorWasEnabled;
        animator.speed = punchAnimatorSpeedRestore;
        punchAnimatorFrozen = false;
        animator.SetFloat(SpeedId, 0f);
        animator.Update(0f);
    }

    IEnumerator PunchVisualRoutine()
    {
        while (Time.time <= punchVisualEnd)
        {
            if (!animatorPunchActive)
                HoldProceduralPunchAnimator();

            UpdatePunchSlashVfx();
            yield return new WaitForEndOfFrame();
        }

        punchVisualRoutine = null;
        animatorPunchActive = false;
        DutzPunchSlashVfx.StopAll();
        RestoreProceduralPunchAnimator();
    }

    void LateUpdate()
    {
        if (!IsPunchingVisual)
            return;

        if (!animatorPunchActive)
            ApplyProceduralPunchVisual();

        UpdatePunchSlashVfx();
    }

    void UpdatePunchSlashVfx()
    {
        if (!IsPunchingVisual || slashVfxPlayed)
            return;

        if (PunchVisualPhase < AnticipationEndPhase)
            return;

        slashVfxPlayed = true;
        var slashPos = punchHand != null ? punchHand.position : GetFistWorldPosition();
        DutzPunchSlashVfx.PlaySlash(slashPos, transform.forward);
    }

    void ApplyProceduralPunchVisual()
    {
        if (!IsPunchingVisual)
            return;

        HoldProceduralPunchAnimator();

        var phase = PunchVisualPhase;
        if (punchShoulder != null)
        {
            var shoulderOffset = SamplePunchEuler(phase, ShoulderAnticipation, ShoulderStrike);
            punchShoulder.localRotation = shoulderRestRotation * Quaternion.Euler(shoulderOffset);
        }

        if (punchArm != null)
        {
            var armOffset = SamplePunchEuler(phase, ArmAnticipation, ArmStrike);
            punchArm.localRotation = armRestRotation * Quaternion.Euler(armOffset);
        }

        if (punchForearm != null)
        {
            var forearmOffset = SamplePunchEuler(phase, ForearmAnticipation, ForearmStrike);
            punchForearm.localRotation = forearmRestRotation * Quaternion.Euler(forearmOffset);
        }

        if (punchHand != null)
        {
            var handOffset = SamplePunchEuler(phase, HandAnticipation, HandStrike);
            punchHand.localRotation = handRestRotation * Quaternion.Euler(handOffset);
        }

        if (punchChest != null)
        {
            var chestOffset = SamplePunchEuler(phase, ChestAnticipation, ChestStrike);
            punchChest.localRotation = chestRestRotation * Quaternion.Euler(chestOffset);
        }
    }

    static Vector3 SamplePunchEuler(float phase, Vector3 anticipation, Vector3 strike)
    {
        if (phase <= AnticipationEndPhase)
        {
            var u = phase / AnticipationEndPhase;
            u = u * u * (3f - 2f * u);
            return Vector3.Lerp(Vector3.zero, anticipation, u);
        }

        if (phase <= StrikeEndPhase)
        {
            var span = StrikeEndPhase - AnticipationEndPhase;
            var u = (phase - AnticipationEndPhase) / span;
            u = 1f - (1f - u) * (1f - u);
            return Vector3.Lerp(anticipation, strike, u);
        }

        var recoverySpan = 1f - StrikeEndPhase;
        var returnU = (phase - StrikeEndPhase) / recoverySpan;
        returnU = returnU * returnU * (3f - 2f * returnU);
        return Vector3.Lerp(strike, Vector3.zero, returnU);
    }

    IEnumerator PunchDamageRoutine()
    {
        if (damageDelaySeconds > 0f)
            yield return new WaitForSeconds(damageDelaySeconds);

        ApplyPunchDamage();
        punchRoutine = null;
    }

    void ApplyPunchDamage()
    {
        GetPunchProbe(out var origin, out var center, out var radius);
        var probes = new List<(Vector3 center, float radius)>(2) { (center, radius) };

        if (DutzCollectibleProgress.IsLevel03Gameplay
            && TryGetLevel03GiantProbe(origin, center, radius, out var giantCenter, out var giantRadius))
        {
            probes.Add((giantCenter, giantRadius));
        }

        var hitNpc = false;
        var processedColliders = new HashSet<int>();
        var damagedNpcs = new HashSet<int>();

        for (var p = 0; p < probes.Count; p++)
        {
            var probe = probes[p];
            var hits = Physics.OverlapSphere(probe.center, probe.radius, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null || !processedColliders.Add(col.GetInstanceID()))
                    continue;

                if (col.transform.IsChildOf(transform) || col.gameObject == gameObject)
                    continue;

                var hitPoints = col.GetComponentInParent<DutzNpcHitPoints>();
                if (hitPoints == null || hitPoints.IsDead)
                    continue;

                if (!damagedNpcs.Add(hitPoints.GetInstanceID()))
                    continue;

                if (hitPoints.TakeDamage(GetPunchDamage(), gameObject))
                {
                    hitNpc = true;
                    ApplyPunchStun(hitPoints);
                    movementSounds?.PlayPunchHit();
                    DutzPunchFx.PlayHit(col.bounds.center);
                    DutzCameraFollow.Instance?.PlayPunchShake(true);
                }
            }
        }

        if (!hitNpc)
        {
            movementSounds?.PlayPunchMiss();
            DutzPunchFx.PlayMiss(center);
        }
    }

    bool TryGetLevel03GiantProbe(Vector3 origin, Vector3 standardCenter, float standardRadius, out Vector3 center, out float radius)
    {
        center = standardCenter;
        radius = standardRadius;

        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return false;

        forward.Normalize();
        var maxRange = standardRadius + Vector3.Distance(origin, standardCenter);
        // Level07 giants are large (scale ~3–4) — extend the assist probe so punches register.
        if (DutzCollectibleProgress.IsLevel07)
            maxRange = Mathf.Max(maxRange * 2.75f, 12f);

        SimpleCitizensGiantHippieHunter best = null;
        var bestDistance = float.MaxValue;
        var facingDot = DutzCollectibleProgress.IsLevel07
            ? Mathf.Min(level03GiantFacingDot, 0.15f)
            : level03GiantFacingDot;

        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (hunter == null || !hunter.gameObject.activeInHierarchy)
                continue;

            if (!DutzCollectibleProgress.IsPunchCombatGiant(hunter.gameObject.name))
                continue;

            var hp = hunter.GetComponent<DutzNpcHitPoints>();
            if (hp == null || hp.IsDead)
                continue;

            var toGiant = hunter.transform.position - transform.position;
            toGiant.y = 0f;
            var distance = toGiant.magnitude;
            if (distance > maxRange || distance < 0.01f)
                continue;

            if (Vector3.Dot(forward, toGiant / distance) < facingDot)
                continue;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = hunter;
        }

        if (best == null)
            return false;

        var target = best.transform.Find("Head_jnt");
        if (target == null)
            target = best.transform;

        // Prefer body hit volume — head can sit far above a foot-sized leftover collider.
        var solid = best.GetComponent<BoxCollider>();
        if (solid != null && !solid.isTrigger && solid.size.y >= 1.5f)
        {
            center = solid.bounds.center;
            radius = Mathf.Max(standardRadius, solid.bounds.extents.magnitude * 0.85f);
            return true;
        }

        center = target.position;
        radius = Mathf.Max(standardRadius, best.transform.lossyScale.x * 1.6f);
        return true;
    }

    float GetPunchScale() => Mathf.Max(1f, transform.lossyScale.y);

    void GetPunchProbe(out Vector3 origin, out Vector3 center, out float radius)
    {
        var scale = GetPunchScale();
        var reach = punchReach * scale;
        var height = punchHeight * scale;
        radius = punchRadius * scale;

        if (DutzCollectibleProgress.IsLevel03Gameplay)
        {
            reach *= level03ReachMultiplier;
            radius *= level03RadiusMultiplier;
        }

        origin = transform.position + Vector3.up * height;
        center = origin + transform.forward * reach;
    }

    Vector3 GetFistWorldPosition()
    {
        if (punchHand != null)
            return punchHand.position;

        GetPunchProbe(out var origin, out var center, out _);
        return Vector3.Lerp(origin, center, 0.72f);
    }

    int GetPunchDamage()
    {
        if (superPunchActiveThisLife)
            return Mathf.Max(1, SUPERPUNCH_DAMAGE);

        if (DutzCollectibleProgress.IsLevel07)
            return Mathf.Max(1, PUNCH_DAMAGE_LEVEL_7);

        if (DutzCollectibleProgress.IsLevel03Gameplay)
            return Mathf.Max(1, PUNCH_DAMAGE_LEVEL_3);

        if (DutzCollectibleProgress.IsLevel02)
            return Mathf.Max(1, PUNCH_DAMAGE_LEVEL_2);

        return Mathf.Max(1, PUNCH_DAMAGE_LEVEL_1);
    }

    static void ApplyPunchStun(DutzNpcHitPoints hitPoints)
    {
        if (hitPoints == null || !DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        if (!DutzCollectibleProgress.IsPunchCombatGiant(hitPoints.gameObject.name))
            return;

        var hunter = hitPoints.GetComponent<SimpleCitizensGiantHippieHunter>();
        hunter?.ApplyPunchStun(DutzNpcHitPoints.Level03GiantPunchStunSeconds);
    }

    void OnGUI()
    {
        if (player == null || player.ControlsLocked)
            return;

        if (superPunchActiveThisLife)
        {
            var superStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 0.25f, 0.2f) }
            };
            GUI.Label(
                new Rect(
                    DutzUpperLeftHudLayout.PaddingX,
                    DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.SuperPunch),
                    260f,
                    DutzUpperLeftHudLayout.TextRowHeight),
                $"SUPER PUNCH — {SUPERPUNCH_DAMAGE} DMG",
                superStyle);
        }

        if (!IsOnCooldown)
            return;

        var remaining = CooldownRemaining;
        if (remaining <= 0.05f)
            return;

        var label = $"PUNCH {remaining:0.0}s";
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.2f, 0.9f) }
        };

        var size = style.CalcSize(new GUIContent(label));
        var rect = new Rect((Screen.width - size.x) * 0.5f, Screen.height * 0.72f, size.x, size.y);
        var shadow = new GUIStyle(style) { normal = { textColor = new Color(0f, 0f, 0f, 0.75f) } };
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), label, shadow);
        GUI.Label(rect, label, style);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null)
            player = GetComponent<DutzPlayerController>();

        if (player == null)
            return;

        GetPunchProbe(out var origin, out var center, out var radius);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(center, radius);
        Gizmos.DrawLine(origin, center);
    }
#endif
}
