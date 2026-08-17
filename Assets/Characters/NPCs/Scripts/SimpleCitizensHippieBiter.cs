using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Human-scale bite / body colliders for small hippies (mesh bounds are often huge in Z).</summary>
public static class DutzHippieBiteCollider
{
    /// <summary>Padding added to player CharacterController bounds for touch tests.</summary>
    public const float PlayerTouchBoundsPadding = 0.2f;
    /// <summary>Max gap between collider surface and player body for a bite (avoids AABB false positives).</summary>
    public const float BiteReachMeters = 1.5f;
    /// <summary>Surface gap treated as physical collision (player capsule vs addict collider).</summary>
    public const float CollisionContactSlop = 0.35f;
    /// <summary>Extra radius on the player capsule for collision overlap scans.</summary>
    public const float PlayerCapsulePadding = 0.25f;
    const float TriggerSizeTolerance = 0.12f;

    public static readonly Vector3 BiteCenter = new Vector3(0f, 1.05f, 0.32f);
    public static readonly Vector3 BiteSize = new Vector3(0.85f, 1.75f, 1f);
    public static readonly Vector3 SolidCenter = new Vector3(0f, 1.4f, 0f);
    public static readonly Vector3 SolidSize = new Vector3(0.75f, 2.8f, 1.4f);
    public static readonly Vector3 MaxSolidSize = new Vector3(1f, 2.8f, 1.4f);
    const float MisalignedSolidCenterX = 1f;

    public static Vector3 ClampSolidSize(Vector3 size) =>
        new Vector3(
            Mathf.Min(size.x, MaxSolidSize.x),
            Mathf.Min(size.y, MaxSolidSize.y),
            Mathf.Min(size.z, MaxSolidSize.z));

    public static void ApplyBiteTrigger(BoxCollider bite)
    {
        if (bite == null)
            return;

        bite.isTrigger = true;
        bite.center = BiteCenter;
        bite.size = BiteSize;
    }

    public static bool NeedsBiteTriggerFix(BoxCollider bite)
    {
        if (bite == null)
            return true;

        var tol = TriggerSizeTolerance;
        return Mathf.Abs(bite.size.x - BiteSize.x) > tol
            || Mathf.Abs(bite.size.y - BiteSize.y) > tol
            || Mathf.Abs(bite.size.z - BiteSize.z) > tol
            || bite.size.z > BiteSize.z + tol
            || Vector3.Distance(bite.center, BiteCenter) > tol;
    }

    public static bool NeedsSolidColliderFix(BoxCollider solid) =>
        solid != null && !solid.isTrigger && Mathf.Abs(solid.center.x) > MisalignedSolidCenterX;

    public static void ApplyHumanoidSolidCollider(BoxCollider solid)
    {
        if (solid == null || solid.isTrigger)
            return;

        solid.center = SolidCenter;
        solid.size = ClampSolidSize(SolidSize);
    }

    /// <summary>Humanoid bite trigger + centered solid body so the player can reach the bite volume.</summary>
    public static void EnsureSmallHippieColliders(GameObject root)
    {
        if (root == null)
            return;

        if (DutzCrocodilePoolMember.IsCrocodile(root))
        {
            EnsureCrocodileColliders(root);
            return;
        }

        var skipBiteTriggerFix = DutzGiantBossNames.IsJonremPolice(root.name);

        BoxCollider bite = null;
        BoxCollider solid = null;
        foreach (var col in root.GetComponents<BoxCollider>())
        {
            if (col == null)
                continue;

            if (col.isTrigger)
                bite = col;
            else
                solid = col;
        }

        if (!skipBiteTriggerFix)
        {
            if (bite == null)
            {
                bite = root.AddComponent<BoxCollider>();
                ApplyBiteTrigger(bite);
            }
            else if (NeedsBiteTriggerFix(bite))
            {
                ApplyBiteTrigger(bite);
            }
        }

        if (solid == null)
        {
            solid = root.AddComponent<BoxCollider>();
            ApplyHumanoidSolidCollider(solid);
        }
        else if (NeedsSolidColliderFix(solid))
        {
            ApplyHumanoidSolidCollider(solid);
        }
    }

    /// <summary>
    /// Tall solid (above player stepOffset) + wide trigger. Kill uses <see cref="CrocBiteReachMeters"/>.
    /// </summary>
    public static void EnsureCrocodileColliders(GameObject root)
    {
        if (root == null || !DutzCrocodilePoolMember.IsCrocodile(root))
            return;

        Physics.SyncTransforms();

        if (!TryGetCrocodileLocalColliderBounds(root, out var localCenter, out var localSize))
            return;

        BoxCollider bite = null;
        BoxCollider solid = null;
        foreach (var col in root.GetComponents<BoxCollider>())
        {
            if (col == null)
                continue;

            if (col.isTrigger)
                bite = col;
            else
                solid = col;
        }

        if (bite == null)
        {
            bite = root.AddComponent<BoxCollider>();
            bite.isTrigger = true;
        }

        if (solid == null)
            solid = root.AddComponent<BoxCollider>();

        var solidBottomY = localCenter.y - localSize.y * 0.5f;
        // Above stepOffset (~1.25) so walk-over is blocked; SuperJump clears via height-aware kill checks.
        var solidHeight = Mathf.Max(localSize.y, CrocSolidMinHeight);
        var solidSize = new Vector3(
            localSize.x + CrocSolidExpandXZ * 2f,
            solidHeight,
            localSize.z + CrocSolidExpandXZ * 2f);
        var solidCenter = new Vector3(localCenter.x, solidBottomY + solidHeight * 0.5f, localCenter.z);

        var biteSize = new Vector3(
            solidSize.x + CrocBiteTriggerExpandXZ * 2f,
            Mathf.Max(solidHeight, CrocBiteTriggerMinHeight),
            solidSize.z + CrocBiteTriggerExpandXZ * 2f);
        var biteCenter = new Vector3(localCenter.x, solidBottomY + biteSize.y * 0.5f, localCenter.z);

        bite.isTrigger = true;
        bite.center = biteCenter;
        bite.size = biteSize;

        solid.isTrigger = false;
        solid.center = solidCenter;
        solid.size = solidSize;
    }

    const float CrocColliderPadding = 0.12f;
    /// <summary>Above stepOffset (~1.25); SuperJump clears when kill tests respect height.</summary>
    const float CrocSolidMinHeight = 1.5f;
    const float CrocSolidExpandXZ = 0.35f;
    const float CrocBiteTriggerExpandXZ = 0.45f;
    const float CrocBiteTriggerMinHeight = 2.2f;
    /// <summary>Tougher than addicts (1.5) without the old midair 3.5 m aura.</summary>
    public const float CrocBiteReachMeters = 2.4f;
    /// <summary>Vault grace: feet this far above croc collider top skip flat proximity kill.</summary>
    public const float CrocVaultClearanceMeters = 0.4f;
    /// <summary>
    /// Max vertical gap between player body and croc collider for a kill.
    /// Blocks upper-deck / Bridge kills from Highway 7 crocs below that share XZ.
    /// </summary>
    public const float CrocMaxVerticalKillSeparationMeters = 3f;

    /// <summary>
    /// False when the player is clearly on a different vertical deck (e.g. Bridge 1 above Highway 7).
    /// </summary>
    public static bool IsPlayerVerticallyInCrocKillRange(CharacterController playerCc, Collider col)
    {
        if (playerCc == null || col == null || !col.enabled)
            return false;

        var colBounds = col.bounds;
        var playerFeetY = playerCc.transform.position.y;

        // SuperJump / upper deck: feet clear of collider top.
        if (playerFeetY >= colBounds.max.y + CrocVaultClearanceMeters)
            return false;

        var playerBounds = GetPlayerBodyBounds(playerCc);
        float verticalGap;
        if (playerBounds.min.y > colBounds.max.y)
            verticalGap = playerBounds.min.y - colBounds.max.y;
        else if (colBounds.min.y > playerBounds.max.y)
            verticalGap = colBounds.min.y - playerBounds.max.y;
        else
            verticalGap = 0f;

        return verticalGap <= CrocMaxVerticalKillSeparationMeters;
    }

    public static bool TryGetCrocodileLocalColliderBounds(GameObject root, out Vector3 center, out Vector3 size)
    {
        center = default;
        size = default;
        if (root == null)
            return false;

        var visual = root.transform.Find(DutzCrocodilePoolMember.VisualChildName);
        if (visual == null)
            return false;

        var hasMesh = false;
        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        foreach (var meshFilter in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            var meshBounds = meshFilter.sharedMesh.bounds;
            var meshTransform = meshFilter.transform;
            foreach (var corner in BoundsCorners(meshBounds))
            {
                var rootLocal = root.transform.InverseTransformPoint(meshTransform.TransformPoint(corner));
                min = Vector3.Min(min, rootLocal);
                max = Vector3.Max(max, rootLocal);
                hasMesh = true;
            }
        }

        if (!hasMesh)
        {
            foreach (var rend in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null)
                    continue;

                var worldBounds = rend.bounds;
                foreach (var corner in BoundsCorners(new Bounds(worldBounds.center, worldBounds.size)))
                {
                    var rootLocal = root.transform.InverseTransformPoint(corner);
                    min = Vector3.Min(min, rootLocal);
                    max = Vector3.Max(max, rootLocal);
                    hasMesh = true;
                }
            }
        }

        if (!hasMesh)
            return false;

        center = (min + max) * 0.5f;
        size = max - min + Vector3.one * (CrocColliderPadding * 2f);
        return size.sqrMagnitude > 0.0001f;
    }

    static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
    {
        var c = bounds.center;
        var e = bounds.extents;
        yield return c + new Vector3(-e.x, -e.y, -e.z);
        yield return c + new Vector3(-e.x, -e.y, e.z);
        yield return c + new Vector3(-e.x, e.y, -e.z);
        yield return c + new Vector3(-e.x, e.y, e.z);
        yield return c + new Vector3(e.x, -e.y, -e.z);
        yield return c + new Vector3(e.x, -e.y, e.z);
        yield return c + new Vector3(e.x, e.y, -e.z);
        yield return c + new Vector3(e.x, e.y, e.z);
    }

    public static Bounds GetPlayerBodyBounds(CharacterController cc)
    {
        if (cc == null)
            return default;

        var pad = PlayerTouchBoundsPadding;
        var center = cc.transform.position + cc.center;
        var size = new Vector3(
            (cc.radius + pad) * 2f,
            cc.height + pad * 2f,
            (cc.radius + pad) * 2f);
        return new Bounds(center, size);
    }

    public static bool IsTouchingPlayerBody(Collider body, CharacterController playerCc) =>
        IsTouchingPlayerBody(body, GetPlayerBodyBounds(playerCc));

    public static bool IsTouchingPlayerBody(Collider body, Bounds playerBounds) =>
        IsTouchingPlayerBody(body, playerBounds, BiteReachMeters);

    public static bool IsTouchingPlayerBody(Collider body, Bounds playerBounds, float maxGapMeters)
    {
        if (body == null || !body.enabled || playerBounds.size.sqrMagnitude < 0.0001f)
            return false;

        if (SupportsColliderClosestPoint(body))
        {
            var closestOnBody = body.ClosestPoint(playerBounds.center);
            var closestOnPlayer = playerBounds.ClosestPoint(closestOnBody);
            var gapSq = (closestOnBody - closestOnPlayer).sqrMagnitude;
            return gapSq <= maxGapMeters * maxGapMeters;
        }

        return IsWithinReachOfBounds(body.bounds, playerBounds, maxGapMeters);
    }

    static bool SupportsColliderClosestPoint(Collider body) =>
        body is BoxCollider
        || body is SphereCollider
        || body is CapsuleCollider
        || (body is MeshCollider mesh && mesh.convex);

    static bool IsWithinReachOfBounds(Bounds targetBounds, Bounds playerBounds, float reach)
    {
        var expanded = playerBounds;
        expanded.Expand(reach * 2f);
        if (!expanded.Intersects(targetBounds))
            return false;

        var closestOnTarget = targetBounds.ClosestPoint(playerBounds.center);
        var closestOnPlayer = playerBounds.ClosestPoint(closestOnTarget);
        return (closestOnTarget - closestOnPlayer).sqrMagnitude <= reach * reach;
    }

    public static bool IsTouchingPlayerBody(Collider[] bodies, CharacterController playerCc)
    {
        if (playerCc == null || bodies == null)
            return false;

        var playerBounds = GetPlayerBodyBounds(playerCc);
        foreach (var body in bodies)
        {
            if (IsTouchingPlayerBody(body, playerBounds))
                return true;
        }

        return false;
    }

    public static void GetPlayerCapsule(CharacterController cc, float padding, out Vector3 bottom, out Vector3 top, out float radius)
    {
        var worldCenter = cc.transform.position + cc.center;
        var halfHeight = cc.height * 0.5f;
        var cylinderHalf = Mathf.Max(halfHeight - cc.radius, 0.01f);
        bottom = worldCenter + Vector3.down * cylinderHalf;
        top = worldCenter + Vector3.up * cylinderHalf;
        radius = cc.radius + padding;
    }

    public static Vector3 ClosestPointOnCapsule(Vector3 point, Vector3 bottom, Vector3 top, float radius)
    {
        var axis = top - bottom;
        var len = axis.magnitude;
        if (len < 0.0001f)
        {
            var flat = point - bottom;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.000001f)
                return bottom + Vector3.forward * radius;
            return bottom + flat.normalized * radius;
        }

        var axisDir = axis / len;
        var t = Vector3.Dot(point - bottom, axisDir);
        t = Mathf.Clamp(t, 0f, len);
        var onSegment = bottom + axisDir * t;
        var toPoint = point - onSegment;
        if (toPoint.sqrMagnitude < 0.000001f)
            return onSegment + axisDir * radius;

        return onSegment + toPoint.normalized * radius;
    }

    public static bool IsColliderContactingPlayerCapsule(
        Collider col,
        CharacterController cc,
        float padding,
        float slop)
    {
        if (col == null || cc == null || !col.enabled)
            return false;

        GetPlayerCapsule(cc, padding, out var bottom, out var top, out var radius);

        var capsuleCenter = (bottom + top) * 0.5f;
        var capsuleHalfHeight = (top - bottom).magnitude * 0.5f + radius;
        var capsuleBounds = new Bounds(
            capsuleCenter,
            new Vector3(radius * 2f, capsuleHalfHeight * 2f, radius * 2f));

        if (!col.bounds.Intersects(capsuleBounds))
            return false;

        var closestOnCol = col.ClosestPoint(capsuleCenter);
        var closestOnCapsule = ClosestPointOnCapsule(closestOnCol, bottom, top, radius);
        closestOnCol = col.ClosestPoint(closestOnCapsule);
        closestOnCapsule = ClosestPointOnCapsule(closestOnCol, bottom, top, radius);
        return (closestOnCol - closestOnCapsule).sqrMagnitude <= slop * slop;
    }

    public static bool IsColliderOverlappingPlayerBody(Collider col, CharacterController cc)
    {
        if (col == null || cc == null || !col.enabled)
            return false;

        return col.bounds.Intersects(GetPlayerBodyBounds(cc));
    }

    public static bool IsColliderContactingPlayerCapsule(Collider col, CharacterController cc) =>
        IsColliderContactingPlayerCapsule(col, cc, PlayerCapsulePadding, CollisionContactSlop);

    const string TrililingMeshName = "SC_Hippie";
    const float TrililingBoundsPadding = 0.15f;
    public static readonly Vector3 TrililingFallbackSolidCenter = new Vector3(0f, 1.4f, 0f);
    public static readonly Vector3 TrililingFallbackSolidSize = new Vector3(2f, 2.8f, 2f);

    /// <summary>Final boss / chase giants: widen solid body box so punches and push contact register.</summary>
    public static void EnsureTrililingSolidCollider(GameObject root)
    {
        if (root == null)
            return;

        // Push-collider giants (Gong Bong / Level07 combat) need solids too — not only Level03 head giants.
        if (!DutzGiantHeadTopCollider.UsesGiantHeadColliders(root.name)
            && !DutzGiantHeadTopCollider.UsesChaseGiantPushColliders(root.name))
            return;

        BoxCollider solid = null;
        foreach (var col in root.GetComponents<BoxCollider>())
        {
            if (col != null && !col.isTrigger)
            {
                solid = col;
                break;
            }
        }

        if (solid == null)
        {
            solid = root.AddComponent<BoxCollider>();
            solid.isTrigger = false;
        }

        ApplyTrililingSolidCollider(solid, root.transform);
    }

    public static void ApplyTrililingSolidCollider(BoxCollider solid, Transform root)
    {
        if (solid == null || solid.isTrigger || root == null)
            return;

        if (TryGetTrililingSolidFromMesh(root, out var center, out var size))
        {
            CapSolidBelowHead(root, ref center, ref size);
            solid.center = center;
            solid.size = size;
            return;
        }

        center = TrililingFallbackSolidCenter;
        size = TrililingFallbackSolidSize;
        CapSolidBelowHead(root, ref center, ref size);
        solid.center = center;
        solid.size = size;
    }

    /// <summary>Body box stops at the neck so the player cannot end up inside the skull volume.</summary>
    static void CapSolidBelowHead(Transform root, ref Vector3 center, ref Vector3 size)
    {
        var headBone = FindHeadBone(root);
        if (headBone == null)
            return;

        var maxBodyTopY = headBone.localPosition.y - 0.15f;
        var bottom = center.y - size.y * 0.5f;
        var top = center.y + size.y * 0.5f;
        if (top <= maxBodyTopY)
            return;

        var newSizeY = maxBodyTopY - bottom;
        if (newSizeY < 0.5f)
            newSizeY = 0.5f;

        size.y = newSizeY;
        center.y = bottom + size.y * 0.5f;
    }

    static Transform FindHeadBone(Transform root)
    {
        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone != null && bone.name == "Head_jnt")
                return bone;
        }

        return null;
    }

    static bool TryGetTrililingSolidFromMesh(Transform root, out Vector3 center, out Vector3 size)
    {
        center = TrililingFallbackSolidCenter;
        size = TrililingFallbackSolidSize;

        // Prefer the visible body mesh. Gong Bong / shop giants use SC_Grandma;
        // inactive SC_Hippie bounds are often empty and used to leave a 1×1×1 foot box.
        var meshRenderer = FindPreferredGiantBodyRenderer(root);
        if (meshRenderer == null)
            return false;

        var worldBounds = meshRenderer.bounds;
        if (worldBounds.size.sqrMagnitude < 0.01f)
            return false;

        var localMin = root.InverseTransformPoint(worldBounds.min);
        var localMax = root.InverseTransformPoint(worldBounds.max);

        var localCenter = (localMin + localMax) * 0.5f;
        var localSize = localMax - localMin;
        localSize.x += TrililingBoundsPadding * 2f;
        localSize.z += TrililingBoundsPadding * 2f;
        localSize.y = Mathf.Max(localSize.y, TrililingFallbackSolidSize.y);

        // Reject degenerate / foot-only boxes so punches can hit the torso.
        if (localSize.x < 1.2f || localSize.y < 1.8f || localSize.z < 1.2f)
            return false;

        center = localCenter;
        size = new Vector3(
            Mathf.Max(localSize.x, TrililingFallbackSolidSize.x),
            localSize.y,
            Mathf.Max(localSize.z, TrililingFallbackSolidSize.z));
        return true;
    }

    static Renderer FindPreferredGiantBodyRenderer(Transform root)
    {
        Renderer hippie = null;
        Renderer grandma = null;
        Renderer anyActive = null;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            var meshName = renderer.gameObject.name;
            if (meshName == GrandmaMeshName)
                grandma = renderer;
            else if (meshName == TrililingMeshName)
                hippie = renderer;
            else if (anyActive == null && meshName.StartsWith("SC_", System.StringComparison.Ordinal))
                anyActive = renderer;
        }

        return grandma != null ? grandma : (hippie != null ? hippie : anyActive);
    }

    const string GrandmaMeshName = "SC_Grandma";
}

/// <summary>Small addicts only: bright orange head/hair + bright red body on SC_Hippie.</summary>
public static class DutzSmallAddictColorfulLook
{
    const string HippieMeshName = "SC_Hippie";
    const string ColorfulMaterialResource = "SimpleCitizens_Hippie_Colorful";

    static Material sharedMaterial;

    public static void Apply(GameObject root)
    {
        if (root == null || !SimpleCitizensHippieBiter.IsSmallAddictName(root.name))
            return;

        var mat = GetMaterial();
        if (mat == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer == null || !renderer.enabled || renderer.gameObject.name != HippieMeshName)
                continue;

            var slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
            var slots = new Material[slotCount];
            for (var i = 0; i < slotCount; i++)
                slots[i] = mat;

            if (Application.isPlaying)
                renderer.materials = slots;
            else
                renderer.sharedMaterials = slots;
        }
    }

    static Material GetMaterial()
    {
        if (sharedMaterial != null)
            return sharedMaterial;

        sharedMaterial = Resources.Load<Material>(ColorfulMaterialResource);
        return sharedMaterial;
    }
}

/// <summary>Small addicts only: 75% bigger overall body scale (1.75×).</summary>
public static class DutzSmallAddictScale
{
    public const float BodyScale = 1.75f;

    public static void Apply(GameObject root)
    {
        if (root == null || !SimpleCitizensHippieBiter.IsSmallAddictName(root.name))
            return;

        if (DutzCrocodilePoolMember.IsCrocodile(root))
            return;

        root.transform.localScale = Vector3.one * BodyScale;
    }
}

/// <summary>
/// When the Hippie (SC_Hippie) touches the player: bite animation, kill, respawn dialog.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SimpleCitizensNpcPhysics))]
[RequireComponent(typeof(SimpleCitizensHippieSounds))]
public class SimpleCitizensHippieBiter : MonoBehaviour
{
    [Header("Bite animation (no bite clip in pack — mouth wipe at high speed)")]
    [SerializeField] string biteStateName = "Idle_WipeMouth";
    [SerializeField] float biteAnimSpeed = 2.4f;
    [SerializeField] float killDelay = 0.35f;
    [SerializeField] float biteDuration = 0.9f;
    [SerializeField] float biteCooldown = 2.5f;
    [SerializeField] string deathMessage = "An addict killed you!";
    const string CrocodileDeathMessage = "A crocodile killed you!";

    public string DeathMessageText =>
        DutzCrocodilePoolMember.IsCrocodile(gameObject)
        && (string.IsNullOrEmpty(deathMessage) || deathMessage == "An addict killed you!")
            ? CrocodileDeathMessage
            : deathMessage;

    [Header("Chomp timing (synced to bite anim)")]
    [SerializeField] float[] chompTimes = { 0.05f, 0.22f, 0.42f };
    [SerializeField] Vector2 chompPitchRange = new Vector2(0.92f, 1.12f);

    SimpleCitizensNpcPhysics npcPhysics;
    SimpleCitizensHippieSounds hippieSounds;
    SimpleCitizensFlyingHippie flyingHippie;
    Animator animator;
    static readonly List<SimpleCitizensHippieBiter> ActiveBiters = new List<SimpleCitizensHippieBiter>(64);

    Collider[] contactColliders = System.Array.Empty<Collider>();
    bool attacking;
    float lastBiteTime;

    static readonly int SpeedId = Animator.StringToHash("Speed_f");

    public static IReadOnlyList<SimpleCitizensHippieBiter> GetActiveBiters() => ActiveBiters;

    public static void EnsureOnNpc(SimpleCitizensNpcPhysics physics)
    {
        if (physics == null)
            return;

        var go = physics.gameObject;
        if (SimpleCitizensNpcPhysics.IsLevel00CrowdWalker(go))
            return;

        if (!IsSmallHippieRoot(go.name) && !SimpleCitizensFlyingHippie.IsFlyingHippieName(go.name))
            return;

        DutzHippieBiteCollider.EnsureSmallHippieColliders(go);

        if (go.GetComponent<SimpleCitizensHippieSounds>() == null)
            go.AddComponent<SimpleCitizensHippieSounds>();

        if (go.GetComponent<SimpleCitizensHippieBiter>() == null)
            go.AddComponent<SimpleCitizensHippieBiter>();

        DutzSmallAddictColorfulLook.Apply(go);
        DutzSmallAddictScale.Apply(go);
        physics.SnapFeetToRoad();
    }

    static bool IsSmallHippieRoot(string objectName) =>
        IsSmallAddictName(objectName);

    public static bool IsSmallAddictName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (objectName.Contains("Giant") || DutzGiantBossNames.IsPrincessZara(objectName))
            return false;

        return objectName.StartsWith("SimpleCitizens_Hippie_Black")
            || objectName.StartsWith("SimpleCitizens_Hippie_Extra_")
            || objectName.StartsWith("SimpleCitizens_Hippie_NearSpawn_")
            || objectName.StartsWith("DutzSegmentHippie_")
            || SimpleCitizensFlyingHippie.IsFlyingHippieName(objectName);
    }

    public static bool IsSmallAddictCollider(Collider col)
    {
        if (col == null)
            return false;

        var biter = col.GetComponentInParent<SimpleCitizensHippieBiter>();
        if (biter != null && biter.isActiveAndEnabled)
            return true;

        var root = col.transform.root;
        if (root != null && DutzCrocodilePoolMember.IsCrocodile(root.gameObject))
            return true;

        return IsSmallAddictName(col.transform.root.name);
    }

    void Awake()
    {
        npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        hippieSounds = GetComponent<SimpleCitizensHippieSounds>();
        flyingHippie = GetComponent<SimpleCitizensFlyingHippie>();
        animator = GetComponent<Animator>();
        DutzHippieBiteCollider.EnsureSmallHippieColliders(gameObject);
        DutzSmallAddictColorfulLook.Apply(gameObject);
        DutzSmallAddictScale.Apply(gameObject);

        RefreshContactColliders();
    }

    void Start()
    {
        if (!DutzCrocodilePoolMember.IsCrocodile(gameObject))
            return;

        DutzHippieBiteCollider.EnsureCrocodileColliders(gameObject);
        RefreshContactColliders();
    }

    void OnEnable()
    {
        if (!ActiveBiters.Contains(this))
            ActiveBiters.Add(this);

        if (DutzCrocodilePoolMember.IsCrocodile(gameObject))
        {
            DutzHippieBiteCollider.EnsureCrocodileColliders(gameObject);
            RefreshContactColliders();
        }
    }

    void OnDisable() => ActiveBiters.Remove(this);

    public void RefreshContactColliders()
    {
        var cols = GetComponents<Collider>();
        var list = new List<Collider>(cols.Length);
        foreach (var col in cols)
        {
            if (col != null && col.enabled)
                list.Add(col);
        }

        contactColliders = list.ToArray();
    }

    public bool OverlapsPlayerBounds(Bounds playerBounds) =>
        IsTouchingPlayer(playerBounds);

    public bool IsTouchingPlayer(Bounds playerBounds)
    {
        if (contactColliders == null || contactColliders.Length == 0)
            RefreshContactColliders();

        var maxGap = KillReachMeters;

        foreach (var col in contactColliders)
        {
            if (col == null || !col.enabled)
                continue;

            if (DutzHippieBiteCollider.IsTouchingPlayerBody(col, playerBounds, maxGap))
                return true;
        }

        return false;
    }

    float KillReachMeters =>
        DutzCrocodilePoolMember.IsCrocodile(gameObject)
            ? DutzHippieBiteCollider.CrocBiteReachMeters
            : DutzHippieBiteCollider.BiteReachMeters;

    public bool IsOverlappingPlayer(CharacterController playerCc)
    {
        if (playerCc == null)
            return false;

        if (contactColliders == null || contactColliders.Length == 0)
            RefreshContactColliders();

        var reach = KillReachMeters;
        var playerBounds = DutzHippieBiteCollider.GetPlayerBodyBounds(playerCc);
        var isCroc = DutzCrocodilePoolMember.IsCrocodile(gameObject);

        foreach (var col in contactColliders)
        {
            if (col == null || !col.enabled)
                continue;

            if (isCroc && !DutzHippieBiteCollider.IsPlayerVerticallyInCrocKillRange(playerCc, col))
                continue;

            if (ColliderReachesPlayer(col, playerCc, playerBounds, reach))
                return true;
        }

        // Lateral proximity kills on the deck — skipped when SuperJump / upper deck clears height.
        if (isCroc)
            return IsCrocodileNearPlayerHeightAware(playerCc, reach);

        return false;
    }

    bool IsCrocodileNearPlayerHeightAware(CharacterController playerCc, float reach)
    {
        var playerPos = playerCc.transform.position;
        var reachSq = reach * reach;

        foreach (var col in contactColliders)
        {
            if (col == null || !col.enabled)
                continue;

            if (!DutzHippieBiteCollider.IsPlayerVerticallyInCrocKillRange(playerCc, col))
                continue;

            var closest = col.ClosestPoint(playerPos);
            var flat = closest - playerPos;
            flat.y = 0f;
            if (flat.sqrMagnitude > reachSq)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>Croc kill: 3D soft reach + height-aware lateral proximity.</summary>
    public bool IsCrocodileContactingPlayer(CharacterController playerCc) =>
        IsOverlappingPlayer(playerCc);

    /// <summary>True overlap / capsule slop only — no soft reach (addicts / generic physical).</summary>
    public bool IsPhysicallyOverlappingPlayer(CharacterController playerCc)
    {
        if (playerCc == null)
            return false;

        if (contactColliders == null || contactColliders.Length == 0)
            RefreshContactColliders();

        var isCroc = DutzCrocodilePoolMember.IsCrocodile(gameObject);

        foreach (var col in contactColliders)
        {
            if (col == null || !col.enabled)
                continue;

            if (isCroc && !DutzHippieBiteCollider.IsPlayerVerticallyInCrocKillRange(playerCc, col))
                continue;

            if (DutzHippieBiteCollider.IsColliderOverlappingPlayerBody(col, playerCc)
                || DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, playerCc))
                return true;
        }

        return false;
    }

    static bool ColliderReachesPlayer(
        Collider col,
        CharacterController playerCc,
        Bounds playerBounds,
        float reachMeters) =>
        DutzHippieBiteCollider.IsColliderOverlappingPlayerBody(col, playerCc)
        || DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, playerCc)
        || DutzHippieBiteCollider.IsTouchingPlayerBody(col, playerBounds, reachMeters);

    public bool IsPhysicallyContactingPlayer(CharacterController playerCc)
    {
        if (playerCc == null)
            return false;

        if (contactColliders == null || contactColliders.Length == 0)
            RefreshContactColliders();

        var isCroc = DutzCrocodilePoolMember.IsCrocodile(gameObject);

        foreach (var col in contactColliders)
        {
            if (col == null || !col.enabled)
                continue;

            if (isCroc && !DutzHippieBiteCollider.IsPlayerVerticallyInCrocKillRange(playerCc, col))
                continue;

            if (DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, playerCc))
                return true;
        }

        return false;
    }

    /// <summary>Instant death on croc contact — ignores spawn grace; still respects shield/dialogs.</summary>
    public static bool TryInstantCrocodileKill(DutzPlayerController player, string message)
    {
        if (player == null)
            return false;

        if (DutzForceField.IsPlayerShielded(player))
            return false;

        if (player.ControlsLocked || DutzLevelObjective.IsLevelFinishedForActiveScene)
            return false;

        if (DutzPoliceCaptureDialog.IsShowing)
            return false;

        var respawn = player.GetComponent<DutzFallRespawn>();
        if (respawn != null && respawn.IsShowingRespawnDialog)
            return false;

        var text = string.IsNullOrEmpty(message) ? "A crocodile killed you!" : message;
        if (respawn != null)
            respawn.TriggerDeathDialog(text);
        else
        {
            player.SetControlsLocked(true);
            player.Respawn();
        }

        return true;
    }

    public static bool TryGetCrocodileFromCollider(Collider col, out SimpleCitizensHippieBiter biter)
    {
        biter = null;
        if (col == null)
            return false;

        biter = col.GetComponentInParent<SimpleCitizensHippieBiter>();
        if (biter != null && DutzCrocodilePoolMember.IsCrocodile(biter.gameObject))
            return true;

        var root = col.transform.root;
        if (root == null || !DutzCrocodilePoolMember.IsCrocodile(root.gameObject))
            return false;

        biter = root.GetComponent<SimpleCitizensHippieBiter>();
        return true;
    }

    /// <summary>Called when Dutz physically overlaps this addict (controller hit or contact scan).</summary>
    public void NotifyPlayerCollision(DutzPlayerController player) => TryBitePlayer(player, requireTouch: true);

    /// <summary>Physics overlap / controller collision — skip redundant gap test.</summary>
    public void NotifyPlayerCollisionFromContact(DutzPlayerController player) => TryBitePlayer(player, requireTouch: false);

    const float BiterPlayerCullDistance = 42f;
    const float BiterPlayerCullDistanceSqr = BiterPlayerCullDistance * BiterPlayerCullDistance;

    void FixedUpdate()
    {
        // Crocs keep trying to kill even mid-bite — the old early-return made grazes free.
        if (attacking && !DutzCrocodilePoolMember.IsCrocodile(gameObject))
            return;

        var playerController = DutzPlayerController.Instance;
        if (playerController == null)
            return;

        var delta = playerController.transform.position - transform.position;
        // Crocs: require both horizontal proximity and same vertical deck before bite tests.
        if (DutzCrocodilePoolMember.IsCrocodile(gameObject))
        {
            var flat = delta;
            flat.y = 0f;
            if (flat.sqrMagnitude > BiterPlayerCullDistanceSqr)
                return;
            if (Mathf.Abs(delta.y) > DutzHippieBiteCollider.CrocMaxVerticalKillSeparationMeters
                + DutzHippieBiteCollider.CrocBiteReachMeters * 2f)
                return;
        }
        else
        {
            delta.y = 0f;
            if (delta.sqrMagnitude > BiterPlayerCullDistanceSqr)
                return;
        }

        var playerCc = playerController.GetComponent<CharacterController>();
        if (playerCc == null)
            return;

        if (DutzCrocodilePoolMember.IsCrocodile(gameObject))
        {
            if (IsOverlappingPlayer(playerCc) || IsPhysicallyContactingPlayer(playerCc))
            {
                if (!TryInstantCrocodileKill(playerController, DeathMessageText) && !attacking)
                    TryBitePlayer(playerController, requireTouch: false);
            }

            return;
        }

        if (attacking)
            return;

        if (!IsOverlappingPlayer(playerCc))
            return;

        TryBitePlayer(playerController, requireTouch: false);
    }

    void OnCollisionEnter(Collision collision) => TryBiteFromCollision(collision);

    void OnCollisionStay(Collision collision)
    {
        if (!attacking && Time.time - lastBiteTime > 1f)
            TryBiteFromCollision(collision);
    }

    void TryBiteFromCollision(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        var player = collision.collider.GetComponent<DutzPlayerController>()
            ?? collision.collider.GetComponentInParent<DutzPlayerController>();
        if (player == null)
            return;

        if (DutzCrocodilePoolMember.IsCrocodile(gameObject))
        {
            TryInstantCrocodileKill(player, DeathMessageText);
            return;
        }

        NotifyPlayerCollisionFromContact(player);
    }

    void TryBitePlayer(DutzPlayerController player, bool requireTouch = true)
    {
        if (player == null)
            return;

        if (DutzForceField.IsPlayerShielded(player))
            return;

        var isCroc = DutzCrocodilePoolMember.IsCrocodile(gameObject);
        if (attacking || (!isCroc && Time.time - lastBiteTime < biteCooldown))
            return;

        if (isCroc && Time.time - lastBiteTime < 0.35f)
            return;

        if (player.ControlsLocked)
            return;

        if (requireTouch && !IsTouchingPlayer(player))
            return;

        var respawn = player.GetComponent<DutzFallRespawn>();
        if (respawn != null && respawn.IsShowingRespawnDialog)
            return;

        if (DutzPoliceCaptureDialog.IsShowing)
            return;

        StartCoroutine(BiteRoutine(player, respawn, confirmedContact: !requireTouch));
    }

    bool IsTouchingPlayer(DutzPlayerController player)
    {
        if (player == null)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        return IsTouchingPlayer(DutzHippieBiteCollider.GetPlayerBodyBounds(cc));
    }

    IEnumerator BiteRoutine(DutzPlayerController player, DutzFallRespawn respawn, bool confirmedContact = false)
    {
        attacking = true;
        lastBiteTime = Time.time;
        npcPhysics.SetWalkingEnabled(false);

        var isCrocodile = DutzCrocodilePoolMember.IsCrocodile(gameObject);
        // Crocs: solid body shove can clear contact before killDelay — don't cancel the kill.
        if (isCrocodile)
            confirmedContact = true;

        var effectiveKillDelay = isCrocodile ? 0f : killDelay;

        FaceTarget(player.transform.position);

        hippieSounds?.SetBiting(true);

        if (animator != null)
        {
            animator.speed = biteAnimSpeed;
            animator.SetFloat(SpeedId, 0f);
            animator.CrossFade(biteStateName, 0.08f, 0);
        }

        var chompCoroutine = StartCoroutine(PlayChompsDuringBite());

        if (effectiveKillDelay > 0f)
            yield return new WaitForSeconds(effectiveKillDelay);

        var cc = player.GetComponent<CharacterController>();
        var stillInContact = confirmedContact
            || IsOverlappingPlayer(cc)
            || IsPhysicallyContactingPlayer(cc)
            || IsTouchingPlayer(player);

        if (stillInContact)
        {
            if (respawn != null)
                respawn.TriggerDeathDialog(deathMessage);
            else
            {
                player.SetControlsLocked(true);
                player.Respawn();
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, biteDuration - effectiveKillDelay));

        if (chompCoroutine != null)
            StopCoroutine(chompCoroutine);

        hippieSounds?.SetBiting(false);

        if (animator != null)
            animator.speed = 1f;

        npcPhysics.SetWalkingEnabled(true);
        attacking = false;
    }

    IEnumerator PlayChompsDuringBite()
    {
        if (hippieSounds == null || chompTimes == null || chompTimes.Length == 0)
            yield break;

        var elapsed = 0f;
        var index = 0;
        while (index < chompTimes.Length && elapsed < biteDuration + 0.1f)
        {
            if (elapsed >= chompTimes[index])
            {
                var pitch = Random.Range(chompPitchRange.x, chompPitchRange.y);
                hippieSounds.PlayChomp(pitch);
                index++;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void FaceTarget(Vector3 worldPosition)
    {
        var dir = worldPosition - transform.position;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        if (flyingHippie != null)
            flyingHippie.ApplySupermanWorldRotation(dir);
        else
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    /// <summary>Stops bite attack state when the player respawns.</summary>
    public void ResetOnPlayerRespawn()
    {
        StopAllCoroutines();
        attacking = false;
        lastBiteTime = 0f;
        hippieSounds?.SetBiting(false);

        if (npcPhysics == null)
            npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        npcPhysics?.SetWalkingEnabled(true);

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetFloat(SpeedId, 0f);
        }
    }
}

/// <summary>
/// Instant kill when Dutz collides with a small addict — direct route to death dialog.
/// Crocodiles: grace-free on solid CC hit / physical overlap only (no soft reach aura).
/// Scans after movement in FixedUpdate and LateUpdate so hits register whether Dutz or the addict moves.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DutzPlayerController))]
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(100)]
public class DutzAddictCollisionBite : MonoBehaviour
{
    const string DeathMessage = "An addict killed you!";
    static readonly Collider[] OverlapBuffer = new Collider[48];

    DutzPlayerController player;
    CharacterController cc;
    DutzFallRespawn fallRespawn;

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        cc = GetComponent<CharacterController>();
        fallRespawn = GetComponent<DutzFallRespawn>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider == null)
            return;

        // Solid non-trigger hit — real CharacterController contact with the croc body.
        if (SimpleCitizensHippieBiter.TryGetCrocodileFromCollider(hit.collider, out var crocBiter))
        {
            if (!CanKillCrocodile())
                return;

            var msg = crocBiter != null ? crocBiter.DeathMessageText : "A crocodile killed you!";
            SimpleCitizensHippieBiter.TryInstantCrocodileKill(player, msg);
            return;
        }

        TryKillFromCollider(hit.collider);
    }

    void FixedUpdate()
    {
        ScanPlayerBodyBounds();
        ScanPlayerCapsule();
    }

    void LateUpdate()
    {
        ScanPlayerBodyBounds();
    }

    void ScanPlayerBodyBounds()
    {
        if (cc == null || player == null)
            return;

        var biters = SimpleCitizensHippieBiter.GetActiveBiters();
        for (var i = 0; i < biters.Count; i++)
        {
            var biter = biters[i];
            if (biter == null || !biter.isActiveAndEnabled)
                continue;

            if (DutzCrocodilePoolMember.IsCrocodile(biter.gameObject))
            {
                if (!CanKillCrocodile())
                    continue;

                if (biter.IsOverlappingPlayer(cc) || biter.IsPhysicallyContactingPlayer(cc))
                    SimpleCitizensHippieBiter.TryInstantCrocodileKill(player, biter.DeathMessageText);
                continue;
            }

            if (!CanKill())
                continue;

            if (biter.IsOverlappingPlayer(cc))
                KillFromAddictContact(biter.DeathMessageText);
        }
    }

    void ScanPlayerCapsule()
    {
        if (cc == null || player == null)
            return;

        DutzHippieBiteCollider.GetPlayerCapsule(
            cc,
            DutzHippieBiteCollider.PlayerCapsulePadding,
            out var bottom,
            out var top,
            out var radius);

        var count = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            OverlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        for (var i = 0; i < count; i++)
        {
            var col = OverlapBuffer[i];
            if (col == null)
                continue;

            if (SimpleCitizensHippieBiter.TryGetCrocodileFromCollider(col, out var crocBiter))
            {
                if (!CanKillCrocodile())
                    continue;

                if (crocBiter == null)
                    continue;

                if (!DutzHippieBiteCollider.IsPlayerVerticallyInCrocKillRange(cc, col))
                    continue;

                if (!crocBiter.IsOverlappingPlayer(cc) && !crocBiter.IsPhysicallyContactingPlayer(cc))
                    continue;

                SimpleCitizensHippieBiter.TryInstantCrocodileKill(player, crocBiter.DeathMessageText);
                continue;
            }

            TryKillFromCollider(col);
        }
    }

    bool CanKill()
    {
        if (player == null || cc == null)
            return false;

        if (DutzForceField.IsPlayerShielded(player))
            return false;

        if (player.ControlsLocked || DutzLevelObjective.IsLevelFinishedForActiveScene)
            return false;

        if (fallRespawn == null)
            return true;

        return !fallRespawn.IsShowingRespawnDialog && !fallRespawn.IsSpawnGraceActive
            && !DutzPoliceCaptureDialog.IsShowing;
    }

    /// <summary>Like CanKill but crocs ignore spawn grace so contact after respawn still kills.</summary>
    bool CanKillCrocodile()
    {
        if (player == null || cc == null)
            return false;

        if (DutzForceField.IsPlayerShielded(player))
            return false;

        if (player.ControlsLocked || DutzLevelObjective.IsLevelFinishedForActiveScene)
            return false;

        if (DutzPoliceCaptureDialog.IsShowing)
            return false;

        if (fallRespawn != null && fallRespawn.IsShowingRespawnDialog)
            return false;

        return true;
    }

    void TryKillFromCollider(Collider col)
    {
        if (col == null || !CanKill())
            return;

        if (!SimpleCitizensHippieBiter.IsSmallAddictCollider(col))
            return;

        if (!IsColliderTouchingPlayer(col))
            return;

        var biter = col.GetComponentInParent<SimpleCitizensHippieBiter>();
        KillFromAddictContact(biter != null ? biter.DeathMessageText : DeathMessage);
    }

    bool IsColliderTouchingPlayer(Collider col) =>
        DutzHippieBiteCollider.IsColliderOverlappingPlayerBody(col, cc)
        || DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, cc)
        || DutzHippieBiteCollider.IsTouchingPlayerBody(
            col,
            DutzHippieBiteCollider.GetPlayerBodyBounds(cc),
            DutzHippieBiteCollider.BiteReachMeters);

    bool IsColliderPhysicallyTouchingPlayer(Collider col) =>
        DutzHippieBiteCollider.IsColliderOverlappingPlayerBody(col, cc)
        || DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, cc);

    void KillFromAddictContact(string message)
    {
        if (!CanKill())
            return;

        if (fallRespawn != null)
            fallRespawn.TriggerDeathDialog(message);
        else
            player.Respawn();
    }
}
