using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Permanently keeps Level 3 bonus giants placed and configured when the scene opens — no menu, no save loops.
/// </summary>
[InitializeOnLoad]
static class DutzLevel03AutoSetup
{
    static string appliedScenePath;

    static DutzLevel03AutoSetup()
    {
        EditorApplication.delayCall += TryApplyOnLoad;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += _ => appliedScenePath = null;
    }

    static void TryApplyOnLoad()
    {
        EditorApplication.delayCall -= TryApplyOnLoad;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SceneManager.GetActiveScene().name == DutzMobileRuntime.Level03SceneName)
            ApplyLevel03BonusGiantsSilent();
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (scene.name != DutzMobileRuntime.Level03SceneName)
            return;

        appliedScenePath = null;
        EditorApplication.delayCall += ApplyLevel03BonusGiantsSilent;
    }

    static void ApplyLevel03BonusGiantsSilent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != DutzMobileRuntime.Level03SceneName)
            return;

        if (appliedScenePath == scene.path)
            return;

        appliedScenePath = scene.path;
        DutzLevel03Setup.EnsureBonusGiantsOnLevel03(log: false, lightweightAutoApply: true);
    }
}
