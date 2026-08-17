using UnityEngine;

/// <summary>
/// Level07 Elevator — vertical patrol platform.
/// Default: idle until player pays via dialog, then patrols.
/// Optional: free-running patrol with one-time suitcase charge on first landing.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-40)]
public class DutzElevatorVerticalPatrol : MonoBehaviour
{
    public const string ElevatorObjectName = "Elevator";
    public const int SuitcaseFare = 5;

    const float ConfirmDelay = 0.18f;
    const int OverlayGuiDepth = -2400;

    [SerializeField, Min(0.5f)] float moveSpeedMetersPerSecond = 10f;
    [SerializeField, Min(0f)] float pauseAtEndsSeconds = 0.4f;
    [SerializeField, Tooltip("Absolute world Y (Unity units) for the top of the patrol.")]
    float maxHeightWorldY;
    [SerializeField] bool carryPlayer = true;
    [SerializeField] float riderHorizontalPadMeters = 1.25f;
    [SerializeField] float riderFeetAboveDeckMeters = 1.6f;
    [SerializeField, Min(1f)] float approachDistanceMeters = 10f;
    [SerializeField, Min(1)] int suitcaseCost = SuitcaseFare;
    [SerializeField, Tooltip("If true, elevator waits still until the player pays via dialog.")]
    bool requirePayDialogToStart = true;
    [SerializeField, Tooltip("If true, patrols immediately and charges suitcases once when the player lands.")]
    bool chargeSuitcasesOnLand = false;

    float bottomY;
    float topY;
    float direction = 1f;
    float pauseUntil;
    Vector3 lastPosition;
    Collider elevatorCollider;
    Rigidbody body;
    BoxCollider standCollider;

    bool paid;
    bool landFarePaid;
    bool wasRiding;
    bool operating;
    bool showingDialog;
    bool wasInRange;
    float shownAt;
    string statusMessage;
    float landFareFlashUntil;
    DutzPlayerController player;

    static DutzElevatorVerticalPatrol instance;

    public float BottomY => bottomY;
    public float TopY => topY;
    public bool IsOperating => operating;
    public bool IsShowingDialog => showingDialog;
    public static bool IsAnyDialogOpen => instance != null && instance.showingDialog;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() => instance = null;

    public static void ResetOnPlayerRespawn()
    {
        foreach (var elevator in Object.FindObjectsOfType<DutzElevatorVerticalPatrol>())
            elevator?.ResetForRespawn();
    }

    void Awake()
    {
        instance = this;
        EnsureStandableCollider();
        CaptureTravelRange();
        SnapToBottom();
        lastPosition = transform.position;
        direction = 1f;
        paid = false;
        landFarePaid = false;
        wasRiding = false;
        operating = !requirePayDialogToStart;
        showingDialog = false;
        wasInRange = false;
        statusMessage = string.Empty;
        landFareFlashUntil = 0f;
        player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void OnValidate()
    {
        moveSpeedMetersPerSecond = Mathf.Max(0.5f, moveSpeedMetersPerSecond);
        pauseAtEndsSeconds = Mathf.Max(0f, pauseAtEndsSeconds);
        approachDistanceMeters = Mathf.Max(1f, approachDistanceMeters);
        suitcaseCost = Mathf.Max(1, suitcaseCost);
    }

    void Reset() => EnsureStandableCollider();

    /// <summary>
    /// MeshColliders (especially non-convex / scaled) fail as CharacterController floors.
    /// Swap to a solid BoxCollider matching the visible deck.
    /// </summary>
    public void EnsureStandableCollider()
    {
        foreach (var meshCol in GetComponents<MeshCollider>())
        {
            if (Application.isPlaying)
                Destroy(meshCol);
            else
                DestroyImmediate(meshCol);
        }

        var renderer = GetComponent<Renderer>();
        Bounds worldBounds;
        if (renderer != null)
            worldBounds = renderer.bounds;
        else
            worldBounds = new Bounds(transform.position, new Vector3(8f, 1f, 8f));

        standCollider = GetComponent<BoxCollider>();
        if (standCollider == null)
            standCollider = gameObject.AddComponent<BoxCollider>();

        var lossy = transform.lossyScale;
        var absX = Mathf.Max(0.0001f, Mathf.Abs(lossy.x));
        var absY = Mathf.Max(0.0001f, Mathf.Abs(lossy.y));
        var absZ = Mathf.Max(0.0001f, Mathf.Abs(lossy.z));

        var localCenter = transform.InverseTransformPoint(worldBounds.center);
        var localSize = new Vector3(
            Mathf.Max(0.5f, worldBounds.size.x / absX),
            Mathf.Max(0.35f / absY, worldBounds.size.y / absY),
            Mathf.Max(0.5f, worldBounds.size.z / absZ));

        standCollider.center = localCenter;
        standCollider.size = localSize;
        standCollider.isTrigger = false;
        standCollider.enabled = true;
        elevatorCollider = standCollider;

        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void CaptureTravelRange()
    {
        bottomY = transform.position.y;
        topY = ResolveTopY();
        if (topY <= bottomY + 0.5f)
            topY = bottomY + 0.5f;
    }

    void SnapToBottom()
    {
        var pos = transform.position;
        pos.y = bottomY;
        if (body != null)
            body.position = pos;
        transform.position = pos;
        lastPosition = pos;
        direction = 1f;
        pauseUntil = 0f;
    }

    void ResetForRespawn()
    {
        CloseDialog();
        paid = false;
        landFarePaid = false;
        wasRiding = false;
        operating = !requirePayDialogToStart;
        wasInRange = false;
        statusMessage = string.Empty;
        landFareFlashUntil = 0f;
        SnapToBottom();
    }

    float ResolveTopY()
    {
        if (maxHeightWorldY > bottomY + 0.5f)
            return maxHeightWorldY;

        // Fallback if the absolute max was never authored.
        return bottomY + 60f;
    }

    void Update()
    {
        if (player == null)
            player = DutzPlayerController.Instance;

        if (showingDialog)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseDialog();
            lastPosition = transform.position;
            return;
        }

        if (!operating)
        {
            if (requirePayDialogToStart)
                UpdateApproachPrompt();
            lastPosition = transform.position;
            return;
        }

        UpdateLandFareCharge();
        TickPatrol();
    }

    void UpdateApproachPrompt()
    {
        if (!requirePayDialogToStart || paid || player == null)
            return;

        var near = IsPlayerNear(player);
        if (!near)
        {
            wasInRange = false;
            return;
        }

        if (!wasInRange)
            OpenDialog();

        wasInRange = near;
    }

    void UpdateLandFareCharge()
    {
        if (!chargeSuitcasesOnLand || landFarePaid || player == null)
        {
            if (player != null)
            {
                var ccIdle = player.GetComponent<CharacterController>();
                wasRiding = ccIdle != null && IsPlayerRiding(ccIdle);
            }

            return;
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc == null || !cc.enabled)
        {
            wasRiding = false;
            return;
        }

        var riding = IsPlayerRiding(cc);
        if (riding && !wasRiding)
            TryChargeLandFare();

        wasRiding = riding;
    }

    void TryChargeLandFare()
    {
        if (landFarePaid)
            return;

        var cost = Mathf.Max(1, suitcaseCost);
        if (!DutzCollectibleProgress.TrySpend(cost))
        {
            landFareFlashUntil = Time.time + 2.5f;
            Debug.Log(
                $"[Dutz] Elevator land fare unpaid — need {cost} suitcases " +
                $"(have {DutzCollectibleProgress.CollectedCount}).");
            return;
        }

        landFarePaid = true;
        paid = true;
        landFareFlashUntil = Time.time + 2f;
        Debug.Log($"[Dutz] Elevator land fare: {cost} suitcases deducted.");
    }

    bool IsPlayerNear(DutzPlayerController target)
    {
        if (target == null)
            return false;

        var cc = target.GetComponent<CharacterController>();
        var playerPoint = cc != null ? cc.bounds.center : target.transform.position;
        var elevPoint = elevatorCollider != null
            ? elevatorCollider.bounds.ClosestPoint(playerPoint)
            : transform.position;

        var delta = playerPoint - elevPoint;
        delta.y *= 0.35f;
        var reach = approachDistanceMeters;
        if (cc != null)
            reach += cc.radius * Mathf.Max(1f, cc.transform.lossyScale.x);

        return delta.sqrMagnitude <= reach * reach;
    }

    void OpenDialog()
    {
        showingDialog = true;
        shownAt = Time.unscaledTime;
        statusMessage = string.Empty;
        if (player != null)
            player.SetControlsLocked(true);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void CloseDialog()
    {
        showingDialog = false;
        statusMessage = string.Empty;
        if (player != null)
            player.SetControlsLocked(false);
    }

    bool CanConfirm() => Time.unscaledTime - shownAt >= ConfirmDelay;

    void TryPayAndStart()
    {
        if (!CanConfirm())
            return;

        var cost = Mathf.Max(1, suitcaseCost);
        if (DutzCollectibleProgress.CollectedCount < cost)
        {
            statusMessage =
                $"Need {cost} suitcases (you have {DutzCollectibleProgress.CollectedCount}).";
            return;
        }

        if (!DutzCollectibleProgress.TrySpend(cost))
        {
            statusMessage = "Could not spend suitcases.";
            return;
        }

        paid = true;
        operating = true;
        direction = 1f;
        pauseUntil = 0f;
        CloseDialog();
        Debug.Log($"[Dutz] Elevator paid ({cost} suitcases) — patrol started.");
    }

    void DismissDialog()
    {
        if (!CanConfirm())
            return;

        CloseDialog();
    }

    void TickPatrol()
    {
        if (Time.time < pauseUntil)
        {
            lastPosition = transform.position;
            return;
        }

        var pos = transform.position;
        pos.y += direction * moveSpeedMetersPerSecond * Time.deltaTime;

        if (direction > 0f && pos.y >= topY)
        {
            pos.y = topY;
            direction = -1f;
            pauseUntil = Time.time + pauseAtEndsSeconds;
        }
        else if (direction < 0f && pos.y <= bottomY)
        {
            pos.y = bottomY;
            direction = 1f;
            pauseUntil = Time.time + pauseAtEndsSeconds;
        }

        if (body != null)
            body.MovePosition(pos);
        else
            transform.position = pos;

        var delta = pos - lastPosition;
        lastPosition = pos;

        if (carryPlayer && delta.sqrMagnitude > 0.0000001f)
            CarryRiders(delta);
    }

    void CarryRiders(Vector3 delta)
    {
        var rider = player != null ? player : DutzPlayerController.Instance;
        if (rider == null || !rider.isActiveAndEnabled)
            return;

        var cc = rider.GetComponent<CharacterController>();
        if (cc == null || !cc.enabled)
            return;

        if (!IsPlayerRiding(cc))
            return;

        cc.Move(delta);
    }

    bool IsPlayerRiding(CharacterController cc)
    {
        var playerPos = cc.transform.position;
        var elevPos = transform.position;

        var horizontal = new Vector2(playerPos.x - elevPos.x, playerPos.z - elevPos.z);
        var maxRadius = riderHorizontalPadMeters;
        if (elevatorCollider != null)
        {
            var extents = elevatorCollider.bounds.extents;
            maxRadius = Mathf.Max(extents.x, extents.z) + riderHorizontalPadMeters;
        }

        if (horizontal.magnitude > maxRadius)
            return false;

        var deckY = elevPos.y;
        if (elevatorCollider != null)
            deckY = elevatorCollider.bounds.max.y;

        var feetY = playerPos.y;
        var scaleY = Mathf.Max(0.01f, cc.transform.lossyScale.y);
        feetY += (cc.center.y - cc.height * 0.5f) * scaleY;

        return feetY >= deckY - 0.35f && feetY <= deckY + riderFeetAboveDeckMeters;
    }

    void OnGUI()
    {
        var fareCost = Mathf.Max(1, suitcaseCost);

        if (Time.time < landFareFlashUntil)
        {
            if (landFarePaid)
                DutzAnnouncementHud.DrawFlash(
                    $"ELEVATOR FARE — {fareCost} SUITCASES",
                    new Color(0.35f, 0.95f, 0.55f));
            else
                DutzAnnouncementHud.DrawFlash(
                    $"NEED {fareCost} SUITCASES TO RIDE",
                    new Color(0.95f, 0.55f, 0.25f));
        }

        if (!showingDialog || !requirePayDialogToStart)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = OverlayGuiDepth;
        DutzCartoonDialogGui.DrawDimOverlay();

        var title = "ELEVATOR";
        var hint =
            $"Pay {fareCost} suitcases to operate the elevator.\n" +
            $"You have {DutzCollectibleProgress.CollectedCount} suitcases.";
        var payLabel = $"PAY {fareCost} SUITCASES";
        var dismissLabel = "DISMISS";
        var labels = new[] { payLabel, dismissLabel };
        var height = DutzCartoonDialogGui.ChoiceDialogHeight(title, hint, labels);
        if (!string.IsNullOrEmpty(statusMessage))
            height += DutzCartoonDialogGui.Scale(28f, 40f);

        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(height);
        DutzCartoonDialogGui.DrawFrame(frame);

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);

        GUILayout.Label(title, DutzCartoonDialogGui.BannerTitleStyle(new Color(0.15f, 0.55f, 0.7f)));
        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        GUILayout.Label(hint, DutzCartoonDialogGui.HintStyle());
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 14f));

        var canPay = DutzCollectibleProgress.CollectedCount >= fareCost;
        GUI.enabled = canPay && CanConfirm();
        if (DutzCartoonDialogGui.ActionButton(
                payLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(payLabel)))
            TryPayAndStart();
        GUI.enabled = true;

        if (!canPay)
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(4f, 6f));
            GUILayout.Label(
                $"Need {fareCost} suitcases (you have {DutzCollectibleProgress.CollectedCount}).",
                DutzCartoonDialogGui.HintStyle());
        }

        GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
        if (DutzCartoonDialogGui.DismissButton(
                dismissLabel,
                heightOverride: DutzCartoonDialogGui.MeasureActionButtonHeight(dismissLabel)))
            DismissDialog();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(DutzCartoonDialogGui.Scale(6f, 10f));
            GUILayout.Label(statusMessage, DutzCartoonDialogGui.BodyStyle());
        }

        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var min = Application.isPlaying ? bottomY : transform.position.y;
        var max = Application.isPlaying ? topY : ResolveTopYPreview();
        var p = transform.position;
        Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.9f);
        Gizmos.DrawLine(new Vector3(p.x, min, p.z), new Vector3(p.x, max, p.z));
        Gizmos.DrawWireSphere(new Vector3(p.x, min, p.z), 0.6f);
        Gizmos.DrawWireSphere(new Vector3(p.x, max, p.z), 0.6f);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(p, approachDistanceMeters);
    }

    float ResolveTopYPreview()
    {
        var savedBottom = bottomY;
        bottomY = transform.position.y;
        var y = ResolveTopY();
        bottomY = savedBottom;
        return y;
    }
#endif
}
