using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Android build profiles: Sample (3 levels) vs Paid (4 levels), plus shared device settings.
/// Legacy names Trainer/Campaign map to Sample/Paid.
/// </summary>
public static class DutzAndroidBuildPrepare
{
    public const string SampleProductName = "Battle of The Hague - Sample";
    public const string PaidProductName = "Battle of The Hague: Bring Him Home";
    public const string Level07ProductName = "Battle of The Hague - Level 07";
    public const string FloodControlProductName = "Battle of The Hague - Flood Control";
    public const string SamplePackageId = "com.dutz.game.trainer";
    public const string PaidPackageId = "com.dutz.battleofthehague";
    /// <summary>Sideload-only package so Level07 test APKs do not overwrite Sample/Paid installs.</summary>
    public const string Level07PackageId = "com.dutz.game.level07";
    public const string FloodControlPackageId = "com.dutz.game.floodcontrol";
    public const string FloodControlScenePath = "Assets/Scenes/LEVEL FLOOD CONTROL.unity";

    /// <summary>Legacy alias for Sample product name.</summary>
    public const string TrainerProductName = SampleProductName;
    /// <summary>Legacy alias for Paid product name.</summary>
    public const string CampaignProductName = PaidProductName;
    /// <summary>Legacy alias for Sample package id.</summary>
    public const string TrainerPackageId = SamplePackageId;
    /// <summary>Legacy alias for Paid package id.</summary>
    public const string CampaignPackageId = PaidPackageId;

    const string AndroidIconFileName = "BattleofTheHagueIcon.png";
    const string AndroidIconAssetRelative = "Assets/AppIcon/" + AndroidIconFileName;

    [InitializeOnLoadMethod]
    static void AutoApplyAndroidBrandingOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var publicIcon = Path.Combine(Path.GetDirectoryName(Application.dataPath), "public", AndroidIconFileName);
            if (!File.Exists(publicIcon))
                return;

            // Icon only — do not overwrite Sample vs Paid product names.
            TryApplyAndroidIcon();
        };
    }

    public static void PrepareAndroidPingBuild()
    {
        ApplyAndroidTechnicalSettings();
        ApplyPaidBranding();
        DutzMobileSmokeSceneSetup.CreatePingScene();
        DutzMobileSmokeSceneSetup.UsePingSceneInBuild();
        EditorUserBuildSettings.buildAppBundle = false;
        Debug.Log(
            "[Dutz] Android PING build ready.\n" +
            "1) File > Build Settings > Build — wait until Unity says Build succeeded.\n" +
            "2) Copy APK to phone with USB cable or Google Drive (NOT WhatsApp — it corrupts APKs).\n" +
            "3) APK should be ~30–50 MB. If tiny (KB), the build failed.\n" +
            "4) Uninstall old Dutz apps, then install. Icon: Dutz. Screen should say ALIVE.");
    }

    public static void PrepareMobileSmokeTestBuild()
    {
        ApplyAndroidTechnicalSettings();
        ApplyPaidBranding();
        DutzMobileSmokeSceneSetup.CreateSmokeScene();
        DutzMobileSmokeSceneSetup.UseSmokeSceneInBuild();
        EditorUserBuildSettings.buildAppBundle = false;
        var scene = EditorBuildSettings.scenes.Length > 0 ? EditorBuildSettings.scenes[0].path : "(none)";
        Debug.Log(
            "[Dutz] MOBILE SMOKE build ready.\n" +
            $"Build scene: {scene}\n" +
            "1) File > Build Settings — confirm ONLY Dutz_MobileSmoke is listed.\n" +
            "2) Build APK (wait for Build succeeded).\n" +
            "3) Uninstall old Dutz app, install new APK.\n" +
            "4) Phone must say SMOKE TEST + show Dutz. If it still says ALIVE, you installed the ping APK.");
    }

    /// <summary>Sample APK: L00–L02 only (3 scenes). Level 02 win shows Play Store CTA.</summary>
    public static void PrepareSampleBuild() => PrepareTrainerSampleBuild();

    /// <summary>Editor Play / APK: L00–L02 only. Level 02 win shows Play Store CTA (no Level 3).</summary>
    public static void PrepareTrainerSampleBuild()
    {
        DutzMobileSmokeSceneSetup.UseTrainerScenesInBuild();
        ApplyAndroidTechnicalSettings(persist: false);
        ApplySampleBranding();
        EditorUserBuildSettings.buildAppBundle = false;

        const string firstScene = "Assets/Scenes/Dutz_Level00.unity";
        if (File.Exists(firstScene))
            EditorSceneManager.OpenScene(firstScene, OpenSceneMode.Single);

        Debug.Log(
            "[Dutz] SAMPLE build ready (Editor Play + APK).\n" +
            $"Product: {SampleProductName}\n" +
            $"Package: {SamplePackageId}\n" +
            "Build Settings: 3 scenes — Dutz_Level00 → L01 → L02 (no L03).\n" +
            "Level 02 win: Play Store download CTA (no GO TO HAGUE).");
    }

    /// <summary>Paid APK: L00–L03 (4 scenes). Level 02 win can GO TO HAGUE → Level 3.</summary>
    public static void PreparePaidBuild() => PrepareCampaignBuild();

    /// <summary>Editor Play / APK: L00–L03. Level 02 win can GO TO HAGUE → Level 3.</summary>
    public static void PrepareCampaignBuild()
    {
        DutzMobileSmokeSceneSetup.UseCampaignScenesInBuild();
        ApplyAndroidTechnicalSettings(persist: false);
        ApplyPaidBranding();
        EditorUserBuildSettings.buildAppBundle = false;

        const string firstScene = "Assets/Scenes/Dutz_Level00.unity";
        if (File.Exists(firstScene))
            EditorSceneManager.OpenScene(firstScene, OpenSceneMode.Single);

        PrepareScenesForMobileBuild();

        Debug.Log(
            "[Dutz] PAID build ready (Editor Play + APK).\n" +
            $"Product: {PaidProductName}\n" +
            $"Package: {PaidPackageId}\n" +
            "Build Settings: 4 scenes — Dutz_Level00 → L01 → L02 → L03.\n" +
            "Level 02 GO TO HAGUE plays video then loads Level 3.");
    }

    /// <summary>Legacy alias — same as PreparePaidBuild / PrepareCampaignBuild.</summary>
    public static void PrepareShowcaseBuild() => PrepareCampaignBuild();

    /// <summary>Sideload APK: Dutz_Level07 only (starts directly in Level 07).</summary>
    public static void PrepareLevel07Build()
    {
        DutzMobileSmokeSceneSetup.UseLevel07OnlyScenesInBuild();
        ApplyAndroidTechnicalSettings(persist: false);
        ApplyLevel07Branding();
        EditorUserBuildSettings.buildAppBundle = false;

        if (File.Exists(DutzLevel02Setup.Level07ScenePath))
            EditorSceneManager.OpenScene(DutzLevel02Setup.Level07ScenePath, OpenSceneMode.Single);

        Debug.Log(
            "[Dutz] LEVEL07-ONLY build ready (Editor Play + APK).\n" +
            $"Product: {Level07ProductName}\n" +
            $"Package: {Level07PackageId}\n" +
            "Build Settings: 1 scene — Dutz_Level07 only.");
    }

    /// <summary>Sideload APK: Flood Control first, then EDSA → Senate → Airport → Hague.</summary>
    public static void PrepareFloodControlBuild()
    {
        if (!File.Exists(FloodControlScenePath))
        {
            Debug.LogError("[Dutz] Flood Control build scene is missing: " + FloodControlScenePath);
            return;
        }

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(FloodControlScenePath, true)
        };

        void AddIfExists(string path)
        {
            if (File.Exists(path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
            else
                Debug.LogWarning("[Dutz] Flood sequel scene missing (skipped): " + path);
        }

        AddIfExists(DutzLevel02Setup.Level00ScenePath);
        AddIfExists(DutzLevel02Setup.Level01ScenePath);
        AddIfExists(DutzLevel02Setup.Level02ScenePath);
        AddIfExists(DutzLevel02Setup.Level03ScenePath);

        EditorBuildSettings.scenes = scenes.ToArray();
        ApplyAndroidTechnicalSettings(persist: false);
        ApplyFloodControlBranding();
        EditorUserBuildSettings.buildAppBundle = false;
        EditorSceneManager.OpenScene(FloodControlScenePath, OpenSceneMode.Single);

        Debug.Log(
            "[Dutz] FLOOD sequel build ready (Editor Play + APK).\n" +
            $"Product: {FloodControlProductName}\n" +
            $"Package: {FloodControlPackageId}\n" +
            "Build Settings: LEVEL FLOOD CONTROL → Dutz_Level00 → L01 → L02 → L03.\n" +
            "Level 07 is not included (separate impeachment app).");
    }

    /// <summary>
    /// Bakes mobile prep into level scenes before APK build — same fixes Editor Play gets from auto-apply hooks.
    /// </summary>
    public static void PrepareScenesForMobileBuild()
    {
        if (EditorApplication.isPlaying)
            return;

        var restoreScenePath = EditorSceneManager.GetActiveScene().path;

        DutzSceneMissingScriptRepair.RepairAllLevelsSilent(log: false);

        if (File.Exists(DutzLevel02Setup.Level02ScenePath))
        {
            SimpleCitizensHippieNpcSetup.ApplySegmentHippiePoolToShowcase(log: false);
            DutzEarlyHighwayContentPlacer.RemoveNearSpawnCoinsFromShowcase();
        }

        DutzMobileLightingSetup.ApplyToAllShowcaseScenes(log: false);

        if (File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            var level03 = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
            DutzLevel03Setup.EnsureBonusGiantsOnLevel03(log: false, lightweightAutoApply: true);
            EditorSceneManager.MarkSceneDirty(level03);
            EditorSceneManager.SaveScene(level03);
        }

        if (!string.IsNullOrEmpty(restoreScenePath) && File.Exists(restoreScenePath))
            EditorSceneManager.OpenScene(restoreScenePath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.ApplyAll</summary>
    public static void ApplyAll()
    {
        ApplyAndroidTechnicalSettings();
        ApplyPaidBranding();
    }

    /// <param name="persist">
    /// When false, skips SaveAssets — caller will save after branding to avoid ProjectSettings lock storms.
    /// </param>
    public static void ApplyAndroidTechnicalSettings(bool persist = true)
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        // Play Console: target API 36+ (Android 16) required for updates from Aug 31, 2026.
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);
        PlayerSettings.Android.forceSDCardPermission = false;
        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, false);
        // Play Console: R8 shrink/obfuscate Java (Technical quality recommendation).
        PlayerSettings.Android.minifyRelease = true;
        ApplyAndroidLargeScreenAndEdgeToEdgeSettings();
        // Do NOT force buildAppBundle=false here — AAB builds re-assert true after Prepare.
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EnableCustomAndroidBuildTemplates();
        TryApplyAndroidIcon();

        if (EditorBuildSettings.scenes.Length == 0
            || System.Array.FindIndex(EditorBuildSettings.scenes, s => s.enabled) < 0)
        {
            Debug.LogWarning("[Dutz] Build Settings has no enabled scenes — run PrepareSampleBuild or PreparePaidBuild.");
        }

        if (persist)
            SavePlayerSettingsWithRetry("ApplyAndroidTechnicalSettings");
    }

    public static bool ApplySampleBranding()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, SamplePackageId);
        PlayerSettings.productName = SampleProductName;
        return SavePlayerSettingsWithRetry("ApplySampleBranding");
    }

    public static bool ApplyPaidBranding()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PaidPackageId);
        PlayerSettings.productName = PaidProductName;
        return SavePlayerSettingsWithRetry("ApplyPaidBranding");
    }

    public static bool ApplyLevel07Branding()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, Level07PackageId);
        PlayerSettings.productName = Level07ProductName;
        return SavePlayerSettingsWithRetry("ApplyLevel07Branding");
    }

    public static bool ApplyFloodControlBranding()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, FloodControlPackageId);
        PlayerSettings.productName = FloodControlProductName;
        return SavePlayerSettingsWithRetry("ApplyFloodControlBranding");
    }

    /// <summary>
    /// Retries AssetDatabase.SaveAssets when Windows briefly locks ProjectSettings.asset
    /// (common when Cursor/AV holds the file during Sample package-id switch).
    /// </summary>
    static bool SavePlayerSettingsWithRetry(string context, int attempts = 8, int delayMs = 300)
    {
        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                AssetDatabase.SaveAssets();
                return true;
            }
            catch (IOException ex)
            {
                Debug.LogWarning(
                    $"[Dutz] ProjectSettings save attempt {i}/{attempts} ({context}): {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogWarning(
                    $"[Dutz] ProjectSettings save attempt {i}/{attempts} ({context}): {ex.Message}");
            }

            Thread.Sleep(delayMs);
        }

        Debug.LogError(
            "[Dutz] ProjectSettings.asset locked — close Cursor tabs on that file, pause AV/cloud sync on the project, then retry.\n" +
            $"Context: {context}");
        return false;
    }

    /// <summary>Legacy alias for ApplySampleBranding.</summary>
    public static void ApplyTrainerBranding() => ApplySampleBranding();

    /// <summary>Legacy alias for ApplyPaidBranding.</summary>
    public static void ApplyCampaignBranding() => ApplyPaidBranding();

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildSampleApkBatch</summary>
    public static void BuildSampleApkBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PrepareSampleBuild,
            fileName: "Dutz_Sample.apk",
            label: "Sample",
            expectedPackageId: SamplePackageId,
            appBundle: false);
    }

    /// <summary>Legacy alias — same as BuildSampleApkBatch.</summary>
    public static void BuildTrainerApkBatch() => BuildSampleApkBatch();

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildPaidApkBatch</summary>
    public static void BuildPaidApkBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PreparePaidBuild,
            fileName: "Dutz_Paid.apk",
            label: "Paid",
            expectedPackageId: PaidPackageId,
            appBundle: false);
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildLevel07ApkBatch</summary>
    public static void BuildLevel07ApkBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PrepareLevel07Build,
            fileName: "Dutz_Level07.apk",
            label: "Level07",
            expectedPackageId: Level07PackageId,
            appBundle: false,
            autoRun: false);
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildLevel07ApkAndRunBatch</summary>
    public static void BuildLevel07ApkAndRunBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PrepareLevel07Build,
            fileName: "Dutz_Level07.apk",
            label: "Level07",
            expectedPackageId: Level07PackageId,
            appBundle: false,
            autoRun: true);
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildFloodControlApkAndRunBatch</summary>
    public static void BuildFloodControlApkAndRunBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PrepareFloodControlBuild,
            fileName: "Dutz_FloodControl.apk",
            label: "FloodControl",
            expectedPackageId: FloodControlPackageId,
            appBundle: false,
            autoRun: true);
    }

    /// <summary>
    /// Play Store update: Flood → EDSA → Senate → Airport → Hague under com.dutz.battleofthehague.
    /// </summary>
    public static void PreparePlayStoreUpdateBuild()
    {
        if (!File.Exists(FloodControlScenePath))
        {
            Debug.LogError("[Dutz] Flood Control build scene is missing: " + FloodControlScenePath);
            return;
        }

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(FloodControlScenePath, true)
        };

        void AddIfExists(string path)
        {
            if (File.Exists(path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
            else
                Debug.LogWarning("[Dutz] Play update scene missing (skipped): " + path);
        }

        AddIfExists(DutzLevel02Setup.Level00ScenePath);
        AddIfExists(DutzLevel02Setup.Level01ScenePath);
        AddIfExists(DutzLevel02Setup.Level02ScenePath);
        AddIfExists(DutzLevel02Setup.Level03ScenePath);

        EditorBuildSettings.scenes = scenes.ToArray();
        ApplyAndroidTechnicalSettings(persist: false);
        ApplyPaidBranding();
        TryApplyAndroidIcon();
        EditorUserBuildSettings.buildAppBundle = true;

        PrepareScenesForMobileBuild();
        EditorSceneManager.OpenScene(FloodControlScenePath, OpenSceneMode.Single);

        Debug.Log(
            "[Dutz] PLAY STORE UPDATE build ready (AAB).\n" +
            $"Product: {PaidProductName}\n" +
            $"Package: {PaidPackageId}\n" +
            "Scenes: FLOOD → EDSA (L00) → Senate (L01) → Airport (L02) → Hague (L03).\n" +
            "Level 07 not included.");
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildPlayStoreUpdateAabBatch</summary>
    public static void BuildPlayStoreUpdateAabBatch()
    {
        // Bump: v11 ad-consent fix; v12 Play Console edge-to-edge, large screen, R8.
        PlayerSettings.bundleVersion = "1.7";
        PlayerSettings.Android.bundleVersionCode = 12;

        BuildVariantAndroidBatch(
            prepare: PreparePlayStoreUpdateBuild,
            fileName: "Dutz_BattleOfTheHague_Update.aab",
            label: "PlayStoreUpdate",
            expectedPackageId: PaidPackageId,
            appBundle: true);
    }

    /// <summary>
    /// Sideload / device smoke test — same scenes + package as Play Store update AAB.
    /// Batch: -executeMethod DutzAndroidBuildPrepare.BuildPlayStoreUpdateApkAndRunBatch
    /// </summary>
    public static void BuildPlayStoreUpdateApkAndRunBatch()
    {
        PlayerSettings.bundleVersion = "1.7";
        PlayerSettings.Android.bundleVersionCode = 12;

        BuildVariantAndroidBatch(
            prepare: PreparePlayStoreUpdateBuild,
            fileName: "Dutz_BattleOfTheHague_Update.apk",
            label: "PlayStoreUpdate",
            expectedPackageId: PaidPackageId,
            appBundle: false,
            autoRun: true);
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildPaidAabBatch — Google Play upload.</summary>
    public static void BuildPaidAabBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PreparePaidBuild,
            fileName: "Dutz_Paid.aab",
            label: "Paid",
            expectedPackageId: PaidPackageId,
            appBundle: true);
    }

    /// <summary>Batch: -executeMethod DutzAndroidBuildPrepare.BuildSampleAabBatch</summary>
    public static void BuildSampleAabBatch()
    {
        BuildVariantAndroidBatch(
            prepare: PrepareSampleBuild,
            fileName: "Dutz_Sample.aab",
            label: "Sample",
            expectedPackageId: SamplePackageId,
            appBundle: true);
    }

    /// <summary>Legacy alias — same as BuildPaidApkBatch.</summary>
    public static void BuildCampaignApkBatch() => BuildPaidApkBatch();

    /// <summary>Legacy alias — same as BuildPaidApkBatch.</summary>
    public static void BuildShowcaseApkBatch() => BuildPaidApkBatch();

    [MenuItem("Window/Build Play Store Update AAB (Flood→Hague)")]
    public static void BuildPlayStoreUpdateAabFromMenu() => BuildPlayStoreUpdateAabBatch();

    [MenuItem("Window/Build And Run Play Store Update APK (Flood→Hague)")]
    public static void BuildPlayStoreUpdateApkAndRunFromMenu() => BuildPlayStoreUpdateApkAndRunBatch();

    [MenuItem("Window/Build Paid AAB for Play (Dutz)")]
    public static void BuildPaidAabFromMenu() => BuildPaidAabBatch();

    [MenuItem("Window/Build Sample AAB for Play (Dutz)")]
    public static void BuildSampleAabFromMenu() => BuildSampleAabBatch();

    [MenuItem("Window/Build Paid APK (Dutz)")]
    public static void BuildPaidApkFromMenu() => BuildPaidApkBatch();

    [MenuItem("Window/Build Sample APK (Dutz)")]
    public static void BuildSampleApkFromMenu() => BuildSampleApkBatch();

    [MenuItem("Window/Build Level07 APK (Dutz)")]
    public static void BuildLevel07ApkFromMenu() => BuildLevel07ApkBatch();

    [MenuItem("Window/Build And Run Level07 APK (Dutz)")]
    public static void BuildLevel07ApkAndRunFromMenu() => BuildLevel07ApkAndRunBatch();

    [MenuItem("Window/Build And Run Flood Control APK (Dutz)")]
    public static void BuildFloodControlApkAndRunFromMenu() => BuildFloodControlApkAndRunBatch();

    [MenuItem("Window/Prepare Paid Build (Dutz)")]
    public static void PreparePaidBuildFromMenu() => PreparePaidBuild();

    [MenuItem("Window/Prepare Sample Build (Dutz)")]
    public static void PrepareSampleBuildFromMenu() => PrepareSampleBuild();

    [MenuItem("Window/Prepare Level07 Build (Dutz)")]
    public static void PrepareLevel07BuildFromMenu() => PrepareLevel07Build();

    static void BuildVariantAndroidBatch(
        System.Action prepare,
        string fileName,
        string label,
        string expectedPackageId,
        bool appBundle,
        bool autoRun = false)
    {
        DutzSceneMissingScriptRepair.RepairAllLevelsSilent(log: false);
        prepare();

        var expectedPackage = expectedPackageId;
        var actualPackage = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        if (!string.Equals(actualPackage, expectedPackage, StringComparison.Ordinal))
        {
            Debug.LogError(
                $"[Dutz] {label} prepare left wrong Android package id.\n" +
                $"Expected: {expectedPackage}\n" +
                $"Got: {actualPackage}\n" +
                "Often caused by a locked ProjectSettings.asset — close Cursor tabs on that file and retry.");
            ExitBatch(1);
            return;
        }

        // Play Console rejects unsigned AABs. Sideload APKs also get the upload key when present.
        if (!ApplyUploadKeystoreSigning())
        {
            Debug.LogError(
                "[Dutz] Android upload keystore missing or failed to apply.\n" +
                "Expected: Android/DutzUpload.keystore (+ DutzUploadKeystore_CREDENTIALS.txt).\n" +
                "Google Play: App bundles must be signed with your upload key.");
            ExitBatch(1);
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[Dutz] Switching active build target to Android…");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[Dutz] Failed to switch build target to Android. Install Android Build Support in Unity Hub.");
                ExitBatch(1);
                return;
            }
        }

        // MUST set after Prepare + after any SwitchActiveBuildTarget — both force APK mode.
        EditorUserBuildSettings.buildAppBundle = appBundle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (enabledScenes.Length == 0)
        {
            Debug.LogError("[Dutz] No enabled scenes in Build Settings.");
            ExitBatch(1);
            return;
        }

        var outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, fileName);
        var formatLabel = appBundle ? "AAB" : "APK";

        if (appBundle && !outputPath.EndsWith(".aab", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("[Dutz] App Bundle output must end with .aab: " + outputPath);
            ExitBatch(1);
            return;
        }

        if (!appBundle && !outputPath.EndsWith(".apk", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("[Dutz] APK output must end with .apk: " + outputPath);
            ExitBatch(1);
            return;
        }

        EditorSceneManager.SaveOpenScenes();
        if (!SavePlayerSettingsWithRetry("pre-BuildPlayer"))
        {
            ExitBatch(1);
            return;
        }

        // Re-assert immediately before BuildPlayer (SaveAssets / domain hooks can reset this).
        EditorUserBuildSettings.buildAppBundle = appBundle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        Debug.Log(
            $"[Dutz] Building {label} {formatLabel}{(autoRun ? " (Build And Run)" : "")}…\n" +
            $"Product: {PlayerSettings.productName}\n" +
            $"Package: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}\n" +
            $"Output: {outputPath}\n" +
            $"buildAppBundle={EditorUserBuildSettings.buildAppBundle} (must be {appBundle})\n" +
            $"autoRun={autoRun}\n" +
            $"Scenes ({enabledScenes.Length}): {string.Join(" → ", enabledScenes.Select(Path.GetFileNameWithoutExtension))}\n" +
            "First IL2CPP build can take 15–45 minutes.");

        if (EditorUserBuildSettings.buildAppBundle != appBundle)
        {
            Debug.LogError("[Dutz] EditorUserBuildSettings.buildAppBundle did not stick — refusing to build a fake .aab.");
            ExitBatch(1);
            return;
        }

        var buildOptions = autoRun ? BuildOptions.AutoRunPlayer : BuildOptions.None;
        var report = BuildPipeline.BuildPlayer(enabledScenes, outputPath, BuildTarget.Android, buildOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[Dutz] {label} {formatLabel} build FAILED: {report.summary.result}");
            ExitBatch(1);
            return;
        }

        if (appBundle && !IsValidAndroidAppBundleFile(outputPath))
        {
            Debug.LogError(
                $"[Dutz] {label} output is NOT a real Android App Bundle (missing BundleConfig.pb / base/).\n" +
                "Unity wrote an APK under a .aab name — Play Console will fail with a vague upload error.\n" +
                $"Path: {outputPath}\n" +
                "Check that buildAppBundle is true and Android App Bundle support is installed.");
            ExitBatch(1);
            return;
        }

        var sizeMb = report.summary.totalSize / (1024f * 1024f);
        if (appBundle)
        {
            Debug.Log(
                $"[Dutz] {label} AAB built successfully (real App Bundle + upload keystore).\n" +
                $"Path: {outputPath}\n" +
                $"Size: {sizeMb:F1} MB\n" +
                "Upload this .aab in Google Play Console → App bundles.\n" +
                "Do NOT jarsigner the outer .aab file.");
        }
        else
        {
            Debug.Log(
                $"[Dutz] {label} APK built successfully.\n" +
                $"Path: {outputPath}\n" +
                $"Size: {sizeMb:F1} MB\n" +
                "1) Uninstall conflicting package on phone if needed.\n" +
                "2) Copy APK via USB or Google Drive (NOT WhatsApp).\n" +
                "3) Install and play.");
        }

        ExitBatch(0);
    }

    /// <summary>True if zip is a Play App Bundle (not an APK renamed .aab).</summary>
    static bool IsValidAndroidAppBundleFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        try
        {
            using (var zip = System.IO.Compression.ZipFile.OpenRead(path))
            {
                var hasConfig = zip.GetEntry("BundleConfig.pb") != null;
                var hasBase = false;
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.StartsWith("base/", System.StringComparison.Ordinal))
                    {
                        hasBase = true;
                        break;
                    }
                }

                return hasConfig && hasBase;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Dutz] Failed to inspect AAB zip: " + ex.Message);
            return false;
        }
    }

    const string UploadKeystoreRelativePath = "Android/DutzUpload.keystore";
    const string UploadKeystoreCredentialsRelativePath = "Android/DutzUploadKeystore_CREDENTIALS.txt";
    const string UploadKeyAlias = "dutzupload";
    /// <summary>Must match Android/DutzUploadKeystore_CREDENTIALS.txt — back that file up offline.</summary>
    const string UploadKeystorePassword = "DutzPlayUpload2026!";

    /// <summary>Configures Player Settings custom keystore for Play / release signing.</summary>
    public static bool ApplyUploadKeystoreSigning()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var keystorePath = Path.Combine(projectRoot, UploadKeystoreRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(keystorePath))
        {
            Debug.LogError("[Dutz] Missing upload keystore: " + keystorePath);
            return false;
        }

        // Absolute path is most reliable for BuildPipeline on Windows.
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = UploadKeystorePassword;
        PlayerSettings.Android.keyaliasName = UploadKeyAlias;
        PlayerSettings.Android.keyaliasPass = UploadKeystorePassword;
        if (!SavePlayerSettingsWithRetry("ApplyUploadKeystoreSigning"))
            return false;

        Debug.Log(
            "[Dutz] Android upload signing applied.\n" +
            $"Keystore: {keystorePath}\n" +
            $"Alias: {UploadKeyAlias}\n" +
            $"Credentials note: {Path.Combine(projectRoot, UploadKeystoreCredentialsRelativePath.Replace('/', Path.DirectorySeparatorChar))}");
        return true;
    }

    static void ExitBatch(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    static void EnableCustomAndroidBuildTemplates()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0)
            return;

        var settings = new SerializedObject(assets[0]);
        var mainTemplate = settings.FindProperty("useCustomMainGradleTemplate");
        var propertiesTemplate = settings.FindProperty("useCustomGradlePropertiesTemplate");
        if (mainTemplate == null || propertiesTemplate == null)
            return;

        var changed = false;
        if (!mainTemplate.boolValue)
        {
            mainTemplate.boolValue = true;
            changed = true;
        }

        if (!propertiesTemplate.boolValue)
        {
            propertiesTemplate.boolValue = true;
            changed = true;
        }

        var launcherManifest = settings.FindProperty("useCustomLauncherManifest");
        if (launcherManifest != null && launcherManifest.boolValue)
        {
            launcherManifest.boolValue = false;
            changed = true;
        }

        if (changed)
            settings.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyAndroidLargeScreenAndEdgeToEdgeSettings()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0)
            return;

        var settings = new SerializedObject(assets[0]);
        var changed = false;

        // Large screens / foldables — resizable freeform window on Android.
        var resizable = settings.FindProperty("androidResizableWindow");
        if (resizable != null && !resizable.boolValue)
        {
            resizable.boolValue = true;
            changed = true;
        }

        // Draw into display cutout / safe area (pairs with DutzUnityPlayerActivity EdgeToEdge).
        var renderOutside = settings.FindProperty("androidRenderOutsideSafeArea");
        if (renderOutside != null && !renderOutside.boolValue)
        {
            renderOutside.boolValue = true;
            changed = true;
        }

        var proguard = settings.FindProperty("useCustomProguardFile");
        if (proguard != null && !proguard.boolValue)
        {
            proguard.boolValue = true;
            changed = true;
        }

        if (changed)
            settings.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Window/Apply Android Launcher Icon (Dutz)")]
    public static void ApplyAndroidLauncherIconFromMenu() => TryApplyAndroidIcon();

    static void TryApplyAndroidIcon()
    {
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var publicIcon = Path.Combine(projectRoot, "public", AndroidIconFileName);
        var assetFull = Path.Combine(Application.dataPath, "AppIcon", AndroidIconFileName);

        if (!File.Exists(publicIcon))
        {
            Debug.LogWarning("[Dutz] Android icon missing: public/" + AndroidIconFileName);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(assetFull));
        var publicTime = File.GetLastWriteTimeUtc(publicIcon);
        var needsCopy = !File.Exists(assetFull) || File.GetLastWriteTimeUtc(assetFull) < publicTime;
        if (needsCopy)
        {
            File.Copy(publicIcon, assetFull, overwrite: true);
            AssetDatabase.ImportAsset(AndroidIconAssetRelative, ImportAssetOptions.ForceUpdate);
        }

        ConfigureAndroidIconImport(AndroidIconAssetRelative);

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AndroidIconAssetRelative);
        if (tex == null)
        {
            Debug.LogWarning("[Dutz] Android icon file found but could not load as Texture2D: " + AndroidIconAssetRelative);
            return;
        }

        // Adaptive requires BOTH layers. SetTexture(tex) alone left foreground empty → blank
        // Play Store launcher icon after AAB update (Build and Run can still look fine via Legacy).
        var adaptiveBg = EnsureOpaqueAdaptiveBackground(tex);

        var adaptiveIcons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
        foreach (var icon in adaptiveIcons)
            icon.SetTextures(new[] { adaptiveBg, tex });
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, adaptiveIcons);

        foreach (var kind in new[] { AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Legacy })
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (var icon in icons)
                icon.SetTexture(tex);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Dutz] Android launcher icon set (Adaptive BG+FG, Round, Legacy) from public/" + AndroidIconFileName);
    }

    /// <summary>
    /// Opaque adaptive background asset. Transparent / missing BG layers often render blank on device.
    /// </summary>
    static Texture2D EnsureOpaqueAdaptiveBackground(Texture2D source)
    {
        const string bgRelative = "Assets/AppIcon/BattleofTheHagueIcon_Background.png";
        var bgFull = Path.Combine(Application.dataPath, "AppIcon", "BattleofTheHagueIcon_Background.png");

        if (!File.Exists(bgFull))
        {
            const int size = 432;
            var bg = new Texture2D(size, size, TextureFormat.RGB24, false);
            // Solid white — safe opaque plate behind the foreground art.
            var fill = new Color32(255, 255, 255, 255);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = fill;
            bg.SetPixels32(pixels);
            bg.Apply(false, false);
            File.WriteAllBytes(bgFull, bg.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(bg);
            AssetDatabase.ImportAsset(bgRelative, ImportAssetOptions.ForceUpdate);
        }

        ConfigureAndroidIconImport(bgRelative);
        var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(bgRelative);
        return loaded != null ? loaded : source;
    }

    static void ConfigureAndroidIconImport(string assetRelative)
    {
        var importer = AssetImporter.GetAtPath(assetRelative) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var defaultPlatform = new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            overridden = true,
            maxTextureSize = 512,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Uncompressed,
        };
        importer.SetPlatformTextureSettings(defaultPlatform);

        var androidPlatform = new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = 512,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Uncompressed,
        };
        importer.SetPlatformTextureSettings(androidPlatform);

        importer.SaveAndReimport();
    }
}
