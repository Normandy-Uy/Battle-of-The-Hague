using UnityEngine;

/// <summary>Level 0 traffic props — baked spawn pose, constant forward drive, respawn on fall.</summary>
[DisallowMultipleComponent]
public class DutzVehicleSpawn : MonoBehaviour, ISerializationCallbackReceiver
{
    public const float DefaultMoveSpeed = 5f;

    [Header("Spawn Pose")]
    [Tooltip("Fixed world spawn position.")]
    [SerializeField] Vector3 spawnPosition;

    [Tooltip("Which way the vehicle faces on the road, in degrees (0–360). Same as turning it left/right on the highway.")]
    [SerializeField] float spawnHeadingDegrees;

    [Tooltip("When enabled, batch/menu bake will not overwrite spawn pose. You can still edit values here anytime.")]
    [SerializeField] bool spawnPoseLocked = false;

    [SerializeField, HideInInspector] Vector3 spawnEulerAngles;
    [SerializeField, HideInInspector] DutzCollectibleSpawnPose spawnPose;
    [SerializeField, HideInInspector] bool flatSpawnFieldsInitialized;

    [Header("Movement")]
    [SerializeField] float moveSpeed = DefaultMoveSpeed;
    [SerializeField] bool snapToRoad = true;

    [Header("Fall Respawn")]
    [SerializeField] float fallYThreshold = -2f;
    [SerializeField] bool respawnEnabled = true;

    BoxCollider bodyCollider;

    public DutzCollectibleSpawnPose SpawnPose => BuildSpawnPose();

    public Vector3 SpawnPosition => spawnPosition;

    public float SpawnHeadingDegrees => spawnHeadingDegrees;

    public bool SpawnPoseLocked => spawnPoseLocked;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public static bool IsVehicleRoot(GameObject go) =>
        go != null && go.name.StartsWith("Vehicle_", System.StringComparison.Ordinal);

    void Awake()
    {
        EnsureBodyCollider();
        ApplySpawnPose();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        SyncLegacyEulerFromHeading();
    }
#endif

    public static bool TryGetHighwayHeadingDegrees(out float headingDegrees)
    {
        headingDegrees = 90f;
        if (!DutzHighwayDirection.TryGetTrackProgressForward(out var forward))
            return false;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return false;

        headingDegrees = Quaternion.LookRotation(forward.normalized, Vector3.up).eulerAngles.y;
        return true;
    }

    /// <summary>Perpendicular to highway (e.g. rally bus blocking the road). ~180° when highway is ~90°.</summary>
    public static bool TryGetAcrossRoadHeadingDegrees(out float headingDegrees)
    {
        if (!TryGetHighwayHeadingDegrees(out headingDegrees))
        {
            headingDegrees = 180f;
            return false;
        }

        headingDegrees = Mathf.Repeat(headingDegrees + 90f, 360f);
        return true;
    }

    public static bool IsRallyBus(GameObject go) =>
        go != null && go.name == "Vehicle_Bus_color01";

    /// <summary>Level 0 — rally bus off in Senior Citizen Mode, on for Easy/Medium/Hard.</summary>
    public static void ApplyLevel00DifficultyRules()
    {
        if (!DutzCollectibleProgress.IsLevel00)
            return;

        var bus = GameObject.Find("Vehicle_Bus_color01");
        if (bus == null)
            return;

        var enableBus = !DutzDifficulty.HasChosen || !DutzDifficulty.IsSeniorCitizenMode();
        if (bus.activeSelf != enableBus)
            bus.SetActive(enableBus);
    }

    void SyncLegacyEulerFromHeading()
    {
        spawnEulerAngles = new Vector3(0f, spawnHeadingDegrees, 0f);
    }

    public void OnBeforeSerialize()
    {
        SyncLegacyEulerFromHeading();
        spawnPose = BuildSpawnPose();
    }

    public void OnAfterDeserialize()
    {
        if (flatSpawnFieldsInitialized)
        {
            if (spawnHeadingDegrees == 0f && spawnEulerAngles.y != 0f)
                spawnHeadingDegrees = spawnEulerAngles.y;
            return;
        }

        if (!spawnPose.HasPosition)
            return;

        spawnPosition = spawnPose.position;
        spawnHeadingDegrees = spawnPose.eulerAngles.y;
        flatSpawnFieldsInitialized = true;
        SyncLegacyEulerFromHeading();
    }

    DutzCollectibleSpawnPose BuildSpawnPose()
    {
        SyncLegacyEulerFromHeading();
        return new DutzCollectibleSpawnPose
        {
            position = spawnPosition,
            eulerAngles = spawnEulerAngles,
            localScale = transform != null ? transform.localScale : Vector3.one
        };
    }

    bool HasSpawnPose() =>
        flatSpawnFieldsInitialized || spawnPose.HasPosition || spawnPosition != Vector3.zero || spawnHeadingDegrees != 0f;

    void Update()
    {
        if (!HasSpawnPose())
            return;

        MoveForward();
        if (snapToRoad)
            SnapToRoadDeck();
        TryRespawnOnFall();
    }

    void MoveForward()
    {
        var speed = Mathf.Max(0f, moveSpeed);
        if (speed <= 0f)
            return;

        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        transform.position += forward * (speed * Time.deltaTime);
    }

    void TryRespawnOnFall()
    {
        if (!respawnEnabled)
            return;

        if (transform.position.y < fallYThreshold)
            ApplySpawnPose();
    }

    public void SetSpawnHeadingDegrees(float headingDegrees)
    {
        spawnHeadingDegrees = headingDegrees;
        flatSpawnFieldsInitialized = true;
        SyncLegacyEulerFromHeading();
    }

    public void CaptureSpawnPoseFromTransform(bool force = false)
    {
        if (!force && spawnPoseLocked && HasSpawnPose())
            return;

        spawnPosition = transform.position;
        spawnHeadingDegrees = transform.eulerAngles.y;
        flatSpawnFieldsInitialized = true;
        SyncLegacyEulerFromHeading();
        spawnPose = BuildSpawnPose();
    }

    public void ApplySpawnPose()
    {
        if (!HasSpawnPose())
            return;

        transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, spawnHeadingDegrees, 0f));
        spawnPose = BuildSpawnPose();
        EnsureBodyCollider();
        if (snapToRoad)
            SnapToRoadDeck();
    }

    public void PrepareGroundContact()
    {
        EnsureBodyCollider();
        SnapToRoadDeck();
    }

    public void EnsureBodyCollider()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<BoxCollider>();

        if (bodyCollider == null)
            bodyCollider = gameObject.AddComponent<BoxCollider>();

        bodyCollider.isTrigger = false;
        if (!TryFitBoxColliderFromRenderers(transform, bodyCollider))
        {
            bodyCollider.center = Vector3.zero;
            bodyCollider.size = new Vector3(2f, 1.2f, 4f);
        }
    }

    static bool TryFitBoxColliderFromRenderers(Transform root, BoxCollider box)
    {
        if (box == null || root == null)
            return false;

        var hasBounds = false;
        var worldBounds = new Bounds(root.position, Vector3.zero);

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                worldBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return false;

        var localCenter = root.InverseTransformPoint(worldBounds.center);
        var localSize = root.InverseTransformVector(worldBounds.size);
        box.center = localCenter;
        box.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z));
        return true;
    }

    void SnapToRoadDeck()
    {
        Physics.SyncTransforms();
        var exclude = (Collider)bodyCollider ?? GetComponent<Collider>();

        var pos = transform.position;
        var feetY = DutzNpcFeet.GetLowestWorldY(gameObject);
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(pos, feetY, exclude, out var deckY)
            || DutzRoadGround.TrySampleRoadDeckForPlacement(pos, pos.y, exclude, out deckY))
        {
            DutzNpcFeet.PlacePivotOnSurface(gameObject, deckY);
        }
    }

    public static void ResetOnPlayerRespawn()
    {
        var vehicles = FindObjectsOfType<DutzVehicleSpawn>();
        for (var i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] != null)
                vehicles[i].ApplySpawnPose();
        }
    }
}
