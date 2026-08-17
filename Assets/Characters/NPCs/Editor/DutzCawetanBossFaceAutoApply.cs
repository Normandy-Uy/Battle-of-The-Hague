using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps Cawetan's boss photo baked on Level 2 after script reload or when the scene opens — no menu required.
/// </summary>
[InitializeOnLoad]
static class DutzCawetanBossFaceAutoApply
{
    static DutzCawetanBossFaceAutoApply()
    {
        EditorApplication.delayCall += TryApplyOnce;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void TryApplyOnce()
    {
        EditorApplication.delayCall -= TryApplyOnce;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.name == DutzMobileRuntime.Level02SceneName)
            DutzGiantHippieBossFaceBuilder.EnsureCawetanBossFaceOnOpenScene(log: false, persistScene: true);
        else if (scene.name == DutzMobileRuntime.Level03SceneName)
        {
            DutzGiantHippieBossFaceBuilder.EnsureHontavirusBossFaceOnOpenScene(log: false, persistScene: true);
            DutzGiantHippieBossFaceBuilder.EnsureLengLengLugawBossFaceOnOpenScene(log: false, persistScene: true);
        }
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall += () =>
        {
            if (scene.name == DutzMobileRuntime.Level02SceneName)
                DutzGiantHippieBossFaceBuilder.EnsureCawetanBossFaceOnOpenScene(log: false, persistScene: true);

            if (scene.name == DutzMobileRuntime.Level03SceneName)
            {
                DutzGiantHippieBossFaceBuilder.EnsureHontavirusBossFaceOnOpenScene(log: false, persistScene: true);
                DutzGiantHippieBossFaceBuilder.EnsureLengLengLugawBossFaceOnOpenScene(log: false, persistScene: true);
            }
        };
    }
}
