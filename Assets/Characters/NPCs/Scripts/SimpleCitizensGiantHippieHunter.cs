using System.Collections;
using UnityEngine;

/// <summary>
/// Giant hippie: wakes up and chases the player across the road to bite.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[DefaultExecutionOrder(-200)]
public class SimpleCitizensGiantHippieHunter : MonoBehaviour
{
    static bool IsGiantHippie(string objectName) =>
        DutzGiantBossNames.IsMidTrackGiant(objectName)
        || DutzGiantBossNames.IsTrililing(objectName)
        || DutzGiantBossNames.IsJonremEscort(objectName)
        || DutzGiantBossNames.IsGongBong(objectName)
        || DutzGiantBossNames.IsCawetan(objectName)
        || (DutzCollectibleProgress.IsLevel03Gameplay && DutzCollectibleProgress.IsLevel03Giant(objectName))
        || (DutzCollectibleProgress.IsLevel07 && DutzCollectibleProgress.IsLevel07CombatGiant(objectName))
        || (DutzCollectibleProgress.IsLevel02 && DutzGiantBossNames.IsHontavirus(objectName));

    [SerializeField] float wakeDistance = 280f;
    [SerializeField] bool huntImmediately;

    const float MidGiantChaseSpeed = 19f;
    const float MidGiantChaseAnimSpeed = 1f;
    const float EndGiantChaseSpeed = 22f;
    const float EndGiantChaseAnimSpeed = 1.5f;

    [Header("Chase (world units/sec)")]
    [SerializeField] float chaseSpeed = MidGiantChaseSpeed;
    [SerializeField] float chaseAnimSpeed = MidGiantChaseAnimSpeed;

    [Header("Chase standoff (meters — world space, not scaled by giant size)")]
    [SerializeField] float chaseStopDistance = 2.5f;

    const float Level03TrackChaseStopDistance = 2.5f;
    const float Level03EndEtOlChaseStopDistance = 3f;
    const float Level03FarWakeInterval = 0.5f;
    const float Level03FarWakeDistance = 120f;
    const float Level03SleepDistance = 200f;
    const float Level01MobileWakeInterval = 0.35f;

    float nextWakeCheckTime;

    SimpleCitizensNpcPhysics npcPhysics;
    DutzPlayerController player;
    bool awakened;
    bool chaseConfigured;
    float punchStunUntil;
    float punchStunAnimatorSpeedRestore = 1f;
    bool punchStunAnimatorFrozen;
    Collider[] wakeColliders;

    public float WakeDistanceMeters => wakeDistance;
    public float ChaseSpeedMetersPerSecond => chaseSpeed;
    public float ChaseAnimSpeed => chaseAnimSpeed;
    public bool HasAwakened => awakened;

    public static void EnsureTrililingColliderOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null)
            return;

        if (DutzGiantHeadTopCollider.UsesGiantHeadColliders(physics.gameObject.name))
        {
            DutzHippieBiteCollider.EnsureTrililingSolidCollider(physics.gameObject);
            DutzGiantHeadTopCollider.EnsureOnGiant(physics.gameObject);
            return;
        }

        DutzGiantHeadTopCollider.EnsureChaseGiantPushColliderOnGiant(physics.gameObject);
    }

    public static void EnsureTrililingColliderOnScene()
    {
        foreach (var physics in Object.FindObjectsOfType<SimpleCitizensNpcPhysics>())
            EnsureTrililingColliderOnNpc(physics);
    }

    public static void EnsureOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null || !IsGiantHippie(physics.gameObject.name))
            return;

        if (physics.GetComponent<SimpleCitizensGiantHippieHunter>() == null)
            physics.gameObject.AddComponent<SimpleCitizensGiantHippieHunter>();
    }

    void Awake()
    {
        // Giants use punch/push colliders; crocs keep HippieBiter for bite kills.
        if (!IsLevel07SlopingHighwayCrocodile())
        {
            var biter = GetComponent<SimpleCitizensHippieBiter>();
            if (biter != null)
                biter.enabled = false;
        }

        // Sara-only freeze must never stay on hippie boss giants.
        var stationary = GetComponent<DutzGrandmaGiantStationary>();
        if (stationary != null)
            Destroy(stationary);

        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        ApplyGiantHuntDefaults();
        ApplyChaseSettings();
        if (!DutzGiantBossNames.IsJonremEscort(gameObject.name))
            chaseConfigured = true;
        if (IsLevel01JonremEscort())
            HoldJonremEscortAtSpawn();
        else if (DutzGiantBossNames.IsJonremEscort(gameObject.name))
            ApplyJonremEscortMovement(false);

        if (DutzGiantHeadTopCollider.UsesGiantHeadColliders(gameObject.name))
        {
            DutzHippieBiteCollider.EnsureTrililingSolidCollider(gameObject);
            DutzGiantHeadTopCollider.EnsureOnGiant(gameObject);
        }
        else if (DutzGiantHeadTopCollider.UsesChaseGiantPushColliders(gameObject.name))
        {
            DutzGiantHeadTopCollider.EnsureChaseGiantPushColliderOnGiant(gameObject);
        }

        CacheWakeColliders();
    }

    void CacheWakeColliders() => wakeColliders = GetComponentsInChildren<Collider>(true);

    void ApplyGiantHuntDefaults()
    {
        if (IsLevel01JonremEscort())
            return;

        if (UsesLevel03ChaseTuning())
        {
            ApplyLevel03ChaseTuning();
            return;
        }

        if (DutzGiantBossNames.IsTrililing(gameObject.name))
        {
            chaseSpeed = EndGiantChaseSpeed;
            chaseAnimSpeed = EndGiantChaseAnimSpeed;
            return;
        }

        chaseSpeed = MidGiantChaseSpeed;
        chaseAnimSpeed = MidGiantChaseAnimSpeed;
    }

    bool UsesLevel03ChaseTuning()
    {
        if (DutzCollectibleProgress.IsLevel03TrackEtOl(gameObject.name))
            return true;

        if (DutzCollectibleProgress.IsLevel03Gameplay
            && DutzCollectibleProgress.IsLevel03BonusGiant(gameObject.name))
            return true;

        return DutzCollectibleProgress.IsLevel03Gameplay && DutzGiantBossNames.IsLevel03EndBoss(gameObject.name);
    }

    bool UsesLevel01RouteLockedChase() =>
        DutzCollectibleProgress.IsLevel01
        && (DutzGiantBossNames.IsTamby(gameObject.name) || DutzGiantBossNames.IsETol(gameObject.name));

    bool UsesLevel02RouteLockedChase() =>
        DutzCollectibleProgress.IsLevel02 && IsGiantHippie(gameObject.name);

    bool UsesRouteLockedHighwayChase() =>
        !IsLevel07Straight2Raptor()
        && !IsLevel07Straight2KBilyar()
        && !IsLevel07Straight3IAmBaby()
        && !IsLevel07SlopingHighwayChaseNpc()
        && !IsLevel07Bridge1LieFivex()
        && !IsLevel07GroundOnlyChaseGiant()
        && (UsesLevel03ChaseTuning() || UsesLevel01RouteLockedChase() || UsesLevel02RouteLockedChase());

    bool IsLevel07Straight2Raptor() =>
        DutzCollectibleProgress.IsLevel07
        && string.Equals(gameObject.name, "RAPTOR", System.StringComparison.Ordinal);

    bool IsLevel07Straight2KBilyar() =>
        DutzCollectibleProgress.IsLevel07
        && DutzGiantBossNames.IsKBilyar(gameObject.name);

    bool IsLevel07Straight3IAmBaby() =>
        DutzCollectibleProgress.IsLevel07
        && DutzGiantBossNames.IsIAmBaby(gameObject.name);

    bool IsLevel07Bridge1LieFivex() =>
        DutzCollectibleProgress.IsLevel07
        && string.Equals(gameObject.name, "Lie Fivex", System.StringComparison.Ordinal);

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

    /// <summary>Level07 chase NPCs locked to a home highway deck.</summary>
    bool IsLevel07SlopingHighwayChaseNpc() =>
        DutzLevel07GiantHomes.HasHomeHighway(gameObject.name);

    /// <summary>Level07 Highway 8 chase NPCs — flat XZ chase clamped to the sloping deck.</summary>
    bool IsLevel07Highway8ChaseNpc() => IsLevel07SlopingHighwayChaseNpc();

    const float Level07RaptorPatrolSpeed = 8f;
    const float Level07RaptorPatrolAnimSpeed = 0.55f;
    const float Level07RaptorPatrolLocalZLimit = 0.38f;
    float level07RaptorPatrolSign = 1f;

    bool UsesLevel01MobileWakeThrottle() =>
        DutzCollectibleProgress.IsLevel01
        && Application.isMobilePlatform
        && IsGiantHippie(gameObject.name);

    /// <summary>Level 02 — throttle wake checks in Editor and on device (deck raycast relief).</summary>
    bool UsesLevel02WakeThrottle() =>
        DutzCollectibleProgress.IsLevel02 && IsGiantHippie(gameObject.name);

    bool UsesWakeThrottle() =>
        (UsesLevel03ChaseTuning() && Application.isMobilePlatform)
        || UsesLevel01MobileWakeThrottle()
        || UsesLevel02WakeThrottle();

    float GetMobileWakeInterval() =>
        UsesLevel03ChaseTuning() || UsesLevel02WakeThrottle()
            ? Level03FarWakeInterval
            : Level01MobileWakeInterval;

    void ApplyLevel03ChaseTuning()
    {
        var isEndEtOl = DutzGiantBossNames.IsLevel03EndBoss(gameObject.name);
        chaseSpeed = isEndEtOl
            ? DutzCollectibleProgress.Level03GiantChaseSpeed
            : DutzCollectibleProgress.Level03TrackGiantChaseSpeed;
        chaseAnimSpeed = isEndEtOl
            ? DutzCollectibleProgress.GetLevel03GiantChaseAnimSpeed()
            : DutzCollectibleProgress.GetLevel03TrackGiantChaseAnimSpeed();
        chaseStopDistance = isEndEtOl
            ? Level03EndEtOlChaseStopDistance
            : Level03TrackChaseStopDistance;
    }

    void ApplyChaseSettings()
    {
        if (npcPhysics == null)
            return;

        if (DutzGiantBossNames.IsJonremEscort(gameObject.name))
            return;

        if (UsesRouteLockedHighwayChase())
        {
            var march = DutzHighwayDirection.GetReferenceForward();
            if (march.sqrMagnitude < 0.0001f)
                march = Vector3.right;
            npcPhysics.ConfigureForHighwayRouteChase(
                chaseSpeed,
                chaseAnimSpeed,
                GetChaseStopDistance(),
                march);
            return;
        }

        // Level07 RAPTOR / K Bilyar / I am baby / Bridge1 Lie Fivex / home-highway giants+crocs — flat XZ chase.
        if (IsLevel07Straight2Raptor() || IsLevel07Straight2KBilyar() || IsLevel07Straight3IAmBaby()
            || IsLevel07Bridge1LieFivex() || IsLevel07SlopingHighwayChaseNpc())
        {
            npcPhysics.SetWalkingEnabled(true);
            npcPhysics.ConfigureForChase(chaseSpeed, chaseAnimSpeed, GetChaseStopDistance());
            return;
        }

        // Level07 ground chase bosses (Boy Idol, Gong Bong, …) — never fly after parachute.
        if (IsLevel07GroundOnlyChaseGiant())
        {
            StripBoyIdolFlightComponents();
            npcPhysics.SetWalkingEnabled(true);
            npcPhysics.ConfigureForGroundChase(chaseSpeed, chaseAnimSpeed, GetChaseStopDistance());
            return;
        }

        npcPhysics.SetWalkingEnabled(true);
        npcPhysics.ConfigureForChase(chaseSpeed, chaseAnimSpeed, GetChaseStopDistance());
    }

    bool IsLevel07BoyIdol() =>
        DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsBoyIdol(gameObject.name);

    bool IsLevel07GongBong() =>
        DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsGongBong(gameObject.name);

    /// <summary>Level07 combat giants that must stay on deck (not birds / flyers).</summary>
    bool IsLevel07GroundOnlyChaseGiant() =>
        DutzCollectibleProgress.IsLevel07
        && DutzCollectibleProgress.IsLevel07CombatGiant(gameObject.name)
        && GetComponent<DutzAlienGiantBirdHunter>() == null
        && GetComponent<SimpleCitizensFlyingHippieHunter>() == null;

    void StripBoyIdolFlightComponents()
    {
        var flyer = GetComponent<SimpleCitizensFlyingHippieHunter>();
        if (flyer != null)
            flyer.enabled = false;
    }

    void ApplyLevel07RaptorIdlePatrol()
    {
        if (npcPhysics == null)
            return;

        var straight2 = GameObject.Find("Highway Straight 2");
        if (straight2 == null)
        {
            npcPhysics.ClearChaseTarget();
            npcPhysics.SetWalkingEnabled(false);
            return;
        }

        var road = straight2.transform;
        var local = road.InverseTransformPoint(transform.position);
        var zLimit = GetLevel07RaptorPatrolLocalZLimit(road);
        if (local.z >= zLimit)
            level07RaptorPatrolSign = -1f;
        else if (local.z <= -zLimit)
            level07RaptorPatrolSign = 1f;

        var march = road.forward * level07RaptorPatrolSign;
        march.y = 0f;
        if (march.sqrMagnitude < 0.0001f)
            march = Vector3.right * level07RaptorPatrolSign;
        else
            march.Normalize();

        npcPhysics.SetWalkingEnabled(true);
        npcPhysics.ConfigureForHighwayMarch(
            Level07RaptorPatrolSpeed,
            Level07RaptorPatrolAnimSpeed,
            march);
    }

    static float GetLevel07RaptorPatrolLocalZLimit(Transform road)
    {
        var col = road != null ? road.GetComponent<MeshCollider>() : null;
        if (col != null && col.sharedMesh != null)
        {
            var half = col.sharedMesh.bounds.extents.z * 0.75f;
            if (half > 0.05f)
                return half;
        }

        return Level07RaptorPatrolLocalZLimit;
    }

    void ApplyJonremEscortMovement(bool hunting)
    {
        if (npcPhysics == null)
            return;

        if (hunting)
        {
            npcPhysics.SetWalkingEnabled(true);
            npcPhysics.ConfigureForChase(chaseSpeed, chaseAnimSpeed, GetChaseStopDistance());
            return;
        }

        var march = transform.forward;
        march.y = 0f;
        if (march.sqrMagnitude < 0.0001f)
            march = Vector3.right;
        else
            march.Normalize();

        npcPhysics.SetWalkingEnabled(true);
        npcPhysics.ConfigureForJonremEscort(
            npcPhysics.GetWalkSpeed(),
            npcPhysics.GetAnimatorWalkSpeed(),
            GetChaseStopDistance(),
            0.35f,
            march);
    }

    bool IsLevel01JonremEscort() =>
        DutzGiantBossNames.IsJonremEscort(gameObject.name) && DutzCollectibleProgress.IsLevel01;

    void HoldJonremEscortAtSpawn()
    {
        npcPhysics?.ClearChaseTarget();
        npcPhysics?.SetWalkingEnabled(false);

        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetFloat(Animator.StringToHash("Speed_f"), 0f);
    }

    float GetChaseStopDistance()
    {
        if (UsesLevel03ChaseTuning())
            return chaseStopDistance;

        if (DutzGiantBossNames.IsTrililing(gameObject.name))
            return 4.5f;

        if (DutzGiantBossNames.IsJonremEscort(gameObject.name))
            return chaseStopDistance;

        return 2f;
    }

    bool IsPlayerWithinWakeRange()
    {
        if (player == null)
            return false;

        var playerFlat = player.transform.position;
        playerFlat.y = 0f;
        var selfFlat = transform.position;
        selfFlat.y = 0f;
        var pivotDistSq = (playerFlat - selfFlat).sqrMagnitude;
        var wakeDistSq = wakeDistance * wakeDistance;
        if (pivotDistSq > wakeDistSq * 1.44f)
            return false;

        if (wakeColliders == null || wakeColliders.Length == 0)
            CacheWakeColliders();

        var closestPoint = transform.position;
        closestPoint.y = 0f;
        var hasBody = false;
        var closestDistSq = float.MaxValue;

        foreach (var col in wakeColliders)
        {
            if (col == null || !col.enabled)
                continue;

            var onBody = col.ClosestPoint(player.transform.position);
            onBody.y = 0f;
            var distSq = (onBody - playerFlat).sqrMagnitude;
            if (distSq >= closestDistSq)
                continue;

            closestDistSq = distSq;
            closestPoint = onBody;
            hasBody = true;
        }

        if (!hasBody)
        {
            var delta = playerFlat - closestPoint;
            return delta.sqrMagnitude <= wakeDistance * wakeDistance;
        }

        var gap = playerFlat - closestPoint;
        return gap.sqrMagnitude <= wakeDistSq;
    }

    /// <summary>Level 3 finale — end E-TOL keeps chasing once Highway 6 is reached.</summary>
    public void WakeForLevel03Finale()
    {
        awakened = true;
        huntImmediately = true;
    }

    public void ApplyPunchStun(float durationSeconds)
    {
        if (durationSeconds <= 0f)
            return;

        punchStunUntil = Mathf.Max(punchStunUntil, Time.time + durationSeconds);
        npcPhysics?.ClearChaseTarget();
        npcPhysics?.SetWalkingEnabled(false);

        var anim = GetComponent<Animator>();
        if (anim == null)
            return;

        anim.SetFloat(Animator.StringToHash("Speed_f"), 0f);
        if (punchStunAnimatorFrozen)
            return;

        punchStunAnimatorSpeedRestore = anim.speed > 0.001f ? anim.speed : 1f;
        anim.speed = 0f;
        punchStunAnimatorFrozen = true;
    }

    public bool IsPunchStunned => Time.time < punchStunUntil;

    void Update()
    {
        if (!IsPunchStunned)
            RestoreAnimatorAfterPunchStun();
    }

    void RestoreAnimatorAfterPunchStun()
    {
        if (!punchStunAnimatorFrozen)
            return;

        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.speed = punchStunAnimatorSpeedRestore;

        punchStunAnimatorFrozen = false;
        npcPhysics?.SetWalkingEnabled(true);
    }

    /// <summary>Unfreeze animator after punch stun — also used when the giant dies.</summary>
    public void RestoreAnimatorAfterPunchStunPublic() => RestoreAnimatorAfterPunchStun();

    void Start()
    {
        player = DutzPlayerController.Instance;

        if (DutzGiantHeadTopCollider.UsesGiantHeadColliders(gameObject.name))
            StartCoroutine(EnsureHeadCollidersAfterRig());
    }

    IEnumerator EnsureHeadCollidersAfterRig()
    {
        // Caricature rig + boss face run in Awake/Start; wait one frame so head scale is final.
        yield return null;
        Physics.SyncTransforms();
        DutzHippieBiteCollider.EnsureTrililingSolidCollider(gameObject);
        DutzGiantHeadTopCollider.EnsureOnGiant(gameObject);
        CacheWakeColliders();
    }

    /// <summary>Sleep at spawn until the player enters wake range again.</summary>
    public void ResetOnPlayerRespawn()
    {
        punchStunUntil = 0f;
        RestoreAnimatorAfterPunchStun();

        if (!huntImmediately)
            awakened = false;

        if (npcPhysics == null)
            npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();

        npcPhysics?.ClearChaseTarget();
        chaseConfigured = false;

        if (IsLevel01JonremEscort())
        {
            HoldJonremEscortAtSpawn();
            DutzJonremEscortSpawnLock.RestoreEscort(gameObject);
        }

        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetFloat(Animator.StringToHash("Speed_f"), 0f);
    }

    public void ResetLevel03HighwayState()
    {
        huntImmediately = false;
        awakened = false;
        chaseConfigured = false;
        punchStunUntil = 0f;
        RestoreAnimatorAfterPunchStun();
        enabled = true;
    }

    public static void ResetAllOnPlayerRespawn()
    {
        foreach (var hunter in FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
            hunter.ResetOnPlayerRespawn();
    }

    void FixedUpdate()
    {
        if (npcPhysics == null)
            return;

        if (player == null)
            player = DutzPlayerController.Instance;

        if (player == null || player.ControlsLocked)
        {
            npcPhysics.ClearChaseTarget();
            return;
        }

        if (UsesRouteLockedHighwayChase()
            && (Application.isMobilePlatform || UsesLevel01RouteLockedChase() || UsesLevel02RouteLockedChase())
            && ShouldSkipLevel03Movement())
        {
            npcPhysics.ClearChaseTarget();
            return;
        }

        if (IsPunchStunned)
        {
            npcPhysics.ClearChaseTarget();
            return;
        }

        if (!ShouldHunt())
        {
            npcPhysics.ClearChaseTarget();
            chaseConfigured = false;
            if (IsLevel01JonremEscort())
                HoldJonremEscortAtSpawn();
            else if (DutzGiantBossNames.IsJonremEscort(gameObject.name))
                ApplyJonremEscortMovement(false);
            else if (IsLevel07Straight2Raptor())
                ApplyLevel07RaptorIdlePatrol();
            return;
        }

        // Home-highway leash: do not pursue the player off this giant's own highway.
        if (DutzLevel07GiantHomes.HasHomeHighway(gameObject.name)
            && !DutzLevel07GiantHomes.IsPlayerNearHomeHighway(gameObject.name, player.transform.position))
        {
            npcPhysics.ClearChaseTarget();
            return;
        }

        // Ground bosses stay on the deck — do not chase parachute / airborne flyers into the sky.
        if (IsLevel07GroundOnlyChaseGiant() && IsPlayerAirborneForGroundGiant())
        {
            npcPhysics.ClearChaseTarget();
            // Keep him on the deck even if a prior frame already launched him.
            npcPhysics.SnapFeetToRoad();
            return;
        }

        if (!chaseConfigured)
        {
            if (DutzGiantBossNames.IsJonremEscort(gameObject.name))
                ApplyJonremEscortMovement(true);
            else
                ApplyChaseSettings();
            chaseConfigured = true;
        }
        else if (IsLevel07GroundOnlyChaseGiant())
        {
            // Re-assert ground chase every wake in case something enabled flight.
            npcPhysics.ConfigureForGroundChase(chaseSpeed, chaseAnimSpeed, GetChaseStopDistance());
        }

        npcPhysics.SetChaseTarget(player.transform);
    }

    bool IsPlayerAirborneForGroundGiant()
    {
        if (player == null)
            return false;

        var parachute = player.GetComponent<DutzPlayerParachute>();
        if (parachute != null && parachute.IsParachuteActive)
            return true;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        if (cc.isGrounded)
            return false;

        // High above nearest deck = airborne flyer / long jump — stay put on Straight 6.
        var feet = player.transform.position;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(feet, feet.y, cc, out var deckY)
            && feet.y > deckY + 4f)
            return true;

        return player.VerticalSpeed > 2f;
    }

    bool ShouldHunt()
    {
        if (DutzGiantBossNames.IsJonremEscort(gameObject.name)
            && DutzCollectibleProgress.IsLevel01
            && !DutzGameBootstrap.IsReady)
        {
            return false;
        }

        if (huntImmediately || awakened)
            return true;

        if (player == null)
            return false;

        if (UsesWakeThrottle() && Time.time < nextWakeCheckTime)
        {
            return false;
        }

        if (IsPlayerWithinWakeRange())
        {
            awakened = true;
            if (DutzGiantBossNames.IsJonremPolice(gameObject.name))
                DutzJonremPoliceBehavior.AwakenAllPoliceForChase();
            return true;
        }

        if (UsesWakeThrottle())
            nextWakeCheckTime = Time.time + GetMobileWakeInterval();

        return false;
    }

    bool ShouldSkipLevel03Movement()
    {
        if (huntImmediately || awakened)
            return false;

        var delta = player.transform.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude > Level03SleepDistance * Level03SleepDistance;
    }

    public void RefreshLevel01JonremEscortState()
    {
        if (!IsLevel01JonremEscort())
            return;

        if (huntImmediately || ShouldHunt())
            ApplyJonremEscortMovement(ShouldHunt());
        else
            HoldJonremEscortAtSpawn();
    }

    public void ConfigureForJonremEscort(
        float wakeDistanceMeters,
        float speed,
        float animSpeed,
        float stopDistance,
        bool huntNow,
        bool forceApplyTuning = false)
    {
        if (forceApplyTuning || !IsLevel01JonremEscort())
        {
            wakeDistance = wakeDistanceMeters;
            chaseSpeed = speed;
            chaseAnimSpeed = animSpeed;
            chaseStopDistance = stopDistance;
        }

        huntImmediately = huntNow;

        if (IsLevel01JonremEscort())
            RefreshLevel01JonremEscortState();
        else
            ApplyJonremEscortMovement(huntNow);
    }

    public void AwakenForChase()
    {
        awakened = true;
        RefreshLevel01JonremEscortState();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (huntImmediately)
            return;

        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, wakeDistance);
    }
#endif
}
