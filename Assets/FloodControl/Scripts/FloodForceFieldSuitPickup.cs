using UnityEngine;

/// <summary>
/// Flood Control Force Field Suit pickup. Grants temporary protection from
/// pipe burns and contact hazards while preserving the shared suit visual.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FloodForceFieldSuitPickup : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] float shieldDurationSeconds = 60f;
    [SerializeField] float spinSpeed = 90f;
    [SerializeField] float bobAmplitude = 0.15f;
    [SerializeField] float bobFrequency = 1.4f;
    [SerializeField] float planarPickupRadius = FloodPlanarPickup.DefaultPlanarRadius;

    Vector3 basePosition;
    Transform suitVisual;
    bool collected;

    void Awake()
    {
        FloodPlanarPickup.SnapToPlayPlane(transform);
        FloodPlanarPickup.EnsureKinematicBody(gameObject);
        FloodPlanarPickup.EnsureDeepTrigger(gameObject);

        basePosition = transform.position;
        suitVisual = transform.Find(DutzForceFieldSuitSetup.SuitModelVisualName);
    }

    void Update()
    {
        if (collected)
            return;

        if (suitVisual != null)
            suitVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

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
        health.ActivateShield(shieldDurationSeconds);
        DutzPowerupPickupSounds.Play(DutzPowerupPickupSounds.Kind.ForceFieldSuit);
        FloodForceFieldVisual.SpawnOnPlayer(health.transform, shieldDurationSeconds, permanent: false);
        gameObject.SetActive(false);
    }

    void OnValidate()
    {
        shieldDurationSeconds = Mathf.Max(0.1f, shieldDurationSeconds);
        bobAmplitude = Mathf.Max(0f, bobAmplitude);
        bobFrequency = Mathf.Max(0f, bobFrequency);
        planarPickupRadius = Mathf.Max(0.5f, planarPickupRadius);
    }
}

/// <summary>Lightweight pulse and lifetime for the Flood force-field bubble.</summary>
public sealed class FloodForceFieldVisual : MonoBehaviour
{
    public const string VisualName = "FloodForceFieldVisual";

    // Player is 2× with a 2-unit capsule; swimming pitch + outstretched arms
    // poke out of the old 2.4 bubble. 4.6 covers the full silhouette.
    const float ShieldLocalDiameter = 4.6f;
    static readonly Color ShieldColor = new Color(0.35f, 0.85f, 1f, 0.22f);

    float expiresAt;
    bool permanent;
    Vector3 baseScale;
    Material runtimeMaterial;

    public static void SpawnOnPlayer(Transform player, float duration, bool permanent)
    {
        if (player == null)
            return;

        Transform existing = player.Find(VisualName);
        if (existing != null)
            Object.Destroy(existing.gameObject);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = VisualName;
        sphere.transform.SetParent(player, false);
        FitToPlayer(sphere.transform, player);

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = ShieldColor;

        MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        FloodForceFieldVisual effect = sphere.AddComponent<FloodForceFieldVisual>();
        if (permanent)
            effect.ConfigurePermanent(material);
        else
            effect.Configure(duration, material);
    }

    public static void FitToPlayer(Transform sphere, Transform player)
    {
        if (sphere == null || player == null)
            return;

        Vector3 center = new Vector3(0f, 1f, 0f);
        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
            center = capsule.center;

        sphere.localPosition = center;
        sphere.localScale = Vector3.one * ShieldLocalDiameter;
    }

    public void CaptureBaseScale()
    {
        baseScale = transform.localScale;
    }

    public void Configure(float duration, Material material)
    {
        permanent = false;
        expiresAt = Time.time + Mathf.Max(0.1f, duration);
        baseScale = transform.localScale;
        runtimeMaterial = material;
    }

    public void ConfigurePermanent(Material material)
    {
        permanent = true;
        baseScale = transform.localScale;
        runtimeMaterial = material;
    }

    void Update()
    {
        if (!permanent && Time.time >= expiresAt)
        {
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 2.2f) * 0.06f;
        transform.localScale = baseScale * pulse;
    }
}
