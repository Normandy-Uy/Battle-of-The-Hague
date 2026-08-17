using System.Collections;
using UnityEngine;

/// <summary>
/// Flood Control adaptation of Player1's regular punch. It preserves the
/// familiar input and animation without requiring the campaign controller.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class FloodPlayerPunch : MonoBehaviour
{
    static readonly int PunchTriggerId = Animator.StringToHash("Punch_b");

    [Header("Input")]
    [SerializeField] KeyCode punchKey = KeyCode.F;

    [Header("Punch")]
    [SerializeField] int damage = 10;
    [SerializeField] float punchReach = 2.2f;
    [SerializeField] float punchRadius = 1.35f;
    [SerializeField] float punchHeight = 1.75f;
    [SerializeField] float punchCooldownDurationSeconds = 1f;
    [SerializeField] float damageDelaySeconds = 0.11f;
    [SerializeField] float animationDurationSeconds = 0.3f;
    [SerializeField, Range(0f, 1f)] float hitSoundVolume = 1f;

    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] FloodPlayerHealth playerHealth;
    [SerializeField] Animator animator;
    [SerializeField] SwimmingAnimationController swimmingAnimation;
    [SerializeField] AudioSource hitAudioSource;

    readonly Collider[] punchHits = new Collider[16];
    float nextPunchTime;
    Coroutine punchRoutine;

    public int Damage => damage;
    public float CooldownRemaining => Mathf.Max(0f, nextPunchTime - Time.time);

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (playerHealth == null)
            playerHealth = GetComponent<FloodPlayerHealth>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (swimmingAnimation == null)
            swimmingAnimation = GetComponent<SwimmingAnimationController>();
        EnsureHitAudioSource();
    }

    void Update()
    {
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;
        if (playerHealth != null && playerHealth.IsDead)
            return;
        if (Time.time < nextPunchTime || !WasPunchPressedThisFrame())
            return;

        BeginPunch();
    }

    public void Configure(int punchDamage, float cooldownSeconds = 1f)
    {
        damage = Mathf.Max(1, punchDamage);
        punchCooldownDurationSeconds = Mathf.Max(0.1f, cooldownSeconds);
    }

    public void SetPunchDamage(int punchDamage)
    {
        damage = Mathf.Max(1, punchDamage);
    }

    bool WasPunchPressedThisFrame()
    {
        if (Input.GetKeyDown(punchKey))
            return true;
        if (DutzRobloxMobileInput.IsMobileControlsActive)
            return DutzRobloxMobileInput.PunchPressedThisFrame;

        return Input.GetMouseButtonDown(0);
    }

    void BeginPunch()
    {
        nextPunchTime = Time.time + punchCooldownDurationSeconds;

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        if (swimmingAnimation != null)
            swimmingAnimation.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger(PunchTriggerId);
            animator.SetTrigger(PunchTriggerId);
        }

        if (damageDelaySeconds > 0f)
            yield return new WaitForSeconds(damageDelaySeconds);

        ApplyPunchDamage();

        float remainingAnimation = Mathf.Max(0f, animationDurationSeconds - damageDelaySeconds);
        if (remainingAnimation > 0f)
            yield return new WaitForSeconds(remainingAnimation);

        if (swimmingAnimation != null)
        {
            swimmingAnimation.enabled = true;
            swimmingAnimation.EnterSwimmingPose();
        }

        punchRoutine = null;
    }

    void ApplyPunchDamage()
    {
        float scale = Mathf.Max(1f, transform.lossyScale.y);
        Vector3 origin = transform.position + Vector3.up * (punchHeight * scale);
        Vector3 center = origin + transform.forward * (punchReach * scale);
        float radius = punchRadius * scale;

        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            punchHits,
            ~0,
            QueryTriggerInteraction.Collide);

        FloodSmallAddictController damagedAddict = null;
        bool connected = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = punchHits[i];
            punchHits[i] = null;
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            FloodSmallAddictController addict =
                hit.GetComponentInParent<FloodSmallAddictController>();
            if (addict == null || addict == damagedAddict)
                continue;

            if (addict.ReceivePunch(damage))
            {
                damagedAddict = addict;
                connected = true;
            }
        }

        if (connected)
            PlayHitSound();
    }

    void EnsureHitAudioSource()
    {
        Transform audioChild = transform.Find("FloodPunchHitAudio");
        GameObject host;
        if (audioChild != null)
        {
            host = audioChild.gameObject;
        }
        else
        {
            host = new GameObject("FloodPunchHitAudio");
            host.transform.SetParent(transform, false);
        }

        hitAudioSource = host.GetComponent<AudioSource>();
        if (hitAudioSource == null)
            hitAudioSource = host.AddComponent<AudioSource>();

        hitAudioSource.playOnAwake = false;
        hitAudioSource.loop = false;
        hitAudioSource.spatialBlend = 0f;
        hitAudioSource.dopplerLevel = 0f;
        hitAudioSource.priority = 24;
        hitAudioSource.ignoreListenerPause = true;
    }

    void PlayHitSound()
    {
        if (hitAudioSource == null)
            EnsureHitAudioSource();
        if (hitAudioSource == null)
            return;

        hitAudioSource.pitch = Random.Range(0.94f, 1.06f);
        hitAudioSource.PlayOneShot(
            FloodAudioClips.PunchHit,
            DutzAudioSettings.ScaleSfx(hitSoundVolume));
    }

    void OnGUI()
    {
        if (gameManager != null && !gameManager.IsGameplayEnabled)
            return;
        if (playerHealth != null && playerHealth.IsDead)
            return;

        float remaining = CooldownRemaining;
        if (remaining <= 0.05f)
            return;

        string label = $"PUNCH {remaining:0.0}s";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = DutzCartoonDialogGui.ScaleFont(22, 30),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        Vector2 size = style.CalcSize(new GUIContent(label));
        Rect rect = new Rect(
            (Screen.width - size.x) * 0.5f,
            Screen.height * 0.72f,
            size.x,
            size.y);
        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), label, shadow);
        GUI.Label(rect, label, style);
    }

    void OnDisable()
    {
        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
            punchRoutine = null;
        }

        if (swimmingAnimation != null && !swimmingAnimation.enabled)
        {
            swimmingAnimation.enabled = true;
            swimmingAnimation.EnterSwimmingPose();
        }
    }

    void OnValidate()
    {
        damage = Mathf.Max(1, damage);
        punchReach = Mathf.Max(0.1f, punchReach);
        punchRadius = Mathf.Max(0.1f, punchRadius);
        punchHeight = Mathf.Max(0f, punchHeight);
        punchCooldownDurationSeconds = Mathf.Max(0.1f, punchCooldownDurationSeconds);
        damageDelaySeconds = Mathf.Max(0f, damageDelaySeconds);
        animationDurationSeconds = Mathf.Max(damageDelaySeconds, animationDurationSeconds);
        hitSoundVolume = Mathf.Clamp01(hitSoundVolume);
    }
}
