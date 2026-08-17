using UnityEngine;

/// <summary>Upper-right collectible counter (coins / suitcases / votes) — 1.5× scale with icons.</summary>
public static class DutzCollectibleHudDraw
{
    const float Scale = 1.5f;
    const float BaseFontSize = 22f;
    const float BasePadding = 16f;
    const float BaseHeight = 32f;
    const float BaseIconGap = 8f;
    const float BaseClusterGap = 28f;
    const float RowBaseWidth = 200f;

    static readonly Color SuitcaseTextColor = new Color(0.88f, 0.68f, 0.38f);
    static readonly Color VotesTextColor = new Color(0.55f, 0.92f, 0.72f);
    static readonly Color CoinTextColor = new Color(1f, 0.82f, 0.15f);

    /// <summary>Right edge margin reserved so centered timer does not overlap the HUD.</summary>
    public static float TimerRightMargin => BasePadding * Scale + RowBaseWidth * Scale * 2f;

    /// <summary>Y just below the upper-right votes / suitcase / coin row.</summary>
    public static float BelowTopRightRowY =>
        BasePadding * Scale + BaseHeight * Scale + BaseIconGap * Scale;

    public static void DrawCoins(int collected) =>
        DrawIconCounterAtRight(DutzCollectibleHudIcons.CoinIcon, collected.ToString(), CoinTextColor);

    public static void DrawSuitcases(int collected, int total) =>
        DrawIconCounterAtRight(
            DutzCollectibleHudIcons.SuitcaseIcon,
            collected.ToString(),
            SuitcaseTextColor);

    /// <summary>Votes alone in the upper-right slot (no suitcase row).</summary>
    public static void DrawVotes(int votes) =>
        DrawVotesClusterAtRight(votes, Screen.width - BasePadding * Scale);

    /// <summary>Votes immediately left of the suitcase counter.</summary>
    public static void DrawVotesBesideSuitcases(int votes, int suitcaseCollected, int suitcaseTotal)
    {
        var suitcaseText = suitcaseCollected.ToString();
        MeasureIconCounter(
            DutzCollectibleHudIcons.SuitcaseIcon,
            suitcaseText,
            out _,
            out _,
            out var suitcaseRowWidth,
            out _);

        var padding = BasePadding * Scale;
        var clusterGap = BaseClusterGap * Scale;
        var suitcaseLeft = Screen.width - padding - suitcaseRowWidth;
        DrawVotesClusterAtRight(votes, suitcaseLeft - clusterGap);
    }

    static void DrawVotesClusterAtRight(int votes, float rightEdge)
    {
        var label = $"VOTES {votes}";
        var fontSize = Mathf.RoundToInt(BaseFontSize * Scale);
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = VotesTextColor }
        };

        var textSize = style.CalcSize(new GUIContent(label));
        var padding = BasePadding * Scale;
        var rowTop = padding;
        var textRect = new Rect(rightEdge - textSize.x, rowTop, textSize.x, textSize.y);
        DutzCartoonDialogGui.DrawOutlinedLabel(textRect, label, style, Color.black);
    }

    static void DrawIconCounterAtRight(Texture2D icon, string countText, Color textColor)
    {
        if (string.IsNullOrEmpty(countText))
            return;

        MeasureIconCounter(icon, countText, out var style, out var textSize, out _, out var rowHeight);
        var padding = BasePadding * Scale;
        var iconSize = BaseHeight * Scale;
        var gap = BaseIconGap * Scale;
        var rowRight = Screen.width - padding;
        var rowTop = padding;

        var textRect = new Rect(rowRight - textSize.x, rowTop, textSize.x, rowHeight);
        var iconRect = new Rect(
            textRect.xMin - gap - iconSize,
            rowTop + (rowHeight - iconSize) * 0.5f,
            iconSize,
            iconSize);

        if (icon != null)
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);

        style.normal.textColor = textColor;
        DutzCartoonDialogGui.DrawOutlinedLabel(textRect, countText, style, Color.black);
    }

    static void MeasureIconCounter(
        Texture2D icon,
        string countText,
        out GUIStyle style,
        out Vector2 textSize,
        out float rowWidth,
        out float rowHeight)
    {
        var fontSize = Mathf.RoundToInt(BaseFontSize * Scale);
        style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = Color.white }
        };

        textSize = style.CalcSize(new GUIContent(countText ?? string.Empty));
        var iconSize = icon != null ? BaseHeight * Scale : 0f;
        var gap = icon != null ? BaseIconGap * Scale : 0f;
        rowHeight = Mathf.Max(iconSize, textSize.y);
        rowWidth = iconSize + gap + textSize.x;
    }
}
