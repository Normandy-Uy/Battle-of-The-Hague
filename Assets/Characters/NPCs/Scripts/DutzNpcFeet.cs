using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Visual foot height for SimpleCitizens NPCs (mesh bounds, not physics collider).
/// Foot bones / pivot-to-feet are cached — Level07 called this every FixedUpdate per NPC.
/// </summary>
public static class DutzNpcFeet
{
    static readonly string[] FootBoneNames =
    {
        "Foot_Left_jnt", "Foot_Right_jnt", "Toe_Left_jnt", "Toe_Right_jnt"
    };

    static readonly Dictionary<int, CachedFeet> Cache = new Dictionary<int, CachedFeet>(64);

    struct CachedFeet
    {
        public Transform[] FootBones;
        public float PivotToFeet;
        public bool HasPivotToFeet;
        public bool IsCroc;
        public Transform CrocVisual;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() => Cache.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneInvalidation()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Cache.Clear();

    public static void Invalidate(GameObject root)
    {
        if (root != null)
            Cache.Remove(root.GetInstanceID());
    }

    static CachedFeet GetOrBuild(GameObject root)
    {
        var id = root.GetInstanceID();
        if (Cache.TryGetValue(id, out var cached))
            return cached;

        cached = new CachedFeet
        {
            FootBones = FindFootBones(root.transform),
            IsCroc = DutzCrocodilePoolMember.IsCrocodile(root),
            CrocVisual = null
        };

        if (cached.IsCroc)
            cached.CrocVisual = root.transform.Find(DutzCrocodilePoolMember.VisualChildName);

        // Sample once — Level07 giants/crocs keep a fixed scale after spawn.
        // Clamp: bad skinned/static-batch bounds must never cache a sky-launch offset.
        // Large bosses (BEYBI M is 4.5×) need a scale-aware cap or feet sink into the deck.
        cached.PivotToFeet = Mathf.Clamp(
            MeasurePivotToFeet(root, cached),
            0.05f,
            MaxPivotToFeetOffset(root));
        cached.HasPivotToFeet = true;
        Cache[id] = cached;
        return cached;
    }

    static Transform[] FindFootBones(Transform root)
    {
        var found = new List<Transform>(4);
        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var b = 0; b < FootBoneNames.Length; b++)
        {
            var boneName = FootBoneNames[b];
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == boneName)
                {
                    found.Add(transforms[i]);
                    break;
                }
            }
        }

        return found.ToArray();
    }

    static float MaxPivotToFeetOffset(GameObject root)
    {
        var scaleY = root != null ? Mathf.Abs(root.transform.lossyScale.y) : 1f;
        return Mathf.Max(4f, 4f * Mathf.Max(1f, scaleY));
    }

    static float MeasurePivotToFeet(GameObject root, CachedFeet cached)
    {
        var lowest = GetLowestWorldY(root, cached);
        return root.transform.position.y - lowest;
    }

    public static float GetLowestWorldY(GameObject root)
    {
        if (root == null)
            return 0f;

        return GetLowestWorldY(root, GetOrBuild(root));
    }

    static float GetLowestWorldY(GameObject root, CachedFeet cached)
    {
        var best = float.PositiveInfinity;

        var bones = cached.FootBones;
        if (bones != null)
        {
            for (var i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                if (bone == null)
                    continue;
                if (bone.position.y < best)
                    best = bone.position.y;
            }
        }

        if (best < float.PositiveInfinity)
            return best;

        if (cached.IsCroc && cached.CrocVisual != null)
        {
            foreach (var renderer in cached.CrocVisual.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (renderer.bounds.min.y < best)
                    best = renderer.bounds.min.y;
            }

            if (best < float.PositiveInfinity)
                return best;
        }

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (renderer is MeshRenderer)
            {
                if (renderer.bounds.min.y < best)
                    best = renderer.bounds.min.y;
                continue;
            }

            if (renderer is SkinnedMeshRenderer &&
                renderer.gameObject.name.StartsWith("SC_", System.StringComparison.Ordinal) &&
                renderer.bounds.min.y < best)
                best = renderer.bounds.min.y;
        }

        if (best < float.PositiveInfinity)
            return best;

        foreach (var col in root.GetComponents<Collider>())
        {
            if (col == null || col.isTrigger)
                continue;

            if (col.bounds.min.y < best)
                best = col.bounds.min.y;
        }

        return best < float.PositiveInfinity ? best : root.transform.position.y;
    }

    public static float GetPivotToFeetOffset(GameObject root)
    {
        if (root == null)
            return 0f;

        var cached = GetOrBuild(root);
        if (cached.HasPivotToFeet)
            return cached.PivotToFeet;

        return MeasurePivotToFeet(root, cached);
    }

    public static void PlacePivotOnSurface(GameObject root, float surfaceY)
    {
        if (root == null)
            return;

        Physics.SyncTransforms();
        var pivotToFeet = GetPivotToFeetOffset(root);
        var pos = root.transform.position;
        pos.y = surfaceY + pivotToFeet;
        root.transform.position = pos;

        if (root.TryGetComponent<Rigidbody>(out var rb))
            rb.position = pos;
    }
}
