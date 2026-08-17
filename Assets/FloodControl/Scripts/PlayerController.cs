using UnityEngine;

/// <summary>
/// Underwater Rigidbody swimming on the X-Y plane. Input is sampled in Update;
/// forces are applied in FixedUpdate.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] Rigidbody body;

    [Header("Horizontal")]
    [SerializeField] float forwardSpeed = 10f;
    [SerializeField] float backwardSpeed = 4f;
    [SerializeField] float acceleration = 12f;
    [SerializeField] float horizontalDrag = 4f;

    [Header("Automatic Forward Movement")]
    [SerializeField] bool automaticForwardMovement = true;
    [SerializeField] float automaticForwardSpeed = 5f;
    [Tooltip("How long Left must be held before backward swimming begins.")]
    [SerializeField] float backwardSwimDelay = 1.5f;
    [Tooltip("How quickly Left removes the automatic forward speed.")]
    [SerializeField] float brakingAcceleration = 24f;

    [Header("Vertical")]
    [SerializeField] float upwardForce = 21f;
    [SerializeField] float gravityScale = 0.75f;
    [SerializeField] float verticalDrag = 1.2f;
    [SerializeField] float maximumVerticalSpeed = 7f;

    bool leftHeld;
    bool rightHeld;
    bool thrustHeld;
    float leftHeldDuration;

    public Rigidbody Body => body;

    void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        ConfigureRigidbody();
    }

    void ConfigureRigidbody()
    {
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        if (gameManager != null && !gameManager.IsGameplayEnabled)
        {
            leftHeld = false;
            rightHeld = false;
            thrustHeld = false;
            leftHeldDuration = 0f;
            return;
        }

        if (DutzRobloxMobileInput.IsMobileControlsActive)
        {
            Vector2 move = DutzRobloxMobileInput.MoveAxis;
            leftHeld = move.x < -0.1f;
            rightHeld = move.x > 0.1f;
            thrustHeld = move.y > 0.1f || DutzRobloxMobileInput.JumpHeld;
        }
        else
        {
            leftHeld = Input.GetKey(KeyCode.LeftArrow);
            rightHeld = Input.GetKey(KeyCode.RightArrow);
            thrustHeld = Input.GetKey(KeyCode.UpArrow);
        }

        if (leftHeld)
            leftHeldDuration += Time.deltaTime;
        else
            leftHeldDuration = 0f;
    }

    void FixedUpdate()
    {
        if (body == null)
            return;

        if (gameManager != null && !gameManager.IsGameplayEnabled)
        {
            body.velocity = Vector3.zero;
            return;
        }

        Vector3 velocity = body.velocity;

        float targetHorizontal;
        float activeAcceleration = acceleration;

        if (leftHeld)
        {
            if (leftHeldDuration < backwardSwimDelay)
            {
                // First brake the automatic forward movement without reversing.
                targetHorizontal = 0f;
                activeAcceleration = brakingAcceleration;
            }
            else
            {
                targetHorizontal = -backwardSpeed;
            }
        }
        else if (rightHeld)
        {
            targetHorizontal = forwardSpeed;
        }
        else
        {
            targetHorizontal = automaticForwardMovement ? automaticForwardSpeed : 0f;
        }

        float nextHorizontal = Mathf.MoveTowards(
            velocity.x,
            targetHorizontal,
            activeAcceleration * Time.fixedDeltaTime);

        if (Mathf.Approximately(targetHorizontal, 0f))
            nextHorizontal = ApplyDrag(nextHorizontal, horizontalDrag, Time.fixedDeltaTime);

        // Custom underwater gravity + optional continuous upward thrust.
        float nextVertical = velocity.y + Physics.gravity.y * gravityScale * Time.fixedDeltaTime;
        if (thrustHeld)
            nextVertical += upwardForce * Time.fixedDeltaTime;

        nextVertical = ApplyDrag(nextVertical, verticalDrag, Time.fixedDeltaTime);
        nextVertical = Mathf.Clamp(nextVertical, -maximumVerticalSpeed, maximumVerticalSpeed);

        body.velocity = new Vector3(nextHorizontal, nextVertical, 0f);
    }

    static float ApplyDrag(float value, float drag, float deltaTime)
    {
        if (drag <= 0f)
            return value;

        float factor = Mathf.Clamp01(1f - drag * deltaTime);
        return value * factor;
    }

    void OnValidate()
    {
        forwardSpeed = Mathf.Max(0f, forwardSpeed);
        backwardSpeed = Mathf.Max(0f, backwardSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        horizontalDrag = Mathf.Max(0f, horizontalDrag);
        automaticForwardSpeed = Mathf.Max(0f, automaticForwardSpeed);
        backwardSwimDelay = Mathf.Max(0f, backwardSwimDelay);
        brakingAcceleration = Mathf.Max(0f, brakingAcceleration);
        upwardForce = Mathf.Max(0f, upwardForce);
        gravityScale = Mathf.Max(0f, gravityScale);
        verticalDrag = Mathf.Max(0f, verticalDrag);
        maximumVerticalSpeed = Mathf.Max(0.1f, maximumVerticalSpeed);
    }
}
