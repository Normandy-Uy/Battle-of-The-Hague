using UnityEngine;

/// <summary>End-of-level flagpole goal (unused when roof win is active).</summary>
[DisallowMultipleComponent]
public class DutzFlagPoleGoal : MonoBehaviour
{
    public const string FlagPoleName = "FlagPole";
    const string WinColliderChildName = "FlagPoleWinCollider";
    const float RoofSnapVerticalThresholdMeters = 8f;
    const float RoofSnapHorizontalThresholdMeters = 6f;
    const float RoofSnapLiftMeters = 0.35f;

    public const float PlayerTouchReachMeters = 2.25f;

    public static void EnsureFromBoot()
    {
        if (!DutzEndHouseCollider.UsesFlagPoleWin)
        {
            RemoveFromScene();
            return;
        }

        var pole = GameObject.Find(FlagPoleName);
        if (pole == null)
            return;

        StripWrongScripts(pole);

        if (pole.GetComponent<DutzFlagPoleGoal>() == null)
            pole.AddComponent<DutzFlagPoleGoal>();

        SnapPoleToEndHouseRoof(pole);
        RefreshWinCollider(pole);
    }

    public static void RemoveFromScene()
    {
        var pole = GameObject.Find(FlagPoleName);
        if (pole != null)
            Object.Destroy(pole);
    }

    static void StripWrongScripts(GameObject pole)
    {
        foreach (var behaviour in pole.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null || behaviour is DutzFlagPoleGoal)
                continue;

            Object.Destroy(behaviour);
        }
    }

    static void SnapPoleToEndHouseRoof(GameObject pole)
    {
        var house = DutzEndHouseCollider.FindInScene();
        if (house == null)
            return;

        if (!house.activeSelf)
            house.SetActive(true);

        DutzEndHouseCollider.EnsureFromBoot();

        if (!DutzEndHouseCollider.TryGetWorldBounds(house, out var houseBounds))
            return;

        var roofY = houseBounds.max.y + RoofSnapLiftMeters;
        var targetXz = new Vector3(houseBounds.center.x, 0f, houseBounds.center.z);
        var poleXz = pole.transform.position;
        poleXz.y = 0f;

        var verticalDelta = Mathf.Abs(pole.transform.position.y - roofY);
        var horizontalDelta = Vector3.Distance(poleXz, targetXz);
        if (verticalDelta < RoofSnapVerticalThresholdMeters
            && horizontalDelta < RoofSnapHorizontalThresholdMeters)
        {
            return;
        }

        if (!TryGetPoleShaftLocalBounds(pole.transform, out var localBounds))
        {
            pole.transform.SetPositionAndRotation(
                new Vector3(targetXz.x, roofY, targetXz.z),
                Quaternion.Euler(0f, house.transform.eulerAngles.y, 0f));
            return;
        }

        var baseLocal = localBounds.center - Vector3.up * localBounds.extents.y;
        var currentBaseWorld = pole.transform.TransformPoint(baseLocal);
        var delta = new Vector3(
            targetXz.x - currentBaseWorld.x,
            roofY - currentBaseWorld.y,
            targetXz.z - currentBaseWorld.z);
        pole.transform.position += delta;
        pole.transform.rotation = Quaternion.Euler(0f, house.transform.eulerAngles.y, 0f);
    }

    static void RefreshWinCollider(GameObject pole)
    {
        var legacyRootCapsule = pole.GetComponent<CapsuleCollider>();
        if (legacyRootCapsule != null)
            Object.Destroy(legacyRootCapsule);

        var winTransform = pole.transform.Find(WinColliderChildName);
        GameObject winGo;
        if (winTransform == null)
        {
            winGo = new GameObject(WinColliderChildName);
            winGo.transform.SetParent(pole.transform, false);
            winGo.transform.localPosition = Vector3.zero;
            winGo.transform.localRotation = Quaternion.identity;
            winGo.transform.localScale = Vector3.one;
        }
        else
        {
            winGo = winTransform.gameObject;
        }

        var capsule = winGo.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = winGo.AddComponent<CapsuleCollider>();

        if (!TryGetPoleShaftLocalBounds(pole.transform, out var localBounds))
            localBounds = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(0.5f, 4f, 0.5f));

        var height = Mathf.Max(localBounds.size.y, 0.75f);
        var radius = Mathf.Clamp(
            Mathf.Max(localBounds.extents.x, localBounds.extents.z) * 0.6f,
            0.15f,
            6f);

        capsule.direction = 1;
        capsule.height = height;
        capsule.radius = radius;
        capsule.center = localBounds.center;
        capsule.isTrigger = false;
    }

    static bool TryGetPoleShaftLocalBounds(Transform poleRoot, out Bounds localBounds)
    {
        localBounds = default;
        var hasBounds = false;
        var renderers = poleRoot.GetComponentsInChildren<Renderer>();
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || IsFlagFabricRenderer(renderer))
                continue;

            var rendererLocalBounds = TransformBoundsToLocal(poleRoot, renderer.bounds);
            if (!hasBounds)
            {
                localBounds = rendererLocalBounds;
                hasBounds = true;
            }
            else
            {
                localBounds.Encapsulate(rendererLocalBounds.min);
                localBounds.Encapsulate(rendererLocalBounds.max);
            }
        }

        return hasBounds;
    }

    static bool IsFlagFabricRenderer(Renderer renderer)
    {
        var transform = renderer.transform;
        while (transform != null)
        {
            if (transform.name == "Flag")
                return true;

            transform = transform.parent;
        }

        return false;
    }

    static Bounds TransformBoundsToLocal(Transform root, Bounds worldBounds)
    {
        var center = worldBounds.center;
        var extents = worldBounds.extents;
        var hasBounds = false;
        var localBounds = default(Bounds);

        for (var xi = -1; xi <= 1; xi += 2)
        {
            for (var yi = -1; yi <= 1; yi += 2)
            {
                for (var zi = -1; zi <= 1; zi += 2)
                {
                    var worldCorner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                    var localCorner = root.InverseTransformPoint(worldCorner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }
        }

        return localBounds;
    }

    public static bool IsFlagPoleCollider(Collider col) =>
        col != null && col.GetComponentInParent<DutzFlagPoleGoal>() != null;
}
