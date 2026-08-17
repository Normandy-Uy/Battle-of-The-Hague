using System;
using UnityEngine;

/// <summary>
/// Huge Roblox-style IMGUI panels for phone-friendly dialogs (shops, start level, death, boot).
/// Bright colors, chunky borders, big fonts, and scroll when content exceeds the panel.
/// </summary>
public static class DutzCartoonDialogGui
{
    static Vector2 panelScrollPosition;

    // Sunny yellow panels + inflated plastic blue/red buttons.
    static readonly Color OuterBorderColor = new Color(0.18f, 0.1f, 0.02f, 1f);
    static readonly Color AccentStripeColor = new Color(1f, 0.55f, 0.08f, 1f);
    static readonly Color InnerStripeColor = new Color(0.98f, 0.78f, 0.05f, 1f);
    static readonly Color PanelFill = new Color(1f, 0.93f, 0.16f, 1f);
    static readonly Color TitleInk = new Color(0.12f, 0.06f, 0.38f, 1f);
    static readonly Color BodyInk = new Color(0.16f, 0.08f, 0.22f, 1f);
    static readonly Color HintInk = new Color(0.22f, 0.1f, 0.08f, 1f);

    public enum PlasticButtonColor
    {
        Blue,
        Red
    }

    // User reference swatches — red #ED1C24, blue #00008F, blue rim #33008F.
    static readonly Color PlasticRedBase = Hex("#ED1C24");
    static readonly Color PlasticRedHighlight = Hex("#FF5A62");
    static readonly Color PlasticRedShadow = Hex("#A50E16");
    static readonly Color PlasticRedRim = Hex("#000000");

    static readonly Color PlasticBlueBase = Hex("#00008F");
    static readonly Color PlasticBlueHighlight = Hex("#3333CC");
    static readonly Color PlasticBlueShadow = Hex("#00004A");
    static readonly Color PlasticBlueRim = Hex("#33008F");

    static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var color))
            return color;
        return Color.magenta;
    }

    public static bool UseMobileScale => Application.isMobilePlatform;

    /// <summary>Phone held sideways — short vertical space is the main layout constraint.</summary>
    public static bool IsLandscapeMobile => UseMobileScale && Screen.width > Screen.height;

    public static bool IsCompactLayout =>
        IsLandscapeMobile || (UseMobileScale && Screen.height < 520);

    public static int ScaleFont(int desktop, int mobile) =>
        IsCompactLayout ? Mathf.RoundToInt(mobile * 0.9f) : (UseMobileScale ? mobile : desktop);

    public static float Scale(float desktop, float mobile) =>
        IsCompactLayout ? mobile * 0.9f : (UseMobileScale ? mobile : desktop);

    public static float PanelWidth =>
        IsCompactLayout
            ? Screen.width * 0.96f
            : Scale(Mathf.Min(Screen.width * 0.62f, 920f), Screen.width * 0.96f);

    public static float ContentWidth =>
        PanelWidth - ContentInset * 2f - PanelPadding * 2f;

    public static float ButtonHeight => IsCompactLayout ? Scale(58f, 80f) : Scale(62f, 112f);

    public static float DismissButtonHeight => IsCompactLayout ? Scale(48f, 64f) : Scale(52f, 88f);

    public static float BorderThickness => Scale(8f, 16f);

    public static float InnerAccent => Scale(6f, 10f);

    public static float ContentInset => BorderThickness + InnerAccent + Scale(12f, 18f);

    public static float PanelPadding => Scale(16f, 28f);

    public static float MaxPanelHeight => Screen.height * 0.96f;

    public static Rect CenteredPanel(float height) =>
        new Rect((Screen.width - PanelWidth) * 0.5f, (Screen.height - height) * 0.5f, PanelWidth, height);

    public static Rect LowerPanel(float height) =>
        new Rect((Screen.width - PanelWidth) * 0.5f, Screen.height - height - Scale(28f, 48f), PanelWidth, height);

    /// <summary>Choice dialogs — centered in landscape so all buttons stay on screen.</summary>
    public static Rect ChoiceDialogFrame(float height)
    {
        var clamped = ClampPanelHeight(height);
        return IsCompactLayout
            ? CenteredPanel(clamped)
            : LowerPanel(clamped);
    }

    /// <summary>Level 0–2 / 7 win choices — bottom pinned, always tall enough to show buttons.</summary>
    public static Rect LevelCompleteChoiceFrame(float height)
    {
        var minHeight = ContentInset * 2f + Scale(220f, 320f);
        var clamped = ClampPanelHeight(Mathf.Max(height, minHeight));
        var maxUsable = Screen.height - Scale(12f, 20f);
        clamped = Mathf.Min(clamped, maxUsable);
        return LowerPanel(clamped);
    }

    public static float MeasureLabelHeight(string text, GUIStyle style, float contentWidth)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        return style.CalcHeight(new GUIContent(text), contentWidth);
    }

    public static float MeasureActionButtonHeight(string label)
    {
        var style = ActionButtonStyle();
        var textHeight = style.CalcHeight(new GUIContent(label ?? string.Empty), ContentWidth);
        return Mathf.Max(ButtonHeight, textHeight + Scale(22f, 32f));
    }

    public static float MeasureStackedPanelHeight(
        string title,
        string hint,
        string[] buttonLabels,
        string[] detailLines = null,
        string footer = null)
    {
        var titleStyle = BannerTitleStyle();
        var hintStyle = HintStyle();
        var detailStyle = BodyStyle();
        detailStyle.fontStyle = FontStyle.Normal;
        var spacing = Scale(8f, 12f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += MeasureLabelHeight(title, titleStyle, ContentWidth);
        if (!string.IsNullOrEmpty(hint))
            height += spacing + MeasureLabelHeight(hint, hintStyle, ContentWidth);
        height += spacing;

        if (buttonLabels != null)
        {
            for (var i = 0; i < buttonLabels.Length; i++)
            {
                height += MeasureActionButtonHeight(buttonLabels[i]) + spacing;
                if (detailLines != null && i < detailLines.Length && !string.IsNullOrEmpty(detailLines[i]))
                    height += Scale(4f, 8f) + MeasureLabelHeight(detailLines[i], detailStyle, ContentWidth);
            }
        }

        if (!string.IsNullOrEmpty(footer))
            height += spacing + MeasureLabelHeight(footer, hintStyle, ContentWidth);

        return height;
    }

    /// <summary>Difficulty picker — buttons first (all tappable), details below in a scroll region.</summary>
    public static float MeasureDifficultyButtonBlockHeight(string title, string hint, string[] buttonLabels) =>
        MeasureStackedPanelHeight(title, hint, buttonLabels);

    public static float MeasureDifficultyDetailsHeight(string[] detailLines, string footer)
    {
        if (detailLines == null || detailLines.Length == 0)
            return string.IsNullOrEmpty(footer)
                ? 0f
                : Scale(6f, 10f) + MeasureLabelHeight(footer, HintStyle(), ContentWidth);

        var detailStyle = BodyStyle();
        detailStyle.fontStyle = FontStyle.Normal;
        var spacing = Scale(3f, 6f);
        var detailsHeight = Scale(6f, 10f);
        foreach (var line in detailLines)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            detailsHeight += MeasureLabelHeight(line, detailStyle, ContentWidth) + spacing;
        }

        if (!string.IsNullOrEmpty(footer))
            detailsHeight += MeasureLabelHeight(footer, HintStyle(), ContentWidth);

        return detailsHeight;
    }

    /// <summary>Difficulty picker — buttons first (all tappable), details below in a scroll region.</summary>
    public static float MeasureDifficultyPanelHeight(
        string title,
        string hint,
        string[] buttonLabels,
        string[] detailLines,
        string footer)
    {
        var buttonBlock = MeasureDifficultyButtonBlockHeight(title, hint, buttonLabels);
        var detailsHeight = MeasureDifficultyDetailsHeight(detailLines, footer);
        return buttonBlock + detailsHeight;
    }

    /// <summary>Begin panel body; scrolls automatically when content is taller than the frame.</summary>
    public static bool BeginPanelContent(Rect frame, float requiredHeight)
    {
        var inner = ContentRect(frame);
        // requiredHeight includes outer ContentInset; only the inner body scrolls.
        var scrollContentHeight = requiredHeight - ContentInset * 2f;
        if (scrollContentHeight > inner.height + 4f)
        {
            var viewRect = new Rect(0f, 0f, inner.width, scrollContentHeight);
            panelScrollPosition = GUI.BeginScrollView(inner, panelScrollPosition, viewRect, false, true);
            return true;
        }

        GUILayout.BeginArea(inner);
        return false;
    }

    public static void EndPanelContent(bool scrolling)
    {
        if (scrolling)
            GUI.EndScrollView();
        else
            GUILayout.EndArea();
    }

    public static void ResetPanelScroll() => panelScrollPosition = Vector2.zero;

    public static Vector2 PanelScrollPosition
    {
        get => panelScrollPosition;
        set => panelScrollPosition = value;
    }

    public static float ClampPanelHeight(float height) => Mathf.Min(height, MaxPanelHeight);

    public static float ChoiceDialogHeight(string title, string hint, string[] buttonLabels) =>
        ClampPanelHeight(MeasureStackedPanelHeight(title, hint, buttonLabels));

    public static float RegistrationSetupDialogHeight(
        string title,
        string hint,
        string photoStatus,
        bool includeClearPhoto,
        bool includeEditorPick,
        string[] footerButtonLabels)
    {
        var titleStyle = BannerTitleStyle();
        var hintStyle = HintStyle();
        var contentWidth = PanelWidth - ContentInset * 2f - PanelPadding * 2f;
        var spacing = Scale(6f, 10f);
        var thumbSize = IsCompactLayout ? Scale(72f, 96f) : Scale(96f, 140f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += MeasureLabelHeight(title, titleStyle, contentWidth);
        height += spacing + MeasureLabelHeight(hint, hintStyle, contentWidth);
        height += Scale(10f, 14f);
        height += MeasureLabelHeight("Your name (optional)", hintStyle, contentWidth);
        height += 4f + Scale(36f, 52f);
        height += 8f + thumbSize;
        height += 4f + MeasureLabelHeight(photoStatus ?? string.Empty, hintStyle, contentWidth);
        height += 8f + MeasureActionButtonHeight("ADD YOUR PHOTO");

        if (includeEditorPick)
            height += spacing + MeasureActionButtonHeight("PICK PHOTO (EDITOR)");

        if (includeClearPhoto)
            height += spacing + DismissButtonHeight;

        height += Scale(10f, 16f);
        if (footerButtonLabels != null)
        {
            foreach (var label in footerButtonLabels)
                height += MeasureActionButtonHeight(label) + spacing;
        }

        return ClampPanelHeight(height);
    }

    public static float VictorySharePreviewHeight() =>
        IsCompactLayout ? Scale(120f, 160f) : Scale(160f, 240f);

    public static float Level03ShareDialogHeight(string hint, string[] footerButtonLabels)
    {
        var titleStyle = BannerTitleStyle(new Color(0.1f, 0.55f, 0.18f));
        var hintStyle = HintStyle();
        var contentWidth = PanelWidth - ContentInset * 2f - PanelPadding * 2f;
        var spacing = Scale(6f, 10f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += MeasureLabelHeight("DUTZ IS FREE!", titleStyle, contentWidth);
        height += spacing + MeasureLabelHeight("Final score: 0", hintStyle, contentWidth);
        height += Scale(10f, 14f) + VictorySharePreviewHeight();
        height += Scale(8f, 12f) + MeasureLabelHeight(hint ?? string.Empty, hintStyle, contentWidth);
        height += PanelPadding;

        if (footerButtonLabels != null)
        {
            height += PanelPadding * 0.5f;
            foreach (var label in footerButtonLabels)
                height += MeasureActionButtonHeight(label) + spacing;
        }

        return ClampPanelHeight(height);
    }

    public static float WinScoreDialogHeight(
        string winMessage,
        string scoreLine,
        string[] breakdownLines,
        bool includeChoices,
        string[] choiceButtonLabels)
    {
        var compact = IsCompactLayout;
        var contentWidth = PanelWidth - ContentInset * 2f - PanelPadding * 2f;
        var spacing = Scale(6f, 10f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += PanelPadding;

        var titleStyle = BannerTitleStyle(new Color(0.1f, 0.55f, 0.18f));
        if (!string.IsNullOrEmpty(winMessage))
        {
            height += MeasureLabelHeight(winMessage, titleStyle, contentWidth);
            height += Scale(6f, 10f);
        }

        var scoreStyle = TitleStyle(new Color(1f, 0.9f, 0.2f));
        scoreStyle.fontSize = ScaleFont(42, compact ? 48 : 64);
        height += MeasureLabelHeight(scoreLine, scoreStyle, contentWidth);
        height += Scale(8f, 12f);

        var lineStyle = HintStyle();
        lineStyle.fontSize = ScaleFont(22, compact ? 24 : 30);
        if (breakdownLines != null)
        {
            for (var i = 0; i < breakdownLines.Length; i++)
            {
                if (i == 2)
                    height += 8f;
                height += MeasureLabelHeight(breakdownLines[i], lineStyle, contentWidth);
            }
        }

        if (includeChoices && choiceButtonLabels != null)
        {
            height += Scale(10f, 14f);
            var hintStyle = HintStyle();
            height += MeasureLabelHeight("What would you like to do next?", hintStyle, contentWidth);
            height += Scale(6f, 10f);
            foreach (var label in choiceButtonLabels)
                height += MeasureActionButtonHeight(label) + spacing;
        }

        return ClampPanelHeight(Mathf.Max(height, Scale(300f, 380f)));
    }

    public static float DeathDialogHeight(string message)
    {
        var titleStyle = BannerTitleStyle(new Color(0.95f, 0.35f, 0.1f));
        var messageStyle = BodyStyle();
        var hintStyle = HintStyle();
        var contentWidth = PanelWidth - ContentInset * 2f - PanelPadding * 2f;
        var spacing = Scale(6f, 10f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += MeasureLabelHeight("OOPS!", titleStyle, contentWidth);
        height += spacing + MeasureLabelHeight(message, messageStyle, contentWidth);
        height += spacing;
        height += MeasureActionButtonHeight("RESPAWN") + spacing;
        height += MeasureActionButtonHeight("RESTART LEVEL") + spacing;
        height += MeasureActionButtonHeight("EXIT THE GAME") + spacing;
        height += MeasureLabelHeight("Choose an option above.", hintStyle, contentWidth);
        height += PanelPadding;

        return ClampPanelHeight(Mathf.Max(height, Scale(320f, 400f)));
    }

    public static Rect ContentRect(Rect frame) =>
        new Rect(frame.x + ContentInset, frame.y + ContentInset,
            frame.width - ContentInset * 2f, frame.height - ContentInset * 2f);

    public static float DifficultyPanelHeight => DifficultyPanelHeightForOptions(4);

    public static float FloodModePanelHeight => DifficultyPanelHeightForOptions(2);

    public static float DifficultyPanelHeightForOptions(int optionCount)
    {
        optionCount = Mathf.Max(1, optionCount);
        var detailStyle = BodyStyle();
        detailStyle.fontStyle = FontStyle.Normal;

        var buttonLabels = new string[optionCount];
        var detailLines = new string[optionCount];
        for (var i = 0; i < optionCount; i++)
        {
            buttonLabels[i] = i == optionCount - 1 && optionCount >= 4
                ? "SENIOR CITIZEN MODE"
                : "OPTION " + (i + 1);
            detailLines[i] = "Sample detail line for layout sizing.";
        }

        return ClampPanelHeight(MeasureStackedPanelHeight(
            "CHOOSE DIFFICULTY",
            "Addict + crocodile chase speed (Hard = 7.0 m/s)",
            buttonLabels,
            detailLines,
            Application.isMobilePlatform ? "Tap a level to start" : "Pick a level to start"));
    }

    public static float ShopDialogHeight(
        string shopTitle,
        bool includeForceField,
        bool includeSuperJump,
        bool includeCrypticHint,
        string crypticHintText,
        string forceFieldButtonLabel,
        string superJumpButtonLabel,
        string forceFieldNeedHint,
        string superJumpNeedHint,
        string statusMessage)
    {
        var headerStyle = ShopHeaderStyle();
        var bodyStyle = BodyStyle();
        var hintStyle = HintStyle();
        var contentWidth = PanelWidth - ContentInset * 2f - PanelPadding * 2f;
        var spacing = Scale(6f, 10f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += DismissButtonHeight + spacing;
        height += MeasureLabelHeight(shopTitle, headerStyle, contentWidth);
        height += spacing;

        if (includeForceField)
        {
            height += MeasureLabelHeight("Force Field for 60 seconds:", bodyStyle, contentWidth);
            height += spacing + MeasureActionButtonHeight(forceFieldButtonLabel);
            height += spacing + MeasureLabelHeight(forceFieldNeedHint, hintStyle, contentWidth);
            if (includeCrypticHint && !string.IsNullOrEmpty(crypticHintText))
                height += spacing + MeasureLabelHeight(crypticHintText, hintStyle, contentWidth);
            height += spacing;
        }

        if (includeSuperJump)
        {
            height += MeasureLabelHeight("Super Jump for this run:", bodyStyle, contentWidth);
            height += spacing + MeasureActionButtonHeight(superJumpButtonLabel);
            height += spacing + MeasureLabelHeight(superJumpNeedHint, hintStyle, contentWidth);
            height += spacing;
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            height += MeasureLabelHeight(statusMessage, bodyStyle, contentWidth);
            height += spacing;
        }

        height += DismissButtonHeight + PanelPadding;
        return ClampPanelHeight(Mathf.Max(height, Scale(280f, 360f)));
    }

    public static float PoliceDialogHeight(
        string title,
        string message,
        bool hasPortrait,
        string[] buttonLabels)
    {
        var titleStyle = BannerTitleStyle(new Color(1f, 0.22f, 0.18f));
        var messageStyle = BodyStyle();
        var hintStyle = HintStyle();
        var contentWidth = PanelWidth - ContentInset * 2f - PanelPadding * 2f;
        var spacing = Scale(6f, 10f);

        var height = ContentInset * 2f + PanelPadding * 2f;
        height += MeasureLabelHeight(title, titleStyle, contentWidth);
        height += spacing;

        if (hasPortrait)
        {
            height += (IsCompactLayout ? Scale(100f, 140f) : Scale(280f, 360f)) + spacing;
        }

        height += MeasureLabelHeight(message, messageStyle, contentWidth);
        height += spacing;

        if (buttonLabels != null)
        {
            foreach (var label in buttonLabels)
                height += MeasureActionButtonHeight(label) + spacing;
        }

        height += MeasureLabelHeight("Choose an option above.", hintStyle, contentWidth);
        height += PanelPadding;

        return ClampPanelHeight(Mathf.Max(height, Scale(320f, 420f)));
    }

    public static float ShopPanelHeight(bool expandedShop) =>
        expandedShop ? MaxPanelHeight : Screen.height * 0.78f;

    public static float DeathPanelHeight => Scale(340f, 560f);

    public static float PolicePanelHeight =>
        IsCompactLayout ? Mathf.Min(Screen.height * 0.86f, 480f) : Scale(780f, Screen.height * 0.88f);

    public static float BootFailureHeight => Scale(280f, 460f);

    public static float LevelCompleteHeight(int buttonCount)
    {
        var spacing = Scale(8f, 14f);
        var header = Scale(120f, 200f);
        return PanelPadding * 2f + header + buttonCount * (ButtonHeight + spacing) + ContentInset * 2f;
    }

    public static float FitMessageBoxWidth(string text, GUIStyle style, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return Scale(240f, 360f);

        var content = new GUIContent(text);
        var measureStyle = new GUIStyle(style) { wordWrap = false };
        var textWidth = measureStyle.CalcSize(content).x;
        var horizontalPad = Scale(40f, 56f);
        var minWidth = Scale(200f, 300f);
        return Mathf.Clamp(textWidth + horizontalPad + ContentInset * 2f, minWidth, maxWidth);
    }

    public static float FitMessageBoxHeight(string text, GUIStyle style, float boxWidth)
    {
        var textWidth = Mathf.Max(32f, boxWidth - ContentInset * 2f - Scale(12f, 20f));
        var textHeight = style.CalcHeight(new GUIContent(text ?? string.Empty), textWidth);
        return textHeight + ContentInset * 2f + Scale(20f, 32f);
    }

    static float MeasureBannerInnerWidth(string text, GUIStyle style, float maxInnerWidth)
    {
        if (string.IsNullOrEmpty(text))
            return Scale(180f, 260f);

        var content = new GUIContent(text);
        var singleLineStyle = new GUIStyle(style) { wordWrap = false };
        var naturalWidth = singleLineStyle.CalcSize(content).x;
        if (naturalWidth <= maxInnerWidth)
            return Mathf.Max(naturalWidth + Scale(28f, 40f), Scale(160f, 240f));

        return maxInnerWidth;
    }

    public static void DrawDimOverlay(float alpha = 0.64f)
    {
        var previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    /// <summary>Full-screen photo backdrop (e.g. Senate mural) behind transparent dialogs.</summary>
    public static void DrawFullscreenBackdrop(Texture texture, ScaleMode scaleMode = ScaleMode.ScaleAndCrop)
    {
        if (texture == null)
            return;

        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), texture, scaleMode, true);
    }

    public static void DrawFrame(Rect rect)
    {
        var previous = GUI.color;
        GUI.color = OuterBorderColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        var accent = Inset(rect, BorderThickness);
        GUI.color = AccentStripeColor;
        GUI.DrawTexture(accent, Texture2D.whiteTexture);

        var innerStripe = Inset(accent, InnerAccent);
        GUI.color = InnerStripeColor;
        GUI.DrawTexture(innerStripe, Texture2D.whiteTexture);

        var fill = Inset(innerStripe, InnerAccent);
        GUI.color = PanelFill;
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    /// <summary>See-through cartoon frame — mural/world stays visible behind buttons and text.</summary>
    public static void DrawTransparentFrame(Rect rect, float fillAlpha = 0.22f, float borderAlpha = 0.72f)
    {
        var previous = GUI.color;
        GUI.color = new Color(OuterBorderColor.r, OuterBorderColor.g, OuterBorderColor.b, borderAlpha);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        var accent = Inset(rect, BorderThickness);
        GUI.color = new Color(AccentStripeColor.r, AccentStripeColor.g, AccentStripeColor.b, borderAlpha * 0.85f);
        GUI.DrawTexture(accent, Texture2D.whiteTexture);

        var innerStripe = Inset(accent, InnerAccent);
        GUI.color = new Color(InnerStripeColor.r, InnerStripeColor.g, InnerStripeColor.b, borderAlpha * 0.65f);
        GUI.DrawTexture(innerStripe, Texture2D.whiteTexture);

        var fill = Inset(innerStripe, InnerAccent);
        GUI.color = new Color(PanelFill.r, PanelFill.g, PanelFill.b, Mathf.Clamp01(fillAlpha));
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static GUIStyle TitleStyle(Color? textColor = null)
    {
        var color = textColor ?? TitleInk;
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = ScaleFont(30, 50),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            clipping = TextClipping.Overflow,
            normal = { textColor = color }
        };
    }

    public static GUIStyle BannerTitleStyle(Color? textColor = null)
    {
        var style = TitleStyle(textColor);
        style.fontSize = ScaleFont(34, 56);
        return style;
    }

    public static GUIStyle BodyStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = ScaleFont(24, 38),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            clipping = TextClipping.Overflow,
            normal = { textColor = BodyInk }
        };
    }

    public static GUIStyle HintStyle(Color? textColor = null)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = ScaleFont(20, 32),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            clipping = TextClipping.Overflow,
            normal = { textColor = textColor ?? HintInk }
        };
    }

    public static GUIStyle ShopHeaderStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = ScaleFont(28, 44),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            clipping = TextClipping.Overflow,
            normal = { textColor = TitleInk }
        };
    }

    public static PlasticButtonColor ButtonColorForIndex(int index) =>
        index % 2 == 0 ? PlasticButtonColor.Blue : PlasticButtonColor.Red;

    public static GUIStyle ActionButtonStyle()
    {
        return new GUIStyle(GUI.skin.button)
        {
            fontSize = ScaleFont(22, 36),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(
                Mathf.RoundToInt(Scale(10f, 16f)),
                Mathf.RoundToInt(Scale(10f, 16f)),
                Mathf.RoundToInt(Scale(8f, 12f)),
                Mathf.RoundToInt(Scale(8f, 12f))),
            normal = { textColor = Color.white }
        };
    }

    public static GUIStyle TextFieldStyle()
    {
        return new GUIStyle(GUI.skin.textField)
        {
            fontSize = ScaleFont(18, 28),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = BodyInk }
        };
    }

    public static bool ActionButton(string label, PlasticButtonColor color = PlasticButtonColor.Blue, float? heightOverride = null)
    {
        var buttonHeight = heightOverride ?? MeasureActionButtonHeight(label);
        var style = ActionButtonStyle();
        var rect = GUILayoutUtility.GetRect(
            new GUIContent(label ?? string.Empty),
            style,
            GUILayout.Height(buttonHeight),
            GUILayout.ExpandWidth(true));

        if (Event.current.type == EventType.Repaint)
            DrawPlasticButton(rect, label, color, style);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    public static bool ActionButtonWithCallback(
        string label,
        PlasticButtonColor color,
        float? heightOverride,
        Action onPress)
    {
        var buttonHeight = heightOverride ?? MeasureActionButtonHeight(label);
        var style = ActionButtonStyle();
        var rect = GUILayoutUtility.GetRect(
            new GUIContent(label ?? string.Empty),
            style,
            GUILayout.Height(buttonHeight),
            GUILayout.ExpandWidth(true));

        if (Event.current.type == EventType.Repaint)
            DrawPlasticButton(rect, label, color, style);

        var pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        if (onPress != null && DutzImGuiTouchPoll.SessionActive)
            DutzImGuiTouchPoll.Register(rect, onPress);

        if (pressed && onPress != null)
        {
            DutzImGuiTouchPoll.NotifyGuiButtonConsumed();
            onPress();
            return true;
        }

        return pressed;
    }

    static void DrawPlasticButton(Rect rect, string label, PlasticButtonColor color, GUIStyle style)
    {
        var baseColor = color == PlasticButtonColor.Blue ? PlasticBlueBase : PlasticRedBase;
        var highlight = color == PlasticButtonColor.Blue ? PlasticBlueHighlight : PlasticRedHighlight;
        var shadow = color == PlasticButtonColor.Blue ? PlasticBlueShadow : PlasticRedShadow;
        var rim = color == PlasticButtonColor.Blue ? PlasticBlueRim : PlasticRedRim;

        var previous = GUI.color;
        var rimSize = Scale(6f, 10f);
        var inset = Scale(3f, 5f);

        // Balloon drop shadow on the yellow panel.
        var puffY = Scale(6f, 10f);
        var puffX = Scale(3f, 5f);
        GUI.color = new Color(0f, 0f, 0f, 0.28f);
        GUI.DrawTexture(
            new Rect(rect.x + puffX, rect.y + puffY, rect.width - puffX * 0.5f, rect.height * 0.92f),
            Texture2D.whiteTexture);

        // Thick outer rim — black on red buttons, purple on blue buttons.
        GUI.color = rim;
        GUI.DrawTexture(
            new Rect(rect.x - rimSize, rect.y - rimSize * 0.85f, rect.width + rimSize * 2f, rect.height + rimSize * 1.7f),
            Texture2D.whiteTexture);

        var body = new Rect(
            rect.x + inset,
            rect.y + inset,
            rect.width - inset * 2f,
            rect.height - inset * 2f);

        // Balloon bulge — lighter dome on top, saturated color on lower curve.
        var dome = new Rect(body.x, body.y, body.width, body.height * 0.52f);
        var belly = new Rect(body.x, body.y + body.height * 0.34f, body.width, body.height * 0.66f);
        GUI.color = highlight;
        GUI.DrawTexture(dome, Texture2D.whiteTexture);
        GUI.color = baseColor;
        GUI.DrawTexture(belly, Texture2D.whiteTexture);

        // Side shading to sell the inflated round shape.
        var sideShadeW = body.width * 0.12f;
        GUI.color = new Color(shadow.r, shadow.g, shadow.b, 0.45f);
        GUI.DrawTexture(new Rect(body.x, body.y, sideShadeW, body.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(body.xMax - sideShadeW, body.y, sideShadeW, body.height), Texture2D.whiteTexture);

        // Bottom inner shadow curve.
        GUI.color = shadow;
        GUI.DrawTexture(
            new Rect(body.x + body.width * 0.08f, body.yMax - body.height * 0.22f, body.width * 0.84f, body.height * 0.18f),
            Texture2D.whiteTexture);

        // Primary glossy balloon specular — big bright oval upper-left.
        var glossMain = new Rect(
            body.x + body.width * 0.12f,
            body.y + body.height * 0.12f,
            body.width * 0.42f,
            body.height * 0.28f);
        GUI.color = new Color(1f, 1f, 1f, 0.72f);
        GUI.DrawTexture(glossMain, Texture2D.whiteTexture);

        // Secondary tight highlight dot — wet plastic shine.
        var glossDot = new Rect(
            body.x + body.width * 0.58f,
            body.y + body.height * 0.2f,
            body.width * 0.12f,
            body.height * 0.1f);
        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        GUI.DrawTexture(glossDot, Texture2D.whiteTexture);

        // Thin top rim catch-light.
        GUI.color = new Color(1f, 1f, 1f, 0.35f);
        GUI.DrawTexture(
            new Rect(body.x + body.width * 0.08f, body.y + Scale(1f, 2f), body.width * 0.84f, Scale(2f, 4f)),
            Texture2D.whiteTexture);

        GUI.color = previous;
        DrawOutlinedLabel(rect, label, style, rim);
    }

    public static bool AltActionButton(string label, float? heightOverride = null) =>
        ActionButton(label, PlasticButtonColor.Red, heightOverride);

    public static bool DismissButton(string label = "DISMISS", float? heightOverride = null)
    {
        var buttonHeight = heightOverride ?? DismissButtonHeight;
        return ActionButton(label, PlasticButtonColor.Red, buttonHeight);
    }

    public static bool DangerButton(string label, float? heightOverride = null) =>
        ActionButton(label, PlasticButtonColor.Red, heightOverride);

    public static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color? outline = null)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var shadow = outline ?? new Color(0f, 0f, 0f, 0.85f);
        var offset = Scale(1f, 2f);
        var shadowStyle = new GUIStyle(style) { normal = { textColor = shadow } };
        GUI.Label(new Rect(rect.x - offset, rect.y + offset, rect.width, rect.height), text, shadowStyle);
        GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, shadowStyle);
        GUI.Label(rect, text, style);
    }

    public static void DrawCartoonBanner(string text, Color textColor, int line = 0) =>
        DrawCartoonBanner(text, textColor, line, fontScale: 1f);

    public static void DrawCartoonBanner(string text, Color textColor, int line, float fontScale) =>
        DrawMuralBumpBanner(text, textColor, line, fontScale);

    /// <summary>Transparent HUD quote for mural bumps — outlined text only, no panel.</summary>
    public static void DrawMuralBumpBanner(string text, Color textColor, int line = 0) =>
        DrawMuralBumpBanner(text, textColor, line, fontScale: 1f);

    public static void DrawMuralBumpBanner(string text, Color textColor, int line, float fontScale)
    {
        if (string.IsNullOrEmpty(text))
            return;

        fontScale = Mathf.Max(0.25f, fontScale);
        var fontSize = Mathf.RoundToInt(ScaleFont(30, 40) * fontScale);
        var maxInnerWidth = Scale(Mathf.Min(Screen.width * 0.72f, 900f), Screen.width * 0.9f);
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = textColor }
        };

        var content = new GUIContent(text);
        var innerWidth = MeasureBannerInnerWidth(text, style, maxInnerWidth);
        var textHeight = style.CalcHeight(content, innerWidth);
        var innerHeight = Mathf.Max(textHeight, fontSize * 1.2f);
        var y = Screen.height * (IsCompactLayout ? 0.10f + line * 0.08f : 0.22f + line * 0.1f);
        var textRect = new Rect(
            (Screen.width - innerWidth) * 0.5f,
            y,
            innerWidth,
            innerHeight);
        DrawOutlinedLabel(textRect, text, style, Color.black);
    }

    /// <summary>Large centered welcome — transparent, roughly half screen tall.</summary>
    public static void DrawLargeWelcomeSplash(string text, Color textColor)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var blockHeight = Screen.height * 0.5f;
        var y = (Screen.height - blockHeight) * 0.5f;
        var fontSize = Mathf.RoundToInt(Mathf.Min(blockHeight * 0.22f, Screen.width * 0.14f));
        fontSize = Mathf.Max(fontSize, ScaleFont(52, 88));

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = textColor }
        };

        var textRect = new Rect(0f, y, Screen.width, blockHeight);
        DrawOutlinedLabel(textRect, text, style, Color.black);
    }

    static Rect Inset(Rect rect, float amount) =>
        new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
}
