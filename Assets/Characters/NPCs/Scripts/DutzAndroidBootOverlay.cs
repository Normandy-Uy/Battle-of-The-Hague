using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mobile boot log helper — delegates overlay status to DutzBootOverlay on Dutz level scenes.
/// </summary>
public class DutzAndroidBootOverlay : MonoBehaviour
{
    static DutzAndroidBootOverlay instance;
    string status = "Booting…";
    float hideAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Show()
    {
        if (!Application.isMobilePlatform)
            return;

        if (DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name)
            || DutzMobileLevelLoader.IsLoaderScene(SceneManager.GetActiveScene().name))
        {
            DutzBootOverlay.EnsureVisible();
            DutzBootOverlay.SetStatus($"Scene: {SceneManager.GetActiveScene().name}");
            return;
        }

        if (instance != null)
            return;

        var go = new GameObject(nameof(DutzAndroidBootOverlay));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzAndroidBootOverlay>();
        instance.status = $"Scene: {SceneManager.GetActiveScene().name}";
        instance.hideAt = Time.realtimeSinceStartup + 8f;
        DutzAndroidBootLog.Write(instance.status);
    }

    public static void SetStatus(string message)
    {
        DutzAndroidBootLog.Write(message);

        if (DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name)
            || DutzMobileLevelLoader.IsLoaderScene(SceneManager.GetActiveScene().name))
        {
            DutzBootOverlay.SetStatus(message);
            return;
        }

        if (instance == null)
            return;

        instance.status = message;
        instance.hideAt = Time.realtimeSinceStartup + 8f;
    }

    void OnGUI()
    {
        if (DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name)
            || DutzMobileLevelLoader.IsLoaderScene(SceneManager.GetActiveScene().name))
            return;

        if (Time.realtimeSinceStartup > hideAt)
            return;

        const int width = 700;
        const int height = 120;
        var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 24f, width, height);
        GUI.Box(rect, GUIContent.none);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Dutz Android boot", GUI.skin.box);
        GUILayout.Label(status);
        GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})");
        GUILayout.EndArea();
    }
}
