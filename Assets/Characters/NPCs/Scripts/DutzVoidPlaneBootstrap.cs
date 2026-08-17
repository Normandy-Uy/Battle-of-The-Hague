using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures a cheap blue void plane exists under highways when the showcase loads (no sea/Suimono).
/// </summary>
public static class DutzVoidPlaneBootstrap
{
    const string VoidObjectName = "Dutz Void";
    const string VoidMaterialPath = "Assets/Characters/NPCs/Materials/DutzVoid.mat";
    static readonly Color VoidColor = new Color(0.02f, 0.14f, 0.32f, 1f);

    public static void EnsureVoidPlane()
    {
        if (GameObject.Find(VoidObjectName) != null)
            return;

        var roadBounds = GetRoadBounds();
        var voidPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        voidPlane.name = VoidObjectName;
        Object.Destroy(voidPlane.GetComponent<Collider>());

        var center = roadBounds.center;
        center.y = roadBounds.min.y - 60f;

        const float margin = 200f;
        var sizeX = Mathf.Max(roadBounds.size.x + margin, 400f);
        var sizeZ = Mathf.Max(roadBounds.size.z + margin, 400f);

        voidPlane.transform.position = center;
        voidPlane.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);

        var renderer = voidPlane.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = LoadVoidMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    static Material LoadVoidMaterial()
    {
#if UNITY_EDITOR
        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(VoidMaterialPath);
        if (mat != null)
            return mat;
#endif
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        var runtimeMat = new Material(shader) { color = VoidColor };
        return runtimeMat;
    }

    static Bounds GetRoadBounds()
    {
        var bounds = default(Bounds);
        var hasBounds = false;

        foreach (var meshFilter in Object.FindObjectsOfType<MeshFilter>(true))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null || !IsRoadSegment(meshFilter.transform))
                continue;

            var worldBounds = GetWorldMeshBounds(meshFilter);
            if (!hasBounds)
            {
                bounds = worldBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(worldBounds);
            }
        }

        return hasBounds
            ? bounds
            : new Bounds(new Vector3(180f, 0f, -190f), new Vector3(1800f, 20f, 750f));
    }

    static Bounds GetWorldMeshBounds(MeshFilter meshFilter)
    {
        var meshBounds = meshFilter.sharedMesh.bounds;
        var matrix = meshFilter.transform.localToWorldMatrix;
        var worldBounds = new Bounds(matrix.MultiplyPoint3x4(meshBounds.center), Vector3.zero);
        var extents = meshBounds.extents;

        for (var xi = -1; xi <= 1; xi += 2)
        for (var yi = -1; yi <= 1; yi += 2)
        for (var zi = -1; zi <= 1; zi += 2)
        {
            var corner = meshBounds.center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
            worldBounds.Encapsulate(matrix.MultiplyPoint3x4(corner));
        }

        return worldBounds;
    }

    static bool IsRoadSegment(Transform t)
    {
        while (t != null)
        {
            var name = t.name;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains("Slogan") || name.Contains("Wall Slogan"))
                    return false;

                if (name.Contains("Highway") || name.Contains("Bridge"))
                    return true;
            }

            t = t.parent;
        }

        return false;
    }
}
