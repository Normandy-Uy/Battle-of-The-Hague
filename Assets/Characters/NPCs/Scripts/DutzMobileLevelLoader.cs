using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Tiny first scene on Android — ends the Unity splash quickly, then loads Level 01 asynchronously.
/// </summary>
public class DutzMobileLevelLoader : MonoBehaviour
{
    const string LoaderSceneName = "Dutz_MobileLoader";

    IEnumerator Start()
    {
        DutzAndroidBootLog.Write("Mobile loader Start");
        StopUnitySplash();

        DutzBootOverlay.EnsureVisible();
        DutzBootOverlay.SetStatus("Preparing game…");
        yield return null;

        var target = DutzMobileRuntime.Level00SceneName;
        DutzBootOverlay.SetStatus($"Loading {target}…");
        DutzAndroidBootLog.Write($"LoadSceneAsync {target}");

        var op = SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
        if (op == null)
        {
            DutzBootOverlay.ShowFailure($"Missing scene: {target}");
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            var pct = Mathf.RoundToInt(op.progress * 100f);
            DutzBootOverlay.SetStatus($"Loading world… {pct}%");
            yield return null;
        }

        DutzBootOverlay.SetStatus("Starting level…");
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        DutzAndroidBootLog.Write("Mobile loader finished — level scene active");
    }

    static void StopUnitySplash()
    {
        if (!SplashScreen.isFinished)
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }

    public static bool IsLoaderScene(string sceneName) =>
        sceneName == LoaderSceneName;
}
