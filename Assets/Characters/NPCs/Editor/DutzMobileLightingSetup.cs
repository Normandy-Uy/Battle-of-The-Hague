using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves bright ambient + directional settings into showcase level scenes (Editor Lighting / Render Settings).
/// </summary>
public static class DutzMobileLightingSetup
{
    static readonly string[] ShowcaseScenePaths =
    {
        DutzLevel02Setup.Level00ScenePath,
        DutzLevel02Setup.Level01ScenePath,
        DutzShowcaseSceneRepair.Level02ScenePath,
        DutzLevel02Setup.Level03ScenePath
    };

    /// <summary>Batch: -executeMethod DutzMobileLightingSetup.ApplyBrightShowcaseLightingBatch</summary>
    public static void ApplyBrightShowcaseLightingBatch() => ApplyToAllShowcaseScenes(log: true);

    /// <summary>Batch: -executeMethod DutzMobileLightingSetup.BakeShowcaseLightingBatch</summary>
    public static void BakeShowcaseLightingBatch()
    {
        if (!ApplyToAllShowcaseScenes(log: true))
            return;

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level01ScenePath, OpenSceneMode.Single);
        Lightmapping.Bake();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dutz] Lightmapping bake finished for Level 1.");
    }

    public static bool ApplyToAllShowcaseScenes(bool log)
    {
        var activePath = SceneManager.GetActiveScene().path;
        var applied = 0;

        foreach (var scenePath in ShowcaseScenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            applied++;
        }

        if (!string.IsNullOrEmpty(activePath) && System.IO.File.Exists(activePath))
            EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);

        if (log)
            Debug.Log($"[Dutz] Bright mobile lighting saved on {applied} showcase scene(s).");

        return applied > 0;
    }

    public static void ApplyToOpenScene()
    {
        DutzMobileLighting.ApplyBrightShowcaseLighting();

        TryConfigureLightmappingSettings();

        foreach (var light in Object.FindObjectsOfType<Light>())
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_Intensity").floatValue = DutzMobileLighting.DirectionalIntensity;
            serialized.FindProperty("m_Color").colorValue = Color.white;
            serialized.FindProperty("m_Shadows.m_Type").intValue = (int)LightShadows.None;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(light);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static void TryConfigureLightmappingSettings()
    {
        try
        {
            var lightmapSettings = Lightmapping.lightingSettings;
            if (lightmapSettings == null)
                return;

            lightmapSettings.realtimeGI = false;
            lightmapSettings.bakedGI = true;
            lightmapSettings.albedoBoost = 2f;
            lightmapSettings.indirectScale = 1.75f;
            lightmapSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Dutz] Skipped Lighting Settings asset tweak: " + ex.Message);
        }
    }
}
