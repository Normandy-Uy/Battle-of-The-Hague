using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Level 00 — when the player reaches Highway Cross Road, duplicate Bridge 1 crowd NPCs onto
/// saved scene spawn slots (Level00CrossroadChaseSpawns). Up to 12 bulk chasers chase the player.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-195)]
public class DutzLevel00CrowdCrossroadRespawn : MonoBehaviour
{
    const string ManagerName = "DutzLevel00CrowdCrossroadRespawn";
    const string CrossroadSegmentName = "Highway Cross Road";
    const string WalkersRootName = "Level00CrowdWalkers";
    const string CitizensRootName = "Level00CrowdCitizens";
    const string ChasersRootName = "Level00CrowdCrossroadChasers";
    const string SpawnSlotsRootName = "Level00CrossroadChaseSpawns";
    public const int MaxActiveChasers = 12;
    const float TriggerMarginMeters = 0.5f;
    const float ProximityFallbackMeters = 8f;

    struct CrowdSnapshot
    {
        public Transform Source;
    }

    struct SpawnSlot
    {
        public Transform Transform;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public struct DiagnosticReport
    {
        public bool ManagerExists;
        public bool TrackReady;
        public bool HasSpawnedChasers;
        public int SnapshotCount;
        public int SpawnSlotCount;
        public int SpawnedChaserCount;
        public bool HasChosen;
        public bool BootstrapReady;
        public Vector3 SpawnRef;
        public Vector3 TravelForward;
        public float CrossroadTriggerAlong;
        public Vector3 CrossroadTransformPosition;
        public Vector3 CrossroadBoundsCenter;
        public bool HasCrossroadProximityBounds;
        public float PlayerAlong;
        public bool ProximityXZ;
        public bool Proximity3D;
        public bool AlongTrackTriggered;
        public string Summary;
    }

    static DutzLevel00CrowdCrossroadRespawn instance;

    Vector3 spawnRef;
    Vector3 travelForward;
    float crossroadTriggerAlong;
    Vector3 crossroadTransformPosition;
    Vector3 crossroadSpawnCenter;
    Bounds crossroadProximityBounds;
    bool hasCrossroadProximityBounds;
    bool trackReady;
    bool hasSpawnedChasers;
    bool bootstrapReinitialized;

    Transform chasersRoot;
    readonly List<CrowdSnapshot> snapshots = new List<CrowdSnapshot>(48);
    readonly List<SpawnSlot> spawnSlots = new List<SpawnSlot>(48);
    readonly List<GameObject> spawnedChasers = new List<GameObject>(48);

    DutzPlayerController player;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        if (FindObjectOfType<DutzLevel00CrowdCrossroadRespawn>() != null)
            return;

        if (GameObject.Find(SpawnSlotsRootName) == null)
            return;

        if (GameObject.Find(WalkersRootName) == null
            && GameObject.Find(CitizensRootName) == null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzLevel00CrowdCrossroadRespawn>();
    }

    public static void RefreshAfterDifficultyChosen()
    {
        if (instance == null)
            instance = FindObjectOfType<DutzLevel00CrowdCrossroadRespawn>();

        if (instance == null)
            EnsureFromBoot();

        instance?.ReinitializeTrack();
    }

    public static void ResetOnPlayerRespawn()
    {
        if (instance == null)
            instance = FindObjectOfType<DutzLevel00CrowdCrossroadRespawn>();

        instance?.DestroyCrossroadChasers();
    }

    public static DiagnosticReport BuildDiagnosticReport()
    {
        if (instance == null)
            instance = FindObjectOfType<DutzLevel00CrowdCrossroadRespawn>();

        if (instance == null)
        {
            return new DiagnosticReport
            {
                ManagerExists = false,
                Summary = "[Dutz] Crossroad crowd: manager missing (EnsureFromBoot not run or spawn slots absent)."
            };
        }

        return instance.BuildLocalDiagnosticReport();
    }

    void Awake()
    {
        if (!DutzCollectibleProgress.IsLevel00)
        {
            Destroy(gameObject);
            return;
        }

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        trackReady = InitializeTrackAndSnapshots();
        enabled = true;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Start()
    {
        player = DutzPlayerController.Instance;
        HideSceneSpawnSlotVisuals();
        if (!trackReady)
            StartCoroutine(RetryInitializeTrack());
    }

    System.Collections.IEnumerator RetryInitializeTrack()
    {
        for (var attempt = 0; attempt < 300 && !trackReady; attempt++)
        {
            yield return null;

            if (DutzGameBootstrap.IsReady && !DutzGameBootstrap.HasFailed)
                TryReinitializeAfterBootstrap();

            if (trackReady)
                break;

            DutzHighwayDirection.InvalidateTrackSegmentCache();
            trackReady = InitializeTrackAndSnapshots();
        }

        if (!trackReady)
            LogInitFailure("retry coroutine exhausted (300 frames)");
    }

    void ReinitializeTrack()
    {
        DutzHighwayDirection.InvalidateTrackSegmentCache();
        trackReady = InitializeTrackAndSnapshots();
        enabled = true;
    }

    void TryReinitializeAfterBootstrap()
    {
        if (bootstrapReinitialized)
            return;

        bootstrapReinitialized = true;
        ReinitializeTrack();
    }

    void Update()
    {
        if (DutzGameBootstrap.IsReady && !DutzGameBootstrap.HasFailed)
            TryReinitializeAfterBootstrap();

        if (!DutzDifficulty.HasChosen)
            return;

        if (!trackReady)
            return;

        if (hasSpawnedChasers || snapshots.Count == 0 || spawnSlots.Count == 0)
            return;

        if (player == null)
            player = DutzPlayerController.Instance;

        if (player == null)
            return;

        if (IsPlayerAtCrossroad(player.transform.position))
            SpawnCrossroadChaserDuplicates(player.transform.position);
    }

    bool IsPlayerAtCrossroad(Vector3 playerPosition)
    {
        if (hasCrossroadProximityBounds && IsInsideBoundsXZ(crossroadProximityBounds, playerPosition, ProximityFallbackMeters))
            return true;

        var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(
            spawnRef, playerPosition, travelForward);

        return playerAlong >= crossroadTriggerAlong;
    }

    static bool IsInsideBoundsXZ(Bounds bounds, Vector3 point, float margin)
    {
        var center = bounds.center;
        var extents = bounds.extents;
        return Mathf.Abs(point.x - center.x) <= extents.x + margin
            && Mathf.Abs(point.z - center.z) <= extents.z + margin;
    }

    bool InitializeTrackAndSnapshots()
    {
        snapshots.Clear();
        spawnSlots.Clear();

        spawnRef = GetSpawnReference();
        travelForward = GetTravelForward(spawnRef);
        if (travelForward.sqrMagnitude < 0.0001f)
        {
            LogInitFailure("travelForward is zero");
            return false;
        }

        CollectSnapshotsFromRoot(WalkersRootName);
        CollectSnapshotsFromRoot(CitizensRootName);
        CollectSpawnSlots();

        if (snapshots.Count == 0)
        {
            LogInitFailure("no bridge crowd snapshots (Level00CrowdWalkers / Level00CrowdCitizens)");
            return false;
        }

        if (spawnSlots.Count == 0)
        {
            LogInitFailure($"no crossroad spawn slots under {SpawnSlotsRootName}");
            return false;
        }

        var crossroad = GameObject.Find(CrossroadSegmentName);
        if (crossroad == null)
        {
            LogInitFailure("Highway Cross Road segment not found in scene");
            return false;
        }

        hasCrossroadProximityBounds = false;
        CacheCrossroadProximityBounds(crossroad);

        crossroadTransformPosition = crossroad.transform.position;
        crossroadSpawnCenter = hasCrossroadProximityBounds
            ? crossroadProximityBounds.center
            : crossroadTransformPosition;

        crossroadTriggerAlong = DutzHighwayDeckSampler.AlongTrackAhead(
            spawnRef, crossroadSpawnCenter, travelForward) - TriggerMarginMeters;

        Debug.Log(
            $"[Dutz] Level 00 crossroad crowd ready — {snapshots.Count} source NPC(s), " +
            $"{spawnSlots.Count} scene spawn slot(s), crossroadDeckCenter={crossroadSpawnCenter}, " +
            $"triggerAlong={crossroadTriggerAlong:F1}m.");

        return true;
    }

    void LogInitFailure(string reason)
    {
        Debug.LogWarning(
            $"[Dutz] Level 00 crowd crossroad respawn init failed: {reason}. " +
            $"spawnRef={spawnRef}, travelForward={travelForward}, " +
            $"spawnSlotsRoot={(GameObject.Find(SpawnSlotsRootName) != null)}, " +
            $"walkersRoot={(GameObject.Find(WalkersRootName) != null)}, " +
            $"citizensRoot={(GameObject.Find(CitizensRootName) != null)}, " +
            $"bootstrapReady={DutzGameBootstrap.IsReady}.");
    }

    void CacheCrossroadProximityBounds(GameObject crossroad)
    {
        var renderer = crossroad.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            crossroadProximityBounds = renderer.bounds;
            hasCrossroadProximityBounds = true;
            return;
        }

        var collider = crossroad.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            crossroadProximityBounds = collider.bounds;
            hasCrossroadProximityBounds = true;
        }
    }

    void CollectSnapshotsFromRoot(string rootName)
    {
        var root = GameObject.Find(rootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (child == null)
                continue;

            snapshots.Add(new CrowdSnapshot { Source = child });
        }
    }

    void CollectSpawnSlots()
    {
        var root = GameObject.Find(SpawnSlotsRootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (child == null)
                continue;

            spawnSlots.Add(new SpawnSlot
            {
                Transform = child,
                Position = child.position,
                Rotation = child.rotation
            });
        }

        spawnSlots.Sort(CompareSpawnSlots);
    }

    static int CompareSpawnSlots(SpawnSlot a, SpawnSlot b)
    {
        var slotA = a.Transform != null ? a.Transform.GetComponent<DutzLevel00CrossroadSpawnSlot>() : null;
        var slotB = b.Transform != null ? b.Transform.GetComponent<DutzLevel00CrossroadSpawnSlot>() : null;

        if (slotA != null && slotB != null)
        {
            var rowCompare = slotA.Row.CompareTo(slotB.Row);
            return rowCompare != 0 ? rowCompare : slotA.Column.CompareTo(slotB.Column);
        }

        return string.CompareOrdinal(a.Transform != null ? a.Transform.name : "", b.Transform != null ? b.Transform.name : "");
    }

    void HideSceneSpawnSlotVisuals()
    {
        foreach (var slot in spawnSlots)
        {
            if (slot.Transform == null)
                continue;

            foreach (var renderer in slot.Transform.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            foreach (var collider in slot.Transform.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = false;
            }

            foreach (var rb in slot.Transform.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb != null)
                    rb.isKinematic = true;
            }
        }
    }

    void EnsureChasersRoot()
    {
        if (chasersRoot != null)
            return;

        var existing = GameObject.Find(ChasersRootName);
        if (existing != null)
        {
            chasersRoot = existing.transform;
            return;
        }

        var go = new GameObject(ChasersRootName);
        go.SetActive(false);
        chasersRoot = go.transform;
    }

    void SpawnCrossroadChaserDuplicates(Vector3 playerPosition)
    {
        EnsureChasersRoot();

        var spawned = 0;
        Vector3 firstSpawnPos = playerPosition;
        Vector3 sumSpawnPos = Vector3.zero;

        var activeSlots = SelectActiveSpawnSlots(spawnSlots);
        for (var i = 0; i < activeSlots.Count; i++)
        {
            var slot = activeSlots[i];
            var sourceIndex = i % snapshots.Count;
            if (snapshots[sourceIndex].Source == null)
                continue;

            var source = snapshots[sourceIndex].Source.gameObject;
            var slotTransform = slot.Transform;
            var world = slotTransform != null ? slotTransform.position : slot.Position;
            world = SnapSpawnWorld(world, source.GetComponent<Collider>());
            var rotation = slotTransform != null ? slotTransform.rotation : slot.Rotation;

            var duplicate = Instantiate(source, world, rotation, chasersRoot);
            duplicate.name = source.name + "_CrossroadChaser";

            DutzLevel00CrossroadCitizenChaser.EnsureOnCrossroadDuplicate(duplicate);
            spawnedChasers.Add(duplicate);
            sumSpawnPos += world;
            if (spawned == 0)
                firstSpawnPos = world;
            spawned++;
        }

        chasersRoot.gameObject.SetActive(true);
        Physics.SyncTransforms();
        hasSpawnedChasers = true;

        var avgSpawnPos = spawned > 0 ? sumSpawnPos / spawned : playerPosition;
        Debug.Log(
            $"[Dutz] Level 00 crossroad crowd duplicated onto {activeSlots.Count}/{spawnSlots.Count} scene slot(s) " +
            $"({spawned} chaser(s) @ {DutzLevel00CrossroadCitizenChaser.ChaserScaleMultiplier}× scale, " +
            $"chase {DutzLevel00CrossroadCitizenChaser.ChaseSpeed} m/s); originals unchanged. " +
            $"playerPos={playerPosition}, avgSpawnPos={avgSpawnPos}, firstSpawnPos={firstSpawnPos}, " +
            $"playerToAvgDist={Vector3.Distance(playerPosition, avgSpawnPos):F1}m.");
    }

    static List<SpawnSlot> SelectActiveSpawnSlots(IReadOnlyList<SpawnSlot> all)
    {
        var result = new List<SpawnSlot>(MaxActiveChasers);
        if (all == null || all.Count == 0)
            return result;

        if (all.Count <= MaxActiveChasers)
        {
            for (var i = 0; i < all.Count; i++)
                result.Add(all[i]);
            return result;
        }

        for (var i = 0; i < MaxActiveChasers; i++)
        {
            var index = i * all.Count / MaxActiveChasers;
            result.Add(all[index]);
        }

        return result;
    }

    Vector3 SnapSpawnWorld(Vector3 world, Collider colliderHint)
    {
        var hintY = world.y > 15f ? spawnRef.y : world.y;
        if (hintY < 0.5f)
            hintY = spawnRef.y;

        if (DutzRoadGround.TrySampleRoadDeckForPlacement(world, hintY, colliderHint, out var deckY)
            || DutzRoadGround.TrySampleWalkableRoadDeckY(world, hintY, colliderHint, out deckY))
        {
            world.y = deckY;
        }

        return world;
    }

    void DestroyCrossroadChasers()
    {
        for (var i = spawnedChasers.Count - 1; i >= 0; i--)
        {
            if (spawnedChasers[i] != null)
                Destroy(spawnedChasers[i]);
        }

        spawnedChasers.Clear();
        hasSpawnedChasers = false;
    }

    DiagnosticReport BuildLocalDiagnosticReport()
    {
        if (player == null)
            player = DutzPlayerController.Instance;

        var playerPos = player != null ? player.transform.position : Vector3.zero;
        var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(spawnRef, playerPos, travelForward);
        var proximityXZ = hasCrossroadProximityBounds
            && IsInsideBoundsXZ(crossroadProximityBounds, playerPos, ProximityFallbackMeters);
        var proximity3D = hasCrossroadProximityBounds
            && crossroadProximityBounds.Contains(playerPos);
        var alongTriggered = playerAlong >= crossroadTriggerAlong;

        var sb = new StringBuilder();
        sb.AppendLine("[Dutz] Crossroad crowd diagnostic report:");
        sb.AppendLine($"  manager={true}, trackReady={trackReady}, hasSpawnedChasers={hasSpawnedChasers}, sources={snapshots.Count}, spawnSlots={spawnSlots.Count}, spawnedChasers={spawnedChasers.Count}");
        sb.AppendLine($"  HasChosen={DutzDifficulty.HasChosen}, bootstrapReady={DutzGameBootstrap.IsReady}");
        sb.AppendLine($"  spawnRef={spawnRef}, travelForward={travelForward}");
        sb.AppendLine($"  crossroadTransform={crossroadTransformPosition}, crossroadDeckCenter={crossroadSpawnCenter}");
        sb.AppendLine($"  triggerAlong={crossroadTriggerAlong:F1}, playerPos={playerPos}, playerAlong={playerAlong:F1}");
        sb.AppendLine($"  proximityXZ={proximityXZ}, proximity3D={proximity3D}, alongTrackTriggered={alongTriggered}");

        return new DiagnosticReport
        {
            ManagerExists = true,
            TrackReady = trackReady,
            HasSpawnedChasers = hasSpawnedChasers,
            SnapshotCount = snapshots.Count,
            SpawnSlotCount = spawnSlots.Count,
            SpawnedChaserCount = spawnedChasers.Count,
            HasChosen = DutzDifficulty.HasChosen,
            BootstrapReady = DutzGameBootstrap.IsReady,
            SpawnRef = spawnRef,
            TravelForward = travelForward,
            CrossroadTriggerAlong = crossroadTriggerAlong,
            CrossroadTransformPosition = crossroadTransformPosition,
            CrossroadBoundsCenter = hasCrossroadProximityBounds ? crossroadProximityBounds.center : Vector3.zero,
            HasCrossroadProximityBounds = hasCrossroadProximityBounds,
            PlayerAlong = playerAlong,
            ProximityXZ = proximityXZ,
            Proximity3D = proximity3D,
            AlongTrackTriggered = alongTriggered,
            Summary = sb.ToString()
        };
    }

    static Vector3 GetSpawnReference()
    {
        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackStart, out _))
            return trackStart;

        var player = DutzPlayerController.Instance;
        if (player == null)
        {
            foreach (var controller in FindObjectsOfType<DutzPlayerController>(true))
            {
                player = controller;
                break;
            }
        }

        return player != null ? player.transform.position : Vector3.zero;
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progressForward)
            && progressForward.sqrMagnitude > 0.0001f)
            return progressForward;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        forward.y = 0f;
        return forward.normalized;
    }
}
