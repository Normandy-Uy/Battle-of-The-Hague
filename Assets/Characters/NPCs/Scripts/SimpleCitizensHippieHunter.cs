using UnityEngine;

/// <summary>
/// Regular hippies: always chase the player when active.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[DefaultExecutionOrder(-190)]
public class SimpleCitizensHippieHunter : MonoBehaviour
{
    const string GiantHippieName = DutzGiantBossNames.Trililing;
    const string MidGiantHippieName = DutzGiantBossNames.GeneralRook;
    const string SmallHippiePrefix = "SimpleCitizens_Hippie_Black";
    const string ExtraHippiePrefix = "SimpleCitizens_Hippie_Extra_";
    const string NearSpawnHippiePrefix = "SimpleCitizens_Hippie_NearSpawn_";
    const float HeadScale = 2f;
    const float SmallHippieChaseSpeed = 7f;
    const float SmallHippieChaseAnimSpeed = 0.66f;
    public const float SmallHippieWakeDistance = 70f;
    public const float SmallHippieMaxHuntDistance = 52f;
    const float SmallHippieAheadAbandonDistance = 8f;
    public const float SmallHippieMaxVerticalChaseMeters = 3.5f;

    [SerializeField] bool huntImmediately;
    [SerializeField] float wakeDistance = SmallHippieWakeDistance;
    [SerializeField] float chaseSpeed = SmallHippieChaseSpeed;
    [SerializeField] float chaseAnimSpeed = SmallHippieChaseAnimSpeed;
    [SerializeField] float maxHuntDistance = SmallHippieMaxHuntDistance;
    [Tooltip("Stop chasing when the player is this far ahead on the road (+X).")]
    [SerializeField] float playerAheadAbandonDistance = SmallHippieAheadAbandonDistance;

    SimpleCitizensNpcPhysics npcPhysics;
    DutzPlayerController player;
    bool awakened;

    public static void EnsureOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null)
            return;

        var go = physics.gameObject;
        if (SimpleCitizensNpcPhysics.IsLevel00CrowdWalker(go))
            return;

        if (DutzGiantBossNames.IsTrililing(go.name) || DutzGiantBossNames.IsMidTrackGiant(go.name) || !IsSmallHippie(go.name))
            return;

        if (SimpleCitizensFlyingHippie.IsFlyingHippieName(go.name))
            return;

        ScaleHead(go.transform);
        DutzSmallAddictScale.Apply(go);
        physics.SnapFeetToRoad();

        if (go.GetComponent<SimpleCitizensHippieHunter>() == null)
            go.AddComponent<SimpleCitizensHippieHunter>();

        var giantHunter = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giantHunter != null)
            giantHunter.enabled = false;
    }

    void Awake()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        ApplySmallHippieHuntDefaults();
        ScaleHead(transform);
        DutzSmallAddictScale.Apply(gameObject);
        ApplyChaseSettings();

        if (npcPhysics != null)
            npcPhysics.SnapFeetToRoad();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplySmallHippieHuntDefaults();
    }
#endif

    void ApplySmallHippieHuntDefaults()
    {
        chaseSpeed = DutzDifficulty.GetSmallHippieChaseSpeed();
        chaseAnimSpeed = DutzDifficulty.GetSmallHippieChaseAnimSpeed();
        wakeDistance = SmallHippieWakeDistance;
        maxHuntDistance = SmallHippieMaxHuntDistance;
        playerAheadAbandonDistance = SmallHippieAheadAbandonDistance;
    }

    public void RefreshDifficultySpeeds()
    {
        ApplySmallHippieHuntDefaults();
        ApplyChaseSettings();
    }

    public static void ApplyDifficultyToAllSmallAddicts()
    {
        foreach (var hunter in FindObjectsOfType<SimpleCitizensHippieHunter>(true))
        {
            if (hunter == null || !hunter.isActiveAndEnabled)
                continue;

            if (!IsSmallHippie(hunter.gameObject.name) && !DutzCrocodilePoolMember.IsCrocodile(hunter.gameObject))
                continue;

            hunter.RefreshDifficultySpeeds();
        }
    }

    void Start()
    {
        player = DutzPlayerController.Instance;
    }

    /// <summary>Segment pool: segment 1 waits for wake distance; later segments chase immediately after teleport.</summary>
    public void ConfigureSegmentPoolHunt(int segmentIndex)
    {
        if (!IsSegmentPoolHippie(gameObject.name))
            return;

        huntImmediately = segmentIndex > 0;
        awakened = segmentIndex > 0;
    }

    /// <summary>Re-arm chase after player respawn (death dialog clears awakened when far on the road).</summary>
    public void WakeOnPlayerRespawn()
    {
        if (IsSegmentPoolHippie(gameObject.name))
            return;

        awakened = true;
    }

    public static void WakeAllOnPlayerRespawn()
    {
        foreach (var hunter in FindObjectsOfType<SimpleCitizensHippieHunter>())
            hunter.WakeOnPlayerRespawn();
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

        if (!ShouldHunt())
        {
            npcPhysics.ClearChaseTarget();
            return;
        }

        ApplyChaseSettings();
        npcPhysics.SetChaseTarget(player.transform);
    }

    void ApplyChaseSettings()
    {
        if (npcPhysics == null)
            return;

        npcPhysics.ConfigureForChase(chaseSpeed, chaseAnimSpeed);
    }

    bool ShouldHunt()
    {
        if (player == null)
            return false;

        var delta = player.transform.position - transform.position;
        delta.y = 0f;

        if (IsPlayerTooFarAheadOnRoad())
        {
            awakened = false;
            return false;
        }

        if (IsPlayerTooFarAboveToChase())
        {
            awakened = false;
            return false;
        }

        // Level07 Straight-2 / Straight-3 addicts are fixed-spawn (no segment teleport). huntImmediately must
        // still respect wake/max range — otherwise they march off their authored deck on Play.
        var level07PitchedAddict = DutzCollectibleProgress.IsLevel07
            && (DutzLevel07Straight3AddictSpawner.IsStraight2Addict(gameObject.name)
                || DutzLevel07Straight3AddictSpawner.IsStraight3Addict(gameObject.name));

        if (huntImmediately && !level07PitchedAddict)
            return true;

        var maxDist = Mathf.Max(maxHuntDistance, wakeDistance);
        if (delta.sqrMagnitude > maxDist * maxDist)
        {
            awakened = false;
            return false;
        }

        if (awakened)
            return true;

        if (delta.sqrMagnitude <= wakeDistance * wakeDistance)
        {
            awakened = true;
            return true;
        }

        return false;
    }

    /// <summary>If the player is far ahead along the highway, stop tailing after a pass.</summary>
    bool IsPlayerTooFarAheadOnRoad()
    {
        if (player == null)
            return false;

        var travelForward = GetTravelForward();
        var aheadOnRoad = Vector3.Dot(
            player.transform.position - transform.position,
            travelForward);

        if (aheadOnRoad <= playerAheadAbandonDistance)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            var playerRoadSpeed = Vector3.Dot(travelForward, cc.velocity);
            if (playerRoadSpeed > 4f && aheadOnRoad > playerAheadAbandonDistance * 0.5f)
                return true;
        }

        return aheadOnRoad > playerAheadAbandonDistance;
    }

    bool IsPlayerTooFarAboveToChase()
    {
        if (player == null)
            return false;

        if (!DutzCrocodilePoolMember.IsCrocodile(gameObject) && !IsSmallHippie(gameObject.name))
            return false;

        // Level07 Straight-2 / Straight-3 addicts: allow chase while player is parachuting / above the deck.
        if (DutzCollectibleProgress.IsLevel07
            && (DutzLevel07Straight3AddictSpawner.IsStraight2Addict(gameObject.name)
                || DutzLevel07Straight3AddictSpawner.IsStraight3Addict(gameObject.name)))
            return false;

        var addictFeetY = DutzNpcFeet.GetLowestWorldY(gameObject);
        var playerFeetY = player.transform.position.y;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            playerFeetY -= cc.height * 0.5f - cc.center.y;

        return playerFeetY - addictFeetY > SmallHippieMaxVerticalChaseMeters;
    }

    Vector3 GetTravelForward()
    {
        if (player != null)
        {
            var playerForward = player.transform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude > 0.0001f)
                return playerForward.normalized;
        }

        var forward = DutzHighwayDirection.GetReferenceForward();
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.right;
    }

    static bool IsSmallHippie(string objectName) =>
        objectName.StartsWith(SmallHippiePrefix)
        || objectName.StartsWith(ExtraHippiePrefix)
        || objectName.StartsWith(NearSpawnHippiePrefix)
        || IsSegmentPoolHippie(objectName);

    static bool IsSegmentPoolHippie(string objectName) =>
        !string.IsNullOrEmpty(objectName) && objectName.StartsWith("DutzSegmentHippie_");

    static void ScaleHead(Transform root)
    {
        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name != "Head_jnt")
                continue;

            bone.localScale = Vector3.one * HeadScale;
            return;
        }
    }
}
