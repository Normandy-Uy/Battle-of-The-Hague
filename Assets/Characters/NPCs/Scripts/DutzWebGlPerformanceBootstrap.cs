#if UNITY_WEBGL
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>WebGL demo checks — psr.ovh Level 00 browser sample.</summary>
public static class DutzWebGlRuntime
{
    public static bool IsWebGlPlayer => Application.platform == RuntimePlatform.WebGLPlayer;

    public static bool IsWebGlMobileBrowser => IsWebGlPlayer && Application.isMobilePlatform;
}

/// <summary>
/// Lowers quality for browser play. Desktop WebGL uses keyboard/mouse; phone browsers get touch pads.
/// </summary>
public static class DutzWebGlPerformanceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyWebGlDefaults()
    {
        if (!DutzWebGlRuntime.IsWebGlPlayer)
            return;

        QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);
        var mipLimit = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 4500 ? 4 : 3;
        QualitySettings.globalTextureMipmapLimit = mipLimit;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.pixelLightCount = 4;
        Application.targetFrameRate = 60;
        Shader.globalMaximumLOD = 200;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (DutzWebGlRuntime.IsWebGlMobileBrowser)
        {
            DutzMobilePerformanceBootstrap.ApplyLandscapeOnlyOrientation();
            DutzRobloxMobileInput.EnsureCreated();
        }

        Debug.Log(
            "[Dutz] WebGL bootstrap active\n" +
            $"  Mobile browser: {DutzWebGlRuntime.IsWebGlMobileBrowser}\n" +
            $"  RAM: {SystemInfo.systemMemorySize} MB\n" +
            $"  GPU: {SystemInfo.graphicsDeviceName}");
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!DutzWebGlRuntime.IsWebGlPlayer)
            return;

        if (scene.name == DutzMobileRuntime.Level00SceneName)
            DutzMobileLighting.ApplyBrightShowcaseLighting();
    }
}
#endif
