using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global pause: freezes Time.timeScale and AudioListener so timers and gameplay stop.
/// Resets on every scene load so a pause never leaks into a restart or the next level.
/// </summary>
public static class DutzGamePause
{
    public static bool IsPaused { get; private set; }

    /// <summary>Latest GUI-space hit rect for the PAUSE/RESUME control (set while drawing).</summary>
    public static Rect GuiHitRect { get; set; }

    static float ignoreTouchesUntilUnscaled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register()
    {
        Resume();
        GuiHitRect = default;
        ignoreTouchesUntilUnscaled = 0f;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Resume();

    public static bool ContainsScreenPoint(Vector2 screenPos)
    {
        if (GuiHitRect.width <= 1f || GuiHitRect.height <= 1f)
            return false;

        var gui = new Vector2(screenPos.x, Screen.height - screenPos.y);
        return GuiHitRect.Contains(gui);
    }

    public static bool ContainsGuiPoint(Vector2 guiPos)
    {
        if (GuiHitRect.width <= 1f || GuiHitRect.height <= 1f)
            return false;
        return GuiHitRect.Contains(guiPos);
    }

    /// <summary>
    /// Poll raw touch/mouse for a press on the pause control. Uses unscaled time so it
    /// works while paused (timeScale 0). Ignores flaky IMGUI GUI.Button on Android.
    /// </summary>
    public static void PollTouchToggle()
    {
        if (Time.unscaledTime < ignoreTouchesUntilUnscaled)
            return;

        if (GuiHitRect.width <= 1f || GuiHitRect.height <= 1f)
            return;

        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;
                if (!ContainsScreenPoint(touch.position))
                    continue;

                Toggle();
                ignoreTouchesUntilUnscaled = Time.unscaledTime + 0.35f;
                return;
            }

            return;
        }

        if (Input.GetMouseButtonDown(0) && ContainsScreenPoint(Input.mousePosition))
        {
            Toggle();
            ignoreTouchesUntilUnscaled = Time.unscaledTime + 0.35f;
        }
    }

    public static void Toggle()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public static void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public static void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
