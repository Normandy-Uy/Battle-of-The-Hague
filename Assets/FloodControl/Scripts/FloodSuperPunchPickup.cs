using UnityEngine;

/// <summary>
/// Flood Control Super Punch glove. Exposes punch damage on the glove and
/// upgrades FloodPlayerPunch when Player1 collects it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FloodSuperPunchPickup : MonoBehaviour
{
    [Header("Upgrade")]
    [SerializeField] int punchDamage = 50;

    [Header("Pickup")]
    [SerializeField] float spinSpeed = 75f;
    [SerializeField] float bobAmplitude = 0.2f;
    [SerializeField] float bobFrequency = 1.2f;
    [SerializeField] float planarPickupRadius = FloodPlanarPickup.DefaultPlanarRadius;

    Vector3 basePosition;
    Transform gloveVisual;
    bool collected;

    public int PunchDamage => punchDamage;

    void Awake()
    {
        // Campaign collector is CharacterController-based; Flood uses Rigidbody triggers.
        DutzSuperPunchPickup campaign = GetComponent<DutzSuperPunchPickup>();
        if (campaign != null)
            campaign.enabled = false;

        DutzSuperPunchCollector collector = GetComponent<DutzSuperPunchCollector>();
        if (collector != null)
            collector.enabled = false;

        gloveVisual = transform.Find(DutzSuperPunchPickupSetup.VisualChildName);
        FloodPlanarPickup.RecenterRootOnVisual(transform, gloveVisual);
        FloodPlanarPickup.SnapToPlayPlane(transform);
        FloodPlanarPickup.EnsureKinematicBody(gameObject);
        FloodPlanarPickup.EnsureDeepTrigger(gameObject);

        basePosition = transform.position;
    }

    void Update()
    {
        if (collected)
            return;

        if (gloveVisual != null)
            gloveVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = basePosition + Vector3.up * bob;

        if (FloodPlanarPickup.IsPlayerInPlanarRange(transform, planarPickupRadius, out FloodPlayerHealth health))
            Collect(health);
    }

    void OnTriggerEnter(Collider other) => TryCollect(other);
    void OnTriggerStay(Collider other) => TryCollect(other);

    void TryCollect(Collider other)
    {
        if (collected || other == null)
            return;

        FloodPlayerHealth health = other.GetComponentInParent<FloodPlayerHealth>();
        if (health == null || health.IsDead)
            return;

        Collect(health);
    }

    void Collect(FloodPlayerHealth health)
    {
        if (collected || health == null || health.IsDead)
            return;

        FloodPlayerPunch punch = health.GetComponent<FloodPlayerPunch>();
        if (punch == null)
            punch = health.gameObject.AddComponent<FloodPlayerPunch>();

        collected = true;
        punch.SetPunchDamage(punchDamage);
        DutzPowerupPickupSounds.Play(DutzPowerupPickupSounds.Kind.SuperPunch);
        gameObject.SetActive(false);
        Debug.Log(
            $"[FloodControl] Super Punch collected — Player1 punch damage set to {punch.Damage}.");
    }

    void OnValidate()
    {
        punchDamage = Mathf.Max(1, punchDamage);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobFrequency = Mathf.Max(0f, bobFrequency);
        planarPickupRadius = Mathf.Max(0.5f, planarPickupRadius);
    }
}
