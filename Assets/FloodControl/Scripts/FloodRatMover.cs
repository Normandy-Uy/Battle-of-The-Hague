using UnityEngine;

/// <summary>
/// Moves a kinematic rat once from right to left, then disables it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FloodRatMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody body;
    [SerializeField] GameManager gameManager;

    [Header("One-Pass Route")]
    [SerializeField] Vector3 startPosition;
    [SerializeField] Vector3 endPosition;
    [SerializeField] float movementSpeed = 5.5f;

    public Vector3 StartPosition => startPosition;
    public Vector3 EndPosition => endPosition;
    public float MovementSpeed => movementSpeed;

    void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        ConfigureBody();
        body.position = startPosition;
    }

    void FixedUpdate()
    {
        if (body == null)
            return;
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        Vector3 next = Vector3.MoveTowards(
            body.position,
            endPosition,
            movementSpeed * Time.fixedDeltaTime);

        if ((next - endPosition).sqrMagnitude <= 0.0025f)
        {
            body.position = endPosition;
            gameObject.SetActive(false);
            return;
        }

        body.MovePosition(next);
    }

    public void Configure(
        Vector3 routeStart,
        Vector3 routeEnd,
        float speed,
        GameManager manager)
    {
        startPosition = routeStart;
        endPosition = routeEnd;
        movementSpeed = Mathf.Max(0f, speed);
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
        movementSpeed = Mathf.Max(0f, movementSpeed);
    }
}
