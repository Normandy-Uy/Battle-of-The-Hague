using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bare-minimum Android sanity check: if you see "ALIVE" past the splash, Unity + IL2CPP work on the phone.
/// </summary>
public class DutzAndroidPing : MonoBehaviour
{
    void Awake()
    {
        DutzAndroidBootLog.Write("DutzAndroidPing Awake");
        DutzAndroidBootOverlay.SetStatus("Ping scene Awake");
    }

    void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != "Dutz_AndroidPing")
            return;

        var rect = new Rect(0f, 0f, Screen.width, Screen.height);
        GUI.Box(rect, GUIContent.none);

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(28, Screen.height / 18),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUI.Label(
            rect,
            "ALIVE\n\nUnity APK runs on this phone.\nNext step: test Mobile Smoke (Dutz).",
            style);
    }
}
