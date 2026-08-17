using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Repairs Senate Building mural panels when copied between scenes (scene-local materials break).</summary>
public static class DutzSenateBuildingMural
{
    public const string RootName = "DutzSenateBuildingMural";
    public const string PanelName = "DutzSenateBuildingMural_Spawn";
    public const string MaterialAssetPath = "Assets/Characters/HighwayBillboards/Materials/SenateBuildingMural.mat";
    const string TextureAssetPath = "Assets/Characters/HighwayBillboards/Textures/SenateBuilding.png";
    const string ShaderName = "Dutz/LowPolyPhotoBillboard";

    static Material cachedSharedMaterial;

    public static void EnsureFromBoot()
    {
        if (!HasPanelsInScene())
            return;

        EnsurePanelMaterials(log: false);
        DutzSenateBuildingMuralGoal.EnsureFromBoot();
    }

    public static bool EnsurePanelMaterials(bool log)
    {
        var material = GetSharedMaterial();
        if (material == null)
        {
            if (log)
                Debug.LogWarning("[Dutz] Senate Building mural material missing — add SenateBuilding.png.");
            return false;
        }

        var changed = false;
        foreach (var panel in FindPanels())
        {
            var renderer = panel.GetComponent<MeshRenderer>();
            if (renderer == null)
                continue;

            if (NeedsMaterialRepair(renderer))
            {
                renderer.sharedMaterial = material;
                changed = true;
            }
        }

        if (log && changed)
            Debug.Log("[Dutz] Repaired Senate Building mural material(s).");

        return changed;
    }

    static bool HasPanelsInScene() => FindPanels().Count > 0;

    static bool NeedsMaterialRepair(Renderer renderer)
    {
        if (renderer == null)
            return false;

        var mat = renderer.sharedMaterial;
        if (mat == null)
            return true;

        if (mat.mainTexture == null)
            return true;

        return mat.name == "Default-Material" || mat.shader == null;
    }

    static Material GetSharedMaterial()
    {
        if (cachedSharedMaterial != null)
            return cachedSharedMaterial;

        cachedSharedMaterial = GetRuntimeSharedMaterial();
        return cachedSharedMaterial;
    }

    public static Material GetRuntimeSharedMaterial()
    {
        if (cachedSharedMaterial != null)
            return cachedSharedMaterial;

#if UNITY_EDITOR
        cachedSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        if (cachedSharedMaterial != null)
            return cachedSharedMaterial;
#endif

        cachedSharedMaterial = Resources.Load<Material>("SenateBuildingMural");
        if (cachedSharedMaterial != null)
            return cachedSharedMaterial;

        var shader = Shader.Find(ShaderName);
        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        var texture = LoadTexture();
        if (shader == null)
            return null;

        cachedSharedMaterial = new Material(shader)
        {
            name = "SenateBuildingMural",
            mainTexture = texture
        };
        return cachedSharedMaterial;
    }

    static Texture2D LoadTexture()
    {
        var fromResources = Resources.Load<Texture2D>("SenateBuilding");
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
#else
        return null;
#endif
    }

    static List<GameObject> FindPanels()
    {
        var panels = new List<GameObject>();
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null)
                continue;

            if (transform.name == PanelName || transform.name == RootName)
                panels.Add(transform.gameObject);
        }

        return panels;
    }
}
