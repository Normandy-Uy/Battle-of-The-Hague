using UnityEngine;

/// <summary>
/// Flood Control green health potion. Restores HP on Player1 contact.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FloodHealthPotionPickup : MonoBehaviour
{
    [Header("Heal")]
    [SerializeField] int healAmount = DutzHealthPotion.DefaultHealAmount;
    [SerializeField] float spinSpeed = 60f;
    [SerializeField] float bobAmplitude = 0.12f;
    [SerializeField] float bobFrequency = 1.5f;
    [SerializeField] float planarPickupRadius = FloodPlanarPickup.DefaultPlanarRadius;

    Vector3 basePosition;
    Transform potionVisual;
    bool collected;

    void Awake()
    {
        FloodPlanarPickup.SnapToPlayPlane(transform);
        FloodPlanarPickup.EnsureKinematicBody(gameObject);
        FloodPlanarPickup.EnsureDeepTrigger(gameObject);

        basePosition = transform.position;
        potionVisual = transform.Find(DutzHealthPotionSetup.VisualChildName);
    }

    void Update()
    {
        if (collected)
            return;

        if (potionVisual != null)
            potionVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        else
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

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

        collected = true;
        health.Heal(healAmount);
        gameObject.SetActive(false);
    }

    void OnValidate()
    {
        healAmount = Mathf.Max(1, healAmount);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobFrequency = Mathf.Max(0f, bobFrequency);
        planarPickupRadius = Mathf.Max(0.5f, planarPickupRadius);
    }
}
