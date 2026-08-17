using UnityEngine;

/// <summary>
/// Crown-only solid collider on giant Head_jnt — blocks treating the skull as walkable ground.
/// No full skull shell (that enclosed the player and dragged them when giants chased).
/// Excluded from player ground raycasts via DutzRoadGround.
/// </summary>
public static class DutzGiantHeadTopCollider
{
    const string HeadBoneName = "Head_jnt";
    const string HippieMeshName = "SC_Hippie";
    const string TopCapObjectName = "GiantHeadTopCap";
    const string ShellObjectName = "GiantHeadShell";

    // Head_jnt local space (head bone is 2x on caricature giants).
    static readonly Vector3 FallbackTopCenter = new Vector3(0f, 0.38f, 0.05f);
    static readonly Vector3 FallbackTopSize = new Vector3(0.58f, 0.18f, 0.52f);

    static readonly Vector3 WideFallbackTopCenter = new Vector3(0f, 0.48f, 0.08f);
    static readonly Vector3 WideFallbackTopSize = new Vector3(0.72f, 0.2f, 0.68f);

    const float HeadVertexWeightThreshold = 0.35f;
    const float BoundsPadding = 0.04f;
    const float TopCapHeight = 0.2f;
    const float MaxHeadLocalAxis = 1.35f;
    const float MaxHeadLocalCenter = 0.85f;

    public static bool UsesGiantHeadColliders(string objectName) =>
        DutzGiantBossNames.IsTrililing(objectName) || DutzCollectibleProgress.IsLevel03Giant(objectName);

    /// <summary>Mid/end track giants without bite or burn — need a solid body to shove the player off the deck.</summary>
    public static bool UsesChaseGiantPushColliders(string objectName) =>
        DutzGiantBossNames.IsMidTrackGiant(objectName)
        || DutzGiantBossNames.IsTrililing(objectName)
        || DutzGiantBossNames.IsGongBong(objectName)
        || (DutzCollectibleProgress.IsLevel02 && DutzGiantBossNames.IsHontavirus(objectName))
        || (DutzCollectibleProgress.IsLevel07 && DutzCollectibleProgress.IsLevel07CombatGiant(objectName));

    static bool UsesGiantBodyPush(string objectName) =>
        UsesGiantHeadColliders(objectName) || UsesChaseGiantPushColliders(objectName);

    public static bool IsGiantHeadCollider(Collider col)
    {
        if (col == null)
            return false;

        var t = col.transform;
        while (t != null)
        {
            if (t.name == TopCapObjectName)
                return true;

            t = t.parent;
        }

        return false;
    }

    public static bool IsGiantSolidCollider(Collider col)
    {
        if (col == null || col.isTrigger || !col.enabled)
            return false;

        var hunter = col.GetComponentInParent<SimpleCitizensGiantHippieHunter>();
        if (hunter == null || !UsesGiantBodyPush(hunter.gameObject.name))
            return false;

        return col.transform == hunter.transform || IsGiantHeadCollider(col);
    }

    const float ChaseGiantEjectPadding = 0.22f;
    const float EjectNearGiantDistance = 80f;
    const float ChaseGiantBaseKnockback = 4f;
    const float ChaseGiantScaleKnockback = 0.65f;
    const float ChaseGiantSpeedKnockback = 0.12f;
    const float ChaseGiantUpwardLift = 2.5f;

    /// <summary>Push the player off chase giants — resolve overlap and apply knockback when awakened.</summary>
    public static void EjectPlayerFromGiantColliders(CharacterController cc, float extraPadding = 0.08f)
    {
        if (cc == null)
            return;

        if (DutzCollectibleProgress.IsLevel03Gameplay
            && !DutzGiantHeat.IsAnyNearPlayer(cc, EjectNearGiantDistance))
            return;

        DutzHippieBiteCollider.GetPlayerCapsule(cc, extraPadding, out var bottom, out var top, out var radius);
        var capsuleCenter = (bottom + top) * 0.5f;
        var hits = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return;

        var totalPush = Vector3.zero;
        var knockbackPlanar = Vector3.zero;
        var upwardLift = 0f;

        foreach (var hit in hits)
        {
            if (!IsGiantSolidCollider(hit))
                continue;

            var hunter = hit.GetComponentInParent<SimpleCitizensGiantHippieHunter>();
            var chaseGiant = hunter != null && UsesChaseGiantPushColliders(hunter.gameObject.name);
            var padding = chaseGiant ? ChaseGiantEjectPadding : extraPadding;

            var closest = hit.ClosestPoint(capsuleCenter);
            var pushDir = capsuleCenter - closest;
            if (pushDir.sqrMagnitude < 0.0001f)
            {
                pushDir = capsuleCenter - hit.bounds.center;
                if (pushDir.sqrMagnitude < 0.0001f)
                    pushDir = Vector3.up;
            }

            var dist = pushDir.magnitude;
            if (dist < 0.0001f)
                continue;

            var penetration = radius - dist + padding;
            if (penetration > 0f)
                totalPush += pushDir / dist * penetration;

            if (!chaseGiant || hunter == null || !hunter.HasAwakened)
                continue;

            var scale = Mathf.Max(hunter.transform.lossyScale.x, 1f);
            var strength = ChaseGiantBaseKnockback
                + scale * ChaseGiantScaleKnockback
                + hunter.ChaseSpeedMetersPerSecond * ChaseGiantSpeedKnockback;

            var away = cc.transform.position - closest;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = GetChasePushDirection(hunter, cc);
            else
                away.Normalize();

            var chaseDir = GetChasePushDirection(hunter, cc);
            knockbackPlanar += (away * 0.55f + chaseDir * 0.45f).normalized * strength;
            upwardLift = Mathf.Max(upwardLift, ChaseGiantUpwardLift + scale * 0.12f);
        }

        if (totalPush.sqrMagnitude > 0.0001f)
            cc.Move(totalPush);

        if (knockbackPlanar.sqrMagnitude < 0.0001f && upwardLift <= 0f)
            return;

        var player = cc.GetComponent<DutzPlayerController>();
        if (player == null || player.ControlsLocked)
            return;

        if (knockbackPlanar.sqrMagnitude > 0.0001f)
            player.ApplyHorizontalImpulse(knockbackPlanar);

        if (upwardLift > 0f)
            player.ApplyVerticalImpulse(upwardLift);

        cc.GetComponent<DutzFallRespawn>()?.NotifyGiantBumpGrace();
    }

    static Vector3 GetChasePushDirection(SimpleCitizensGiantHippieHunter hunter, CharacterController cc)
    {
        if (hunter == null)
            return Vector3.forward;

        var dir = hunter.transform.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = cc.transform.position - hunter.transform.position;
            dir.y = 0f;
        }

        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
    }

    public static void EnsureChaseGiantPushColliderOnGiant(GameObject root)
    {
        if (root == null || !UsesChaseGiantPushColliders(root.name))
            return;

        if (UsesGiantHeadColliders(root.name))
            return;

        DutzHippieBiteCollider.EnsureTrililingSolidCollider(root);
        Physics.SyncTransforms();
    }

    /// <summary>Post-caricature boot pass — all giants that need head colliders (Level 1 E-TOL + all Level 3 E-TOLs).</summary>
    public static void EnsureFromBoot()
    {
        var adjusted = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (UsesGiantHeadColliders(hunter.gameObject.name))
            {
                DutzHippieBiteCollider.EnsureTrililingSolidCollider(hunter.gameObject);
                EnsureOnGiant(hunter.gameObject);
                adjusted++;
                continue;
            }

            if (UsesChaseGiantPushColliders(hunter.gameObject.name))
            {
                EnsureChaseGiantPushColliderOnGiant(hunter.gameObject);
                adjusted++;
            }
        }

        if (adjusted > 0)
            Physics.SyncTransforms();
    }

    public static int EnsureOnAllLevel03Giants()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return 0;

        var adjusted = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (!UsesGiantHeadColliders(hunter.gameObject.name))
                continue;

            EnsureOnGiant(hunter.gameObject);
            adjusted++;
        }

        return adjusted;
    }

    public static void EnsureOnGiant(GameObject root)
    {
        if (root == null || !UsesGiantHeadColliders(root.name))
            return;

        var headBone = FindHeadBone(root.transform);
        if (headBone == null)
            return;

        ApplyHeadColliders(root.transform, headBone);

        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone == null || bone == headBone || bone.name != HeadBoneName)
                continue;

            ApplyHeadColliders(root.transform, bone);
        }
    }

    static void ApplyHeadColliders(Transform root, Transform headBone)
    {
        RemoveShellCollider(headBone);

        if (TryComputeTopCapBounds(root, headBone, out var topCenter, out var topSize))
            EnsureBoxCollider(headBone, TopCapObjectName, topCenter, topSize);
        else
            EnsureBoxCollider(headBone, TopCapObjectName, WideFallbackTopCenter, WideFallbackTopSize);
    }

    static void RemoveShellCollider(Transform headBone)
    {
        var shell = headBone.Find(ShellObjectName);
        if (shell == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(shell.gameObject);
        else
            Object.DestroyImmediate(shell.gameObject);
    }

    static void EnsureBoxCollider(Transform parent, string objectName, Vector3 center, Vector3 size)
    {
        var child = parent.Find(objectName);
        if (child == null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        var box = child.GetComponent<BoxCollider>();
        if (box == null)
            box = child.gameObject.AddComponent<BoxCollider>();

        box.isTrigger = false;
        box.center = center;
        box.size = size;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(box);
#endif
    }

    static Transform FindHeadBone(Transform root)
    {
        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone != null && bone.name == HeadBoneName)
                return bone;
        }

        return null;
    }

    static bool TryComputeTopCapBounds(
        Transform root,
        Transform headBone,
        out Vector3 topCenter,
        out Vector3 topSize)
    {
        topCenter = FallbackTopCenter;
        topSize = FallbackTopSize;

        var renderer = FindHippieRenderer(root);
        if (renderer == null)
            return false;

        var headIndex = FindHeadBoneIndex(renderer);
        if (headIndex < 0)
            return false;

        var bakedMesh = BakeMesh(renderer);
        if (bakedMesh == null)
            return false;

        var ownsMesh = bakedMesh != renderer.sharedMesh;
        var vertices = bakedMesh.vertices;
        var weights = bakedMesh.boneWeights;
        var useWeights = !ownsMesh && weights != null && weights.Length == vertices.Length;

        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        var found = false;

        for (var i = 0; i < vertices.Length; i++)
        {
            if (useWeights)
            {
                var weight = GetHeadBoneWeight(weights[i], headIndex);
                if (weight < HeadVertexWeightThreshold)
                    continue;
            }

            var world = renderer.transform.TransformPoint(vertices[i]);
            var local = headBone.InverseTransformPoint(world);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
            found = true;
        }

        if (ownsMesh)
        {
            if (Application.isPlaying)
                Object.Destroy(bakedMesh);
            else
                Object.DestroyImmediate(bakedMesh);
        }

        if (!found)
            return false;

        min -= Vector3.one * BoundsPadding;
        max += Vector3.one * BoundsPadding;

        var topMin = new Vector3(min.x, max.y - TopCapHeight, min.z);
        topCenter = (topMin + max) * 0.5f;
        topSize = max - topMin;
        topSize.y = Mathf.Max(topSize.y, TopCapHeight * 0.75f);

        return IsValidHeadLocalBounds(topCenter, topSize);
    }

    static bool IsValidHeadLocalBounds(Vector3 center, Vector3 size)
    {
        if (size.x <= 0.05f || size.y <= 0.05f || size.z <= 0.05f)
            return false;

        if (size.x > MaxHeadLocalAxis || size.y > MaxHeadLocalAxis || size.z > MaxHeadLocalAxis)
            return false;

        if (Mathf.Abs(center.x) > MaxHeadLocalCenter
            || Mathf.Abs(center.y) > MaxHeadLocalCenter
            || Mathf.Abs(center.z) > MaxHeadLocalCenter)
            return false;

        return true;
    }

    static SkinnedMeshRenderer FindHippieRenderer(Transform root)
    {
        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer != null && renderer.gameObject.name == HippieMeshName)
                return renderer;
        }

        return null;
    }

    static int FindHeadBoneIndex(SkinnedMeshRenderer renderer)
    {
        var bones = renderer.bones;
        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == HeadBoneName)
                return i;
        }

        return -1;
    }

    static Mesh BakeMesh(SkinnedMeshRenderer renderer)
    {
        if (renderer == null)
            return null;

        var mesh = renderer.sharedMesh;
        if (mesh != null && mesh.isReadable)
            return mesh;

        var baked = new Mesh { name = "GiantHeadColliderBake" };
        renderer.BakeMesh(baked);
        return baked;
    }

    static float GetHeadBoneWeight(BoneWeight weight, int headIndex)
    {
        var sum = 0f;
        if (weight.boneIndex0 == headIndex)
            sum += weight.weight0;
        if (weight.boneIndex1 == headIndex)
            sum += weight.weight1;
        if (weight.boneIndex2 == headIndex)
            sum += weight.weight2;
        if (weight.boneIndex3 == headIndex)
            sum += weight.weight3;
        return sum;
    }
}

