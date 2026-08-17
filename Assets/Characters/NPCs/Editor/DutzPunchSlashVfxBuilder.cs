using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates DutzPunchSlashVfx prefab and additive slash material.</summary>
public static class DutzPunchSlashVfxBuilder
{
    const string PrefabPath = "Assets/Characters/Level03/Prefabs/DutzPunchSlashVfx.prefab";
    const string MaterialPath = "Assets/Characters/Level03/Materials/DutzPunchSlash.mat";

    public static void SetupFromMenu()
    {
        if (!BuildSlashVfxPrefab(log: true))
        {
            EditorUtility.DisplayDialog("Punch Slash VFX", "Could not build slash VFX prefab. Check Console.", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Punch Slash VFX", "Slash VFX prefab and material are ready.", "OK");
    }

    public static void BuildSlashVfxPrefabBatch() => BuildSlashVfxPrefab(log: true);

    public static bool BuildSlashVfxPrefab(bool log)
    {
        Directory.CreateDirectory("Assets/Characters/Level03/Prefabs");
        Directory.CreateDirectory("Assets/Characters/Level03/Materials");

        var material = CreateOrLoadMaterial();
        if (material == null)
        {
            Debug.LogError("[Dutz] Could not create punch slash material.");
            return false;
        }

        var root = new GameObject("DutzPunchSlashVfx");
        var vfx = root.AddComponent<DutzPunchSlashVfx>();

        var so = new SerializedObject(vfx);
        so.FindProperty("slashMaterial").objectReferenceValue = material;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (log)
            Debug.Log("[Dutz] Punch slash VFX prefab saved: " + PrefabPath);

        return true;
    }

    static Material CreateOrLoadMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Mobile/Particles/Additive")
            ?? Shader.Find("Legacy Shaders/Particles/Additive")
            ?? Shader.Find("Particles/Standard Unlit");

        if (shader == null)
        {
            Debug.LogError("[Dutz] No particle additive shader found.");
            return null;
        }

        var mat = new Material(shader)
        {
            color = new Color(0.55f, 0.92f, 1f, 0.85f)
        };

        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }
}
