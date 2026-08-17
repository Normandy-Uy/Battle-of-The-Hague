using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Cheap blue void under highways (no sea water). Removes leftover Suimono scene objects.
/// Batch: DutzVoidPlaneSetup.ApplyBlueVoidBatch
/// </summary>
public static class DutzVoidPlaneSetup
{
    const string ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string VoidObjectName = "Dutz Void";
    const string VoidMaterialPath = "Assets/Characters/NPCs/Materials/DutzVoid.mat";

    public static readonly Color DefaultVoidColor = new Color(0.02f, 0.14f, 0.32f, 1f);

    public static void EnsureBlueVoidFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Blue Void", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyToShowcase(log: true))
            EditorUtility.DisplayDialog("Blue Void", "Could not update Dutz_Level02.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzVoidPlaneSetup.ApplyBlueVoidBatch</summary>
    public static void ApplyBlueVoidBatch() => EnsureBlueVoidFromMenu();

    public static bool ApplyToShowcase(bool log = false)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RemoveLegacySuimonoObjects();
        var material = EnsureVoidMaterial();
        if (material == null)
            return false;

        var roadBounds = GetRoadBounds();
        var voidPlane = GameObject.Find(VoidObjectName);
        if (voidPlane == null)
        {
            voidPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            voidPlane.name = VoidObjectName;
            Undo.RegisterCreatedObjectUndo(voidPlane, "Add Dutz Void");
            Object.DestroyImmediate(voidPlane.GetComponent<Collider>());
            voidPlane.isStatic = true;
        }
        else
        {
            Undo.RecordObject(voidPlane.transform, "Refresh Dutz Void");
        }

        FitVoidTransform(voidPlane, roadBounds);
        ApplyVoidRendererSettings(voidPlane, material);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            var scale = voidPlane.transform.localScale;
            Debug.Log(
                $"[Dutz] Blue void under roads — center {voidPlane.transform.position}, " +
                $"size {scale.x * 10f:F0}×{scale.z * 10f:F0} m, Y={voidPlane.transform.position.y:F1}. " +
                "Legacy Suimono objects removed.");
        }

        return true;
    }

    static void RemoveLegacySuimonoObjects()
    {
        foreach (var objectName in new[] { "SUIMONO_Module", "SUIMONO_Surface_Ocean", "SUIMONO_Surface" })
        {
            var go = GameObject.Find(objectName);
            if (go != null)
                Undo.DestroyObjectImmediate(go);
        }

        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            if (cam == null)
                continue;

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cam.gameObject);
        }
    }

    static Material EnsureVoidMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(VoidMaterialPath);
        if (mat != null)
        {
            mat.color = DefaultVoidColor;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Standard");

        mat = new Material(shader) { name = "DutzVoid", color = DefaultVoidColor };
        AssetDatabase.CreateAsset(mat, VoidMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void FitVoidTransform(GameObject voidPlane, Bounds roadBounds)
    {
        const float margin = 200f;
        const float belowRoad = 60f;

        var center = roadBounds.center;
        center.y = roadBounds.min.y - belowRoad;

        var sizeX = Mathf.Max(roadBounds.size.x + margin, 400f);
        var sizeZ = Mathf.Max(roadBounds.size.z + margin, 400f);

        voidPlane.transform.position = center;
        voidPlane.transform.rotation = Quaternion.identity;
        voidPlane.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
    }

    static void ApplyVoidRendererSettings(GameObject voidPlane, Material material)
    {
        var renderer = voidPlane.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    static Bounds GetRoadBounds()
    {
        var bounds = default(Bounds);
        var hasBounds = false;

        foreach (var meshFilter in Object.FindObjectsOfType<MeshFilter>(true))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            if (!IsRoadSegment(meshFilter.transform))
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

        if (!hasBounds)
            bounds = new Bounds(new Vector3(180f, 0f, -190f), new Vector3(1800f, 20f, 750f));

        return bounds;
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
