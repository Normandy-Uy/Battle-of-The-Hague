using UnityEngine;

/// <summary>Parachute glide feedback — canopy puffs, wind streaks, and back trail while gliding.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(52)]
public class DutzParachuteGlideVfx : MonoBehaviour
{
    static readonly Color GlideColorA = new Color(0.55f, 0.9f, 1f, 0.85f);
    static readonly Color GlideColorB = new Color(0.85f, 0.98f, 1f, 0.65f);
    static readonly Color PackedColor = new Color(0.45f, 0.82f, 1f, 0.55f);

    Transform anchorBone;
    Transform canopyAnchor;
    Transform trailAnchor;

    ParticleSystem canopyPuffs;
    ParticleSystem windStreaks;
    ParticleSystem packedSparkles;
    TrailRenderer glideTrail;

    DutzPlayerParachute parachute;
    CharacterController cc;
    bool glideEffectsPlaying;
    bool packedEffectsPlaying;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var player = DutzPlayerController.Instance
            ?? Object.FindObjectOfType<DutzPlayerController>();
        if (player == null)
            return;

        if (player.GetComponent<DutzParachuteGlideVfx>() == null)
            player.gameObject.AddComponent<DutzParachuteGlideVfx>();
    }

    void Awake()
    {
        parachute = GetComponent<DutzPlayerParachute>();
        cc = GetComponent<CharacterController>();
        CacheAnchorBone();
        BuildEffectsIfNeeded();
        SetAllEffectsActive(false);
    }

    void CacheAnchorBone()
    {
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Spine_jnt" || child.name == "Chest_jnt")
            {
                anchorBone = child;
                return;
            }
        }

        anchorBone = transform;
    }

    float GetEffectScale()
    {
        if (cc != null)
            return Mathf.Max(1f, cc.transform.lossyScale.y);

        return Mathf.Max(1f, transform.lossyScale.y);
    }

    void BuildEffectsIfNeeded()
    {
        if (anchorBone == null)
            CacheAnchorBone();

        var mat = CreateRuntimeMaterial();
        if (mat == null)
            return;

        var scale = GetEffectScale();

        if (canopyAnchor == null)
        {
            var go = new GameObject("ParachuteCanopyVfx");
            go.transform.SetParent(anchorBone, false);
            go.transform.localPosition = new Vector3(0f, 0.55f * scale, -0.12f * scale);
            canopyAnchor = go.transform;
        }

        if (trailAnchor == null)
        {
            var go = new GameObject("ParachuteTrailVfx");
            go.transform.SetParent(anchorBone, false);
            go.transform.localPosition = new Vector3(0f, 0.2f * scale, -0.28f * scale);
            trailAnchor = go.transform;
        }

        if (canopyPuffs == null)
            canopyPuffs = CreateCanopySystem(canopyAnchor, mat, scale);

        if (windStreaks == null)
            windStreaks = CreateWindStreakSystem(canopyAnchor, mat, scale);

        if (glideTrail == null)
            glideTrail = CreateGlideTrail(trailAnchor, mat, scale);

        if (packedSparkles == null)
            packedSparkles = CreatePackedSparkleSystem(trailAnchor, mat, scale);
    }

    static Material CreateRuntimeMaterial()
    {
        var shader = Shader.Find("Mobile/Particles/Additive")
            ?? Shader.Find("Legacy Shaders/Particles/Additive")
            ?? Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            return null;

        var mat = new Material(shader);
        mat.color = GlideColorA;
        return mat;
    }

    static void PrepareNewParticleSystem(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
    }

    static ParticleSystem CreateCanopySystem(Transform parent, Material mat, float scale)
    {
        var go = new GameObject("CanopyPuffs");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        PrepareNewParticleSystem(ps);
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f * scale, 0.85f * scale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f * scale, 0.55f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.75f * scale);
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(GlideColorB, GlideColorA);

        var emission = ps.emission;
        emission.rateOverTime = 14f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f * scale;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.35f * scale, 0.15f * scale);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.65f, 1f, 1.15f));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(GlideColorB, 0f),
                new GradientColorKey(GlideColorA, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.35f, 1f)
            });
        colorOverLife.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    static ParticleSystem CreateWindStreakSystem(Transform parent, Material mat, float scale)
    {
        var go = new GameObject("WindStreaks");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -0.08f * scale, 0f);
        go.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        PrepareNewParticleSystem(ps);
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f * scale, 0.5f * scale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scale, 5f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.18f * scale);
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.95f, 1f, 0.55f),
            new Color(0.4f, 0.8f, 1f, 0.25f));

        var emission = ps.emission;
        emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.35f * scale;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.8f;
        renderer.velocityScale = 0.25f;
        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    static ParticleSystem CreatePackedSparkleSystem(Transform parent, Material mat, float scale)
    {
        var go = new GameObject("PackedSparkles");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        PrepareNewParticleSystem(ps);
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.2f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.12f * scale);
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = PackedColor;

        var emission = ps.emission;
        emission.rateOverTime = 4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f * scale;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    static TrailRenderer CreateGlideTrail(Transform parent, Material mat, float scale)
    {
        var go = new GameObject("GlideTrail");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.45f * scale;
        trail.endWidth = 0.04f * scale;
        trail.minVertexDistance = 0.08f * scale;
        trail.numCapVertices = 4;
        trail.numCornerVertices = 4;
        trail.material = mat;
        trail.startColor = new Color(0.55f, 0.92f, 1f, 0.75f);
        trail.endColor = new Color(0.35f, 0.8f, 1f, 0f);
        trail.emitting = false;
        trail.autodestruct = false;
        return trail;
    }

    void Update()
    {
        if (parachute == null)
            parachute = GetComponent<DutzPlayerParachute>();

        BuildEffectsIfNeeded();

        var shouldGlide = parachute != null && parachute.IsGlidingSafely;
        var shouldShowPacked = parachute != null
            && parachute.HasParachuteActive
            && cc != null
            && cc.isGrounded
            && !shouldGlide;

        if (shouldGlide != glideEffectsPlaying)
        {
            glideEffectsPlaying = shouldGlide;
            SetGlideEffectsActive(shouldGlide);
        }

        if (shouldShowPacked != packedEffectsPlaying)
        {
            packedEffectsPlaying = shouldShowPacked;
            SetPackedEffectsActive(shouldShowPacked);
        }
    }

    void SetAllEffectsActive(bool on)
    {
        SetGlideEffectsActive(on);
        SetPackedEffectsActive(on);
        glideEffectsPlaying = on;
        packedEffectsPlaying = on;
    }

    void SetGlideEffectsActive(bool on)
    {
        if (on)
        {
            if (canopyPuffs != null)
            {
                canopyPuffs.Clear(true);
                canopyPuffs.Play(true);
            }

            if (windStreaks != null)
            {
                windStreaks.Clear(true);
                windStreaks.Play(true);
            }

            if (glideTrail != null)
            {
                glideTrail.Clear();
                glideTrail.emitting = true;
            }
        }
        else
        {
            if (canopyPuffs != null)
                canopyPuffs.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (windStreaks != null)
                windStreaks.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (glideTrail != null)
                glideTrail.emitting = false;
        }
    }

    void SetPackedEffectsActive(bool on)
    {
        if (packedSparkles == null)
            return;

        if (on)
        {
            packedSparkles.Clear(true);
            packedSparkles.Play(true);
        }
        else
        {
            packedSparkles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
