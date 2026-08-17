using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Applies safe Android settings automatically on every APK build so splash-then-crash fixes are not skipped.
/// </summary>
public sealed class DutzAndroidBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        DutzAndroidBuildPrepare.ApplyAndroidTechnicalSettings();
        DutzAndroidBuildPrepare.PrepareScenesForMobileBuild();
        // Scene list is owned by PrepareTrainerSampleBuild / PrepareCampaignBuild — do not force L03 here.
        UnityEngine.Debug.Log(
            "[Dutz] Android preprocess: technical settings + mobile scene prep (lighting, hippies, repairs).");
    }
}
