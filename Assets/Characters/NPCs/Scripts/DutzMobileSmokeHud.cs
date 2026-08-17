using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes the mobile smoke APK visually distinct from the ping APK.
/// </summary>
public class DutzMobileSmokeHud : MonoBehaviour
{
    const string SmokeSceneName = "Dutz_MobileSmoke";
    float hideAt;

    void Awake()
    {
        if (SceneManager.GetActiveScene().name != SmokeSceneName)
        {
            enabled = false;
            return;
        }

        hideAt = Time.realtimeSinceStartup + 6f;
        DutzAndroidBootLog.Write("Dutz_MobileSmoke scene running");
        DutzAndroidBootOverlay.SetStatus("SMOKE TEST scene loaded");
    }

    void OnGUI()
    {
        if (Time.realtimeSinceStartup > hideAt)
            return;

        var rect = new Rect(0f, 0f, Screen.width, Screen.height * 0.22f);
        GUI.Box(rect, GUIContent.none);

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(24, Screen.height / 22),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUI.Label(rect, "SMOKE TEST\nDutz on ground — use touch to move", style);
    }
}
