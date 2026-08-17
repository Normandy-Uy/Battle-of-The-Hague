using UnityEngine;

/// <summary>
/// Third-person follow camera. Roblox style: orbit with right mouse; movement uses camera forward.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class DutzCameraFollow : MonoBehaviour
{
    const string TargetName = DutzPlayerController.PlayerObjectName;
    const string HouseName = "Building_House_04_color02";

    [SerializeField] Transform target;
    [Header("Style")]
    [SerializeField] bool robloxStyle = true;
    [Header("Orbit")]
    [SerializeField] float pitch = 18f;
    [SerializeField] float yaw;
    [SerializeField] float distance = 20f;
    [SerializeField] float heightBoost = 2f;
    [SerializeField] float focusHeight = 4f;
    [SerializeField] float fieldOfView = 70f;
    [SerializeField] float positionSmoothTime = 0.12f;
    [Header("Legacy Kingshot (when Roblox Style is off)")]
    [SerializeField] float kingshotSideYaw = 50f;
    [SerializeField] float lookAhead = 4f;
    [SerializeField] bool orbitRelativeToFacing = true;
    [SerializeField] float focusSmoothTime = 0.1f;
    [SerializeField] float forwardSmoothSpeed = 10f;
    [Header("Mouse look (hold Right Mouse Button)")]
    [SerializeField] bool requireRightMouseToOrbit = true;
    [SerializeField] float mouseSensitivity = 2.5f;
    [SerializeField] float mobileLookSensitivity = 0.2f;
    [SerializeField] float minPitch = 5f;
    [SerializeField] float maxPitch = 70f;

    Camera cam;
    Vector3 positionVelocity;
    Vector3 focusVelocity;
    Vector3 smoothedForward = Vector3.right;
    Vector3 smoothedFocus;
    bool hasSmoothedState;
    bool highwaySpawnViewReady;
    float punchShakeTimeLeft;
    float punchShakeDuration;
    float punchShakeIntensity;

    public static DutzCameraFollow Instance { get; private set; }

    public void PlayPunchShake(bool heavyHit)
    {
        punchShakeIntensity = heavyHit ? 0.42f : 0.18f;
        punchShakeDuration = heavyHit ? 0.14f : 0.08f;
        punchShakeTimeLeft = punchShakeDuration;
    }

    /// <summary>Camera forward on the XZ plane (for Roblox-style movement).</summary>
    public Vector3 FlatForward
    {
        get
        {
            var forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }
    }

    /// <summary>Camera right on the XZ plane.</summary>
    public Vector3 FlatRight
    {
        get
        {
            var right = transform.right;
            right.y = 0f;
            return right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        }
    }

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        highwaySpawnViewReady = !robloxStyle;
        ApplyRobloxDefaults();
        ApplyProjection();
        DisableConflictingCameraScripts();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        ResolveTarget();
        if (!robloxStyle && target != null)
            ApplyView(snap: true);
    }

    void Update()
    {
        if (!Application.isPlaying || !robloxStyle)
            return;

        ResolveTarget();
        if (target == null || !highwaySpawnViewReady)
            return;

        HandleMouseLook();
        ApplyView(snap: positionSmoothTime <= 0f);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        ResolveTarget();
        if (target == null)
            return;

        if (robloxStyle)
            return;

        HandleMouseLook();
        ApplyView(snap: positionSmoothTime <= 0f);
    }

    void HandleMouseLook()
    {
        if (DutzRobloxMobileInput.IsMobileControlsActive)
        {
            var delta = DutzRobloxMobileInput.LookDelta;
            if (delta.sqrMagnitude > 0.0001f)
            {
                yaw += delta.x * mobileLookSensitivity;
                pitch -= delta.y * mobileLookSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            return;
        }

        var orbiting = !requireRightMouseToOrbit || Input.GetMouseButton(1);
        if (requireRightMouseToOrbit)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (!orbiting)
            return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    public void ApplyRobloxDefaults()
    {
        robloxStyle = true;
        requireRightMouseToOrbit = true;
        pitch = 18f;
        distance = 20f;
        heightBoost = 2f;
        focusHeight = 4f;
        fieldOfView = 70f;
        positionSmoothTime = 0.12f;
        mouseSensitivity = 2.5f;
        minPitch = 5f;
        maxPitch = 70f;
    }

    public void BindTarget(Transform followTarget)
    {
        target = followTarget;
        if (target == null || robloxStyle)
            return;

        ApplyView(snap: true);
    }

    /// <summary>Roblox spawn: face down the highway, camera behind so W runs forward along the road.</summary>
    public void SnapRobloxSpawnFacing()
    {
        if (target == null)
            return;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(target.position);
        if (forward.sqrMagnitude < 0.0001f)
            return;

        SnapRobloxSpawnFacing(forward);
    }

    /// <summary>Camera behind Dutz using a flat world forward (e.g. inverted spawn facing).</summary>
    public void SnapRobloxSpawnFacing(Vector3 flatForward)
    {
        if (target == null)
            return;

        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f)
            return;

        flatForward.Normalize();
        target.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        yaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        highwaySpawnViewReady = true;
        smoothedForward = flatForward;
        hasSmoothedState = true;
        ApplyRobloxView(snap: true);
    }

    /// <summary>Forward of the track spawn segment (Highway Bridge 1).</summary>
    public static Vector3 GetHighwayForward() => DutzHighwayDirection.GetReferenceForward();

    /// <summary>Flat direction along the road at spawn (tangent sign from spawn segment).</summary>
    public static Vector3 GetSpawnForwardAt(Vector3 worldPosition) =>
        DutzHighwayDirection.GetSpawnForwardAt(worldPosition);

    public Vector3 GetLevelForward()
    {
        if (target != null)
            return GetSpawnForwardAt(target.position);

        return GetHighwayForward();
    }

    void DisableConflictingCameraScripts()
    {
        var rts = GetComponent<DutzRtsCamera>();
        if (rts != null)
            rts.enabled = false;
    }

    void ApplyProjection()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        if (cam == null)
            return;

        cam.orthographic = false;
        cam.fieldOfView = fieldOfView;
        cam.farClipPlane = 500f;
    }

    void ResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            if (target.GetComponentInParent<DutzPlayerController>() != null)
                return;
        }

        var player = DutzPlayerController.Instance;
        if (player != null)
        {
            target = player.transform;
            return;
        }

        var go = GameObject.Find(TargetName);
        if (go != null)
            target = go.transform;
    }

    void ApplyView(bool snap)
    {
        if (robloxStyle)
            ApplyRobloxView(snap);
        else
            ApplyKingshotView(snap);
    }

    void ApplyRobloxView(bool snap)
    {
        var focus = target.position + Vector3.up * focusHeight;
        var orbitRot = Quaternion.Euler(pitch, yaw, 0f);
        var desiredPos = focus + orbitRot * new Vector3(0f, heightBoost, -distance);
        ApplyPunchShakeOffset(ref desiredPos);

        if (snap || positionSmoothTime <= 0f)
        {
            transform.position = desiredPos;
            positionVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref positionVelocity, positionSmoothTime);
        }

        var lookDir = focus - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    void ApplyKingshotView(bool snap)
    {
        var targetForward = target.forward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude < 0.01f)
            targetForward = Vector3.right;
        else
            targetForward.Normalize();

        if (snap || !hasSmoothedState)
        {
            smoothedForward = targetForward;
            hasSmoothedState = true;
        }
        else
        {
            smoothedForward = Vector3.Slerp(
                smoothedForward,
                targetForward,
                Mathf.Clamp01(forwardSmoothSpeed * Time.deltaTime));
            if (smoothedForward.sqrMagnitude < 0.0001f)
                smoothedForward = targetForward;
            else
                smoothedForward.Normalize();
        }

        var targetFocus = target.position + smoothedForward * lookAhead + Vector3.up * focusHeight;
        if (snap || focusSmoothTime <= 0f || !hasSmoothedState)
        {
            smoothedFocus = targetFocus;
            focusVelocity = Vector3.zero;
        }
        else
        {
            smoothedFocus = Vector3.SmoothDamp(
                smoothedFocus,
                targetFocus,
                ref focusVelocity,
                focusSmoothTime);
        }

        var orbitYaw = kingshotSideYaw;
        if (orbitRelativeToFacing)
        {
            var facingYaw = Mathf.Atan2(smoothedForward.x, smoothedForward.z) * Mathf.Rad2Deg;
            orbitYaw += facingYaw;
        }

        var offsetRot = Quaternion.Euler(pitch, orbitYaw, 0f);
        var desiredPos = smoothedFocus + offsetRot * new Vector3(0f, heightBoost, -distance);
        ApplyPunchShakeOffset(ref desiredPos);

        if (snap || positionSmoothTime <= 0f)
        {
            transform.position = desiredPos;
            positionVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref positionVelocity, positionSmoothTime);
        }

        var lookDir = smoothedFocus - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
    }

    void ApplyPunchShakeOffset(ref Vector3 desiredPos)
    {
        if (punchShakeTimeLeft <= 0f || punchShakeDuration <= 0f)
            return;

        punchShakeTimeLeft = Mathf.Max(0f, punchShakeTimeLeft - Time.deltaTime);
        var strength = punchShakeIntensity * (punchShakeTimeLeft / punchShakeDuration);
        desiredPos += Random.insideUnitSphere * strength;
    }

    void OnValidate()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        ApplyProjection();
    }
}
