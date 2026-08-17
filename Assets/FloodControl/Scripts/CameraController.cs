using UnityEngine;

/// <summary>
/// Orthographic side camera with fixed rotation/zoom/Y/Z.
/// Tracks player X only after leaving a horizontal viewport dead zone.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Fixed Framing")]
    [SerializeField] float fixedY = 10f;
    [SerializeField] float fixedZ = -24f;
    [SerializeField] float orthographicSize = 14.2857f;
    [SerializeField] Vector3 fixedEulerAngles = new Vector3(0f, 0f, 0f);

    [Header("Dead Zone (viewport X 0..1)")]
    [Tooltip("Player screen position when the scene starts.")]
    [SerializeField] float viewportAnchorX = 0.25f;
    [Tooltip("Camera moves backward only after the player crosses this screen position.")]
    [SerializeField] float leftDeadZoneX = 0.17f;
    [Tooltip("Camera moves forward only after the player reaches 30% of the screen.")]
    [SerializeField] float rightDeadZoneX = 0.30f;

    Camera cam;
    float cameraX;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cameraX = transform.position.x;
        ApplyFixedCameraSettings();
    }

    void Start()
    {
        PlaceTargetAtViewportX(viewportAnchorX);
    }

    void LateUpdate()
    {
        ApplyFixedCameraSettings();

        if (target == null || cam == null)
            return;

        Vector3 viewport = cam.WorldToViewportPoint(target.position);
        float clampedViewportX = Mathf.Clamp(viewport.x, leftDeadZoneX, rightDeadZoneX);
        if (!Mathf.Approximately(viewport.x, clampedViewportX))
        {
            // Keep the player at the crossed dead-zone edge without snapping back
            // to the starting anchor.
            Vector3 desiredWorld = cam.ViewportToWorldPoint(new Vector3(
                clampedViewportX,
                viewport.y,
                viewport.z));

            cameraX += target.position.x - desiredWorld.x;
        }

        transform.position = new Vector3(cameraX, fixedY, fixedZ);
        transform.rotation = Quaternion.Euler(fixedEulerAngles);
    }

    void PlaceTargetAtViewportX(float viewportX)
    {
        if (target == null || cam == null)
            return;

        ApplyFixedCameraSettings();
        Vector3 targetViewport = cam.WorldToViewportPoint(target.position);
        Vector3 anchorWorld = cam.ViewportToWorldPoint(new Vector3(
            viewportX,
            targetViewport.y,
            targetViewport.z));

        cameraX += target.position.x - anchorWorld.x;
        transform.position = new Vector3(cameraX, fixedY, fixedZ);
    }

    void ApplyFixedCameraSettings()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
        transform.rotation = Quaternion.Euler(fixedEulerAngles);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void OnValidate()
    {
        orthographicSize = Mathf.Max(0.1f, orthographicSize);
        viewportAnchorX = Mathf.Clamp01(viewportAnchorX);
        leftDeadZoneX = Mathf.Clamp(leftDeadZoneX, 0f, viewportAnchorX);
        rightDeadZoneX = Mathf.Clamp(rightDeadZoneX, viewportAnchorX, 1f);
    }
}
