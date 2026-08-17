using UnityEngine;

/// <summary>
/// Jonrem Police escorts: march forward on the highway like JONREM, then chase when the player is near.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
public class DutzJonremPoliceBehavior : MonoBehaviour
{
    public const string MountieRootName = "SimpleCitizens_Mountie_Brown";
    public const float PoliceScale = 2f;
    public const int PoliceCount = 3;
    public const float BehindSpacing = 16f;
    public const float LaneSpacing = 5f;
    const string MountieOutfit = "SC_Mountie";

    const float JonremWalkSpeed = 19f;
    const float JonremAnimSpeed = 1f;
    const float JonremStopDistance = 2.5f;
    const float JonremGroundCheck = 0.35f;
    const float JonremWakeDistance = 5f;
    const float GiantHunterDefaultWakeDistance = 280f;

    public const float PoliceWakeDistanceMeters = 55f;
    public const float PoliceChaseSpeedMetersPerSecond = 20f;
    public const float PoliceChaseAnimSpeed = 1.15f;
    public const float PoliceChaseStopDistanceMeters = 1f;
    public const float PoliceCaptureReachMeters = 0.45f;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        DutzJonremEscortRuntime.EnsureOnAllEscorts();
        DutzJonremEscortSpawnLock.EnsureRuntimeAnchors();
        DutzJonremEscortPlacement.EnsureOnLevel01();
        DutzGameBootstrap.Ready -= OnBootstrapReadyRestoreJonremEscorts;
        DutzGameBootstrap.Ready += OnBootstrapReadyRestoreJonremEscorts;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RestoreJonremEscortsAfterSceneLoad()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        DutzJonremEscortSpawnLock.EnsureRuntimeAnchors();
        DutzJonremEscortRuntime.EnsureOnAllEscorts();
    }

    static void OnBootstrapReadyRestoreJonremEscorts()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        DutzJonremEscortSpawnLock.EnsureRuntimeAnchors();
        DutzJonremEscortPlacement.EnsureOnLevel01();
    }

    public static bool IsPoliceCandidate(string objectName) =>
        DutzGiantBossNames.IsJonremPolice(objectName);

    public static Vector3 GetEscortSlotWorldPosition(Transform jonrem, int slotIndex) =>
        GetEscortSlotWorldPosition(jonrem.position, DutzJonremEscortPlacement.TravelForward, slotIndex);

    public static Vector3 GetEscortSlotWorldPosition(Vector3 jonremAnchor, Vector3 marchForward, int slotIndex) =>
        DutzJonremEscortPlacement.GetPoliceSlotWorldPosition(jonremAnchor, marchForward, slotIndex);

    public static void PlacePoliceFormationBehindJonrem(GameObject jonrem, GameObject[] police)
    {
        PlacePoliceFormationBehindJonrem(jonrem, police, DutzJonremEscortPlacement.TravelForward);
    }

    public static void PlacePoliceFormationBehindJonrem(GameObject jonrem, GameObject[] police, Vector3 marchForward)
    {
        if (jonrem == null || police == null)
            return;

        var rotation = jonrem.transform.rotation;
        var count = Mathf.Min(police.Length, PoliceCount);
        for (var i = 0; i < count; i++)
        {
            var officer = police[i];
            if (officer == null)
                continue;

            officer.transform.SetPositionAndRotation(
                GetEscortSlotWorldPosition(jonrem.transform.position, marchForward, i),
                rotation);
        }
    }

    public static void ApplyFromJonrem(GameObject police, GameObject jonrem) =>
        ApplyFromJonrem(police, jonrem, DutzJonremEscortPlacement.TravelForward);

    public static void ApplyFromJonrem(GameObject police, GameObject jonrem, Vector3 marchForward) =>
        ApplyFromJonrem(police, jonrem, marchForward, snapToHighway: true);

    public static void ApplyFromJonrem(
        GameObject police,
        GameObject jonrem,
        Vector3 marchForward,
        bool snapToHighway)
    {
        if (police == null || jonrem == null || !IsPoliceCandidate(police.name))
            return;

        DutzSimpleCitizensSetup.EnableOutfitOnly(police, MountieOutfit);

        if (Mathf.Abs(police.transform.localScale.x - PoliceScale) > 0.01f)
            police.transform.localScale = Vector3.one * PoliceScale;

        police.transform.rotation = jonrem.transform.rotation;

        EnsureRigidbody(police);
        DutzHippieBiteCollider.EnsureSmallHippieColliders(police);

        var physics = police.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = police.AddComponent<SimpleCitizensNpcPhysics>();
        physics.Apply();
        ResolveEscortMarchSpeed(physics, out var marchSpeed, out var marchAnim);
        physics.ConfigureForJonremEscort(
            marchSpeed, marchAnim, JonremStopDistance, JonremGroundCheck, marchForward);

        RemoveIfPresent<SimpleCitizensHippieHunter>(police);
        RemoveIfPresent<SimpleCitizensHippieBiter>(police);

        var hunter = police.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter == null)
            hunter = police.AddComponent<SimpleCitizensGiantHippieHunter>();
        hunter.ConfigureForJonremEscort(
            PoliceWakeDistanceMeters,
            PoliceChaseSpeedMetersPerSecond,
            PoliceChaseAnimSpeed,
            PoliceChaseStopDistanceMeters,
            false,
            forceApplyTuning: true);

        if (police.GetComponent<SimpleCitizensHippieSounds>() == null)
            police.AddComponent<SimpleCitizensHippieSounds>();

        var respawn = police.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = police.AddComponent<SimpleCitizensNpcRespawn>();

        if (police.GetComponent<DutzJonremPoliceBehavior>() == null)
            police.AddComponent<DutzJonremPoliceBehavior>();

        DutzJonremPoliceCapture.EnsureOnPolice(police);
        ApplyLevel01PoliceTuning(police);

        if (snapToHighway)
        {
            var pos = police.transform.position;
            if (DutzJonremEscortPlacement.TrySnapFeetOnHighwayTwo(ref pos))
                police.transform.position = pos;
        }

        respawn.RecordSpawnPoint();
    }

    public static void EnsureEscortComponents(GameObject escort)
    {
        if (escort == null || !DutzGiantBossNames.IsJonremEscort(escort.name))
            return;

        if (escort.GetComponent<DutzJonremEscortRuntime>() == null)
            escort.AddComponent<DutzJonremEscortRuntime>();

        var physics = escort.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = escort.AddComponent<SimpleCitizensNpcPhysics>();
        physics.Apply();

        if (escort.GetComponent<SimpleCitizensGiantHippieHunter>() == null)
            escort.AddComponent<SimpleCitizensGiantHippieHunter>();

        if (escort.GetComponent<SimpleCitizensHippieSounds>() == null)
            escort.AddComponent<SimpleCitizensHippieSounds>();

        if (escort.GetComponent<SimpleCitizensNpcRespawn>() == null)
            escort.AddComponent<SimpleCitizensNpcRespawn>();

        if (DutzGiantBossNames.IsJonremPolice(escort.name))
        {
            if (escort.GetComponent<DutzJonremPoliceBehavior>() == null)
                escort.AddComponent<DutzJonremPoliceBehavior>();

            DutzJonremPoliceCapture.EnsureOnPolice(escort);
            ApplyLevel01PoliceTuning(escort);
        }
        else
        {
            escort.GetComponent<SimpleCitizensGiantHippieHunter>()?.RefreshLevel01JonremEscortState();
        }
    }

    public static void ApplyLevel01PoliceTuning(GameObject police)
    {
        if (police == null || !IsPoliceCandidate(police.name) || !DutzCollectibleProgress.IsLevel01)
            return;

        var hunter = police.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
        {
            hunter.ConfigureForJonremEscort(
                PoliceWakeDistanceMeters,
                PoliceChaseSpeedMetersPerSecond,
                PoliceChaseAnimSpeed,
                PoliceChaseStopDistanceMeters,
                huntNow: false,
                forceApplyTuning: true);
        }

        var capture = police.GetComponent<DutzJonremPoliceCapture>();
        if (capture != null)
            capture.ApplyLevel01CaptureTuning();
    }

    public static void AwakenAllPoliceForChase()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        foreach (var officer in FindJonremPolice())
        {
            if (officer == null)
                continue;

            officer.GetComponent<SimpleCitizensGiantHippieHunter>()?.AwakenForChase();
        }
    }

    static void ResolveEscortMarchSpeed(SimpleCitizensNpcPhysics physics, out float speed, out float animSpeed)
    {
        speed = physics != null && physics.GetWalkSpeed() > 0.01f
            ? physics.GetWalkSpeed()
            : JonremWalkSpeed;
        animSpeed = physics != null && physics.GetAnimatorWalkSpeed() > 0.01f
            ? physics.GetAnimatorWalkSpeed()
            : JonremAnimSpeed;
    }

    static float ResolveEscortWakeDistance(SimpleCitizensGiantHippieHunter hunter)
    {
        if (hunter == null)
            return JonremWakeDistance;

        var current = hunter.WakeDistanceMeters;
        if (current <= 0.01f || Mathf.Approximately(current, GiantHunterDefaultWakeDistance))
            return JonremWakeDistance;

        return current;
    }

    static float ResolveEscortChaseSpeed(SimpleCitizensGiantHippieHunter hunter) =>
        hunter != null && hunter.ChaseSpeedMetersPerSecond > 0.01f
            ? hunter.ChaseSpeedMetersPerSecond
            : JonremWalkSpeed;

    static float ResolveEscortChaseAnimSpeed(SimpleCitizensGiantHippieHunter hunter) =>
        hunter != null && hunter.ChaseAnimSpeed > 0.01f
            ? hunter.ChaseAnimSpeed
            : JonremAnimSpeed;

    static void RemoveIfPresent<T>(GameObject police) where T : Component
    {
        var component = police.GetComponent<T>();
        if (component == null)
            return;

        if (Application.isPlaying)
            Destroy(component);
        else
            DestroyImmediate(component);
    }

    static void EnsureRigidbody(GameObject police)
    {
        var rb = police.GetComponent<Rigidbody>();
        if (rb == null)
            rb = police.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.mass = 50f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public static GameObject[] FindJonremPolice()
    {
        var results = new System.Collections.Generic.List<GameObject>();

        foreach (var root in Object.FindObjectsOfType<Transform>(true))
        {
            if (root == null || !IsPoliceCandidate(root.name))
                continue;

            results.Add(root.gameObject);
        }

        results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        if (results.Count > PoliceCount)
            results.RemoveRange(PoliceCount, results.Count - PoliceCount);

        return results.ToArray();
    }

    /// <summary>Legacy helper — prefer <see cref="FindJonremPolice"/>.</summary>
    public static GameObject[] FindPoliceNearJonrem(Transform jonrem) => FindJonremPolice();
}

/// <summary>
/// Level 01 spawn poses for JONREM and Jonrem Police.
/// Scene-baked spawn points on SimpleCitizensNpcRespawn take priority over defaults.
/// </summary>
public static class DutzJonremEscortSpawnLock
{
    public const string AnchorGiantName = "Gong Bong";

    static readonly Quaternion SharedRotation = new(0f, -0.7015413f, 0f, 0.7126288f);

    public static readonly Vector3 JonremPosition = new(-214.2f, 5.375145f, -14.6f);

    static readonly Vector3[] DefaultPolicePositions =
    {
        new(-236.8f, 5.4953537f, -19.1f),
        new(-225.7f, 5.33146667f, -14.2f),
        new(-231.4f, 5.16758728f, -9.1f)
    };

    public static bool TryGetPose(GameObject escort, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = SharedRotation;

        if (escort == null)
            return false;

        var respawn = escort.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn != null && respawn.HasBakedSpawnPoint)
        {
            respawn.GetBakedSpawnPoint(out position, out rotation);
            return true;
        }

        return TryGetDefaultPose(escort.name, out position, out rotation);
    }

    public static bool TryGetPose(string objectName, out Vector3 position, out Quaternion rotation) =>
        TryGetDefaultPose(objectName, out position, out rotation);

    static bool TryGetDefaultPose(string objectName, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = SharedRotation;

        if (DutzGiantBossNames.IsJonrem(objectName))
        {
            position = JonremPosition;
            return true;
        }

        if (!DutzGiantBossNames.IsJonremPolice(objectName))
            return false;

        if (!TryGetPoliceSlotIndex(objectName, out var slotIndex)
            || slotIndex < 0
            || slotIndex >= DefaultPolicePositions.Length)
        {
            return false;
        }

        position = DefaultPolicePositions[slotIndex];
        return true;
    }

    public static void RestoreAllOnLevel01()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        RestoreEscort(DutzGiantBossNames.FindJonrem());

        var police = DutzJonremPoliceBehavior.FindJonremPolice();
        for (var i = 0; i < police.Length && i < DefaultPolicePositions.Length; i++)
            RestoreEscort(police[i]);
    }

    public static void BakeAllEscortSpawnPointsFromScene()
    {
        foreach (var escort in FindAllEscorts())
        {
            if (escort == null)
                continue;

            var respawn = escort.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = escort.AddComponent<SimpleCitizensNpcRespawn>();

            respawn.RecordSpawnPoint();
            respawn.GetBakedSpawnPoint(out var position, out var rotation);
            ApplyPose(escort, position, rotation);
        }
    }

    public static void ApplyDefaultLockToAllEscorts()
    {
        RestoreEscort(DutzGiantBossNames.FindJonrem());

        var police = DutzJonremPoliceBehavior.FindJonremPolice();
        for (var i = 0; i < police.Length && i < DefaultPolicePositions.Length; i++)
        {
            var officer = police[i];
            if (officer == null)
                continue;

            if (!TryGetDefaultPose(officer.name, out var position, out var rotation))
                continue;

            ApplyPose(officer, position, rotation);
        }
    }

    public static void RestoreEscort(GameObject escort)
    {
        if (escort == null || !TryGetPose(escort, out var position, out var rotation))
            return;

        ApplyPose(escort, position, rotation);
    }

    public static void ApplyPose(GameObject escort, Vector3 position, Quaternion rotation)
    {
        if (escort == null)
            return;

        if (!escort.activeSelf)
            escort.SetActive(true);

        escort.transform.SetPositionAndRotation(position, rotation);

        var rb = escort.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
        }

        var respawn = escort.GetComponent<SimpleCitizensNpcRespawn>();
        respawn?.SetLockedSpawnPoint(position, rotation);
    }

    public static void EnsureRuntimeAnchors()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        foreach (var escort in FindAllEscorts())
            RestoreEscort(escort);
    }

    public static System.Collections.Generic.List<GameObject> FindAllEscorts()
    {
        var results = new System.Collections.Generic.List<GameObject>(4);
        var jonrem = DutzGiantBossNames.FindJonrem();
        if (jonrem != null)
            results.Add(jonrem);

        var police = DutzJonremPoliceBehavior.FindJonremPolice();
        for (var i = 0; i < police.Length; i++)
        {
            if (police[i] != null)
                results.Add(police[i]);
        }

        return results;
    }

    static bool TryGetPoliceSlotIndex(string objectName, out int slotIndex)
    {
        slotIndex = -1;
        if (!DutzGiantBossNames.IsJonremPolice(objectName))
            return false;

        var suffix = objectName.Substring(DutzGiantBossNames.JonremPolicePrefix.Length).Trim();
        if (!int.TryParse(suffix, out var oneBased) || oneBased < 1 || oneBased > DefaultPolicePositions.Length)
            return false;

        slotIndex = oneBased - 1;
        return true;
    }
}

/// <summary>
/// Level 01 Jonrem escort placement. Editor setup rebakes poses; runtime boot restores Sen Gong Bong lock.
/// </summary>
public static class DutzJonremEscortPlacement
{
    public const string SegmentName = "Highway Straight 2";
    public const float JonremAlongSegment = 0.42f;
    public const float JonremLaneZ = -9f;

    static DutzHighwayDeckSampler.SegmentPath highwayTwoPath;
    static Vector3 trackSpawn;
    static Vector3 travelForward = Vector3.right;
    static bool pathCached;

    public static Vector3 TravelForward
    {
        get
        {
            EnsurePathCached();
            return ResolveHighwayTwoMarchForward();
        }
    }

    /// <summary>Runtime boot: restore scene-baked escort spawns (or defaults when unset).</summary>
    public static void EnsureOnLevel01()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        DutzJonremEscortSpawnLock.RestoreAllOnLevel01();

        DutzJonremPoliceBehavior.EnsureEscortComponents(DutzGiantBossNames.FindJonrem());

        foreach (var officer in DutzJonremPoliceBehavior.FindJonremPolice())
            DutzJonremPoliceBehavior.EnsureEscortComponents(officer);
    }

    static Vector3 ResolveMarchForwardForJonrem(GameObject jonrem)
    {
        if (jonrem != null)
        {
            var flatForward = jonrem.transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.0001f)
                return flatForward.normalized;
        }

        var marchForward = ResolveHighwayTwoMarchForward();
        if (marchForward.sqrMagnitude > 0.0001f)
            return marchForward;

        return Vector3.right;
    }

    public static bool TryGetJonremAnchor(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (!EnsurePathCached())
            return false;

        if (!DutzHighwayDeckSampler.TrySampleOnPath(
                highwayTwoPath.Samples, JonremAlongSegment, out var sample))
        {
            return false;
        }

        position = DutzHighwayDeckSampler.PlaceOnLane(sample, JonremLaneZ, trackSpawn);
        rotation = ResolveEscortRotation();
        return true;
    }

    public static Vector3 GetPoliceSlotWorldPosition(Vector3 jonremAnchor, Vector3 marchForward, int slotIndex)
    {
        if (EnsurePathCached() && highwayTwoPath.Samples != null && highwayTwoPath.Samples.Count > 0)
            return GetPoliceSlotOnHighwayTwoPath(jonremAnchor, slotIndex);

        var forward = marchForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;
        else
            forward.Normalize();

        var right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.forward;
        else
            right.Normalize();

        var behind = jonremAnchor - forward * DutzJonremPoliceBehavior.BehindSpacing;
        behind.z = jonremAnchor.z;
        return behind + right * ((slotIndex - 1) * DutzJonremPoliceBehavior.LaneSpacing);
    }

    static Vector3 GetPoliceSlotOnHighwayTwoPath(Vector3 jonremAnchor, int slotIndex)
    {
        var forward = ResolveHighwayTwoMarchForward();
        var right = Vector3.Cross(Vector3.up, forward);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.forward;
        else
            right.Normalize();

        var pos = jonremAnchor
            - forward * DutzJonremPoliceBehavior.BehindSpacing
            + right * ((slotIndex - 1) * DutzJonremPoliceBehavior.LaneSpacing);

        TrySnapFeetOnHighwayTwo(ref pos);
        return pos;
    }

    static Vector3 ResolveHighwayTwoMarchForward()
    {
        if (!EnsurePathCached() || highwayTwoPath.Samples == null || highwayTwoPath.Samples.Count < 2)
            return travelForward;

        var start = highwayTwoPath.Samples[0].Position;
        var end = highwayTwoPath.Samples[highwayTwoPath.Samples.Count - 1].Position;
        var segmentForward = end - start;
        segmentForward.y = 0f;
        if (segmentForward.sqrMagnitude < 0.0001f)
            return travelForward;

        segmentForward.Normalize();
        if (Vector3.Dot(segmentForward, travelForward) < 0f)
            segmentForward = -segmentForward;

        return segmentForward;
    }

    public static bool TrySnapFeetOnHighwayTwo(ref Vector3 worldPosition, bool preserveHorizontal = true)
    {
        if (!EnsurePathCached() || highwayTwoPath.Segment == null)
            return false;

        if (preserveHorizontal)
            return TrySampleDeckYOnHighwayTwo(worldPosition.x, worldPosition.z, ref worldPosition.y);

        var along = DutzHighwayDeckSampler.AlongTrackAhead(trackSpawn, worldPosition, travelForward);
        if (along < highwayTwoPath.StartAlong - 8f || along > highwayTwoPath.EndAlong + 8f)
            return false;

        if (!DutzHighwayDeckSampler.TrySampleMinAheadOnPath(highwayTwoPath.Samples, along, out var sample))
            return false;

        var snapped = DutzHighwayDeckSampler.PlaceOnLane(sample, worldPosition.z, trackSpawn);
        worldPosition.x = snapped.x;
        worldPosition.y = snapped.y;
        worldPosition.z = snapped.z;
        return true;
    }

    public static bool TrySampleDeckYOnHighwayTwo(float worldX, float worldZ, ref float worldY)
    {
        if (!EnsurePathCached() || highwayTwoPath.Segment == null)
            return false;

        var segment = highwayTwoPath.Segment;
        if (!TryGetSegmentBounds(segment, out var bounds))
            return false;

        var xz = new Vector3(worldX, 0f, worldZ);
        var boundsXZ = new Vector3(bounds.ClosestPoint(new Vector3(worldX, bounds.center.y, worldZ)).x, 0f,
            bounds.ClosestPoint(new Vector3(worldX, bounds.center.y, worldZ)).z);
        if (Vector3.Distance(xz, boundsXZ) > 28f)
            return false;

        var probe = new Vector3(worldX, bounds.max.y + 12f, worldZ);
        var hits = Physics.RaycastAll(probe, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
        var bestY = float.NegativeInfinity;
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            if (!hit.collider.transform.IsChildOf(segment.transform) && hit.collider.gameObject != segment)
                continue;

            if (hit.point.y > bestY)
                bestY = hit.point.y;
        }

        if (float.IsNegativeInfinity(bestY))
        {
            worldY = bounds.max.y - 0.5f;
            return true;
        }

        worldY = bestY;
        return true;
    }

    static bool TryGetSegmentBounds(GameObject segment, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in segment.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (var collider in segment.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || collider.isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    static bool EnsurePathCached()
    {
        if (pathCached && highwayTwoPath.Samples != null && highwayTwoPath.Samples.Count > 0)
            return true;

        pathCached = false;
        highwayTwoPath = default;

        if (!DutzHighwayDirection.TryGetTrackStartSpawnPosition(out trackSpawn, out travelForward)
            || travelForward.sqrMagnitude < 0.0001f)
        {
            travelForward = Vector3.right;
        }
        else
        {
            travelForward.y = 0f;
            travelForward.Normalize();
        }

        var segment = GameObject.Find(SegmentName);
        if (segment == null)
            return false;

        highwayTwoPath = DutzHighwayDeckSampler.BuildSegmentPath(segment, SegmentName, trackSpawn, travelForward);
        pathCached = highwayTwoPath.Samples != null && highwayTwoPath.Samples.Count > 0;
        return pathCached;
    }

    static Quaternion ResolveEscortRotation()
    {
        var tamby = DutzGiantBossNames.FindTamby();
        if (tamby != null)
            return tamby.transform.rotation;

        return Quaternion.LookRotation(travelForward, Vector3.up);
    }

    static void SnapEscortToHighwayTwo(GameObject escort)
    {
        if (escort == null)
            return;

        var pos = escort.transform.position;
        if (TrySnapFeetOnHighwayTwo(ref pos))
            escort.transform.position = pos;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => pathCached = false;
}

/// <summary>Per-escort guard: re-apply scene spawn pose whenever the object enables.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class DutzJonremEscortRuntime : MonoBehaviour
{
    void Awake() => TryRestorePose();
    void OnEnable() => TryRestorePose();

    void TryRestorePose()
    {
        if (!Application.isPlaying || !DutzCollectibleProgress.IsLevel01)
            return;

        if (!DutzGiantBossNames.IsJonremEscort(gameObject.name))
            return;

        var hunter = GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null && hunter.HasAwakened)
            return;

        DutzJonremEscortSpawnLock.RestoreEscort(gameObject);
    }

    public static void EnsureOnAllEscorts()
    {
        if (!DutzCollectibleProgress.IsLevel01)
            return;

        foreach (var escort in DutzJonremEscortSpawnLock.FindAllEscorts())
        {
            if (escort == null)
                continue;

            if (escort.GetComponent<DutzJonremEscortRuntime>() == null)
                escort.AddComponent<DutzJonremEscortRuntime>();
        }
    }
}
