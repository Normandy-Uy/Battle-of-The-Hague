using UnityEngine;

/// <summary>
/// Sends SimpleCitizens NPCs back to their starting position when they fall off the road.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[DefaultExecutionOrder(-90)]
public class SimpleCitizensNpcRespawn : MonoBehaviour
{
    [Header("Fall detection")]
    [SerializeField] float fallYThreshold = -2f;
    [SerializeField] float longFallStartY = 4f;
    [SerializeField] float longFallSeconds = 2.5f;
    [SerializeField] bool respawnEnabled = true;

    [Header("Spawn point (auto-set at play start)")]
    [SerializeField] Vector3 spawnPosition;
    [SerializeField] Quaternion spawnRotation = Quaternion.identity;
    [SerializeField] bool spawnPointSet;

    SimpleCitizensNpcPhysics npcPhysics;
    float ungroundedTimer;

    public static void EnsureOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null || physics.GetComponent<DutzPlayerController>() != null)
            return;

        if (physics.GetComponent<SimpleCitizensNpcRespawn>() == null)
            physics.gameObject.AddComponent<SimpleCitizensNpcRespawn>();
    }

    void Awake()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();

        if (IsGiantHippieNpc(gameObject.name))
            respawnEnabled = false;
    }

    static bool IsGiantHippieNpc(string objectName) =>
        DutzGiantBossNames.IsMidTrackGiant(objectName)
        || DutzGiantBossNames.IsTrililing(objectName)
        || DutzGiantBossNames.IsJonremEscort(objectName)
        || (DutzCollectibleProgress.IsLevel03Gameplay && DutzCollectibleProgress.IsLevel03Giant(objectName));

    void Start()
    {
        if (DutzSegmentHippieIdentity.IsPoolHippie(gameObject.name))
            return;

        if (!spawnPointSet)
            RecordSpawnPoint();
    }

    void Update()
    {
        if (!respawnEnabled || npcPhysics == null)
            return;

        var poolHippie = DutzSegmentHippieIdentity.IsPoolHippie(gameObject.name);
        if (!poolHippie && !spawnPointSet)
            return;

        var pos = transform.position;

        if (pos.y < fallYThreshold)
        {
            RespawnToStart();
            return;
        }

        if (npcPhysics.IsGroundedOnRoad() || pos.y >= longFallStartY)
        {
            ungroundedTimer = 0f;
            return;
        }

        ungroundedTimer += Time.deltaTime;
        if (ungroundedTimer >= longFallSeconds)
            RespawnToStart();
    }

    public void RecordSpawnPoint()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        spawnPointSet = true;
    }

    public void SetLockedSpawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
        spawnPointSet = true;
    }

    public bool HasBakedSpawnPoint => spawnPointSet;

    public void GetBakedSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        position = spawnPosition;
        rotation = spawnRotation;
    }

    public void RespawnToStart()
    {
        if (DutzSegmentHippieIdentity.IsPoolHippie(gameObject.name))
        {
            if (npcPhysics == null)
                npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();

            DutzSegmentHippieManager.RespawnPoolMemberToCurrentSegment(npcPhysics);
            return;
        }

        if (!spawnPointSet)
            RecordSpawnPoint();

        if (DutzCollectibleProgress.IsLevel01
            && DutzGiantBossNames.IsJonremEscort(gameObject.name)
            && DutzJonremEscortSpawnLock.TryGetPose(gameObject, out var lockedPos, out var lockedRot))
        {
            spawnPosition = lockedPos;
            spawnRotation = lockedRot;
        }

        if (npcPhysics == null)
            npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();

        ungroundedTimer = 0f;
        if (npcPhysics != null)
        {
            npcPhysics.SetWalkingEnabled(true);
            npcPhysics.ClearChaseTarget();
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
        }

        if (npcPhysics != null)
            npcPhysics.SnapFeetToRoad();
        Physics.SyncTransforms();
    }

    /// <summary>Called when the player respawns — restores all NPCs to their recorded spawn points.</summary>
    public static void RespawnAllToSpawn()
    {
        foreach (var biter in FindObjectsOfType<SimpleCitizensHippieBiter>())
            biter.ResetOnPlayerRespawn();

        SimpleCitizensGiantHippieHunter.ResetAllOnPlayerRespawn();

        foreach (var npc in FindObjectsOfType<SimpleCitizensNpcRespawn>(true))
        {
            if (DutzSegmentHippieIdentity.IsPoolHippie(npc.gameObject.name))
                continue;

            npc.RespawnToStart();
        }

        DutzSegmentHippieManager.SyncPoolOnPlayerRespawn();

        SimpleCitizensHippieHunter.WakeAllOnPlayerRespawn();
        SimpleCitizensFlyingHippieHunter.WakeAllOnPlayerRespawn();
    }
}
