using UnityEngine;

/// <summary>
/// Moves a kinematic crocodile vertically between two points. At either endpoint,
/// it instantly turns 180 degrees and swims back along the same route.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class VerticalCrocodilePatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody body;
    [SerializeField] GameManager gameManager;

    [Header("Patrol")]
    [SerializeField] Vector3 startPosition;
    [SerializeField] Vector3 endPosition;
    [SerializeField] float patrolSpeed = 1.75f;
    [SerializeField] float arrivalDistance = 0.05f;

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
        body.position = startPosition;
        movingTowardEnd = true;
        currentDestination = endPosition;
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
            body.rotation *= Quaternion.Euler(0f, 0f, 180f);
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
            movingTowardEnd = true;
            currentDestination = endPosition;
        }
        else
        {
            transform.position = startPosition;
        }
    }

    void ConfigureBody()
    {
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void OnValidate()
    {
        patrolSpeed = Mathf.Max(0f, patrolSpeed);
        arrivalDistance = Mathf.Max(0.001f, arrivalDistance);
    }
}
