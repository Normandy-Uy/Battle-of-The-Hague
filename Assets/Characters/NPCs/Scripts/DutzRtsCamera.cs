using UnityEngine;

/// <summary>
/// Legacy RTS camera — conflicts with DutzCameraFollow. Do not use on Main Camera.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
public class DutzRtsCamera : MonoBehaviour
{
    const string PlayerObjectName = DutzPlayerController.PlayerObjectName;

    [SerializeField] Transform target;
    [Header("Angle (StarCraft / Kingshot)")]
    [SerializeField] float pitch = 63f;
    [SerializeField] float yaw = 37f;
    [Header("Framing")]
    [SerializeField] float distance = 38f;
    [SerializeField] float heightBoost = 10f;
    [SerializeField] float lookAheadDistance = 8f;
    [SerializeField] float focusHeight = 1.1f;
    [Tooltip("0 = camera locked on player with no lag")]
    [SerializeField] float followSmoothTime = 0f;

    static readonly Vector3 DefaultLookForward = Vector3.right;

    Vector3 dampVelocity;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.farClipPlane = 500f;
    }

    void OnEnable()
    {
        ResolveTarget();
        ApplyFollow(snap: true);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        ResolveTarget();
        if (target == null)
            return;

        ApplyFollow(snap: followSmoothTime <= 0f);
    }

    void ResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
            return;

        var player = FindObjectOfType<DutzPlayerController>();
        if (player != null)
        {
            target = player.transform;
            return;
        }

        var named = GameObject.Find(PlayerObjectName);
        if (named != null)
            target = named.transform;
    }

    public void BindTarget(Transform followTarget)
    {
        target = followTarget;
        ApplyFollow(snap: true);
    }

    public void TrackPlayer(Transform player)
    {
        target = player;
        ApplyFollow(snap: followSmoothTime <= 0f);
    }

    public void SnapToTarget()
    {
        ApplyFollow(snap: true);
    }

    void ApplyFollow(bool snap)
    {
        var focus = GetFocusPoint();
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        var desiredPosition = focus + rot * new Vector3(0f, heightBoost, -distance);

        if (snap)
        {
            transform.SetPositionAndRotation(desiredPosition, rot);
            dampVelocity = Vector3.zero;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref dampVelocity, followSmoothTime);
        transform.rotation = rot;
    }

    Vector3 GetFocusPoint()
    {
        var basePos = target != null ? target.position : Vector3.zero;
        return basePos + GetLookAheadOffset() + Vector3.up * focusHeight;
    }

    Vector3 GetLookAheadOffset()
    {
        if (target == null)
            return DefaultLookForward * lookAheadDistance;

        var forward = target.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = DefaultLookForward;

        return forward.normalized * lookAheadDistance;
    }

    public static void EnsureOnMainCamera(Transform player)
    {
        var main = Camera.main;
        if (main == null)
        {
            var any = FindObjectOfType<Camera>();
            if (any != null)
                any.tag = "MainCamera";
            main = Camera.main;
        }

        if (main == null)
            return;

        var rts = main.GetComponent<DutzRtsCamera>();
        if (rts == null)
            rts = main.gameObject.AddComponent<DutzRtsCamera>();

        if (player != null)
            rts.BindTarget(player);
        else
            rts.SnapToTarget();
    }

#if UNITY_EDITOR
    public static void ApplyEditorPose(Transform camera, Vector3 playerPosition, Vector3 playerForward)
    {
        var rts = camera.GetComponent<DutzRtsCamera>();
        if (rts == null)
            return;

        var forward = playerForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = DefaultLookForward;

        var focus = playerPosition + forward.normalized * rts.lookAheadDistance + Vector3.up * rts.focusHeight;
        var rot = Quaternion.Euler(rts.pitch, rts.yaw, 0f);
        camera.SetPositionAndRotation(
            focus + rot * new Vector3(0f, rts.heightBoost, -rts.distance),
            rot);
    }
#endif
}
