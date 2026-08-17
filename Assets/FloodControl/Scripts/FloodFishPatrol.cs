using UnityEngine;

/// <summary>
/// Cosmetic fish swim: kinematic ping-pong along a straight corridor.
/// Scene-authored rotation is the spawn / outbound facing; turns use a 180° yaw flip.
/// No colliders, damage, or gameplay interaction.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FloodFishPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody body;
    [SerializeField] GameManager gameManager;

    [Header("Patrol")]
    [SerializeField] Vector3 startPosition;
    [SerializeField] Vector3 endPosition;
    [SerializeField] float patrolSpeed = 3.5f;
    [SerializeField] float arrivalDistance = 0.08f;

    Quaternion facingOutbound;
    Quaternion facingReturn;
    Vector3 currentDestination;
    bool movingTowardEnd;

    public Vector3 StartPosition => startPosition;
    public Vector3 EndPosition => endPosition;
    public float PatrolSpeed => patrolSpeed;

    void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        ConfigureBody();
        CaptureSpawnFacing();
        body.position = startPosition;
        movingTowardEnd = true;
        currentDestination = endPosition;
        ApplyFacing(facingOutbound);
    }

    void FixedUpdate()
    {
        if (body == null)
            return;

        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        Vector3 next = Vector3.MoveTowards(
            body.position,
            currentDestination,
            patrolSpeed * Time.fixedDeltaTime);

        if ((next - currentDestination).sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            body.position = currentDestination;
            movingTowardEnd = !movingTowardEnd;
            currentDestination = movingTowardEnd ? endPosition : startPosition;
            ApplyFacing(movingTowardEnd ? facingOutbound : facingReturn);
            return;
        }

        body.MovePosition(next);
    }

    public void Configure(
        Vector3 patrolStart,
        Vector3 patrolEnd,
        float speed,
        GameManager manager)
    {
        startPosition = patrolStart;
        endPosition = patrolEnd;
        patrolSpeed = Mathf.Max(0f, speed);
        gameManager = manager;

        if (body == null)
            body = GetComponent<Rigidbody>();
        if (body != null)
        {
            ConfigureBody();
            body.position = startPosition;
        }
        else
        {
            transform.position = startPosition;
        }

        CaptureSpawnFacing();
        movingTowardEnd = true;
        currentDestination = endPosition;
        ApplyFacing(facingOutbound);
    }

    void CaptureSpawnFacing()
    {
        // Prefer the Transform pose the user authored in the scene.
        facingOutbound = transform.rotation;
        if (body != null)
            body.rotation = facingOutbound;
        facingReturn = facingOutbound * Quaternion.Euler(0f, 180f, 0f);
    }

    void ApplyFacing(Quaternion facing)
    {
        if (body != null)
            body.rotation = facing;
        transform.rotation = facing;
    }

    void ConfigureBody()
    {
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.detectCollisions = false;
    }

    void OnValidate()
    {
        patrolSpeed = Mathf.Max(0f, patrolSpeed);
        arrivalDistance = Mathf.Max(0.001f, arrivalDistance);
    }
}
