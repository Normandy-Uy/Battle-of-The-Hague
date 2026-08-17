using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Level 0 end goal — touching the Senate Building mural wins the level.</summary>
[DisallowMultipleComponent]
public class DutzSenateBuildingMuralGoal : MonoBehaviour
{
    const string WinColliderChildName = "SenateBuildingWinZone";
    const float WinZonePaddingMeters = 1.5f;
    const float TriggerDepthMeters = 1.5f;
    const float RoadWinZoneAlongMeters = 18f;
    const float RoadWinZoneLateralMeters = 14f;
    const float RoadWinZoneHeightMeters = 5f;

    public const float PlayerTouchReachMeters = 12f;

    static DutzSenateBuildingMuralGoal cached;

    Vector3 roadWinCenter;
    Vector3 roadWinHalfExtents;
    bool hasRoadWinZone;

    public static bool UsesSenateBuildingWin =>
        SceneManager.GetActiveScene().name == DutzMobileRuntime.Level00SceneName;

    public static void EnsureFromBoot()
    {
        if (!UsesSenateBuildingWin)
            return;

        var panel = FindPanelObject();
        if (panel == null)
            panel = CreateRuntimePanelIfMissing();

        if (panel == null)
        {
            Debug.LogWarning("[Dutz] Senate Building mural not found in Level 0 — win goal missing.");
            return;
        }

        if (!panel.activeSelf)
            panel.SetActive(true);

        var marker = panel.GetComponent<DutzSenateBuildingMuralGoal>();
        if (marker == null)
            marker = panel.AddComponent<DutzSenateBuildingMuralGoal>();

        marker.EnsureTouchColliders();
        cached = marker;
    }

    void Awake()
    {
        cached = this;
        EnsureTouchColliders();
    }

    void OnEnable() => cached = this;

    void OnDisable()
    {
        if (cached == this)
            cached = null;
    }

    public void EnsureTouchColliders()
    {
        RemoveLegacyMeshColliders();
        EnsureWinTriggerCollider();
        EnsureRoadWinZone();
    }

    void RemoveLegacyMeshColliders()
    {
        var meshColliders = GetComponentsInChildren<MeshCollider>(true);
        for (var i = 0; i < meshColliders.Length; i++)
        {
            var meshCol = meshColliders[i];
            if (meshCol == null)
                continue;

            if (Application.isPlaying)
                Destroy(meshCol);
            else
                DestroyImmediate(meshCol);
        }
    }

    void EnsureWinTriggerCollider()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        if (!box.isTrigger)
            box.isTrigger = true;

        FitTriggerToRenderer(box);
    }

    void EnsureRoadWinZone()
    {
        if (!TryComputeRoadWinZone(out roadWinCenter, out roadWinHalfExtents))
        {
            hasRoadWinZone = false;
            return;
        }

        hasRoadWinZone = true;

        var child = transform.Find(WinColliderChildName);
        if (child == null)
        {
            var go = new GameObject(WinColliderChildName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        child.position = roadWinCenter;
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var travelForward)
            && travelForward.sqrMagnitude > 0.0001f)
        {
            travelForward.y = 0f;
            child.rotation = Quaternion.LookRotation(travelForward.normalized, Vector3.up);
        }

        var roadBox = child.GetComponent<BoxCollider>();
        if (roadBox == null)
            roadBox = child.gameObject.AddComponent<BoxCollider>();

        roadBox.isTrigger = true;
        roadBox.center = Vector3.zero;
        roadBox.size = new Vector3(
            RoadWinZoneAlongMeters,
            RoadWinZoneHeightMeters,
            RoadWinZoneLateralMeters * 2f);

        if (child.GetComponent<DutzSenateBuildingMuralGoalTrigger>() == null)
            child.gameObject.AddComponent<DutzSenateBuildingMuralGoalTrigger>();
    }

    bool TryComputeRoadWinZone(out Vector3 center, out Vector3 halfExtents)
    {
        center = transform.position;
        halfExtents = new Vector3(
            RoadWinZoneAlongMeters * 0.5f,
            RoadWinZoneHeightMeters * 0.5f,
            RoadWinZoneLateralMeters);

        if (!DutzHighwayDirection.TryGetTrackProgressForward(out var travelForward)
            || travelForward.sqrMagnitude < 0.0001f)
            return false;

        travelForward.y = 0f;
        travelForward.Normalize();

        var lateral = Vector3.Cross(Vector3.up, travelForward);
        if (lateral.sqrMagnitude < 0.0001f)
            lateral = Vector3.right;
        else
            lateral.Normalize();

        var renderer = GetComponent<Renderer>();
        var anchor = renderer != null ? renderer.bounds.center : transform.position;
        center = anchor - lateral * 11f;
        center.y = anchor.y;

        var player = DutzPlayerController.Instance ?? Object.FindObjectOfType<DutzPlayerController>();
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (DutzRoadGround.TrySampleWalkableRoadDeckY(center, player.transform.position.y, cc, out var deckY)
                || DutzRoadGround.TrySampleRoadDeckY(center, player.transform.position.y, cc, out deckY))
            {
                center.y = deckY + 1.5f;
            }
        }

        return true;
    }

    void FitTriggerToRenderer(BoxCollider box)
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            var bounds = renderer.bounds;
            var localCenter = transform.InverseTransformPoint(bounds.center);
            var localSize = transform.InverseTransformVector(bounds.size);
            localSize.x = Mathf.Max(Mathf.Abs(localSize.x), TriggerDepthMeters);
            localSize.y = Mathf.Max(Mathf.Abs(localSize.y), TriggerDepthMeters);
            localSize.z = Mathf.Max(Mathf.Abs(localSize.z), TriggerDepthMeters);

            box.center = localCenter;
            box.size = localSize;
            return;
        }

        box.center = Vector3.zero;
        box.size = new Vector3(10f, TriggerDepthMeters, 10f);
    }

    void Start()
    {
        EnsureTouchColliders();
    }

    void Update()
    {
        if (!UsesSenateBuildingWin || !DutzDifficulty.HasChosen)
            return;

        var player = DutzPlayerController.Instance;
        if (player == null || player.ControlsLocked)
            return;

        if (IsPlayerTouchingSenateBuildingMural(player))
            DutzLevelObjective.NotifySenateBuildingMuralReached();
    }

    void LateUpdate()
    {
        if (!UsesSenateBuildingWin)
            return;

        var box = GetComponent<BoxCollider>();
        if (box == null || box.size.y >= TriggerDepthMeters)
            return;

        FitTriggerToRenderer(box);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!UsesSenateBuildingWin || !IsPlayerCollider(other))
            return;

        DutzLevelObjective.NotifySenateBuildingMuralReached();
    }

    void OnTriggerStay(Collider other) => OnTriggerEnter(other);

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.GetComponentInParent<DutzPlayerController>() != null)
            return true;

        return other.CompareTag("Player");
    }

    public static bool IsSenateBuildingMuralCollider(Collider col)
    {
        if (col == null)
            return false;

        for (var t = col.transform; t != null; t = t.parent)
        {
            if (t.name == DutzSenateBuildingMural.PanelName
                || t.name == DutzSenateBuildingMural.RootName
                || t.name == WinColliderChildName
                || t.GetComponent<DutzSenateBuildingMuralGoal>() != null
                || t.GetComponent<DutzSenateBuildingMuralGoalTrigger>() != null)
                return true;
        }

        return false;
    }

    public static bool IsPlayerTouchingSenateBuildingMural(DutzPlayerController player)
    {
        if (!UsesSenateBuildingWin || player == null || !DutzDifficulty.HasChosen)
            return false;

        var goal = GetCached();
        if (goal == null)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        var playerBounds = DutzHippieBiteCollider.GetPlayerBodyBounds(cc);

        if (goal.hasRoadWinZone)
        {
            var roadBounds = new Bounds(goal.roadWinCenter, goal.roadWinHalfExtents * 2f);
            if (IsWithinReach(roadBounds, playerBounds, PlayerTouchReachMeters))
                return true;
        }

        var box = goal.GetComponent<BoxCollider>();
        if (box == null || !box.enabled)
            return false;

        return IsWithinReach(box.bounds, playerBounds, PlayerTouchReachMeters);
    }

    static bool IsWithinReach(Bounds targetBounds, Bounds playerBounds, float reach)
    {
        var expanded = playerBounds;
        expanded.Expand(reach * 2f);
        if (!expanded.Intersects(targetBounds))
            return false;

        var closestOnTarget = targetBounds.ClosestPoint(playerBounds.center);
        var closestOnPlayer = playerBounds.ClosestPoint(closestOnTarget);
        return (closestOnTarget - closestOnPlayer).sqrMagnitude <= reach * reach;
    }

    static DutzSenateBuildingMuralGoal GetCached()
    {
        if (cached != null)
            return cached;

        var panel = FindPanelObject();
        if (panel == null)
            return null;

        cached = panel.GetComponent<DutzSenateBuildingMuralGoal>();
        return cached;
    }

    public static GameObject FindPanelObject()
    {
        var panel = GameObject.Find(DutzSenateBuildingMural.PanelName);
        if (panel != null)
            return panel;

        var root = GameObject.Find(DutzSenateBuildingMural.RootName);
        if (root != null)
        {
            var child = root.transform.Find(DutzSenateBuildingMural.PanelName);
            if (child != null)
                return child.gameObject;
        }

        foreach (var rootGo in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var child in rootGo.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == DutzSenateBuildingMural.PanelName)
                    return child.gameObject;
            }
        }

        return null;
    }

    const float RuntimeLookAheadMeters = 20f;
    const float RuntimeLateralOffsetMeters = 11f;
    const float RuntimePanelWidthMeters = 26f;
    const float RuntimeElevatedHeightAboveDeck = 2f;

    static GameObject CreateRuntimePanelIfMissing()
    {
        var player = DutzPlayerController.Instance
            ?? UnityEngine.Object.FindObjectOfType<DutzPlayerController>();
        if (player == null)
            return null;

        if (!TryGetRuntimeMuralPose(player, out var boardCenter, out var faceDir, out var deckY, out var panelHeight))
            return null;

        boardCenter.y = deckY + RuntimeElevatedHeightAboveDeck + panelHeight * 0.5f;

        var root = new GameObject(DutzSenateBuildingMural.RootName);
        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        panel.name = DutzSenateBuildingMural.PanelName;
        panel.transform.SetParent(root.transform, false);
        panel.transform.position = boardCenter;
        panel.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        panel.transform.localScale = new Vector3(RuntimePanelWidthMeters / 10f, 1f, panelHeight / 10f);

        var meshCollider = panel.GetComponent<MeshCollider>();
        if (meshCollider != null)
            UnityEngine.Object.Destroy(meshCollider);

        var material = DutzSenateBuildingMural.GetRuntimeSharedMaterial();
        if (material != null)
        {
            var renderer = panel.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        Debug.Log("[Dutz] Spawned runtime Senate Building mural win goal for Level 0.");
        return panel;
    }

    static bool TryGetRuntimeMuralPose(
        DutzPlayerController player,
        out Vector3 boardCenter,
        out Vector3 faceDir,
        out float deckY,
        out float panelHeight)
    {
        boardCenter = Vector3.zero;
        faceDir = Vector3.right;
        deckY = player.transform.position.y;
        panelHeight = RuntimePanelWidthMeters * 0.75f;

        var spawn = player.transform.position;
        var travelForward = player.transform.forward;
        travelForward.y = 0f;
        if (travelForward.sqrMagnitude < 0.0001f)
        {
            travelForward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
            if (travelForward.sqrMagnitude < 0.0001f)
                travelForward = Vector3.right;
        }

        travelForward.Normalize();
        faceDir = -travelForward;

        var lateral = Vector3.Cross(Vector3.up, travelForward);
        if (lateral.sqrMagnitude < 0.0001f)
            lateral = Vector3.right;
        else
            lateral.Normalize();

        boardCenter = spawn + travelForward * RuntimeLookAheadMeters + lateral * RuntimeLateralOffsetMeters;

        var cc = player.GetComponent<CharacterController>();
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(boardCenter, spawn.y, cc, out var sampledDeckY))
            deckY = sampledDeckY;
        else if (DutzRoadGround.TrySampleRoadDeckY(boardCenter, spawn.y, cc, out sampledDeckY))
            deckY = sampledDeckY;

        var material = DutzSenateBuildingMural.GetRuntimeSharedMaterial();
        var texture = material != null ? material.mainTexture as Texture2D : null;
        if (texture != null && texture.height > 0)
            panelHeight = RuntimePanelWidthMeters * (texture.height / (float)Mathf.Max(1, texture.width));

        return true;
    }
}

/// <summary>Road-deck win trigger child for the Senate mural (offset from the billboard mesh).</summary>
[DisallowMultipleComponent]
public class DutzSenateBuildingMuralGoalTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!DutzSenateBuildingMuralGoal.UsesSenateBuildingWin)
            return;

        if (other == null)
            return;

        if (other.GetComponentInParent<DutzPlayerController>() == null && !other.CompareTag("Player"))
            return;

        DutzLevelObjective.NotifySenateBuildingMuralReached();
    }

    void OnTriggerStay(Collider other) => OnTriggerEnter(other);
}
