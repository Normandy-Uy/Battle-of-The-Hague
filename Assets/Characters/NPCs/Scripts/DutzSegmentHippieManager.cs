using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Teleports a pool of 7 small hippies using Inspector-authored poses as Dutz advances.
/// Default: segment 1 on Play start; segments 2–6 when crossing each segment boundary.
/// Level 07: pool stays on Highway Straight 2 only.
/// </summary>
[DefaultExecutionOrder(-200)]
public class DutzSegmentHippieManager : MonoBehaviour
{
    static readonly string[] DefaultSegmentNames =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        "Highway Straight 6"
    };

    // Level 07: addicts only spawn / stay on Highway Straight 2.
    static readonly string[] Level07SegmentNames =
    {
        "Highway Straight 2"
    };

    [SerializeField] Transform poolRoot;
    [SerializeField] DutzSegmentHippieTeleportProfile teleportProfile;
    [SerializeField] SimpleCitizensNpcPhysics[] hippiePool;

    Vector3 spawnRef;
    Vector3 travelForward;
    List<DutzHighwayDeckSampler.SegmentPath> segments = new List<DutzHighwayDeckSampler.SegmentPath>();
    bool[] teleportedToSegment;
    int currentSegmentIndex;
    DutzPlayerController player;
    string[] activeSegmentNames;

    static string[] ResolveSegmentNames() =>
        DutzCollectibleProgress.IsLevel07 ? Level07SegmentNames : DefaultSegmentNames;

    public static void EnsureFromBoot()
    {
        if (DutzCollectibleProgress.IsLevel07)
            return;

        if (GameObject.Find(DutzSegmentHippieIdentity.ManagerObjectName) != null)
            return;

        var poolRoot = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
        if (poolRoot == null)
            return;

        var go = new GameObject(DutzSegmentHippieIdentity.ManagerObjectName);
        go.AddComponent<DutzSegmentHippieManager>();
    }

    void Awake()
    {
        ResolvePool();
        ResolveTeleportProfile();

        if (!InitializeTrack())
        {
            enabled = false;
            return;
        }

        EnsurePoolNpcComponents();
        DutzCrocodilePoolMember.EnsureCrocodileScale();
        PlacePoolOnCurrentSegment();
    }

    void Start()
    {
        player = DutzPlayerController.Instance;
    }

    public static void SyncPoolOnPlayerRespawn()
    {
        var manager = FindObjectOfType<DutzSegmentHippieManager>();
        if (manager != null)
            manager.ResyncPoolToPlayerSegment();
    }

    public static void RespawnPoolMemberToCurrentSegment(SimpleCitizensNpcPhysics physics)
    {
        var manager = FindObjectOfType<DutzSegmentHippieManager>();
        if (manager != null)
            manager.RespawnMemberToCurrentSegment(physics);
    }

    public void ResyncPoolToPlayerSegment()
    {
        if (!enabled || segments.Count == 0 || hippiePool == null || hippiePool.Length == 0)
            return;

        ResolvePool();
        ResolveTeleportProfile();

        if (!HasTeleportData())
            return;

        player = DutzPlayerController.Instance;
        if (player == null)
            return;

        var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(
            spawnRef, player.transform.position, travelForward);

        var activeProfile = 0;
        for (var i = 0; i < segments.Count; i++)
        {
            if (playerAlong >= segments[i].StartAlong - 0.5f)
                activeProfile = segments[i].ProfileIndex;
        }

        if (teleportedToSegment == null || teleportedToSegment.Length != segments.Count)
            teleportedToSegment = new bool[segments.Count];

        for (var i = 0; i < teleportedToSegment.Length; i++)
            teleportedToSegment[i] = i <= activeProfile;

        PlacePoolOnCurrentSegment(activeProfile);
    }

    void PlacePoolOnCurrentSegment(int segmentIndex = 0)
    {
        var maxIndex = Mathf.Max(0, segments.Count - 1);
        segmentIndex = Mathf.Clamp(segmentIndex, 0, maxIndex);

        if (teleportedToSegment == null || teleportedToSegment.Length != segments.Count)
            teleportedToSegment = new bool[segments.Count];

        for (var i = 0; i < teleportedToSegment.Length; i++)
            teleportedToSegment[i] = i <= segmentIndex;

        TeleportPoolToSegment(segmentIndex);
    }

    void Update()
    {
        if (segments.Count == 0 || hippiePool == null || hippiePool.Length == 0)
            return;

        if (player == null)
            player = DutzPlayerController.Instance;

        if (player == null || player.ControlsLocked)
            return;

        var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(
            spawnRef, player.transform.position, travelForward);

        while (currentSegmentIndex < segments.Count - 1)
        {
            var currentPath = segments[currentSegmentIndex];
            if (playerAlong < currentPath.EndAlong - 0.5f)
                break;

            var nextProfile = currentSegmentIndex + 1;
            if (nextProfile >= segments.Count)
                break;

            if (teleportedToSegment[nextProfile])
            {
                currentSegmentIndex = nextProfile;
                continue;
            }

            TeleportPoolToSegment(nextProfile);
            teleportedToSegment[nextProfile] = true;
        }
    }

    void ResolvePool()
    {
        if (poolRoot == null)
        {
            var root = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
            if (root != null)
                poolRoot = root.transform;
        }

        if (hippiePool == null || hippiePool.Length == 0)
        {
            var list = new List<SimpleCitizensNpcPhysics>();
            if (poolRoot != null)
            {
                foreach (Transform child in poolRoot)
                {
                    if (!DutzSegmentHippieIdentity.IsPoolHippie(child.name))
                        continue;

                    var physics = child.GetComponent<SimpleCitizensNpcPhysics>();
                    if (physics == null)
                        physics = child.GetComponentInChildren<SimpleCitizensNpcPhysics>();

                    if (physics != null)
                        list.Add(physics);
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
            hippiePool = list.ToArray();
        }
    }

    void ResolveTeleportProfile()
    {
        if (teleportProfile != null)
            return;

        if (poolRoot != null)
            teleportProfile = poolRoot.GetComponent<DutzSegmentHippieTeleportProfile>();

        if (teleportProfile == null)
        {
            var pool = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
            if (pool != null)
                teleportProfile = pool.GetComponent<DutzSegmentHippieTeleportProfile>();
        }
    }

    bool InitializeTrack()
    {
        ResolvePool();
        ResolveTeleportProfile();

        if (hippiePool == null || hippiePool.Length == 0)
        {
            Debug.LogWarning("[Dutz] Segment hippie pool is empty — manager disabled.");
            return false;
        }

        if (!HasTeleportData())
        {
            Debug.LogError("[Dutz] Segment hippie teleport data missing — add DutzSegmentHippieTeleportSlots on pool hippies.");
            return false;
        }

        spawnRef = GetSpawnReference();
        travelForward = GetTravelForward(spawnRef);
        activeSegmentNames = ResolveSegmentNames();
        segments = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(activeSegmentNames, spawnRef, travelForward);

        if (segments.Count == 0)
        {
            Debug.LogError("[Dutz] No highway segments found for hippie teleport pool.");
            return false;
        }

        teleportedToSegment = new bool[segments.Count];
        return true;
    }

    void TeleportPoolToSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= segments.Count)
            return;

        // Pose slots are always the 6-highway profile. Level07 maps its only runtime
        // segment (Straight 2) onto profile slot 1 so chase matches Level02 Straight 2.
        var poseSegmentIndex = GetPoseSegmentIndex(segmentIndex);
        if (poseSegmentIndex < 0 || poseSegmentIndex >= DutzSegmentHippieTeleportProfile.SegmentCount)
            return;

        currentSegmentIndex = segmentIndex;

        var names = activeSegmentNames ?? ResolveSegmentNames();
        var segmentName = segmentIndex < names.Length
            ? names[segmentIndex]
            : $"segment {segmentIndex + 1}";

        var count = Mathf.Min(hippiePool.Length, DutzSegmentHippieTeleportProfile.HippieCount);
        for (var i = 0; i < count; i++)
            TeleportHippieToSegment(i, poseSegmentIndex);

        Physics.SyncTransforms();
        for (var i = 0; i < count; i++)
        {
            var physics = hippiePool[i];
            if (physics != null)
                DutzCrocodilePoolMember.RefreshCombatColliders(physics.gameObject);
        }

        Debug.Log($"[Dutz] Segment hippies teleported to authored positions on {segmentName}.");
    }

    public void RespawnMemberToCurrentSegment(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null || hippiePool == null || hippiePool.Length == 0)
            return;

        ResolvePool();

        for (var i = 0; i < hippiePool.Length; i++)
        {
            if (hippiePool[i] != physics)
                continue;

            TeleportHippieToSegment(i, GetPoseSegmentIndex(currentSegmentIndex));
            Physics.SyncTransforms();
            return;
        }
    }

    int GetPoseSegmentIndex(int runtimeSegmentIndex)
    {
        if (DutzCollectibleProgress.IsLevel07)
            return 1; // Highway Straight 2 in the authored 6-slot profile

        return runtimeSegmentIndex;
    }

    void TeleportHippieToSegment(int hippieIndex, int poseSegmentIndex)
    {
        if (hippieIndex < 0 || hippieIndex >= hippiePool.Length)
            return;

        var physics = hippiePool[hippieIndex];
        if (physics == null)
            return;

        var pose = GetTeleportPose(hippieIndex, poseSegmentIndex);
        var world = pose.position;
        if (DutzRoadGround.TrySampleRoadDeckForPlacement(world, world.y, physics.GetComponent<Collider>(), out var deckY))
            world.y = deckY;

        var rotation = pose.Rotation;

        physics.ClearChaseTarget();
        physics.SetWalkingEnabled(false);
        physics.transform.SetPositionAndRotation(world, rotation);

        var rb = physics.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = world;
            rb.rotation = rotation;
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        physics.SnapFeetToRoad();
        physics.SetWalkingEnabled(true);

        foreach (var renderer in physics.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        var hunter = physics.GetComponent<SimpleCitizensHippieHunter>();
        if (hunter != null)
        {
            // Level02 Straight 2 uses profile index 1 → hunt immediately. Keep that on Level07.
            var huntSegmentIndex = DutzCollectibleProgress.IsLevel07
                ? 1
                : poseSegmentIndex;
            hunter.ConfigureSegmentPoolHunt(huntSegmentIndex);
        }
    }

    bool HasTeleportData()
    {
        if (hippiePool != null)
        {
            foreach (var physics in hippiePool)
            {
                if (physics != null && physics.GetComponent<DutzSegmentHippieTeleportSlots>() != null)
                    return true;
            }
        }

        ResolveTeleportProfile();
        return teleportProfile != null && teleportProfile.HasValidData;
    }

    DutzSegmentHippieTeleportProfile.TeleportPose GetTeleportPose(int hippieIndex, int segmentIndex)
    {
        if (hippieIndex >= 0 && hippieIndex < hippiePool.Length)
        {
            var slots = hippiePool[hippieIndex].GetComponent<DutzSegmentHippieTeleportSlots>();
            if (slots != null)
                return slots.GetPose(segmentIndex);
        }

        ResolveTeleportProfile();
        if (teleportProfile != null)
            return teleportProfile.GetPose(hippieIndex, segmentIndex);

        return default;
    }

    void EnsurePoolNpcComponents()
    {
        foreach (var physics in hippiePool)
        {
            if (physics == null)
                continue;

            SimpleCitizensHippieHunter.EnsureOnNpc(physics);
            SimpleCitizensHippieBiter.EnsureOnNpc(physics);
            SimpleCitizensHippieSounds.EnsureOnNpc(physics);
            SimpleCitizensNpcRespawn.EnsureOnNpc(physics);
        }
    }

    static Vector3 GetSpawnReference()
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
        {
            foreach (var controller in FindObjectsOfType<DutzPlayerController>(true))
            {
                player = controller;
                break;
            }
        }

        if (player != null)
            return player.transform.position;

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackStart, out _))
            return trackStart;

        return Vector3.zero;
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
        {
            foreach (var controller in FindObjectsOfType<DutzPlayerController>(true))
            {
                player = controller;
                break;
            }
        }

        if (player != null)
        {
            var playerForward = player.transform.forward;
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude > 0.0001f)
                return playerForward.normalized;
        }

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        forward.y = 0f;
        return forward.normalized;
    }
}
