using UnityEngine;

/// <summary>
/// Starts the crocodile bite on contact, then kills Player1 at the bite impact.
/// Force-field players are ignored and never immobilized.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodCrocodileKill : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] CrocodileBiteAnimation biteAnimation;
    [SerializeField] FloodCrocodileSounds crocodileSounds;

    [Header("Death")]
    [SerializeField] string deathHint = "A crocodile killed you!";
    [Tooltip("Short wind-up so the bite is visible before the death dialog appears.")]
    [SerializeField] float biteImpactDelay = 0.75f;

    FloodPlayerHealth pendingHealth;
    PlayerController frozenController;
    float impactTimer;
    bool waitingForImpact;

    public string DeathHint => deathHint;

    void Awake()
    {
        if (biteAnimation == null)
            biteAnimation = GetComponent<CrocodileBiteAnimation>();
        if (crocodileSounds == null)
            crocodileSounds = GetComponent<FloodCrocodileSounds>();
    }

    public void Configure(
        string hint,
        CrocodileBiteAnimation animation = null)
    {
        if (!string.IsNullOrWhiteSpace(hint))
            deathHint = hint;
        if (animation != null)
            biteAnimation = animation;
    }

    void Update()
    {
        if (!waitingForImpact)
            return;

        if (pendingHealth == null || pendingHealth.IsDead || pendingHealth.IsShielded)
        {
            CancelPendingBite();
            return;
        }

        impactTimer -= Time.deltaTime;
        if (impactTimer > 0f)
            return;

        waitingForImpact = false;
        FloodPlayerHealth health = pendingHealth;
        pendingHealth = null;
        frozenController = null;

        if (health != null && !health.IsDead && !health.IsShielded)
            health.Kill(deathHint);
        else
            RestoreController(health);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryKill(collision != null ? collision.collider : null);
    }

    void OnCollisionStay(Collision collision)
    {
        TryKill(collision != null ? collision.collider : null);
    }

    void OnTriggerEnter(Collider other)
    {
        TryKill(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryKill(other);
    }

    void TryKill(Collider other)
    {
        if (!enabled || other == null || waitingForImpact)
            return;

        FloodPlayerHealth health = other.GetComponentInParent<FloodPlayerHealth>();
        if (health == null || health.IsDead)
            return;

        // Shielded players must keep swimming — do not freeze them.
        if (health.IsShielded)
            return;

        if (biteAnimation != null)
            biteAnimation.TriggerBite(health.transform);
        if (crocodileSounds != null)
            crocodileSounds.PlayBite();

        PlayerController playerController = health.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
            frozenController = playerController;
        }

        Rigidbody playerBody = health.GetComponent<Rigidbody>();
        if (playerBody != null)
            playerBody.velocity = Vector3.zero;

        pendingHealth = health;
        impactTimer = biteImpactDelay;
        waitingForImpact = true;
    }

    void CancelPendingBite()
    {
        FloodPlayerHealth health = pendingHealth;
        waitingForImpact = false;
        pendingHealth = null;
        RestoreController(health);
        frozenController = null;
    }

    void RestoreController(FloodPlayerHealth health)
    {
        PlayerController controller = frozenController;
        if (controller == null && health != null)
            controller = health.GetComponent<PlayerController>();
        if (controller != null)
            controller.enabled = true;
    }

    void OnValidate()
    {
        biteImpactDelay = Mathf.Max(0.05f, biteImpactDelay);
    }
}
