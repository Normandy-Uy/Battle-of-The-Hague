using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Roblox-style: WASD moves relative to camera; character turns toward movement.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(50)]
public class DutzPlayerController : MonoBehaviour
{
    public const string PlayerObjectName = "Player1";

    [Header("Movement")]
    [SerializeField] float moveSpeed = 15f;
    [SerializeField] float runSpeed = 30f;
    [SerializeField] KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] float jumpForce = 14f;
    public const float SuperJumpForceDefault = 28f;
    /// <summary>Horizontal launch during Super Jump — matches old EDSA Player1 run override (30 m/s) so vault gaps still clear.</summary>
    public const float SuperJumpHorizSpeed = 30f;
    const int Level07SuperJumpCharges = 4;
    float superJumpForce = SuperJumpForceDefault;
    [SerializeField] float gravity = -20f;
    [Header("Facing (rotate toward movement direction)")]
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] float airTurnSpeed = 360f;
    [SerializeField] bool allowTurnInAir = true;
    [SerializeField] float airSteerAccel = 22f;
    [Header("Spawn")]
    [SerializeField] Vector3 spawnPosition = new Vector3(250f, 8f, -2.3f);
    [Tooltip("When on, spawn facing uses Spawn Euler Angles (degrees) instead of highway auto-facing.")]
    [SerializeField] bool useSpawnRotation;
    [Tooltip("Degrees. Y = yaw (look left/right). Ignore X/Z unless you need tilt.")]
    [SerializeField] Vector3 spawnEulerAngles;
    [SerializeField] bool invertSpawnFacing = true;

    CharacterController cc;
    Vector3 velocity;
    Vector3 horizontalVelocity;
    int spawnFacingFramesRemaining;
    bool hasCompletedInitialSpawn;
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public bool ControlsLocked { get; private set; }
    public float VerticalSpeed => velocity.y;
    public bool HasSuperJumpActive =>
        superJumpActiveThisLife || superJumpCharges > 0 || superJumpChargeAirborne;

    /// <summary>True when the upper-left Super Jump label should draw.</summary>
    public bool ShowsSuperJumpHud =>
        superJumpCharges > 0 || superJumpChargeAirborne
        || (superJumpActiveThisLife && superJumpCharges <= 0 && !superJumpChargeAirborne);

    public event Action Jumped;

    bool superJumpActiveThisLife;
    int superJumpCharges;
    bool superJumpChargeAirborne;

    public static DutzPlayerController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        gameObject.name = PlayerObjectName;
        EnsureMovementSounds();
        EnsureFallRespawn();
        EnsureAddictCollisionBite();
        EnsureLevel00CrowdPushback();
        EnsureCoinCollector();
        EnsureForceFieldSuitCollector();
        EnsureSuperPunchCollector();
        EnsureSuperJumpCollector();
        EnsureParachuteCollector();
        EnsureDifficultySelect();
        EnsureRobloxMobileInput();
        EnsurePlayerPunch();
        EnsurePlayerHitPoints();
        EnsureHealthPotionCollector();
    }

    void EnsurePlayerHitPoints()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        if (GetComponent<DutzPlayerHitPoints>() == null)
            gameObject.AddComponent<DutzPlayerHitPoints>();
    }

    void EnsureHealthPotionCollector()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        if (GetComponent<DutzHealthPotionCollector>() == null)
            gameObject.AddComponent<DutzHealthPotionCollector>();
    }

    void EnsurePlayerPunch()
    {
        if (GetComponent<DutzPlayerPunch>() == null)
            gameObject.AddComponent<DutzPlayerPunch>();

        var walk = GetComponent<DutzWalkAnimation>();
        if (walk != null)
        {
            if (Application.isPlaying)
                Destroy(walk);
            else
                DestroyImmediate(walk);
        }

        DutzPunchFx.EnsureFromBoot();
        DutzPunchSlashVfx.EnsureFromBoot();

        if (GetComponent<DutzSimpleCitizensSecondaryMotion>() == null)
            gameObject.AddComponent<DutzSimpleCitizensSecondaryMotion>();
    }

    void EnsureForceFieldSuitCollector()
    {
        if (GetComponent<DutzForceFieldSuitCollector>() == null)
            gameObject.AddComponent<DutzForceFieldSuitCollector>();

        DutzForceFieldSuitPickup.EnsureOnSceneSuit();
    }

    void EnsureSuperPunchCollector()
    {
        if (GetComponent<DutzSuperPunchCollector>() == null)
            gameObject.AddComponent<DutzSuperPunchCollector>();

        DutzSuperPunchPickup.EnsureOnScenePickup();
    }

    void EnsureSuperJumpCollector()
    {
        if (GetComponent<DutzSuperJumpCollector>() == null)
            gameObject.AddComponent<DutzSuperJumpCollector>();

        DutzSuperJumpPickup.EnsureOnScenePickup();
    }

    void EnsureParachuteCollector()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        if (GetComponent<DutzParachuteCollector>() == null)
            gameObject.AddComponent<DutzParachuteCollector>();

        if (GetComponent<DutzPlayerParachute>() == null)
            gameObject.AddComponent<DutzPlayerParachute>();

        DutzParachuteGlideVfx.EnsureFromBoot();
    }

    void EnsureRobloxMobileInput()
    {
        DutzRobloxMobileInput.EnsureCreated();
    }

    void EnsureCoinCollector()
    {
        if (DutzCollectibleProgress.UsesSuitcases)
        {
            var goldCollector = GetComponent<DutzGoldCoinCollector>();
            if (goldCollector != null)
                Destroy(goldCollector);

            if (GetComponent<DutzSuitcaseCollector>() == null)
                gameObject.AddComponent<DutzSuitcaseCollector>();
            return;
        }

        var suitcaseCollector = GetComponent<DutzSuitcaseCollector>();
        if (suitcaseCollector != null)
            Destroy(suitcaseCollector);

        if (GetComponent<DutzGoldCoinCollector>() == null)
            gameObject.AddComponent<DutzGoldCoinCollector>();
    }

    void EnsureDifficultySelect()
    {
        if (GetComponent<DutzDifficultySelect>() == null)
            gameObject.AddComponent<DutzDifficultySelect>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureFallRespawn()
    {
        if (GetComponent<DutzFallRespawn>() == null)
            gameObject.AddComponent<DutzFallRespawn>();
    }

    void EnsureAddictCollisionBite()
    {
        if (DutzCollectibleProgress.IsLevel00)
        {
            var existing = GetComponent<DutzAddictCollisionBite>();
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing);
                else
                    DestroyImmediate(existing);
            }

            return;
        }

        if (GetComponent<DutzAddictCollisionBite>() == null)
            gameObject.AddComponent<DutzAddictCollisionBite>();
    }

    void EnsureLevel00CrowdPushback()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        if (GetComponent<DutzLevel00PlayerCrowdPushback>() == null)
            gameObject.AddComponent<DutzLevel00PlayerCrowdPushback>();
    }

    void EnsureMovementSounds()
    {
        if (GetComponent<DutzMovementSounds>() != null && GetComponent<AudioSource>() != null)
            return;

        if (GetComponent<AudioSource>() == null)
            gameObject.AddComponent<AudioSource>();

        if (GetComponent<DutzMovementSounds>() == null)
            gameObject.AddComponent<DutzMovementSounds>();
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            Debug.LogError("[Dutz] Missing CharacterController!");
            enabled = false;
            return;
        }

        cc.stepOffset = Mathf.Max(cc.stepOffset, 1.25f);
        cc.slopeLimit = 55f;

        BindMainCameraFollow();
        ResetToSpawn();
        StartCoroutine(DeferredSpawnPhysicsRefinement());
        Debug.Log(
            "[Dutz] Roblox-style controls (GAME tab, then Play):\n" +
            "WASD = move relative to camera | Hold RMB + mouse = look around\n" +
            "Space = jump | Shift = run");
    }

    void BindMainCameraFollow()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        var rts = cam.GetComponent<DutzRtsCamera>();
        if (rts != null)
            rts.enabled = false;

        var follow = cam.GetComponent<DutzCameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<DutzCameraFollow>();

        follow.enabled = true;
        follow.ApplyRobloxDefaults();
        follow.BindTarget(transform);
    }

    public void SetControlsLocked(bool locked)
    {
        ControlsLocked = locked;
        if (locked)
        {
            velocity = Vector3.zero;
            horizontalVelocity = Vector3.zero;
            IsMoving = false;
            IsRunning = false;
        }
    }

    public void EnableSuperJumpForLife(float force = SuperJumpForceDefault)
    {
        superJumpActiveThisLife = true;
        superJumpCharges = 0;
        superJumpChargeAirborne = false;
        superJumpForce = Mathf.Max(jumpForce, force);
    }

    /// <summary>Level07 Super Jump pickup — limited Super Jump charges (HUD shows remaining).</summary>
    public void EnableSuperJumpCharges(int charges = Level07SuperJumpCharges, float force = SuperJumpForceDefault)
    {
        superJumpActiveThisLife = false;
        superJumpCharges = Mathf.Max(superJumpCharges, Mathf.Max(1, charges));
        superJumpChargeAirborne = false;
        superJumpForce = Mathf.Max(jumpForce, force);
    }

    /// <summary>Approx apex height (m) for a launch velocity under the player's gravity: v²/(2g).</summary>
    public static float EstimateJumpHeight(float launchVelocity, float gravityMagnitude = 20f) =>
        gravityMagnitude <= 0f ? 0f : (launchVelocity * launchVelocity) / (2f * gravityMagnitude);

    public void ClearSessionPowers()
    {
        superJumpActiveThisLife = false;
        superJumpCharges = 0;
        superJumpChargeAirborne = false;
        DutzPlayerPunch.ResetSuperPunchForLife();
        GetComponent<DutzPlayerParachute>()?.ClearForRespawn();
    }

    public void ApplyHorizontalImpulse(Vector3 impulse) => horizontalVelocity += impulse;

    public void ApplyVerticalImpulse(float upwardSpeed) =>
        velocity.y = Mathf.Max(velocity.y, upwardSpeed);

    float GetEffectiveJumpForce()
    {
        if (superJumpCharges > 0 || superJumpActiveThisLife)
            return superJumpForce;
        return jumpForce;
    }

    void ConsumeSuperJumpChargeIfUsed()
    {
        if (superJumpCharges <= 0)
            return;

        superJumpCharges--;
        superJumpChargeAirborne = true;
    }

    public void Respawn()
    {
        SetControlsLocked(false);
        DutzForceField.DeactivateForPlayer(this);
        ResetToSpawn();
    }

    /// <summary>
    /// Mid-life respawn near the death pose. Does not reset world progress (pickups, NPCs, timer).
    /// </summary>
    public void RespawnNear(Vector3 worldPos, Quaternion worldRot)
    {
        SetControlsLocked(false);
        DutzForceField.DeactivateForPlayer(this);

        var fallRespawn = GetComponent<DutzFallRespawn>();
        fallRespawn?.BeginSpawnGrace();

        cc.enabled = false;
        transform.SetPositionAndRotation(worldPos, worldRot);
        SnapFeetToGround();
        velocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;

        var citizensAnim = GetComponent<DutzSimpleCitizensAnimator>();
        if (citizensAnim != null)
            citizensAnim.ResetToStanding();
        else
            ResetAnimatorStandingFallback();

        cc.enabled = true;
        Physics.SyncTransforms();
        if (!cc.isGrounded)
            cc.Move(Vector3.down * 0.35f);

        GetComponent<DutzPlayerHitPoints>()?.ResetOnRespawn();
        spawnFacingFramesRemaining = 0;
    }

    IEnumerator DeferredSpawnPhysicsRefinement()
    {
        yield return null;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        SnapFeetToGround();
        spawnPosition = transform.position;
        ApplySpawnFacing();
    }

    void ResetToSpawn()
    {
        var fallRespawn = GetComponent<DutzFallRespawn>();
        fallRespawn?.BeginSpawnGrace();

        cc.enabled = false;
        spawnPosition = ResolveSpawnPosition();
        transform.position = spawnPosition;
        SnapFeetToGround();
        spawnPosition = transform.position;
        ApplySpawnFacing();
        velocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;

        var citizensAnim = GetComponent<DutzSimpleCitizensAnimator>();
        if (citizensAnim != null)
            citizensAnim.ResetToStanding();
        else
            ResetAnimatorStandingFallback();

        cc.enabled = true;
        Physics.SyncTransforms();
        if (cc.isGrounded == false)
            cc.Move(Vector3.down * 0.35f);

        SimpleCitizensNpcRespawn.RespawnAllToSpawn();
        DutzVehicleSpawn.ResetOnPlayerRespawn();
        DutzLevel00CrowdCrossroadRespawn.ResetOnPlayerRespawn();
        if (DutzCollectibleProgress.IsLevel03Gameplay)
        {
            if (hasCompletedInitialSpawn)
                DutzLevel03Finale.ResetOnPlayerRespawn();
            else
                DutzLevel03Finale.EnsureTrackGiantsVisible();
            DutzLevel03BonusGiants.EnsureFromBoot();
        }
        DutzLevelObjective.ResetTimerOnPlayerRespawn();
        ClearSessionPowers();
        DutzForceField.DeactivateForPlayer(this);
        DutzCollectibleProgress.ResetOnPlayerRespawn();
        DutzForceFieldSuitPickup.ResetOnPlayerRespawn();
        DutzSuperPunchPickup.ResetOnPlayerRespawn();
        DutzSuperJumpPickup.ResetOnPlayerRespawn();
        GetComponent<DutzPlayerHitPoints>()?.ResetOnRespawn();
        DutzDifficulty.ApplySeniorCitizenPerks(this);

        spawnFacingFramesRemaining = 8;
        hasCompletedInitialSpawn = true;
    }

    void LateUpdate()
    {
        if (spawnFacingFramesRemaining > 0)
        {
            spawnFacingFramesRemaining--;
            ApplySpawnFacing();
        }

        if (DutzCollectibleProgress.IsLevel03Gameplay)
            DutzLevel03Finale.TryTriggerFromPlayerPosition(transform.position);

        if (cc == null || ControlsLocked)
            return;

        DutzGiantHeadTopCollider.EjectPlayerFromGiantColliders(cc);
    }

    void ResetAnimatorStandingFallback()
    {
        var anim = GetComponent<Animator>();
        if (anim == null)
            return;

        anim.SetBool(Animator.StringToHash("Death_b"), false);
        anim.SetBool(Animator.StringToHash("Jump_b"), false);
        anim.SetFloat(Animator.StringToHash("Speed_f"), 0f);
        anim.SetBool(Animator.StringToHash("Grounded_b"), true);
        anim.Play("Idle", 0, 0f);
        anim.Update(0f);
    }

    void SnapFeetToGround()
    {
        if (cc == null)
            return;

        var pos = transform.position;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(pos, pos.y, cc, out var roadY)
            || DutzRoadGround.TrySampleRoadDeckForPlacement(pos, pos.y, cc, out roadY))
        {
            DutzNpcFeet.PlacePivotOnSurface(gameObject, roadY);
            return;
        }

        if (DutzRoadGround.TrySampleWalkSurface(pos, cc, out var surfaceY))
        {
            DutzNpcFeet.PlacePivotOnSurface(gameObject, surfaceY);
            return;
        }

        Debug.LogWarning(
            $"[Dutz] No road under spawn {spawnPosition} — place Dutz on the highway deck. Using raw spawn Y.");
    }

    Vector3 ResolveSpawnPosition()
    {
        if (IsSpawnOverRoad(spawnPosition))
            return spawnPosition;

        DutzHighwayDirection.InvalidateReferenceCache();
        if (!DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackSpawn, out _))
            return spawnPosition;

        Debug.LogWarning(
            $"[Dutz] spawnPosition {spawnPosition} is not over the highway deck — using Highway Bridge 1 start {trackSpawn}.");
        spawnPosition = trackSpawn;
        return trackSpawn;
    }

    bool IsSpawnOverRoad(Vector3 pos)
    {
        if (cc == null)
            return false;

        return DutzRoadGround.TrySampleWalkableRoadDeckY(pos, pos.y, cc, out _)
            || DutzRoadGround.TrySampleWalkSurface(pos, cc, out _);
    }

    void Update()
    {
        if (cc == null || ControlsLocked)
            return;

        var parachuteGlideEarly = GetComponent<DutzPlayerParachute>();
        var suppressParachuteGround = parachuteGlideEarly != null
            && parachuteGlideEarly.ShouldSuppressGroundWhileGliding;
        var grounded = !suppressParachuteGround && cc.isGrounded && velocity.y <= 0.5f;
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;
        if (grounded)
            superJumpChargeAirborne = false;

        ReadCameraRelativeInput(out var moveDir, out var hasMoveInput);

        if (hasMoveInput)
            RotateTowardMovement(moveDir, grounded);

        IsRunning = hasMoveInput && (
            ShouldUseMobileGameplayInput()
                ? DutzRobloxMobileInput.MovementGear == MobileMovementGear.Run
                : DutzGameplayInput.GetKey(runKey));
        var speed = IsRunning ? runSpeed : moveSpeed;

        if (grounded)
        {
            horizontalVelocity = hasMoveInput ? moveDir * speed : Vector3.zero;
        }
        else if (hasMoveInput)
        {
            // Match ground walk/run in air (was flat 7 m/s, same as hippie chase — felt like they caught you moving backward).
            var airSpeed = IsRunning ? runSpeed : moveSpeed;
            var target = moveDir * airSpeed;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                target,
                airSteerAccel * Time.deltaTime);
        }
        else
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, 5f * Time.deltaTime);
        }

        IsMoving = hasMoveInput || horizontalVelocity.sqrMagnitude > 0.5f;

        var jump = ShouldUseMobileGameplayInput()
            ? DutzRobloxMobileInput.JumpPressedThisFrame
            : DutzGameplayInput.GetKeyDown(KeyCode.Space);
        if (jump && grounded)
        {
            var usingChargedSuperJump = superJumpCharges > 0;
            var parachute = GetComponent<DutzPlayerParachute>();
            var bridgeJump = parachute != null
                && parachute.TryArmBridgeJump(moveDir, speed, ref velocity.y, GetEffectiveJumpForce());

            if (!bridgeJump)
            {
                velocity.y = GetEffectiveJumpForce();
                if (hasMoveInput)
                {
                    var horizSpeed = GetEffectiveJumpForce() > jumpForce + 0.01f
                        ? Mathf.Max(speed, SuperJumpHorizSpeed)
                        : speed;
                    var jumpHoriz = moveDir * horizSpeed;
                    if (horizontalVelocity.sqrMagnitude < jumpHoriz.sqrMagnitude)
                        horizontalVelocity = jumpHoriz;
                }
            }

            if (usingChargedSuperJump)
                ConsumeSuperJumpChargeIfUsed();

            Jumped?.Invoke();
        }

        var parachuteGlide = GetComponent<DutzPlayerParachute>();
        var effectiveGravity = parachuteGlide != null
            ? parachuteGlide.GetEffectiveGravity(gravity, velocity.y, transform.position, grounded)
            : gravity;
        velocity.y += effectiveGravity * Time.deltaTime;
        if (parachuteGlide != null)
            velocity.y = parachuteGlide.ClampVerticalVelocity(velocity.y);
        var horizontalDelta = horizontalVelocity * Time.deltaTime;
        var moveFlags = cc.Move(horizontalDelta);
        if ((moveFlags & CollisionFlags.Sides) != 0 && horizontalDelta.sqrMagnitude > 0.0001f)
            TryStepOntoAheadDeck(horizontalDelta.normalized);

        cc.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    void TryStepOntoAheadDeck(Vector3 moveDir)
    {
        if (cc == null)
            return;

        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.0001f)
            return;

        moveDir.Normalize();
        var ahead = transform.position + moveDir * 0.85f;
        if (!DutzRoadGround.TrySampleSupportDeckBelowFeet(ahead, transform.position.y + 2f, cc, out var deckY))
            return;

        var stepUp = deckY - transform.position.y;
        if (stepUp <= 0.05f || stepUp > cc.stepOffset + 0.15f)
            return;

        cc.Move(Vector3.up * stepUp);
    }

    void ReadCameraRelativeInput(out Vector3 moveDir, out bool hasMoveInput)
    {
        Vector2 input;
        if (ShouldUseMobileGameplayInput())
        {
            input = DutzRobloxMobileInput.MoveAxis;
            if (input.sqrMagnitude < 0.01f)
            {
                moveDir = Vector3.zero;
                hasMoveInput = false;
                return;
            }

            moveDir = CameraRelativeDirection(input);
            hasMoveInput = true;
            return;
        }

        input = DutzGameplayInput.ReadMoveAxis();
        if (input.sqrMagnitude < 0.01f)
        {
            moveDir = Vector3.zero;
            hasMoveInput = false;
            return;
        }

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        moveDir = CameraRelativeDirection(input);
        hasMoveInput = true;
    }

    static Vector3 CameraRelativeDirection(Vector2 input)
    {
        var camFollow = DutzCameraFollow.Instance;
        Vector3 forward;
        Vector3 right;
        if (camFollow != null)
        {
            forward = camFollow.FlatForward;
            right = camFollow.FlatRight;
        }
        else if (Camera.main != null)
        {
            forward = Camera.main.transform.forward;
            right = Camera.main.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
        }
        else
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }

        return (forward * input.y + right * input.x).normalized;
    }

    void RotateTowardMovement(Vector3 moveDir, bool grounded)
    {
        if (!grounded && !allowTurnInAir)
            return;

        var targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
        var rotateSpeed = grounded ? turnSpeed : airTurnSpeed;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime);
    }

    static bool ShouldUseMobileGameplayInput() =>
        Application.isMobilePlatform && DutzRobloxMobileInput.IsMobileControlsActive;

    const float SpawnFacingTrackStartRadiusMeters = 120f;

    Vector3 GetSpawnFacingForward()
    {
        if (useSpawnRotation)
        {
            var locked = Quaternion.Euler(spawnEulerAngles) * Vector3.forward;
            locked.y = 0f;
            return locked.sqrMagnitude > 0.0001f ? locked.normalized : Vector3.zero;
        }

        var facingPosition = transform.position;
        var hasTrackStart = DutzHighwayDirection.TryGetTrackStartSpawnPosition(
            out var trackStart, out _);

        // Bridge 1 mesh forward is often perpendicular to the drive direction — use segment chain.
        if (hasTrackStart
            && IsNearTrackStart(facingPosition, trackStart)
            && DutzHighwayDirection.TryGetTrackProgressForward(out var progressForward))
            return progressForward;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(facingPosition);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        forward.Normalize();
        if (invertSpawnFacing)
            forward = -forward;

        return forward;
    }

    static bool IsNearTrackStart(Vector3 worldPosition, Vector3 trackStart)
    {
        var offset = worldPosition - trackStart;
        offset.y = 0f;
        return offset.sqrMagnitude <= SpawnFacingTrackStartRadiusMeters * SpawnFacingTrackStartRadiusMeters;
    }

    void ApplySpawnFacing()
    {
        Vector3 forward;
        if (useSpawnRotation)
        {
            transform.rotation = Quaternion.Euler(spawnEulerAngles);
            forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;
            forward.Normalize();
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        else
        {
            forward = GetSpawnFacingForward();
            if (forward.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        var camFollow = DutzCameraFollow.Instance;
        if (camFollow == null && Camera.main != null)
            camFollow = Camera.main.GetComponent<DutzCameraFollow>();

        if (camFollow != null)
        {
            camFollow.BindTarget(transform);
            camFollow.SnapRobloxSpawnFacing(forward);
        }

        Debug.Log(
            $"[Dutz] Spawn at {spawnPosition}, facing {forward} (yaw {transform.eulerAngles.y:F0}°)");
    }

    void OnGUI()
    {
        var showCharges = superJumpCharges > 0 || superJumpChargeAirborne;
        var showUnlimited = superJumpActiveThisLife && !showCharges;
        if (!showCharges && !showUnlimited)
            return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(1f, 0.82f, 0.15f) }
        };
        var label = showCharges
            ? $"SUPER JUMP  ×{superJumpCharges}"
            : "SUPER JUMP";
        GUI.Label(
            new Rect(DutzUpperLeftHudLayout.PaddingX, DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.SuperJump), 320f, DutzUpperLeftHudLayout.TextRowHeight),
            label,
            style);
    }
}

/// <summary>Difficulty affects small ground addict chase speed only (Hard = current tuning).</summary>
public enum DutzDifficultyLevel
{
    Easy,
    Medium,
    Hard,
    SeniorCitizen
}

public static class DutzDifficulty
{
    public const float HardSmallHippieChaseSpeed = 7f;
    public const float HardSmallHippieChaseAnimSpeed = 0.66f;

    /// <summary>Highway Cross Road duplicate chasers only (12) — Bridge 1 crowd unchanged.</summary>
    public const float EasyCrossroadChaseSpeed = 20f;
    public const float MediumCrossroadChaseSpeed = 25f;
    public const float HardCrossroadChaseSpeed = 30f;
    public const float SeniorCitizenCrossroadChaseSpeed = 15f;
    const float CrossroadChaseAnimReferenceSpeed = 30f;
    const float CrossroadChaseAnimAtReference = 4.8f;

    static bool chosen;
    static DutzDifficultyLevel selected = DutzDifficultyLevel.Hard;

    public static bool HasChosen => chosen;
    public static DutzDifficultyLevel Selected => selected;

    public static bool UsesDifficultySelect() => true;

    public static bool IsSeniorCitizenMode() => selected == DutzDifficultyLevel.SeniorCitizen;

    public static void ResetForNewRun()
    {
        chosen = false;
        selected = DutzDifficultyLevel.Hard;
    }

    public static void Choose(DutzDifficultyLevel level)
    {
        selected = level;
        chosen = true;
    }

    public static float GetSpeedMultiplier(DutzDifficultyLevel level) => level switch
    {
        DutzDifficultyLevel.Easy => 0.5f,
        DutzDifficultyLevel.Medium => 0.72f,
        DutzDifficultyLevel.SeniorCitizen => 1f,
        _ => 1f
    };

    public static float GetChaseSpeedForLevel(DutzDifficultyLevel level) =>
        HardSmallHippieChaseSpeed * GetSpeedMultiplier(level);

    public static float GetSmallHippieChaseSpeed() =>
        GetChaseSpeedForLevel(selected);

    public static float GetSmallHippieChaseAnimSpeed() =>
        HardSmallHippieChaseAnimSpeed * GetSpeedMultiplier(selected);

    public static float GetCrossroadChaseSpeedForLevel(DutzDifficultyLevel level) => level switch
    {
        DutzDifficultyLevel.Easy => EasyCrossroadChaseSpeed,
        DutzDifficultyLevel.Medium => MediumCrossroadChaseSpeed,
        DutzDifficultyLevel.SeniorCitizen => SeniorCitizenCrossroadChaseSpeed,
        _ => HardCrossroadChaseSpeed,
    };

    public static float GetCrossroadChaseSpeed() =>
        HasChosen ? GetCrossroadChaseSpeedForLevel(selected) : HardCrossroadChaseSpeed;

    public static float GetCrossroadChaseAnimSpeed()
    {
        var speed = GetCrossroadChaseSpeed();
        return CrossroadChaseAnimAtReference * (speed / CrossroadChaseAnimReferenceSpeed);
    }

    public static string GetDisplayName(DutzDifficultyLevel level) => level switch
    {
        DutzDifficultyLevel.Easy => "Easy",
        DutzDifficultyLevel.Medium => "Medium",
        DutzDifficultyLevel.SeniorCitizen => "Senior Citizen Mode",
        _ => "Hard"
    };

    public static string GetSpeedSummary(DutzDifficultyLevel level) =>
        GetDifficultyDetailText(level);

    public static bool UsesCrocodileEnemies()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        return sceneName == DutzMobileRuntime.Level01SceneName
            || sceneName == DutzMobileRuntime.Level02SceneName;
    }

    public static bool IsEdsaLevel() =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level00SceneName;

    public static float GetEdsaCrossroadCrowdChaseSpeed() =>
        GetCrossroadChaseSpeedForLevel(selected);

    public static string GetDifficultyDialogTitle()
    {
        if (IsEdsaLevel())
            return "CHOOSE DIFFICULTY — EDSA";

        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == DutzMobileRuntime.Level01SceneName)
            return "CHOOSE DIFFICULTY — SENATE";
        if (sceneName == DutzMobileRuntime.Level02SceneName)
            return "CHOOSE DIFFICULTY — AIRPORT";
        if (sceneName == DutzMobileRuntime.Level03SceneName)
            return "CHOOSE DIFFICULTY — HAGUE";

        return "CHOOSE DIFFICULTY";
    }

    public static string GetDifficultySubtitle()
    {
        if (IsEdsaLevel())
            return $"Crossroad chasers (Hard = {HardCrossroadChaseSpeed:0.#} m/s)";

        if (UsesCrocodileEnemies())
            return $"Rallyist + crocodile chase speed (Hard = {HardSmallHippieChaseSpeed:0.#} m/s)";

        return $"Rallyist chase speed (Hard = {HardSmallHippieChaseSpeed:0.#} m/s)";
    }

    public static string GetDifficultyDetailText(DutzDifficultyLevel level)
    {
        if (level == DutzDifficultyLevel.SeniorCitizen)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == DutzMobileRuntime.Level00SceneName)
            {
                return $"Crossroad chasers {SeniorCitizenCrossroadChaseSpeed:0.#} m/s  •  " +
                       "Unlimited force field  •  Super Jump  •  Score ×1  •  GRAVITY STILL APPLIES.";
            }

            if (sceneName == DutzMobileRuntime.Level01SceneName)
                return "Unlimited force field  •  Super Jump  •  Police cannot capture  •  Score ×1  •  GRAVITY STILL APPLIES.";
            if (sceneName == DutzMobileRuntime.Level02SceneName)
                return "Unlimited force field  •  Super Jump  •  Score ×1  •  GRAVITY STILL APPLIES.";
            return "Unlimited force field  •  Score ×1  •  GRAVITY STILL APPLIES.";
        }

        var score = GetScoreMultiplier(level);
        if (IsEdsaLevel())
        {
            var crossroadSpeed = GetCrossroadChaseSpeedForLevel(level);
            return $"Crossroad chasers {crossroadSpeed:0.#} m/s  •  Score ×{score}";
        }

        var speed = GetChaseSpeedForLevel(level);
        if (UsesCrocodileEnemies())
        {
            return $"Rallyist chase {speed:0.#} m/s  •  Crocodiles {speed:0.#} m/s  •  Score ×{score}";
        }

        return $"Rallyist chase {speed:0.#} m/s  •  Score ×{score}";
    }

    public static string GetDifficultyButtonLabel(DutzDifficultyLevel level, bool isDefault = false)
    {
        var label = GetDisplayName(level).ToUpperInvariant();
        if (isDefault)
            label += " (DEFAULT)";

        return label;
    }

    public static int GetScoreMultiplier(DutzDifficultyLevel level) => level switch
    {
        DutzDifficultyLevel.Easy => 1,
        DutzDifficultyLevel.Medium => 2,
        DutzDifficultyLevel.SeniorCitizen => 1,
        _ => 3
    };

    public static int GetScoreMultiplier() => GetScoreMultiplier(selected);

    /// <summary>Senior Citizen perks: unlimited force field; Super Jump on EDSA, Senate, and Airport.</summary>
    public static void ApplySeniorCitizenPerks(DutzPlayerController player)
    {
        if (player == null || !IsSeniorCitizenMode())
            return;

        var field = player.GetComponent<DutzForceField>();
        if (field == null)
            field = player.gameObject.AddComponent<DutzForceField>();
        field.ActivatePermanent(player);

        if (GrantsSeniorCitizenSuperJump(SceneManager.GetActiveScene().name))
            player.EnableSuperJumpForLife();
    }

    static bool GrantsSeniorCitizenSuperJump(string sceneName) =>
        sceneName == DutzMobileRuntime.Level00SceneName
        || sceneName == DutzMobileRuntime.Level01SceneName
        || sceneName == DutzMobileRuntime.Level02SceneName;
}

/// <summary>Start-of-game Easy / Medium / Hard picker (small addict chase speed only).</summary>
[RequireComponent(typeof(DutzPlayerController))]
[DefaultExecutionOrder(45)]
public class DutzDifficultySelect : MonoBehaviour
{
    DutzPlayerController player;
    bool awaitingSelection = true;
    bool bootstrapGateActive;

    public bool AwaitingSelection => awaitingSelection;

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        DutzDifficulty.ResetForNewRun();
        player?.SetControlsLocked(true);
        awaitingSelection = true;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bootstrapGateActive = DutzMobileRuntime.IsDutzLevelScene(scene);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (!awaitingSelection)
            return;

        DutzDialogCursor.EnsureUnlockedForDialog();

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (DutzLevelStartGate.IsBlockingStart)
            return;

        if (!bootstrapGateActive || (DutzGameBootstrap.IsReady && !DutzGameBootstrap.HasFailed))
        {
            if (DutzGameplayInput.GetKeyDown(KeyCode.Alpha1) || DutzGameplayInput.GetKeyDown(KeyCode.Keypad1))
                Choose(DutzDifficultyLevel.Easy);
            else if (DutzGameplayInput.GetKeyDown(KeyCode.Alpha2) || DutzGameplayInput.GetKeyDown(KeyCode.Keypad2))
                Choose(DutzDifficultyLevel.Medium);
            else if (DutzGameplayInput.GetKeyDown(KeyCode.Alpha3) || DutzGameplayInput.GetKeyDown(KeyCode.Keypad3))
                Choose(DutzDifficultyLevel.Hard);
            else if (DutzGameplayInput.GetKeyDown(KeyCode.Alpha4) || DutzGameplayInput.GetKeyDown(KeyCode.Keypad4))
                Choose(DutzDifficultyLevel.SeniorCitizen);
        }
    }

    void OnGUI()
    {
        if (!awaitingSelection)
            return;

        if (DutzLevelStartGate.IsBlockingStart)
            return;

        if (bootstrapGateActive && (!DutzGameBootstrap.IsReady || DutzGameBootstrap.HasFailed))
            return;

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        DutzDialogCursor.EnsureUnlockedForDialog();

        DutzCartoonDialogGui.ResetPanelScroll();

        var previousDepth = GUI.depth;
        GUI.depth = -3200;
        DutzCartoonDialogGui.DrawDimOverlay();

        var title = DutzDifficulty.GetDifficultyDialogTitle();
        var subtitle = DutzDifficulty.GetDifficultySubtitle();
        var footer = Application.isMobilePlatform
            ? "Tap a level to start"
            : "Pick a level to start (or press 1 / 2 / 3 / 4)";

        var buttonLabels = new[]
        {
            DutzDifficulty.GetDifficultyButtonLabel(DutzDifficultyLevel.Easy),
            DutzDifficulty.GetDifficultyButtonLabel(DutzDifficultyLevel.Medium),
            DutzDifficulty.GetDifficultyButtonLabel(DutzDifficultyLevel.Hard, isDefault: true),
            DutzDifficulty.GetDifficultyButtonLabel(DutzDifficultyLevel.SeniorCitizen)
        };

        var detailLines = new[]
        {
            DutzDifficulty.GetDifficultyDetailText(DutzDifficultyLevel.Easy),
            DutzDifficulty.GetDifficultyDetailText(DutzDifficultyLevel.Medium),
            DutzDifficulty.GetDifficultyDetailText(DutzDifficultyLevel.Hard),
            DutzDifficulty.GetDifficultyDetailText(DutzDifficultyLevel.SeniorCitizen)
        };

        var buttonBlockHeight = DutzCartoonDialogGui.MeasureDifficultyButtonBlockHeight(
            title, subtitle, buttonLabels);
        var detailsHeight = DutzCartoonDialogGui.MeasureDifficultyDetailsHeight(detailLines, footer);
        var maxDetailsVisible = Application.isMobilePlatform
            ? Screen.height * 0.24f
            : Screen.height * 0.3f;
        var visibleDetailsHeight = Mathf.Min(detailsHeight, maxDetailsVisible);
        var frameHeight = DutzCartoonDialogGui.ClampPanelHeight(
            buttonBlockHeight + visibleDetailsHeight);
        var frame = Application.isMobilePlatform
            ? DutzCartoonDialogGui.CenteredPanel(frameHeight)
            : DutzCartoonDialogGui.ChoiceDialogFrame(frameHeight);
        DutzCartoonDialogGui.DrawFrame(frame);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();
        var speedStyle = DutzCartoonDialogGui.BodyStyle();
        speedStyle.fontSize = DutzCartoonDialogGui.ScaleFont(20, 30);
        speedStyle.fontStyle = FontStyle.Normal;

        var inner = DutzCartoonDialogGui.ContentRect(frame);
        var buttonInnerHeight = buttonBlockHeight - DutzCartoonDialogGui.ContentInset * 2f;
        var buttonArea = new Rect(inner.x, inner.y, inner.width, buttonInnerHeight);
        var detailsArea = new Rect(
            inner.x,
            inner.y + buttonInnerHeight,
            inner.width,
            Mathf.Max(0f, inner.height - buttonInnerHeight));

        GUILayout.BeginArea(buttonArea);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(title, titleStyle);
        GUILayout.Space(8f);
        GUILayout.Label(subtitle, hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 18f));

        DrawDifficultyOption(DutzDifficultyLevel.Easy, DutzCartoonDialogGui.PlasticButtonColor.Blue);
        GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
        DrawDifficultyOption(DutzDifficultyLevel.Medium, DutzCartoonDialogGui.PlasticButtonColor.Red);
        GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
        DrawDifficultyOption(DutzDifficultyLevel.Hard, DutzCartoonDialogGui.PlasticButtonColor.Blue, isDefault: true);
        GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
        DrawDifficultyOption(DutzDifficultyLevel.SeniorCitizen, DutzCartoonDialogGui.PlasticButtonColor.Red);
        GUILayout.EndArea();

        if (detailsHeight > detailsArea.height + 1f)
        {
            var viewRect = new Rect(0f, 0f, detailsArea.width, detailsHeight);
            var scrollPos = GUI.BeginScrollView(detailsArea, DutzCartoonDialogGui.PanelScrollPosition, viewRect, false, true);
            DutzCartoonDialogGui.PanelScrollPosition = scrollPos;
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
            GUILayout.Label(detailLines[0], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(3f, 6f));
            GUILayout.Label(detailLines[1], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(3f, 6f));
            GUILayout.Label(detailLines[2], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(3f, 6f));
            GUILayout.Label(detailLines[3], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
            GUILayout.Label(footer, hintStyle);
            GUI.EndScrollView();
        }
        else
        {
            GUILayout.BeginArea(detailsArea);
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 8f));
            GUILayout.Label(detailLines[0], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(3f, 6f));
            GUILayout.Label(detailLines[1], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(3f, 6f));
            GUILayout.Label(detailLines[2], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(3f, 6f));
            GUILayout.Label(detailLines[3], speedStyle);
            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
            GUILayout.Label(footer, hintStyle);
            GUILayout.EndArea();
        }
        GUI.depth = previousDepth;
    }

    void DrawDifficultyOption(
        DutzDifficultyLevel level,
        DutzCartoonDialogGui.PlasticButtonColor buttonColor,
        bool isDefault = false)
    {
        var label = DutzDifficulty.GetDifficultyButtonLabel(level, isDefault);
        if (DutzCartoonDialogGui.ActionButton(
                label,
                buttonColor,
                DutzCartoonDialogGui.MeasureActionButtonHeight(label)))
            Choose(level);
    }

    void Choose(DutzDifficultyLevel level)
    {
        DutzDifficulty.Choose(level);
        SimpleCitizensHippieHunter.ApplyDifficultyToAllSmallAddicts();
        DutzVehicleSpawn.ApplyLevel00DifficultyRules();
        DutzLevel00CrowdCrossroadRespawn.RefreshAfterDifficultyChosen();
        if (level == DutzDifficultyLevel.SeniorCitizen)
            DutzDifficulty.ApplySeniorCitizenPerks(player);
        awaitingSelection = false;
        player?.SetControlsLocked(false);
        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        Debug.Log($"[Dutz] Difficulty: {DutzDifficulty.GetDisplayName(level)} (rallyist chase {DutzDifficulty.GetSmallHippieChaseSpeed():0.#} m/s).");
    }
}

/// <summary>Spawns Player1 from Dutz.prefab when a campaign scene has no player (e.g. EDSA after Flood).</summary>
public static class DutzPlayerSpawn
{
    const string ResourcesPrefabPath = "DutzPlayer";
#if UNITY_EDITOR
    const string EditorPrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";
#endif

    public static bool EnsureInScene(out string error)
    {
        error = null;
        var existing = DutzPlayerController.Instance ?? UnityEngine.Object.FindObjectOfType<DutzPlayerController>();
        if (existing != null)
            return true;

        var prefab = LoadPrefab();
        if (prefab == null)
        {
            error = "Player prefab missing — add Resources/DutzPlayer.prefab.";
            return false;
        }

        var playerGo = UnityEngine.Object.Instantiate(prefab);
        playerGo.name = DutzPlayerController.PlayerObjectName;
        PositionSpawnedPlayer(playerGo);

        if (playerGo.GetComponent<DutzPlayerController>() == null)
        {
            error = "Spawned player is missing DutzPlayerController.";
            UnityEngine.Object.Destroy(playerGo);
            return false;
        }

        Debug.Log("[Dutz] Spawned Player1 — scene had no player.");
        return true;
    }

    static GameObject LoadPrefab()
    {
        var fromResources = Resources.Load<GameObject>(ResourcesPrefabPath);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EditorPrefabPath);
#else
        return null;
#endif
    }

    static void PositionSpawnedPlayer(GameObject playerGo)
    {
        var cc = playerGo.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var spawn, out _))
        {
            playerGo.transform.position = spawn;
            if (DutzRoadGround.TrySampleWalkableRoadDeckY(spawn, spawn.y, cc, out var deckY))
                DutzNpcFeet.PlacePivotOnSurface(playerGo, deckY);
        }

        DutzHighwayDirection.InvalidateReferenceCache();
        var forward = DutzHighwayDirection.GetSpawnForwardAt(playerGo.transform.position);
        if (forward.sqrMagnitude > 0.0001f)
            playerGo.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

        if (cc != null)
            cc.enabled = true;
    }
}
