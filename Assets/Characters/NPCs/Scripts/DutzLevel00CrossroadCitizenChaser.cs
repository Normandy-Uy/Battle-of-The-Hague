using UnityEngine;

/// <summary>
/// Level 00 crossroad citizens — spawn at the Senate end, march towards Bridge 1, chase the player.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[DefaultExecutionOrder(-188)]
public class DutzLevel00CrossroadCitizenChaser : MonoBehaviour
{
    public static float ChaseSpeed => DutzDifficulty.GetCrossroadChaseSpeed();

    public static float ChaseAnimSpeed => DutzDifficulty.GetCrossroadChaseAnimSpeed();
    public const float ChaserScaleMultiplier = 2f;
    public const float ChaserMassMultiplier = 2f;
    public const float ChaserPushMultiplier = 2f;
    const float ChaseStopDistance = 0.45f;

    SimpleCitizensNpcPhysics npcPhysics;
    DutzPlayerController player;

    public static bool IsCrossroadChasingCitizen(GameObject go) =>
        go != null && go.GetComponent<DutzLevel00CrossroadCitizenChaser>() != null;

    /// <summary>Crossroad duplicate — strip march scripts, then chase the player on the deck.</summary>
    public static void EnsureOnCrossroadDuplicate(GameObject duplicate)
    {
        if (duplicate == null)
            return;

        StripCrowdWalkerComponents(duplicate);
        ApplyChaserBulk(duplicate);
        EnsureOnCitizen(duplicate);
    }

    /// <summary>Crossroad chasers are fewer but bulkier — 2× scale/mass for stronger edge push.</summary>
    public static void ApplyChaserBulk(GameObject duplicate)
    {
        if (duplicate == null || Mathf.Approximately(ChaserScaleMultiplier, 1f))
            return;

        duplicate.transform.localScale *= ChaserScaleMultiplier;
    }

    public static void EnsureOnCitizen(GameObject citizen)
    {
        if (citizen == null || DutzLevel00CrowdWalker.IsCrowdWalker(citizen))
            return;

        if (!DutzLevel00StaticCrowdColliders.IsStaticCrowdNpc(citizen))
            return;

        var physics = citizen.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = citizen.AddComponent<SimpleCitizensNpcPhysics>();

        physics.Apply();
        physics.ConfigureForGroundChase(
            ChaseSpeed,
            ChaseAnimSpeed,
            ChaseStopDistance * ChaserScaleMultiplier);
        physics.MultiplyMass(ChaserMassMultiplier);
        physics.SnapFeetToRoad();

        if (citizen.GetComponent<DutzLevel00CrossroadCitizenChaser>() == null)
            citizen.AddComponent<DutzLevel00CrossroadCitizenChaser>();
    }

    public static void RemoveFromCitizen(GameObject citizen)
    {
        if (citizen == null)
            return;

        var chaser = citizen.GetComponent<DutzLevel00CrossroadCitizenChaser>();
        if (chaser != null)
            Destroy(chaser);

        var physics = citizen.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
            Destroy(physics);

        DutzLevel00StaticCrowdColliders.ApplyColliderOnly(citizen);
    }

    static void StripCrowdWalkerComponents(GameObject go)
    {
        if (go == null)
            return;

        foreach (var walker in go.GetComponents<DutzLevel00CrowdWalker>())
        {
            if (walker != null)
                Object.DestroyImmediate(walker);
        }

        foreach (var march in go.GetComponents<DutzLevel00CrowdWalkerPhysics>())
        {
            if (march != null)
                Object.DestroyImmediate(march);
        }
    }

    void Awake()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        if (npcPhysics != null)
            npcPhysics.ConfigureForGroundChase(
                ChaseSpeed,
                ChaseAnimSpeed,
                ChaseStopDistance * ChaserScaleMultiplier);
    }

    void Start()
    {
        player = DutzPlayerController.Instance;
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

        npcPhysics.SetWalkingEnabled(true);
        npcPhysics.SetChaseTarget(player.transform);
    }
}
