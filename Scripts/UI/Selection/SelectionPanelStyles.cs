using Godot;

namespace IdleNet;

public static class SelectionPanelStyles
{
    public static StyleBoxFlat CreateFrameStyle(Color accent)
    {
        Color border = accent.Lerp(new Color(0.78f, 0.62f, 0.35f, 0.96f), 0.62f);
        return new StyleBoxFlat
        {
            BgColor = new Color(0.14f, 0.10f, 0.07f, 0.97f),
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 20,
            CornerRadiusTopRight = 20,
            CornerRadiusBottomLeft = 20,
            CornerRadiusBottomRight = 20,
            ShadowColor = new Color(0.06f, 0.04f, 0.03f, 0.30f),
            ShadowSize = 10,
            ShadowOffset = new Vector2(0.0f, 3.0f),
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
        };
    }

    public static StyleBoxFlat CreateInsetStyle(Color background, Color border, int radius, int horizontalPadding, int verticalPadding)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ShadowColor = new Color(0.04f, 0.03f, 0.02f, 0.16f),
            ShadowSize = 4,
            ShadowOffset = new Vector2(0.0f, 1.0f),
            ContentMarginLeft = horizontalPadding,
            ContentMarginTop = verticalPadding,
            ContentMarginRight = horizontalPadding,
            ContentMarginBottom = verticalPadding,
        };
    }

    public static StyleBoxFlat CreateDividerStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.72f, 0.56f, 0.31f, 0.34f),
            BorderColor = new Color(0.88f, 0.75f, 0.47f, 0.16f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
        };
    }

    public static StyleBoxFlat CreateChipStyle(Color accent)
    {
        return CreateInsetStyle(
            new Color(0.27f, 0.18f, 0.11f, 0.92f),
            accent.Lerp(new Color(0.74f, 0.57f, 0.31f, 0.80f), 0.68f),
            12,
            5,
            4);
    }

    public static StyleBoxFlat CreateMedallionStyle(Color accent)
    {
        Color shadow = accent.Darkened(0.45f);
        return new StyleBoxFlat
        {
            BgColor = accent.Lightened(0.32f),
            BorderColor = accent.Lerp(new Color(0.97f, 0.87f, 0.62f, 0.96f), 0.58f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            ShadowColor = new Color(shadow.R, shadow.G, shadow.B, 0.36f),
            ShadowSize = 7,
            ShadowOffset = new Vector2(0.0f, 1.0f),
            ContentMarginLeft = 5,
            ContentMarginTop = 5,
            ContentMarginRight = 5,
            ContentMarginBottom = 5,
        };
    }

    public static StyleBoxFlat CreateBadgeStyle(Color accent)
    {
        return CreateInsetStyle(
            accent.Lightened(0.28f),
            accent.Lerp(new Color(0.99f, 0.90f, 0.64f, 0.94f), 0.50f),
            10,
            4,
            2);
    }

    public static StyleBoxFlat CreateCalloutStyle(Color accent, bool emphasized)
    {
        Color baseColor = emphasized
            ? accent.Lerp(new Color(0.63f, 0.46f, 0.24f, 0.94f), 0.38f)
            : new Color(0.24f, 0.18f, 0.12f, 0.88f);
        Color border = emphasized
            ? accent.Lerp(new Color(0.95f, 0.84f, 0.58f, 0.88f), 0.46f)
            : new Color(0.56f, 0.44f, 0.26f, 0.54f);
        return CreateInsetStyle(baseColor, border, 12, 6, 4);
    }

    public static void ApplyActionButtonStyle(Button button, Color accent, bool primary, Vector2 minimumSize, int fontSize)
    {
        Color baseColor = primary
            ? accent.Lerp(new Color(0.88f, 0.73f, 0.39f, 0.96f), 0.40f)
            : new Color(0.31f, 0.22f, 0.14f, 0.92f);
        Color borderColor = primary
            ? accent.Lerp(new Color(1.00f, 0.90f, 0.66f, 0.98f), 0.52f)
            : accent.Lerp(new Color(0.66f, 0.52f, 0.30f, 0.84f), 0.60f);
        Color textColor = primary
            ? new Color(0.21f, 0.11f, 0.05f)
            : new Color(0.95f, 0.88f, 0.77f);

        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeColorOverride("font_disabled_color", primary
            ? new Color(0.56f, 0.49f, 0.42f)
            : new Color(0.58f, 0.53f, 0.49f));
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeStyleboxOverride("normal", CreateInsetStyle(baseColor, borderColor, primary ? 14 : 12, 8, primary ? 7 : 5));
        button.AddThemeStyleboxOverride("hover", CreateInsetStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), primary ? 14 : 12, 8, primary ? 7 : 5));
        button.AddThemeStyleboxOverride("pressed", CreateInsetStyle(baseColor.Darkened(0.10f), borderColor, primary ? 14 : 12, 8, primary ? 7 : 5));
        button.AddThemeStyleboxOverride("focus", CreateInsetStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), primary ? 14 : 12, 8, primary ? 7 : 5));
        button.AddThemeStyleboxOverride("disabled", CreateInsetStyle(
            new Color(0.35f, 0.29f, 0.24f, 0.62f),
            new Color(0.46f, 0.40f, 0.35f, 0.42f),
            primary ? 14 : 12,
            8,
            primary ? 7 : 5));
        button.CustomMinimumSize = minimumSize;
    }
}
