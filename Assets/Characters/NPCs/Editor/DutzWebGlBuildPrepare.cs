using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Level 00 WebGL demo for psr.ovh — separate from the Android APK pipeline.
/// </summary>
public static class DutzWebGlBuildPrepare
{
    public const string Level00Scene = "Assets/Scenes/Dutz_Level00.unity";
    public const string WebGlProductName = "Battle of The Hague - Level 00 Demo";
    public const string WebGlBuildFolderName = "WebGL";
    public const string HtAccessExampleRelative = "public/webgl-htaccess.example";

    /// <summary>Menu: Tools/Dutz/WebGL/Prepare Level 00 Demo Build</summary>
    [MenuItem("Tools/Dutz/WebGL/Prepare Level 00 Demo Build")]
    public static void PrepareLevel00DemoBuild()
    {
        UseLevel00OnlyInBuild();
        ApplyWebGlPlayerSettings();
        PrepareLevel00SceneForWebGl();
        DutzGameMusicSetup.SyncWebGlDemoMedia();

        if (File.Exists(Level00Scene))
            EditorSceneManager.OpenScene(Level00Scene, OpenSceneMode.Single);

        Debug.Log(
            "[Dutz] WebGL Level 00 demo build is ready.\n" +
            "1) File > Build Settings — confirm ONLY Dutz_Level00 is enabled.\n" +
            "2) Platform: WebGL (switch if needed).\n" +
            "3) Tools > Dutz > WebGL > Build Level 00 WebGL — or Build in Build Settings.\n" +
            "4) Upload Builds/WebGL/* to psr.ovh (see public/WEBGL_PSR_OVH_DEPLOY.txt).\n" +
            "5) Copy public/webgl-htaccess.example to the upload folder as .htaccess");
    }

    /// <summary>Menu: Tools/Dutz/WebGL/Build Level 00 WebGL</summary>
    [MenuItem("Tools/Dutz/WebGL/Build Level 00 WebGL")]
    public static void BuildLevel00WebGlFromMenu() => BuildLevel00WebGlBatch();

    /// <summary>Batch: -executeMethod DutzWebGlBuildPrepare.BuildLevel00WebGlBatch</summary>
    public static void BuildLevel00WebGlBatch()
    {
        UseLevel00OnlyInBuild();
        ApplyWebGlPlayerSettings();
        PrepareLevel00SceneForWebGl();
        DutzGameMusicSetup.SyncWebGlDemoMedia();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("[Dutz] Switching active build target to WebGL…");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[Dutz] Failed to switch to WebGL. Install WebGL Build Support in Unity Hub.");
                ExitBatch(1);
                return;
            }
        }

        var enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (enabledScenes.Length != 1 || enabledScenes[0] != Level00Scene)
        {
            Debug.LogError("[Dutz] WebGL demo requires ONLY " + Level00Scene + " in Build Settings.");
            ExitBatch(1);
            return;
        }

        if (File.Exists(Level00Scene))
            EditorSceneManager.OpenScene(Level00Scene, OpenSceneMode.Single);

        var outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", WebGlBuildFolderName));
        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, recursive: true);

        Directory.CreateDirectory(outputDir);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Dutz] Building WebGL Level 00 demo…\n" +
            $"Output folder: {outputDir}\n" +
            "First WebGL build can take several minutes.");

        var report = BuildPipeline.BuildPlayer(enabledScenes, outputDir, BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[Dutz] WebGL build FAILED: " + report.summary.result);
            ExitBatch(1);
            return;
        }

        CopyHtAccessExampleToBuild(outputDir);

        var sizeMb = report.summary.totalSize / (1024f * 1024f);
        Debug.Log(
            "[Dutz] WebGL Level 00 demo built successfully.\n" +
            $"Folder: {outputDir}\n" +
            $"Size: {sizeMb:F1} MB\n" +
            "Deploy: run deploy-1/deploy.ps1 from project root (see public/WEBGL_PSR_OVH_DEPLOY.txt).\n" +
            "Share link: https://psr.ovh/play/");
        ExitBatch(0);
    }

    public static void UseLevel00OnlyInBuild()
    {
        if (!File.Exists(Level00Scene))
        {
            Debug.LogError("[Dutz] Missing scene: " + Level00Scene);
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Level00Scene, true)
        };
    }

    public static void ApplyWebGlPlayerSettings()
    {
        PlayerSettings.productName = WebGlProductName;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;

        AssetDatabase.SaveAssets();
    }

    public static void PrepareLevel00SceneForWebGl()
    {
        if (EditorApplication.isPlaying)
            return;

        var restoreScenePath = EditorSceneManager.GetActiveScene().path;
        DutzSceneMissingScriptRepair.RepairAllLevelsSilent(log: false);
        DutzMobileLightingSetup.ApplyToAllShowcaseScenes(log: false);

        if (!string.IsNullOrEmpty(restoreScenePath) && File.Exists(restoreScenePath))
            EditorSceneManager.OpenScene(restoreScenePath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    static void CopyHtAccessExampleToBuild(string outputDir)
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var source = Path.Combine(projectRoot, HtAccessExampleRelative);
        if (!File.Exists(source))
        {
            Debug.LogWarning("[Dutz] Missing " + HtAccessExampleRelative + " — add .htaccess manually on psr.ovh.");
            return;
        }

        File.Copy(source, Path.Combine(outputDir, ".htaccess"), overwrite: true);
    }

    static void ExitBatch(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
