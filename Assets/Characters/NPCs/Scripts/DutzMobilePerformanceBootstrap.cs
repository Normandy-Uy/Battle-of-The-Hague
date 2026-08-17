using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Shared mobile checks — showcase defers heavy NPC bootstrap so phones survive past the splash.
/// </summary>
public static class DutzMobileRuntime
{
    public const string Level00SceneName = "Dutz_Level00";
    public const string Level01SceneName = "Dutz_Level01";
    public const string Level02SceneName = "Dutz_Level02";
    public const string Level03SceneName = "Dutz_Level03";
    public const string Level07SceneName = "Dutz_Level07";
    public const string FloodControlSceneName = "LEVEL FLOOD CONTROL";

    public static bool IsFloodControlScene =>
        SceneManager.GetActiveScene().name == FloodControlSceneName;

    public static bool IsDutzLevelScene(string sceneName) =>
        sceneName == Level00SceneName
        || sceneName == Level01SceneName
        || sceneName == Level02SceneName
        || sceneName == Level03SceneName
        || sceneName == Level07SceneName;

    public static bool IsMobileShowcase =>
        Application.isMobilePlatform && SceneManager.GetActiveScene().name == Level00SceneName;

    public static bool ShouldDeferNpcBootstrap => IsMobileShowcase;

    public static bool IsMobileLevel03 =>
        Application.isMobilePlatform
        && (SceneManager.GetActiveScene().name == Level03SceneName
            || SceneManager.GetActiveScene().name == Level07SceneName);
}

/// <summary>
/// Lowers quality and logs device info on phones before the heavy showcase scene loads.
/// In the Editor, also forces Very Low quality while Playing Dutz levels (matches device), then restores.
/// </summary>
public static class DutzMobilePerformanceBootstrap
{
#if UNITY_EDITOR
    static bool editorPlayPerfActive;
    static int editorSavedQualityLevel;
    static int editorSavedMipLimit;
    static AnisotropicFiltering editorSavedAniso;
    static SkinWeights editorSavedSkinWeights;
    static ShadowQuality editorSavedShadows;
    static int editorSavedPixelLights;
    static int editorSavedTargetFps;
    static int editorSavedGlobalLod;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyMobileDefaults()
    {
        if (!Application.isMobilePlatform)
            return;

        ApplyLandscapeOnlyOrientation();
        ApplyLowPerformanceQuality(mobileDevice: true);

        SceneManager.sceneLoaded -= OnSceneLoadedForMobileTuning;
        SceneManager.sceneLoaded += OnSceneLoadedForMobileTuning;

        Debug.Log(
            "[Dutz] Mobile bootstrap active\n" +
            $"  Device: {SystemInfo.deviceModel}\n" +
            $"  OS: {SystemInfo.operatingSystem}\n" +
            $"  RAM: {SystemInfo.systemMemorySize} MB\n" +
            $"  GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)\n" +
            $"  Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");

        DutzRobloxMobileInput.EnsureCreated();
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyEditorPlayPerfBootstrap()
    {
        if (!Application.isPlaying || Application.isMobilePlatform)
            return;

        EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
        SceneManager.sceneLoaded -= OnEditorSceneLoadedForPerf;
        SceneManager.sceneLoaded += OnEditorSceneLoadedForPerf;
        TryApplyEditorPlayPerf(SceneManager.GetActiveScene());
    }
#endif

    static void ApplyLowPerformanceQuality(bool mobileDevice)
    {
        QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);
        var mipLimit = mobileDevice && SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 4500
            ? 4
            : 3;
        QualitySettings.globalTextureMipmapLimit = mipLimit;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.pixelLightCount = 4;
        Application.targetFrameRate = mobileDevice && SystemInfo.systemMemorySize < 6000 ? 30 : 60;
        Shader.globalMaximumLOD = 200;
    }

#if UNITY_EDITOR
    static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SceneManager.sceneLoaded -= OnEditorSceneLoadedForPerf;
            RestoreEditorPlayPerf();
        }
    }

    static void OnEditorSceneLoadedForPerf(Scene scene, LoadSceneMode mode) =>
        TryApplyEditorPlayPerf(scene);

    static void TryApplyEditorPlayPerf(Scene scene)
    {
        if (!Application.isPlaying || Application.isMobilePlatform)
            return;

        if (!DutzMobileRuntime.IsDutzLevelScene(scene.name))
            return;

        if (!editorPlayPerfActive)
        {
            editorSavedQualityLevel = QualitySettings.GetQualityLevel();
            editorSavedMipLimit = QualitySettings.globalTextureMipmapLimit;
            editorSavedAniso = QualitySettings.anisotropicFiltering;
            editorSavedSkinWeights = QualitySettings.skinWeights;
            editorSavedShadows = QualitySettings.shadows;
            editorSavedPixelLights = QualitySettings.pixelLightCount;
            editorSavedTargetFps = Application.targetFrameRate;
            editorSavedGlobalLod = Shader.globalMaximumLOD;
            editorPlayPerfActive = true;
        }

        ApplyLowPerformanceQuality(mobileDevice: false);

        if (scene.name == DutzMobileRuntime.Level02SceneName)
        {
            Debug.Log(
                "[Dutz] Level 02 Editor Play perf — Very Low quality, route-locked giants, coin spin cull. FPS cap: "
                + Application.targetFrameRate);
        }
        else
        {
            Debug.Log(
                "[Dutz] Editor Play perf — Very Low quality for "
                + scene.name
                + ". FPS cap: "
                + Application.targetFrameRate);
        }
    }

    static void RestoreEditorPlayPerf()
    {
        if (!editorPlayPerfActive)
            return;

        QualitySettings.SetQualityLevel(editorSavedQualityLevel, applyExpensiveChanges: true);
        QualitySettings.globalTextureMipmapLimit = editorSavedMipLimit;
        QualitySettings.anisotropicFiltering = editorSavedAniso;
        QualitySettings.skinWeights = editorSavedSkinWeights;
        QualitySettings.shadows = editorSavedShadows;
        QualitySettings.pixelLightCount = editorSavedPixelLights;
        Application.targetFrameRate = editorSavedTargetFps;
        Shader.globalMaximumLOD = editorSavedGlobalLod;
        editorPlayPerfActive = false;
    }
#endif

    static void OnSceneLoadedForMobileTuning(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isMobilePlatform)
            return;

        if (scene.name == DutzMobileRuntime.Level03SceneName
            || scene.name == DutzMobileRuntime.Level07SceneName)
        {
            Application.targetFrameRate = SystemInfo.systemMemorySize >= 6000 ? 60 : 30;
            Debug.Log("[Dutz] Level 3-style mobile perf — route-locked giants, HUD throttle active. FPS cap: "
                + Application.targetFrameRate);
            return;
        }

        if (scene.name == DutzMobileRuntime.Level02SceneName)
        {
            Application.targetFrameRate = SystemInfo.systemMemorySize >= 6000 ? 60 : 30;
            Debug.Log("[Dutz] Level 2 mobile perf — route-locked giants, coin spin cull. FPS cap: "
                + Application.targetFrameRate);
        }
    }

    /// <summary>
    /// Force landscape-only on phones/tablets — never portrait, even when the OS rotation lock is on.
    /// </summary>
    public static void ApplyLandscapeOnlyOrientation()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
            case ScreenOrientation.PortraitUpsideDown:
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                break;
            default:
                Screen.orientation = ScreenOrientation.AutoRotation;
                break;
        }
    }
}

/// <summary>
/// Bright flat ambient + strong sun for phones — levels were authored with very dark sea-blue ambient.
/// </summary>
public static class DutzMobileLighting
{
    public const float FlatAmbientIntensity = 1.75f;
    public const float DirectionalIntensity = 1.9f;
    public const float ReflectionIntensity = 0.85f;

    static readonly Color FlatAmbient = new Color(0.74f, 0.78f, 0.84f, 1f);
    static readonly Color FlatAmbientGround = new Color(0.68f, 0.72f, 0.78f, 1f);
    static readonly Color DirectionalColor = Color.white;

    public static void EnsureFromBoot()
    {
        if (!Application.isMobilePlatform)
            return;

        if (!DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name))
            return;

        ApplyBrightShowcaseLighting();
    }

    public static void ApplyBrightShowcaseLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientSkyColor = FlatAmbient;
        RenderSettings.ambientEquatorColor = FlatAmbient;
        RenderSettings.ambientGroundColor = FlatAmbientGround;
        RenderSettings.ambientLight = FlatAmbient;
        RenderSettings.ambientIntensity = FlatAmbientIntensity;
        RenderSettings.reflectionIntensity = ReflectionIntensity;
        RenderSettings.fog = false;

        foreach (var light in Object.FindObjectsOfType<Light>())
        {
            if (light == null || !light.enabled)
                continue;

            if (light.type == LightType.Directional)
            {
                light.color = DirectionalColor;
                light.intensity = DirectionalIntensity;
                light.shadows = LightShadows.None;
                RenderSettings.sun = light;
                continue;
            }

            if (Application.isMobilePlatform)
                light.shadows = LightShadows.None;
        }
    }
}
