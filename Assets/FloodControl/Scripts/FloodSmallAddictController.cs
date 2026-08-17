using UnityEngine;

/// <summary>
/// Flood Control compatibility for the Level02 small addict.
/// Its original hunter and biter components remain attached; this controller
/// targets the Flood Rigidbody player used in this scene.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(100)]
public sealed class FloodSmallAddictController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody body;
    [SerializeField] PlayerController player;
    [SerializeField] GameManager gameManager;
    [SerializeField] Animator animator;
    [SerializeField] DutzNpcHitPoints hitPoints;

    [Header("Chase")]
    [SerializeField] float wakeDistance = 55f;
    [SerializeField] float chaseSpeed = 7f;
    [SerializeField] float stopDistance = 2.25f;
    [SerializeField] float lockedZ;

    [Header("Kill")]
    [SerializeField] string deathHint = "An addict killed you!";

    [Header("Punch Combat")]
    [SerializeField] int maximumHitPoints = 50;
    [SerializeField] float punchStunSeconds = 0.5f;
    [SerializeField] float punchKnockbackDistance = 1.35f;
    [SerializeField] float playerKnockbackSpeed = 6f;
    [SerializeField] float deathDespawnDelay = 1.5f;

    [Header("Health Display")]
    [SerializeField] bool showHealthWhenNear = true;
    [SerializeField] float healthDisplayDistance = 35f;
    [SerializeField] float healthBarWidth = 210f;
    [SerializeField] float healthBarHeight = 20f;
    [SerializeField] float healthBarRightMargin = 24f;
    [SerializeField] float healthBarTop = 92f;

    static readonly int SpeedId = Animator.StringToHash("Speed_f");
    float stunnedUntil;
    GUIStyle healthLabelStyle;

    void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (hitPoints == null)
            hitPoints = GetComponent<DutzNpcHitPoints>();

        ConfigureBody();
        ConfigureNonBlockingContact();
        lockedZ = transform.position.z;
    }

    void Start() => ConfigureNonBlockingContact();

    void FixedUpdate()
    {
        if (body == null || player == null)
            return;
        if (hitPoints != null && hitPoints.IsDead)
        {
            SetWalking(false);
            enabled = false;
            return;
        }
        if (gameManager != null && !gameManager.IsGameplayEnabled)
        {
            SetWalking(false);
            return;
        }
        if (Time.time < stunnedUntil)
        {
            SetWalking(false);
            return;
        }

        Vector3 current = body.position;
        Vector3 target = player.transform.position;
        target.z = lockedZ;

        Vector3 delta = target - current;
        float distance = delta.magnitude;
        if (distance > wakeDistance || distance <= stopDistance)
        {
            SetWalking(false);
            return;
        }

        Vector3 next = Vector3.MoveTowards(
            current,
            target,
            chaseSpeed * Time.fixedDeltaTime);
        next.z = lockedZ;
        body.MovePosition(next);

        if (Mathf.Abs(delta.x) > 0.05f)
        {
            float yaw = delta.x < 0f ? -90f : 90f;
            body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        SetWalking(true);
    }

    public void Configure(
        float speed,
        float activationDistance,
        float zPosition,
        GameManager manager,
        PlayerController target)
    {
        chaseSpeed = Mathf.Max(0f, speed);
        wakeDistance = Mathf.Max(0f, activationDistance);
        lockedZ = zPosition;
        gameManager = manager;
        player = target;

        if (body == null)
            body = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponent<Animator>();
        ConfigureBody();
        ConfigureNonBlockingContact();
    }

    public void ConfigureCombat(int hitPointTotal)
    {
        maximumHitPoints = Mathf.Max(1, hitPointTotal);
        if (hitPoints == null)
            hitPoints = GetComponent<DutzNpcHitPoints>();
        if (hitPoints == null)
            hitPoints = gameObject.AddComponent<DutzNpcHitPoints>();

        hitPoints.Configure(maximumHitPoints);
        ConfigureNonBlockingContact();
    }

    public bool ReceivePunch(int damage)
    {
        if (!enabled || damage <= 0)
            return false;
        if (hitPoints == null)
            hitPoints = GetComponent<DutzNpcHitPoints>();
        if (hitPoints == null || hitPoints.IsDead)
            return false;

        bool damaged = hitPoints.TakeDamage(damage, player != null ? player.gameObject : null);
        if (!damaged)
            return false;

        ApplyPunchKnockback();
        stunnedUntil = Time.time + punchStunSeconds;
        SetWalking(false);
        if (hitPoints.IsDead)
        {
            enabled = false;
            Destroy(gameObject, deathDespawnDelay);
        }

        return true;
    }

    void ApplyPunchKnockback()
    {
        if (body == null || player == null)
            return;

        Vector3 away = body.position - player.transform.position;
        away.z = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = -player.transform.forward;

        away.Normalize();

        Vector3 knocked = body.position + away * punchKnockbackDistance;
        knocked.z = lockedZ;
        body.MovePosition(knocked);

        Rigidbody playerBody = player.Body;
        if (playerBody != null && !playerBody.isKinematic)
        {
            Vector3 velocity = playerBody.velocity;
            Vector3 planar = away * playerKnockbackSpeed;
            playerBody.velocity = new Vector3(planar.x, velocity.y, 0f);
        }
    }

    void OnTriggerEnter(Collider other) => TryKill(other);
    void OnTriggerStay(Collider other) => TryKill(other);
    void OnCollisionEnter(Collision collision) =>
        TryKill(collision != null ? collision.collider : null);
    void OnCollisionStay(Collision collision) =>
        TryKill(collision != null ? collision.collider : null);

    void TryKill(Collider other)
    {
        if (!enabled || other == null)
            return;

        FloodPlayerHealth health = other.GetComponentInParent<FloodPlayerHealth>();
        if (health == null || health.IsDead)
            return;
        if (hitPoints != null && hitPoints.IsDead)
            return;

        health.Kill(deathHint);
    }

    void ConfigureBody()
    {
        if (body == null)
            return;

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.constraints = RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;
    }

    void ConfigureNonBlockingContact()
    {
        // The Flood player uses a dynamic Rigidbody. A solid kinematic addict
        // can otherwise pin a shielded player against the pipe.
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].isTrigger = true;
        }
    }

    void SetWalking(bool walking)
    {
        if (animator != null)
            animator.SetFloat(SpeedId, walking ? 0.66f : 0f);
    }

    void OnGUI()
    {
        if (!showHealthWhenNear || hitPoints == null || hitPoints.IsDead || player == null)
            return;
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;

        Vector3 toPlayer = player.transform.position - transform.position;
        if (toPlayer.sqrMagnitude > healthDisplayDistance * healthDisplayDistance)
            return;

        float scale = Mathf.Max(1f, Screen.height / 720f);
        float width = healthBarWidth * scale;
        float height = healthBarHeight * scale;
        float x = Screen.width - width - healthBarRightMargin * scale;
        float y = healthBarTop * scale;
        Rect bar = new Rect(x, y, width, height);
        float fill = hitPoints.MaxHitPoints > 0
            ? Mathf.Clamp01((float)hitPoints.CurrentHitPoints / hitPoints.MaxHitPoints)
            : 0f;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = new Color(0.92f, 0.2f, 0.08f, 0.95f);
        GUI.DrawTexture(
            new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * fill, bar.height - 4f),
            Texture2D.whiteTexture);
        GUI.color = previous;

        if (healthLabelStyle == null)
        {
            healthLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        GUI.Label(
            bar,
            $"ADDICT HP: {hitPoints.CurrentHitPoints} / {hitPoints.MaxHitPoints}",
            healthLabelStyle);
    }

    void OnValidate()
    {
        wakeDistance = Mathf.Max(0f, wakeDistance);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
        stopDistance = Mathf.Max(0f, stopDistance);
        maximumHitPoints = Mathf.Max(1, maximumHitPoints);
        punchStunSeconds = Mathf.Max(0f, punchStunSeconds);
        punchKnockbackDistance = Mathf.Max(0f, punchKnockbackDistance);
        playerKnockbackSpeed = Mathf.Max(0f, playerKnockbackSpeed);
        deathDespawnDelay = Mathf.Max(0f, deathDespawnDelay);
        healthDisplayDistance = Mathf.Max(1f, healthDisplayDistance);
        healthBarWidth = Mathf.Max(80f, healthBarWidth);
        healthBarHeight = Mathf.Max(12f, healthBarHeight);
        healthBarRightMargin = Mathf.Max(0f, healthBarRightMargin);
        healthBarTop = Mathf.Max(0f, healthBarTop);
    }
}
