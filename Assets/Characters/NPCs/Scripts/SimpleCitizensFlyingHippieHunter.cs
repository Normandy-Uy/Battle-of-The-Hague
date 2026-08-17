using UnityEngine;

/// <summary>Flying small hippies: 3D chase and hunt when the player is on the ground or in the air.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[RequireComponent(typeof(SimpleCitizensFlyingHippie))]
[DefaultExecutionOrder(-189)]
public class SimpleCitizensFlyingHippieHunter : MonoBehaviour
{
    const float HeadScale = 2f;
    const float ChaseSpeed = 7f;
    const float ChaseAnimSpeed = 0.66f;
    const float WakeDistance = 70f;
    const float MaxHuntDistance = 52f;
    const float PlayerAheadAbandonDistance = 8f;
    static readonly Vector3 HighwayForward = Vector3.right;

    [SerializeField] float wakeDistance = WakeDistance;
    [SerializeField] float chaseSpeed = ChaseSpeed;
    [SerializeField] float chaseAnimSpeed = ChaseAnimSpeed;
    [SerializeField] float maxHuntDistance = MaxHuntDistance;
    [SerializeField] float playerAheadAbandonDistance = PlayerAheadAbandonDistance;

    SimpleCitizensNpcPhysics npcPhysics;
    DutzPlayerController player;
    bool awakened;

    public static void EnsureOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null || !SimpleCitizensFlyingHippie.IsFlyingHippieName(physics.gameObject.name))
            return;

        var go = physics.gameObject;
        ScaleHead(go.transform);
        DutzSmallAddictScale.Apply(go);
        go.GetComponent<SimpleCitizensFlyingHippie>()?.ApplyFlightPhysics();

        if (go.GetComponent<SimpleCitizensFlyingHippieHunter>() == null)
            go.AddComponent<SimpleCitizensFlyingHippieHunter>();

        var groundHunter = go.GetComponent<SimpleCitizensHippieHunter>();
        if (groundHunter != null)
            groundHunter.enabled = false;
    }

    void Awake()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        GetComponent<SimpleCitizensFlyingHippie>()?.ApplyFlightPhysics();
        ScaleHead(transform);
        DutzSmallAddictScale.Apply(gameObject);

        var groundHunter = GetComponent<SimpleCitizensHippieHunter>();
        if (groundHunter != null)
            groundHunter.enabled = false;
    }

    void Start() => player = DutzPlayerController.Instance;

    public void WakeOnPlayerRespawn() => awakened = true;

    public static void WakeAllOnPlayerRespawn()
    {
        foreach (var hunter in FindObjectsOfType<SimpleCitizensFlyingHippieHunter>())
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
            npcPhysics.ConfigureForFlightPatrol(
                SimpleCitizensFlyingHippie.PatrolCruiseSpeed,
                SimpleCitizensFlyingHippie.PatrolAnimSpeed);
            return;
        }

        npcPhysics.ConfigureForFlightChase(chaseSpeed, chaseAnimSpeed);
        npcPhysics.SetChaseTarget(player.transform);
    }

    bool ShouldHunt()
    {
        if (player == null)
            return false;

        if (IsPlayerTooFarAheadOnRoad())
        {
            awakened = false;
            return false;
        }

        var delta = player.transform.position - transform.position;
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

    bool IsPlayerTooFarAheadOnRoad()
    {
        if (player == null)
            return false;

        var aheadOnRoad = player.transform.position.x - transform.position.x;
        if (aheadOnRoad <= playerAheadAbandonDistance)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            var playerRoadSpeed = Vector3.Dot(HighwayForward, cc.velocity);
            if (playerRoadSpeed > 4f && aheadOnRoad > playerAheadAbandonDistance * 0.5f)
                return true;
        }

        return aheadOnRoad > playerAheadAbandonDistance;
    }

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
