using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level07 — when Player1 reaches Highway Straight 3, spawn a mirror of the
/// Straight 2 Level02-style addicts onto Straight 3's pitched deck (one-shot).
/// </summary>
[DefaultExecutionOrder(-150)]
[DisallowMultipleComponent]
public class DutzLevel07Straight3AddictSpawner : MonoBehaviour
{
    public const string ManagerName = "DutzLevel07Straight3AddictSpawner";
    public const string SourceGroupName = "Level07_Straight2_Addicts";
    public const string SpawnGroupName = "Level07_Straight3_Addicts";
    public const string SpawnPrefix = "SimpleCitizens_Hippie_Extra_L07_S3_";
    public const string SourcePrefix = "SimpleCitizens_Hippie_Extra_L07_";
    const string Straight2Name = "Highway Straight 2";
    const string Straight3Name = "Highway Straight 3";
    const float SegmentEnterSlopMeters = 2f;

    bool spawned;
    DutzPlayerController player;
    Vector3 spawnRef;
    Vector3 travelForward;
    float straight3StartAlong = float.PositiveInfinity;
    bool trackReady;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        if (Object.FindObjectOfType<DutzLevel07Straight3AddictSpawner>() != null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzLevel07Straight3AddictSpawner>();
    }

    public static bool IsStraight3Addict(string objectName) =>
        !string.IsNullOrEmpty(objectName)
        && objectName.StartsWith(SpawnPrefix, System.StringComparison.Ordinal);

    public static bool IsStraight2Addict(string objectName) =>
        !string.IsNullOrEmpty(objectName)
        && objectName.StartsWith(SourcePrefix, System.StringComparison.Ordinal)
        && !IsStraight3Addict(objectName);

    void Awake() => TryInitTrack();

    void Start() => player = DutzPlayerController.Instance;

    void Update()
    {
        if (spawned || !DutzCollectibleProgress.IsLevel07)
            return;

        if (!trackReady && !TryInitTrack())
            return;

        if (player == null)
            player = DutzPlayerController.Instance;
        if (player == null || player.ControlsLocked)
            return;

        if (DutzLevelStartGate.IsBlockingStart || DutzLevelObjective.IsStartMessageActive)
            return;

        var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(
            spawnRef, player.transform.position, travelForward);
        if (playerAlong < straight3StartAlong - SegmentEnterSlopMeters)
            return;

        if (SpawnMirroredAddicts())
            spawned = true;
    }

    bool TryInitTrack()
    {
        spawnRef = GetSpawnReference();
        travelForward = GetTravelForward(spawnRef);
        var paths = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(
            new[] { Straight2Name, Straight3Name }, spawnRef, travelForward);
        if (paths == null || paths.Count == 0)
            return false;

        for (var i = 0; i < paths.Count; i++)
        {
            if (paths[i].SegmentName == Straight3Name)
            {
                straight3StartAlong = paths[i].StartAlong;
                trackReady = true;
                return true;
            }
        }

        // Fallback: use Straight 3 world bounds center along-track.
        var straight3 = GameObject.Find(Straight3Name);
        if (straight3 == null)
            return false;

        straight3StartAlong = DutzHighwayDeckSampler.AlongTrackAhead(
            spawnRef, straight3.transform.position, travelForward);
        trackReady = true;
        return true;
    }

    bool SpawnMirroredAddicts()
    {
        if (GameObject.Find(SpawnGroupName) != null)
            return true;

        var sourceRoot = GameObject.Find(SourceGroupName);
        var straight2 = GameObject.Find(Straight2Name);
        var straight3 = GameObject.Find(Straight3Name);
        if (sourceRoot == null || straight2 == null || straight3 == null)
        {
            Debug.LogWarning(
                "[Dutz] Straight3 addict mirror skipped — missing Straight2 addicts or highway.");
            return false;
        }

        var sources = CollectSourceAddicts(sourceRoot);
        if (sources.Count == 0)
        {
            Debug.LogWarning("[Dutz] Straight3 addict mirror skipped — no Straight2 addicts found.");
            return false;
        }

        Physics.SyncTransforms();

        var group = new GameObject(SpawnGroupName);
        var road2 = straight2.transform;
        var road3 = straight3.transform;
        var spawnedCount = 0;

        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            if (source == null)
                continue;

            var clone = Object.Instantiate(source, group.transform);
            clone.name = $"{SpawnPrefix}{i + 1:00}";
            clone.SetActive(true);

            var local = road2.InverseTransformPoint(source.transform.position);
            var world = road3.TransformPoint(local);
            var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(clone);
            pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 3.5f);
            if (!DutzRoadGround.TryClampOntoLevel07Straight3Deck(ref world, pivotToFeet))
            {
                if (DutzRoadGround.TrySampleLevel07Straight3DeckPoint(world, out var deck, out var deckUp))
                    world = deck + deckUp * pivotToFeet;
            }

            var forward = road3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.right;
            forward.Normalize();
            var rotation = Quaternion.LookRotation(forward, Vector3.up);

            clone.transform.SetPositionAndRotation(world, rotation);
            if (clone.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.position = world;
                rb.rotation = rotation;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var respawn = clone.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = clone.AddComponent<SimpleCitizensNpcRespawn>();
            respawn.SetLockedSpawnPoint(world, rotation);

            var hunter = clone.GetComponent<SimpleCitizensHippieHunter>();
            if (hunter != null)
                hunter.enabled = true;

            var physics = clone.GetComponent<SimpleCitizensNpcPhysics>();
            if (physics != null)
            {
                physics.enabled = true;
                physics.Apply();
                physics.SetWalkingEnabled(true);
            }

            spawnedCount++;
        }

        Debug.Log(
            $"[Dutz] Mirrored {spawnedCount} Level02-style addicts from Straight 2 onto Straight 3 " +
            $"(player reached {Straight3Name}).");
        return spawnedCount > 0;
    }

    static List<GameObject> CollectSourceAddicts(GameObject sourceRoot)
    {
        var list = new List<GameObject>();
        foreach (Transform child in sourceRoot.transform)
        {
            if (child == null || !IsStraight2Addict(child.name))
                continue;
            list.Add(child.gameObject);
        }

        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }

    static Vector3 GetSpawnReference()
    {
        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var spawn, out _))
            return spawn;

        var player = DutzPlayerController.Instance ?? Object.FindObjectOfType<DutzPlayerController>();
        if (player != null)
            return player.transform.position;

        return new Vector3(-1002f, 7.4f, -9.1f);
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progress)
            && progress.sqrMagnitude > 0.0001f)
            return progress.normalized;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;
        return forward.normalized;
    }
}
