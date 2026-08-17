using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Async Dutz level loads so bootstrap runs after the scene hierarchy is active
/// (fixes Flood Control → EDSA failing on the first frame).
/// </summary>
public static class DutzSceneLoadRunner
{
    static DutzSceneLoadRunnerHost host;

    public static void LoadDutzLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        EnsureHost().BeginLoad(sceneName);
    }

    static DutzSceneLoadRunnerHost EnsureHost()
    {
        if (host != null)
            return host;

        var existing = UnityEngine.Object.FindObjectOfType<DutzSceneLoadRunnerHost>();
        if (existing != null)
        {
            host = existing;
            return host;
        }

        var go = new GameObject(nameof(DutzSceneLoadRunnerHost));
        UnityEngine.Object.DontDestroyOnLoad(go);
        host = go.AddComponent<DutzSceneLoadRunnerHost>();
        return host;
    }

    sealed class DutzSceneLoadRunnerHost : MonoBehaviour
    {
        public void BeginLoad(string sceneName)
        {
            StopAllCoroutines();
            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            DutzGameBootstrap.PrepareForSceneLoad();
            DutzBootOverlay.DestroyInstance();

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                SceneManager.LoadScene(sceneName);
                yield break;
            }

            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
        }
    }
}

/// <summary>Defers Dutz bootstrap until scene objects have Awake'd.</summary>
public static class DutzSceneBootstrapDefer
{
    static DutzSceneBootstrapDeferHost host;

    public static void Run(Action action)
    {
        if (action == null)
            return;

        EnsureHost().StartDeferred(action);
    }

    static DutzSceneBootstrapDeferHost EnsureHost()
    {
        if (host != null)
            return host;

        var existing = UnityEngine.Object.FindObjectOfType<DutzSceneBootstrapDeferHost>();
        if (existing != null)
        {
            host = existing;
            return host;
        }

        var go = new GameObject(nameof(DutzSceneBootstrapDeferHost));
        UnityEngine.Object.DontDestroyOnLoad(go);
        host = go.AddComponent<DutzSceneBootstrapDeferHost>();
        return host;
    }

    sealed class DutzSceneBootstrapDeferHost : MonoBehaviour
    {
        public void StartDeferred(Action action)
        {
            StopAllCoroutines();
            StartCoroutine(DeferredRoutine(action));
        }

        IEnumerator DeferredRoutine(Action action)
        {
            for (var i = 0; i < 3; i++)
                yield return null;

            Physics.SyncTransforms();
            action?.Invoke();
        }
    }
}
