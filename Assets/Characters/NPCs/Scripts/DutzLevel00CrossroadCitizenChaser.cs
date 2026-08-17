using UnityEngine;

/// <summary>
/// Level 00 crossroad citizens — spawn at the Senate end, march towards Bridge 1, chase the player.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[DefaultExecutionOrder(-188)]
public class DutzLevel00CrossroadCitizenChaser : MonoBehaviour
{
    public const float ChaseSpeed = 20f;
    public const float ChaseAnimSpeed = 3.2f;
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
        EnsureOnCitizen(duplicate);
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
        physics.ConfigureForGroundChase(ChaseSpeed, ChaseAnimSpeed, ChaseStopDistance);
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
            npcPhysics.ConfigureForGroundChase(ChaseSpeed, ChaseAnimSpeed, ChaseStopDistance);
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
