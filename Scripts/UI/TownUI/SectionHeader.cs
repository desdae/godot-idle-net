using Godot;

namespace IdleNet;

public partial class SectionHeader : MarginContainer
{
    private Label? _titleLabel;
    private PanelContainer? _lineLeft;
    private PanelContainer? _lineRight;
    private PanelContainer? _gemFrame;
    private Label? _gemLabel;

    [Export]
    public string TitleText { get; set; } = "Header";

    [Export]
    public string GemGlyph { get; set; } = "*";

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("HBox/TitleLabel");
        _lineLeft = GetNode<PanelContainer>("HBox/LineLeft");
        _lineRight = GetNode<PanelContainer>("HBox/LineRight");
        _gemFrame = GetNode<PanelContainer>("HBox/GemFrame");
        _gemLabel = GetNode<Label>("HBox/GemFrame/GemLabel");

        AddThemeConstantOverride("margin_left", 2);
        AddThemeConstantOverride("margin_right", 2);
        AddThemeConstantOverride("margin_top", 2);
        AddThemeConstantOverride("margin_bottom", 2);

        StyleLine(_lineLeft);
        StyleLine(_lineRight);
        StyleGem();
        SetTitle(TitleText);
    }

    public void SetTitle(string title)
    {
        TitleText = title;
        if (_titleLabel is null)
        {
            return;
        }

        _titleLabel.Text = title;
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.89f, 0.74f));
        _titleLabel.AddThemeColorOverride("font_shadow_color", new Color(0.10f, 0.06f, 0.03f, 0.9f));
        _titleLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        _titleLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        _titleLabel.AddThemeFontSizeOverride("font_size", 20);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
    }

    private static void StyleLine(PanelContainer? panel)
    {
        if (panel is null)
        {
            return;
        }

        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.42f, 0.28f, 0.14f, 0.92f),
            BorderColor = new Color(0.73f, 0.57f, 0.30f, 0.88f),
            BorderWidthBottom = 1,
            ExpandMarginBottom = 1.0f,
            ContentMarginTop = 8.0f,
        };
        panel.AddThemeStyleboxOverride("panel", style);
    }

    private void StyleGem()
    {
        if (_gemFrame is null || _gemLabel is null)
        {
            return;
        }

        StyleBoxFlat frameStyle = new()
        {
            BgColor = new Color(0.23f, 0.15f, 0.08f, 0.95f),
            BorderColor = new Color(0.84f, 0.67f, 0.33f, 0.94f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
        };
        _gemFrame.AddThemeStyleboxOverride("panel", frameStyle);
        _gemLabel.Text = GemGlyph;
        _gemLabel.AddThemeColorOverride("font_color", new Color(0.72f, 0.95f, 0.62f));
        _gemLabel.AddThemeFontSizeOverride("font_size", 18);
        _gemLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _gemLabel.VerticalAlignment = VerticalAlignment.Center;
    }
}
