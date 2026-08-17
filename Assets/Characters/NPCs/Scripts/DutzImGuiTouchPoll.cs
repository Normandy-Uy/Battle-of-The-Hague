using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reliable tap handling for modal IMGUI dialogs on Android (GUI.Button is flaky).
/// Dialogs call BeginSession, register button rects via ActionButton(onPress), then Poll from Update.
/// </summary>
public static class DutzImGuiTouchPoll
{
    struct Entry
    {
        public Rect rect;
        public Action action;
    }

    static readonly List<Entry> entries = new List<Entry>(12);
    static float ignoreUntilUnscaled;
    static Vector2 areaOrigin;
    static Vector2 scrollOffset;

    public static bool SessionActive { get; private set; }

    public static void BeginSession()
    {
        SessionActive = true;
        areaOrigin = Vector2.zero;
        scrollOffset = Vector2.zero;
    }

    public static void ClearEntries() => entries.Clear();

    public static void EndSession()
    {
        SessionActive = false;
        entries.Clear();
        areaOrigin = Vector2.zero;
        scrollOffset = Vector2.zero;
    }

    /// <summary>GUILayout.BeginArea / scroll-view origin in GUI screen space.</summary>
    public static void SetAreaOrigin(Vector2 guiOrigin) => areaOrigin = guiOrigin;

    /// <summary>Active scroll position when buttons live inside GUI.BeginScrollView.</summary>
    public static void SetScrollOffset(Vector2 guiOffset) => scrollOffset = guiOffset;

    public static void Register(Rect guiRect, Action onPress)
    {
        if (!SessionActive || onPress == null || guiRect.width <= 1f || guiRect.height <= 1f)
            return;

        var screenRect = new Rect(
            areaOrigin.x + guiRect.x - scrollOffset.x,
            areaOrigin.y + guiRect.y - scrollOffset.y,
            guiRect.width,
            guiRect.height);
        entries.Add(new Entry { rect = screenRect, action = onPress });
    }

    public static void NotifyGuiButtonConsumed() =>
        ignoreUntilUnscaled = Time.unscaledTime + 0.35f;

    public static void Poll()
    {
        if (!SessionActive || entries.Count == 0)
            return;

        if (Time.unscaledTime < ignoreUntilUnscaled)
            return;

        if (!TryGetPressGuiPoint(out var guiPoint))
            return;

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (!entries[i].rect.Contains(guiPoint))
                continue;

            ignoreUntilUnscaled = Time.unscaledTime + 0.35f;
            entries[i].action?.Invoke();
            return;
        }
    }

    static bool TryGetPressGuiPoint(out Vector2 guiPoint)
    {
        guiPoint = default;

        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                guiPoint = ScreenToGui(touch.position);
                return true;
            }

            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            guiPoint = ScreenToGui(Input.mousePosition);
            return true;
        }

        return false;
    }

    static Vector2 ScreenToGui(Vector2 screenPos) =>
        new Vector2(screenPos.x, Screen.height - screenPos.y);
}
