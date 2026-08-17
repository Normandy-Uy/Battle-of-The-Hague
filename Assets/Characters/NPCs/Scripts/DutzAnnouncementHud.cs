using UnityEngine;

/// <summary>Shared flash text for track warnings (no background box).</summary>
public static class DutzAnnouncementHud
{
    public const int FlashFontSize = 32;
    const float VerticalScreenFraction = 0.28f;
    const float LineSpacingScreenFraction = 0.09f;

    /// <summary>Level start / objective flashes (e.g. Addicts Incoming).</summary>
    public const int StartMessageLine = 0;

    /// <summary>Giant proximity warnings (e.g. JOLES IS COMING).</summary>
    public const int TrackGiantLine = 1;

    public static readonly Color DefaultFlashColor = new Color(1f, 0.45f, 0.1f);

    public static void DrawFlash(string text, Color color) =>
        DrawFlash(text, color, FlashFontSize, StartMessageLine);

    public static void DrawFlash(string text, Color color, int fontSize) =>
        DrawFlash(text, color, fontSize, StartMessageLine);

    public static void DrawFlash(string text, Color color, int fontSize, int line)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (line == StartMessageLine)
        {
            DrawCartoonBanner(text, color, line);
            return;
        }

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            normal = { textColor = color }
        };

        var content = new GUIContent(text);
        var size = style.CalcSize(content);
        var y = Screen.height * (VerticalScreenFraction + line * LineSpacingScreenFraction);
        var rect = new Rect(
            (Screen.width - size.x) * 0.5f,
            y,
            size.x,
            size.y);
        GUI.Label(rect, content, style);
    }

    public static void DrawCartoonBanner(string text, Color color, int line = 0) =>
        DrawCartoonBanner(text, color, line, fontScale: 1f);

    public static void DrawCartoonBanner(string text, Color color, int line, float fontScale) =>
        DutzCartoonDialogGui.DrawCartoonBanner(text, color, line, fontScale);
}
