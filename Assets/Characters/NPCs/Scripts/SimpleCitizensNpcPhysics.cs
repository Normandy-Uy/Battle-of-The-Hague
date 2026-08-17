using System.Collections;
using UnityEngine;

/// <summary>
/// SimpleCitizens NPC: kinematic walk glued to road deck (same ground ray as player).
/// Shared by hippie/giant chasers — Level 00 ambient walkers use DutzLevel00CrowdWalkerPhysics instead.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class SimpleCitizensNpcPhysics : MonoBehaviour
{
    [SerializeField] float mass = 50f;
    [SerializeField] bool freezeRotation = true;
    [SerializeField] bool snapToGroundOnStart = true;
    [SerializeField] bool followGround = true;
    [SerializeField] bool chaseIn3D;
    [SerializeField] bool supermanFlight;
    [SerializeField] float groundCheckDistance = 0.6f;

    [Header("Walk forward (facing direction)")]
    [SerializeField] bool walkForward = true;
    [SerializeField] bool lockForwardToHighway;
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float animatorWalkSpeed = 0.5f;

    bool routeLockedHighwayChase;
    bool routeChaseInitialized;
    string routeSegmentName;

    static readonly int SpeedId = Animator.StringToHash("Speed_f");
    static readonly int GroundedId = Animator.StringToHash("Grounded_b");

    Rigidbody rb;
    Animator animator;
    Collider bodyCollider;
    Transform chaseTarget;
    float chaseStopDistance;
    bool walkingEnabled = true;
    SimpleCitizensFlyingHippie flyingHippie;
    Vector3 lastMoveDirection = Vector3.right;
    Vector3 highwayMarchDirection = Vector3.right;
    float jonremEscortAnchoredPivotY = float.NaN;

    public bool IsChasing => chaseTarget != null;
    public bool IsFlyingMoving { get; private set; }
    public bool TryGetRigidbody(out Rigidbody body)
    {
        body = rb;
        return rb != null;
    }

    void Reset() => Apply();

    void Awake() => Apply();

#if UNITY_EDITOR
    void OnEnable()
    {
        // Level07 Straight-2 / Highway-8 ground NPCs keep baked scene poses — edit-mode snap must not restack them.
        if (!Application.isPlaying && followGround && !ShouldPreserveCrowdWalkerPose(gameObject)
            && !IsLevel07SpecialHighwayGroundNpc())
            SnapFeetToRoad();
    }
#endif

    void Start()
    {
        // Level07 Straight-2 / Highway-8 ground NPCs: baked spawn is already on their authored deck.
        // Generic SnapFeetToRoad uses world-up deck rays and can yank them off before FixedUpdate clamp.
        if (IsLevel07SpecialHighwayGroundNpc())
        {
            SnapFeetToRoad();
            return;
        }

        if (followGround && snapToGroundOnStart && !ShouldPreserveJonremEscortScenePose(gameObject)
            && !ShouldPreserveCrowdWalkerPose(gameObject))
            SnapFeetToRoad();

        if (followGround && !ShouldPreserveJonremEscortScenePose(gameObject) && !ShouldPreserveCrowdWalkerPose(gameObject))
            StartCoroutine(DeferredGroundSnap());
    }

    IEnumerator DeferredGroundSnap()
    {
        yield return null;
        if (followGround && !ShouldPreserveJonremEscortScenePose(gameObject) && !ShouldPreserveCrowdWalkerPose(gameObject)
            && !IsLevel07SpecialHighwayGroundNpc())
            SnapFeetToRoad();
        yield return new WaitForFixedUpdate();
        if (followGround && !ShouldPreserveJonremEscortScenePose(gameObject) && !ShouldPreserveCrowdWalkerPose(gameObject)
            && !IsLevel07SpecialHighwayGroundNpc())
            SnapFeetToRoad();
    }

    static bool ShouldPreserveCrowdWalkerPose(GameObject npc) =>
        npc != null && IsLevel00CrowdWalker(npc);

    static bool ShouldPreserveJonremEscortScenePose(GameObject npc) =>
        Application.isPlaying
        && DutzCollectibleProgress.IsLevel01
        && npc != null
        && DutzGiantBossNames.IsJonremEscort(npc.name);

    public void Apply()
    {
        if (GetComponent<DutzPlayerController>() != null)
            return;

        animator = GetComponent<Animator>();
        flyingHippie = GetComponent<SimpleCitizensFlyingHippie>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
        }

        bodyCollider = GetSolidCollider();
        if (bodyCollider == null)
            bodyCollider = gameObject.AddComponent<BoxCollider>();

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = freezeRotation
            ? RigidbodyConstraints.FreezeRotation
            : RigidbodyConstraints.None;
    }

    public void SetWalkingEnabled(bool enabled)
    {
        walkingEnabled = enabled;
        if (animator != null)
            animator.SetFloat(SpeedId, 0f);
    }

    public void SetChaseTarget(Transform target) => chaseTarget = target;

    public void ClearChaseTarget()
    {
        chaseTarget = null;
        routeChaseInitialized = false;
    }

    public void SetChaseStopDistance(float meters) => chaseStopDistance = Mathf.Max(0f, meters);

    public float GetWalkSpeed() => walkSpeed;

    public float GetAnimatorWalkSpeed() => animatorWalkSpeed;

    public void ConfigureForChase(float speed, float animSpeed, float stopDistance = 0f)
    {
        routeLockedHighwayChase = false;
        routeChaseInitialized = false;
        walkForward = false;
        chaseIn3D = false;
        followGround = true;
        snapToGroundOnStart = true;
        lockForwardToHighway = false;
        walkSpeed = speed;
        animatorWalkSpeed = animSpeed;
        chaseStopDistance = Mathf.Max(0f, stopDistance);
        ClearFlightMode();
    }

    /// <summary>Force ground-only pursuit — never fly or follow airborne Y.</summary>
    public void ConfigureForGroundChase(float speed, float animSpeed, float stopDistance = 0f)
    {
        ConfigureForChase(speed, animSpeed, stopDistance);
        ClearFlightMode();
        followGround = true;
        chaseIn3D = false;
    }

    void ClearFlightMode()
    {
        chaseIn3D = false;
        supermanFlight = false;
        IsFlyingMoving = false;
    }

    /// <summary>Level 3 giants — flat XZ chase toward player; Y from scene + segment-cross snap only (no per-tick deck raycasts).</summary>
    public void ConfigureForHighwayRouteChase(
        float speed,
        float animSpeed,
        float stopDistance,
        Vector3 marchDirection)
    {
        routeLockedHighwayChase = true;
        walkForward = false;
        chaseIn3D = false;
        followGround = false;
        snapToGroundOnStart = false;
        walkSpeed = speed;
        animatorWalkSpeed = animSpeed;
        chaseStopDistance = Mathf.Max(0f, stopDistance);
        highwayMarchDirection = marchDirection.sqrMagnitude > 0.0001f
            ? marchDirection.normalized
            : Vector3.right;
        highwayMarchDirection.y = 0f;

        if (routeChaseInitialized)
            return;

        var segment = DutzHighwayDirection.FindNearestTrackSegment(transform.position);
        routeSegmentName = segment != null ? segment.name : null;
        routeChaseInitialized = true;
    }

    public void ConfigureForMarch(float speed, float animSpeed)
    {
        walkForward = true;
        walkSpeed = speed;
        animatorWalkSpeed = animSpeed;
        ClearChaseTarget();
    }

    public void ConfigureForHighwayMarch(float speed, float animSpeed, Vector3 marchDirection, bool preserveSpawnPose = false)
    {
        walkForward = true;
        lockForwardToHighway = true;
        followGround = true;
        chaseIn3D = false;
        walkSpeed = speed;
        animatorWalkSpeed = animSpeed;
        chaseStopDistance = 0f;
        snapToGroundOnStart = !preserveSpawnPose;
        ClearChaseTarget();
        highwayMarchDirection = marchDirection.sqrMagnitude > 0.0001f
            ? marchDirection.normalized
            : Vector3.right;
        highwayMarchDirection.y = 0f;
    }

    public void ConfigureForJonremEscort(float speed, float animSpeed, float stopDistance, float groundCheck)
    {
        ConfigureForJonremEscort(speed, animSpeed, stopDistance, groundCheck, Vector3.right);
    }

    public void ConfigureForJonremEscort(
        float speed,
        float animSpeed,
        float stopDistance,
        float groundCheck,
        Vector3 marchDirection)
    {
        walkForward = true;
        lockForwardToHighway = true;
        followGround = true;
        chaseIn3D = false;
        walkSpeed = speed;
        animatorWalkSpeed = animSpeed;
        chaseStopDistance = Mathf.Max(0f, stopDistance);
        groundCheckDistance = Mathf.Max(groundCheck, 1.5f);
        highwayMarchDirection = marchDirection.sqrMagnitude > 0.0001f
            ? marchDirection.normalized
            : Vector3.right;
        SeedJonremEscortGroundAnchor();
    }

    public void ConfigureForFlight()
    {
        followGround = false;
        chaseIn3D = true;
        supermanFlight = false;
        walkForward = false;
        snapToGroundOnStart = false;
    }

    public void ConfigureForSupermanFlight(float patrolSpeed, float animSpeed)
    {
        followGround = false;
        chaseIn3D = true;
        supermanFlight = true;
        walkForward = true;
        lockForwardToHighway = true;
        snapToGroundOnStart = false;
        walkSpeed = patrolSpeed;
        animatorWalkSpeed = animSpeed;
        ClearChaseTarget();
    }

    public void ConfigureForFlightChase(float speed, float animSpeed)
    {
        ConfigureForSupermanFlight(speed, animSpeed);
        walkForward = false;
    }

    public void ConfigureForFlightPatrol(float speed, float animSpeed)
    {
        ConfigureForSupermanFlight(speed, animSpeed);
    }

    static bool IsJonremEscortMovementPausedByBootstrap()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return false;

        return !DutzGameBootstrap.IsReady;
    }

    bool ShouldPauseJonremEscortMovement() =>
        DutzGiantBossNames.IsJonremEscort(gameObject.name) && IsJonremEscortMovementPausedByBootstrap();

    bool IsJonremEscortNpc() => DutzGiantBossNames.IsJonremEscort(gameObject.name);

    void SeedJonremEscortGroundAnchor()
    {
        if (!IsJonremEscortNpc())
            return;

        if (DutzCollectibleProgress.IsLevel01
            && DutzJonremEscortSpawnLock.TryGetPose(gameObject, out var lockedPos, out _))
        {
            jonremEscortAnchoredPivotY = lockedPos.y;
            return;
        }

        jonremEscortAnchoredPivotY = transform.position.y;
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (!rb.isKinematic)
            rb.isKinematic = true;

        if (rb.useGravity)
            rb.useGravity = false;

        var pos = rb.position;
        var isMoving = false;
        var moveDir = lastMoveDirection;

        if (walkingEnabled && !ShouldPauseJonremEscortMovement())
        {
            Vector3 dir;
            if (chaseTarget != null)
            {
                if (routeLockedHighwayChase)
                    ApplyFlatChase(ref pos, ref moveDir, ref isMoving);
                else
                    ApplyDirectChase(ref pos, ref moveDir, ref isMoving);
            }
            else if (walkForward)
            {
                if (lockForwardToHighway)
                    dir = highwayMarchDirection;
                else
                {
                    dir = transform.forward;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.0001f)
                        dir = Vector3.right;
                    else
                        dir.Normalize();
                }

                moveDir = dir;
                pos += dir * (walkSpeed * Time.fixedDeltaTime);
                isMoving = true;
            }
        }

        if (supermanFlight && isMoving)
            lastMoveDirection = moveDir;

        if (supermanFlight)
        {
            flyingHippie ??= GetComponent<SimpleCitizensFlyingHippie>();
            flyingHippie?.ApplyFlightBob(ref pos);

            var flightDir = isMoving ? moveDir : lastMoveDirection;
            var look = SimpleCitizensFlyingHippie.SupermanRotation(flightDir);
            transform.rotation = look;
            rb.rotation = look;
        }
        else if (isMoving)
        {
            var look = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = look;
            rb.rotation = look;
        }

        var grounded = false;
        if (routeLockedHighwayChase)
        {
            TrySnapDeckOnSegmentCross(ref pos);
            grounded = true;
        }
        else if (DutzLevel07GiantHomes.TryClampOntoHomeHighway(
                     gameObject.name, ref pos, DutzNpcFeet.GetPivotToFeetOffset(gameObject)))
        {
            grounded = true;
        }
        else if (IsLevel07Straight2GroundNpc())
        {
            // Always clamp onto pitched Straight 2 — chase/patrol must not float off the slab.
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            grounded = DutzRoadGround.TryClampOntoLevel07Straight2Deck(ref pos, pivotToFeet);
        }
        else if (IsLevel07Straight3Addict() || IsLevel07Straight3IAmBaby())
        {
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            grounded = DutzRoadGround.TryClampOntoLevel07Straight3Deck(ref pos, pivotToFeet);
        }
        else if (IsLevel07Highway8Giant() || IsLevel07Highway8Crocodile())
        {
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            grounded = DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pos, pivotToFeet);
        }
        else if (IsLevel07Highway7Giant() || IsLevel07Highway7Crocodile())
        {
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            grounded = DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref pos, pivotToFeet);
        }
        else if (followGround)
        {
            if (ShouldPauseJonremEscortMovement())
                grounded = true;
            else
                grounded = StickFeetToGround(ref pos);
        }

        // Level07 ground bosses (Boy Idol, Gong Bong, …) — hard deck lock; never sky-chase.
        if (IsLevel07GroundLockedCombatGiant())
            grounded = ClampLevel07GroundCombatGiantToDeck(ref pos) || grounded;

        IsFlyingMoving = supermanFlight && isMoving;
        rb.MovePosition(pos);
        transform.SetPositionAndRotation(pos, rb.rotation);

        UpdateWalkAnimator(grounded, isMoving);
    }

    /// <summary>Level07 chase giants that must stay on a road deck (not birds / flyers).</summary>
    bool IsLevel07GroundLockedCombatGiant()
    {
        if (!DutzCollectibleProgress.IsLevel07
            || !DutzCollectibleProgress.IsLevel07CombatGiant(gameObject.name)
            || GetComponent<DutzAlienGiantBirdHunter>() != null
            || GetComponent<SimpleCitizensFlyingHippieHunter>() != null)
            return false;

        // Bridge 1 multi-deck MeshCollider: placement clamp picks the top beam and
        // ratchets Piyaya onto it before the player arrives. Keep normal near-feet footing.
        if (DutzGiantBossNames.IsPiyaya(gameObject.name))
            return false;

        return true;
    }

    /// <summary>
    /// Snap runaway ground bosses back onto their deck. Caps corrupt pivot-to-feet so mesh-bounds
    /// bugs cannot yeet them thousands of units up (same failure mode as Straight 2).
    /// </summary>
    bool ClampLevel07GroundCombatGiantToDeck(ref Vector3 worldPosition)
    {
        chaseIn3D = false;
        supermanFlight = false;
        followGround = true;

        var pivotToFeet = Mathf.Clamp(DutzNpcFeet.GetPivotToFeetOffset(gameObject), 0.05f, 4f);
        bodyCollider = GetSolidCollider();

        // Boy Idol lives on Straight 6 — clamp there first.
        if (DutzGiantBossNames.IsBoyIdol(gameObject.name)
            && DutzRoadGround.TryClampOntoLevel07NamedHighwayDeck(
                "Highway Straight 6", ref worldPosition, pivotToFeet))
            return true;

        // Sky-high recovery: sample from a Level07 deck-height hint, not from runaway Y.
        var hintY = worldPosition.y > 200f ? 90f : worldPosition.y;

        if (DutzRoadGround.TrySampleRoadDeckForPlacement(
                worldPosition, hintY, bodyCollider, out var deckY))
        {
            worldPosition.y = deckY + pivotToFeet;
            return true;
        }

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(
                new Vector3(worldPosition.x, hintY, worldPosition.z),
                hintY,
                bodyCollider,
                out deckY))
        {
            worldPosition.y = deckY + pivotToFeet;
            return true;
        }

        return false;
    }

    bool IsLevel07Straight2Addict() =>
        DutzLevel07Straight3AddictSpawner.IsStraight2Addict(gameObject.name);

    bool IsLevel07Straight2Raptor() =>
        DutzCollectibleProgress.IsLevel07
        && string.Equals(gameObject.name, "RAPTOR", System.StringComparison.Ordinal);

    bool IsLevel07Straight2KBilyar() =>
        DutzCollectibleProgress.IsLevel07
        && DutzGiantBossNames.IsKBilyar(gameObject.name);

    bool IsLevel07Straight3IAmBaby() =>
        DutzCollectibleProgress.IsLevel07
        && DutzGiantBossNames.IsIAmBaby(gameObject.name);

    bool IsLevel07Straight3Addict() =>
        DutzLevel07Straight3AddictSpawner.IsStraight3Addict(gameObject.name);

    bool IsLevel07Highway8Giant() =>
        DutzCollectibleProgress.IsLevel07
        && DutzLevel07GiantHomes.TryGetHomeHighway(gameObject.name, out var home)
        && home == DutzLevel07GiantHomes.Highway8;

    bool IsLevel07Highway7Giant() =>
        DutzCollectibleProgress.IsLevel07
        && DutzLevel07GiantHomes.TryGetHomeHighway(gameObject.name, out var home)
        && home == DutzLevel07GiantHomes.Highway7;

    bool IsLevel07Highway8Crocodile() =>
        DutzCollectibleProgress.IsLevel07
        && gameObject.name.StartsWith("Level07_Highway8_Croc_", System.StringComparison.Ordinal);

    bool IsLevel07Highway7Crocodile() =>
        DutzCollectibleProgress.IsLevel07
        && gameObject.name.StartsWith("Level07_Highway7_Croc_", System.StringComparison.Ordinal);

    bool IsLevel07SlopingHighwayCrocodile() =>
        IsLevel07Highway8Crocodile() || IsLevel07Highway7Crocodile();

    /// <summary>Level07 Straight 2 ground NPCs — small addicts, RAPTOR, and K Bilyar.</summary>
    bool IsLevel07Straight2GroundNpc() =>
        IsLevel07Straight2Addict() || IsLevel07Straight2Raptor() || IsLevel07Straight2KBilyar();

    /// <summary>Level07 NPCs on non-main-track highways (Straight 2 / Straight 3 / Highway 7 / Highway 8).</summary>
    bool IsLevel07SpecialHighwayGroundNpc() =>
        DutzLevel07GiantHomes.HasHomeHighway(gameObject.name)
        || IsLevel07Straight2GroundNpc()
        || IsLevel07Straight3Addict()
        || IsLevel07Straight3IAmBaby()
        || IsLevel07Highway8Giant()
        || IsLevel07Highway7Giant()
        || IsLevel07SlopingHighwayCrocodile();

    float GetGroundProbeScale()
    {
        var s = transform.lossyScale;
        return Mathf.Max(s.x, s.y, s.z, 1f);
    }

    void UpdateWalkAnimator(bool grounded, bool isMoving)
    {
        if (animator == null)
            return;

        var hitPoints = GetComponent<DutzNpcHitPoints>();
        if (hitPoints != null && hitPoints.IsDead)
            return;

        var animGrounded = routeLockedHighwayChase || (followGround ? grounded : true);
        var animMoving = routeLockedHighwayChase ? isMoving : (followGround ? grounded && isMoving : isMoving);
        if (IsJonremEscortNpc())
        {
            animGrounded = true;
            animMoving = isMoving;
        }

        animator.SetBool(GroundedId, animGrounded);
        animator.SetFloat(SpeedId, animMoving ? animatorWalkSpeed : 0f);
    }

    bool IsGrounded()
    {
        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);
        var origin = new Vector3(transform.position.x, feetY + 0.15f, transform.position.z);
        var scale = GetGroundProbeScale();
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance * scale, ~0, QueryTriggerInteraction.Ignore);
    }

    bool StickFeetToGround(ref Vector3 worldPosition)
    {
        if (IsJonremEscortNpc())
            return StickJonremEscortFeetToGround(ref worldPosition);

        bodyCollider = GetSolidCollider();
        if (bodyCollider == null)
            return false;

        DutzRoadGround.SyncTransformsIfNeeded();
        // Cap corrupt pivot-to-feet (static-batched mesh bounds can be thousands of units).
        var pivotToFeet = Mathf.Clamp(DutzNpcFeet.GetPivotToFeetOffset(gameObject), 0.05f, 4f);
        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);

        if (DutzCrocodilePoolMember.IsCrocodile(gameObject)
            && !IsLevel07SlopingHighwayCrocodile())
        {
            if (DutzRoadGround.TrySampleCrocodileRoadDeckY(worldPosition, feetY, bodyCollider, out var crocSurfaceY))
            {
                worldPosition.y = crocSurfaceY + pivotToFeet;
                return true;
            }

            return IsGrounded();
        }

        if (DutzLevel07GiantHomes.TryClampOntoHomeHighway(gameObject.name, ref worldPosition, pivotToFeet))
            return true;

        if (IsLevel07Highway8Crocodile()
            && DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref worldPosition, pivotToFeet))
            return true;

        if (IsLevel07Highway7Crocodile()
            && DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref worldPosition, pivotToFeet))
            return true;

        if (IsLevel07Highway7Giant()
            && DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref worldPosition, pivotToFeet))
            return true;

        if (DutzCollectibleProgress.UsesLevel03GiantRoadFooting(gameObject.name)
            && !routeLockedHighwayChase
            && !DutzLevel07GiantHomes.HasHomeHighway(gameObject.name)
            && !IsLevel07Highway8Giant()
            && !IsLevel07Highway7Giant()
            && !IsLevel07Straight2GroundNpc()
            && !IsLevel07Straight3Addict()
            && !IsLevel07Straight3IAmBaby())
        {
            if (DutzRoadGround.TrySampleGiantRoadDeckY(worldPosition, feetY, bodyCollider, out var giantSurfaceY))
            {
                worldPosition.y = giantSurfaceY + pivotToFeet;
                return true;
            }

            return IsGrounded();
        }

        // Level07 Straight-2 ground NPCs: steep pitched slab — stand along road.up, not world up.
        if (IsLevel07Straight2GroundNpc()
            && DutzRoadGround.TryClampOntoLevel07Straight2Deck(ref worldPosition, pivotToFeet))
            return true;

        if ((IsLevel07Straight3Addict() || IsLevel07Straight3IAmBaby())
            && DutzRoadGround.TryClampOntoLevel07Straight3Deck(ref worldPosition, pivotToFeet))
            return true;

        if (IsLevel07Highway8Giant()
            && DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref worldPosition, pivotToFeet))
            return true;

        if (DutzRoadGround.TrySampleWalkableRoadDeckY(worldPosition, feetY, bodyCollider, out var deckSurfaceY))
        {
            worldPosition.y = deckSurfaceY + pivotToFeet;
            return true;
        }

        if (TrySnapFromFeetRaycast(ref worldPosition, pivotToFeet))
            return true;

        return IsGrounded();
    }

    bool StickJonremEscortFeetToGround(ref Vector3 worldPosition)
    {
        if (DutzCollectibleProgress.IsLevel01)
            return HoldJonremEscortGroundHeight(ref worldPosition);

        bodyCollider = GetSolidCollider();
        if (bodyCollider == null)
            return HoldJonremEscortGroundHeight(ref worldPosition);

        DutzRoadGround.SyncTransformsIfNeeded();
        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);

        if (DutzJonremEscortPlacement.TrySnapFeetOnHighwayTwo(ref worldPosition))
        {
            jonremEscortAnchoredPivotY = worldPosition.y;
            return true;
        }

        var marchDir = lockForwardToHighway ? highwayMarchDirection : transform.forward;
        marchDir.y = 0f;
        if (marchDir.sqrMagnitude > 0.0001f)
            marchDir.Normalize();
        else
            marchDir = highwayMarchDirection.sqrMagnitude > 0.0001f ? highwayMarchDirection : Vector3.right;

        var step = walkSpeed * Time.fixedDeltaTime;
        var lookahead = Mathf.Max(step * 3f, 2.5f);
        var samplePositions = new[]
        {
            worldPosition + marchDir * lookahead,
            worldPosition + marchDir * (lookahead * 0.5f),
            worldPosition
        };

        for (var i = 0; i < samplePositions.Length; i++)
        {
            if (!TrySampleJonremEscortDeck(samplePositions[i], feetY, pivotToFeet, ref worldPosition))
                continue;

            jonremEscortAnchoredPivotY = worldPosition.y;
            return true;
        }

        return HoldJonremEscortGroundHeight(ref worldPosition);
    }

    bool TrySampleJonremEscortDeck(
        Vector3 samplePosition,
        float feetY,
        float pivotToFeet,
        ref Vector3 worldPosition)
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return false;

        var pos = new Vector3(samplePosition.x, worldPosition.y, samplePosition.z);
        if (!DutzJonremEscortPlacement.TrySnapFeetOnHighwayTwo(ref pos))
            return false;

        worldPosition.y = pos.y;
        return true;
    }

    bool HoldJonremEscortGroundHeight(ref Vector3 worldPosition)
    {
        if (float.IsNaN(jonremEscortAnchoredPivotY))
        {
            if (DutzCollectibleProgress.IsLevel01
                && DutzJonremEscortSpawnLock.TryGetPose(gameObject, out var lockedPos, out _))
            {
                jonremEscortAnchoredPivotY = lockedPos.y;
            }
            else
            {
                return IsGrounded();
            }
        }

        worldPosition.y = jonremEscortAnchoredPivotY;
        return true;
    }

    void ApplyDirectChase(ref Vector3 pos, ref Vector3 moveDir, ref bool isMoving)
    {
        if (!chaseIn3D)
        {
            ApplyFlatChase(ref pos, ref moveDir, ref isMoving);
            return;
        }

        var dir = chaseTarget.position - pos;
        var distSq = dir.sqrMagnitude;
        var stopSq = chaseStopDistance * chaseStopDistance;
        if (distSq > stopSq && distSq > 0.25f)
        {
            dir.Normalize();
            moveDir = dir;
            var step = walkSpeed * Time.fixedDeltaTime;
            if (stopSq > 0.0001f && distSq <= (Mathf.Sqrt(stopSq) + step) * (Mathf.Sqrt(stopSq) + step))
            {
                var moveDist = Mathf.Sqrt(distSq) - chaseStopDistance;
                if (moveDist > 0f)
                    pos += dir * moveDist;
            }
            else
            {
                pos += dir * step;
            }

            isMoving = true;
        }
        else if (distSq > 0.25f)
        {
            dir.Normalize();
            moveDir = dir;
        }
    }

    void ApplyFlatChase(ref Vector3 pos, ref Vector3 moveDir, ref bool isMoving)
    {
        var dir = chaseTarget.position - pos;
        dir.y = 0f;

        var distSq = dir.sqrMagnitude;
        var stopSq = chaseStopDistance * chaseStopDistance;
        if (distSq > stopSq && distSq > 0.25f)
        {
            dir.Normalize();
            moveDir = dir;
            var step = walkSpeed * Time.fixedDeltaTime;
            if (stopSq > 0.0001f && distSq <= (Mathf.Sqrt(stopSq) + step) * (Mathf.Sqrt(stopSq) + step))
            {
                var moveDist = Mathf.Sqrt(distSq) - chaseStopDistance;
                if (moveDist > 0f)
                    pos += dir * moveDist;
            }
            else
            {
                pos += dir * step;
            }

            isMoving = true;
        }
        else if (distSq > 0.25f)
        {
            dir.Normalize();
            moveDir = dir;
        }
    }

    void TrySnapDeckOnSegmentCross(ref Vector3 worldPosition)
    {
        if (!routeLockedHighwayChase)
            return;

        var segment = DutzHighwayDirection.FindNearestTrackSegment(worldPosition);
        var segName = segment != null ? segment.name : null;
        if (segName == routeSegmentName)
            return;

        routeSegmentName = segName;
        bodyCollider = GetSolidCollider();
        if (bodyCollider == null)
            return;

        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);
        if (!DutzRoadGround.TrySampleGiantRoadDeckY(worldPosition, feetY, bodyCollider, out var deckY))
            return;

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
        worldPosition.y = deckY + pivotToFeet;
    }

    bool TrySnapFromFeetRaycast(ref Vector3 worldPosition, float pivotToFeet)
    {
        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);
        var origin = new Vector3(worldPosition.x, feetY + 0.25f, worldPosition.z);
        var reach = groundCheckDistance * GetGroundProbeScale() + 12f;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider == null || hit.collider.isTrigger)
            return false;

        // Never stand on parachutes, props, or other NPCs — only highway decks.
        if (!DutzRoadGround.IsHighwayRoadCollider(hit.collider))
            return false;

        var feet = Mathf.Clamp(pivotToFeet, 0.05f, 4f);
        worldPosition.y = hit.point.y + feet;
        return true;
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

    public void SnapFeetToGround() => SnapFeetToRoad();

    public void SnapFeetToRoad()
    {
        Apply();

        if (!followGround)
            return;

        if (DutzGiantBossNames.IsJonremEscort(gameObject.name) && DutzCollectibleProgress.IsLevel01)
        {
            if (Application.isPlaying)
                return;

            var pos = transform.position;
            if (DutzJonremEscortPlacement.TrySnapFeetOnHighwayTwo(ref pos))
            {
                transform.position = pos;
                jonremEscortAnchoredPivotY = pos.y;
            }

            return;
        }

        bodyCollider = GetSolidCollider();

        if (DutzCrocodilePoolMember.IsCrocodile(gameObject) && !IsLevel07SlopingHighwayCrocodile())
        {
            var probe = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            if (DutzRoadGround.TrySampleRoadDeckForPlacement(probe, transform.position.y, bodyCollider, out var deckY))
                DutzNpcFeet.PlacePivotOnSurface(gameObject, deckY);
            return;
        }

        if (DutzLevel07GiantHomes.HasHomeHighway(gameObject.name))
        {
            var pos = transform.position;
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            if (DutzLevel07GiantHomes.TryClampOntoHomeHighway(gameObject.name, ref pos, pivotToFeet))
            {
                transform.position = pos;
                if (rb != null)
                    rb.position = pos;
            }

            return;
        }

        if (IsLevel07Straight2GroundNpc())
        {
            var pos = transform.position;
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            if (DutzRoadGround.TryClampOntoLevel07Straight2Deck(ref pos, pivotToFeet))
            {
                transform.position = pos;
                if (rb != null)
                    rb.position = pos;
            }

            return;
        }

        if (IsLevel07Straight3Addict() || IsLevel07Straight3IAmBaby())
        {
            var pos = transform.position;
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            if (DutzRoadGround.TryClampOntoLevel07Straight3Deck(ref pos, pivotToFeet))
            {
                transform.position = pos;
                if (rb != null)
                    rb.position = pos;
            }

            return;
        }

        if (IsLevel07Highway8Giant() || IsLevel07Highway8Crocodile())
        {
            var pos = transform.position;
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            if (DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pos, pivotToFeet))
            {
                transform.position = pos;
                if (rb != null)
                    rb.position = pos;
            }

            return;
        }

        if (IsLevel07Highway7Crocodile())
        {
            var pos = transform.position;
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            if (DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref pos, pivotToFeet))
            {
                transform.position = pos;
                if (rb != null)
                    rb.position = pos;
            }

            return;
        }

        if (IsLevel07Highway7Giant())
        {
            var pos = transform.position;
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(gameObject);
            if (DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref pos, pivotToFeet))
            {
                transform.position = pos;
                if (rb != null)
                    rb.position = pos;
            }

            return;
        }

        if (DutzCollectibleProgress.UsesLevel03GiantRoadFooting(gameObject.name))
        {
            var giantFeetY = DutzNpcFeet.GetLowestWorldY(gameObject);
            if (DutzRoadGround.TrySampleGiantRoadDeckY(transform.position, giantFeetY, bodyCollider, out var giantDeckY))
                DutzNpcFeet.PlacePivotOnSurface(gameObject, giantDeckY);

            return;
        }

        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);
        if (!DutzRoadGround.TrySampleWalkableRoadDeckY(transform.position, feetY, bodyCollider, out var walkSurfaceY)
            && !DutzRoadGround.TrySampleRoadDeckForPlacement(
                transform.position, transform.position.y, bodyCollider, out walkSurfaceY))
        {
            SeedJonremEscortGroundAnchor();
            return;
        }

        DutzNpcFeet.PlacePivotOnSurface(gameObject, walkSurfaceY);
        if (IsJonremEscortNpc())
            jonremEscortAnchoredPivotY = transform.position.y;
    }

    public bool IsGroundedOnRoad() => IsGrounded();

    const string Level00CrowdWalkerTypeName = "DutzLevel00CrowdWalker";

    public static bool IsLevel00CrowdWalker(GameObject go)
    {
        if (go == null)
            return false;

        foreach (var behaviour in go.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == Level00CrowdWalkerTypeName)
                return true;
        }

        return false;
    }
}
