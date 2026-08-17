using UnityEngine;

/// <summary>Lightning slash and charge sparks on player punch — particle burst + trail on fist.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(31900)]
public class DutzPunchSlashVfx : MonoBehaviour
{
    const string PrefabResourcePath = "DutzPunchSlashVfx";

    [SerializeField] Material slashMaterial;
    [SerializeField] float chargeDuration = 0.08f;
    [SerializeField] float slashDuration = 0.12f;

    static DutzPunchSlashVfx instance;

    Transform fistBone;
    ParticleSystem chargeParticles;
    ParticleSystem slashParticles;
    TrailRenderer fistTrail;
    float chargeEndTime = -1f;
    float slashEndTime = -1f;

    public static void EnsureFromBoot()
    {
        var player = DutzPlayerController.Instance
            ?? Object.FindObjectOfType<DutzPlayerController>();
        if (player == null)
            return;

        if (player.GetComponent<DutzPunchSlashVfx>() == null)
            player.gameObject.AddComponent<DutzPunchSlashVfx>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        CacheFistBone();
        BuildEffectsIfNeeded();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void CacheFistBone()
    {
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Hand_Right_jnt")
            {
                fistBone = child;
                return;
            }
        }
    }

    void BuildEffectsIfNeeded()
    {
        if (fistBone == null)
            CacheFistBone();

        if (fistBone == null)
            return;

        var mat = slashMaterial != null ? slashMaterial : CreateRuntimeMaterial();

        if (chargeParticles == null)
            chargeParticles = CreateChargeSystem(fistBone, mat);

        if (slashParticles == null)
            slashParticles = CreateSlashSystem(fistBone, mat);

        if (fistTrail == null)
            fistTrail = CreateTrail(fistBone, mat);
    }

    static Material CreateRuntimeMaterial()
    {
        var shader = Shader.Find("Mobile/Particles/Additive")
            ?? Shader.Find("Legacy Shaders/Particles/Additive")
            ?? Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            return null;

        var mat = new Material(shader);
        mat.color = new Color(0.55f, 0.92f, 1f, 0.85f);
        return mat;
    }

    static ParticleSystem CreateChargeSystem(Transform parent, Material mat)
    {
        var go = new GameObject("PunchChargeVfx");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 0.12f;
        main.startSpeed = 0.35f;
        main.startSize = 0.08f;
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.4f, 1f),
            new Color(0.4f, 0.85f, 1f, 1f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;

        return ps;
    }

    static ParticleSystem CreateSlashSystem(Transform parent, Material mat)
    {
        var go = new GameObject("PunchSlashVfx");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.15f;
        main.loop = false;
        main.startLifetime = 0.1f;
        main.startSpeed = 2.5f;
        main.startSize = 0.22f;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.75f, 1f),
            new Color(0.35f, 0.9f, 1f, 0.9f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14, 18) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.04f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 3f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.2f;
        renderer.velocityScale = 0.35f;
        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    static TrailRenderer CreateTrail(Transform parent, Material mat)
    {
        var go = new GameObject("PunchFistTrail");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.1f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0.02f;
        trail.minVertexDistance = 0.02f;
        trail.numCapVertices = 4;
        trail.numCornerVertices = 4;
        trail.material = mat;
        trail.startColor = new Color(1f, 0.95f, 0.45f, 0.9f);
        trail.endColor = new Color(0.35f, 0.85f, 1f, 0f);
        trail.emitting = false;
        trail.autodestruct = false;
        return trail;
    }

    public static void PlayCharge(Vector3 worldPosition)
    {
        EnsureInstance();
        instance?.PlayChargeInternal(worldPosition);
    }

    public static void PlaySlash(Vector3 worldPosition, Vector3 forward)
    {
        EnsureInstance();
        instance?.PlaySlashInternal(worldPosition, forward);
    }

    public static void StopAll()
    {
        if (instance == null)
            return;

        instance.chargeEndTime = -1f;
        instance.slashEndTime = -1f;

        if (instance.fistTrail != null)
            instance.fistTrail.emitting = false;
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        EnsureFromBoot();
    }

    void PlayChargeInternal(Vector3 worldPosition)
    {
        BuildEffectsIfNeeded();
        if (chargeParticles == null)
            return;

        if (fistBone != null)
            chargeParticles.transform.position = worldPosition;

        chargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        chargeParticles.Play(true);
        chargeEndTime = Time.time + chargeDuration;
    }

    void PlaySlashInternal(Vector3 worldPosition, Vector3 forward)
    {
        BuildEffectsIfNeeded();
        if (slashParticles == null)
            return;

        if (fistBone != null)
        {
            slashParticles.transform.position = worldPosition;
            if (forward.sqrMagnitude > 0.0001f)
                slashParticles.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        slashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        slashParticles.Play(true);
        slashEndTime = Time.time + slashDuration;

        if (fistTrail != null)
        {
            fistTrail.Clear();
            fistTrail.emitting = true;
        }
    }

    void Update()
    {
        if (chargeEndTime > 0f && Time.time > chargeEndTime)
            chargeEndTime = -1f;

        if (slashEndTime > 0f && Time.time > slashEndTime)
        {
            slashEndTime = -1f;
            if (fistTrail != null)
                fistTrail.emitting = false;
        }
    }
}
