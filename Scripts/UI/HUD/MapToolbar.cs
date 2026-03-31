using Godot;

namespace IdleNet;

public partial class MapToolbar : PanelContainer
{
    private Label? _hintLabel;
    private Label? _statusLabel;
    private Label? _coordsLabel;

    public Label? HintLabel => _hintLabel;
    public Label? StatusLabel => _statusLabel;
    public Label? CoordsLabel => _coordsLabel;

    public override void _Ready()
    {
        Label mapTitleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/TitleColumn/MapTitleLabel");
        _hintLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/TitleColumn/HintLabel");
        _coordsLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/MetaRow/CoordsBadge/CoordsMargin/CoordsLabel");
        _statusLabel = GetNode<Label>("OuterMargin/RootColumn/StatusLabel");
        PanelContainer coordsBadge = GetNode<PanelContainer>("OuterMargin/RootColumn/HeaderRow/MetaRow/CoordsBadge");

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.05f, 0.08f, 0.06f, 1.0f),
            new Color(0.34f, 0.48f, 0.34f, 0.74f),
            20,
            14,
            10));
        coordsBadge.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.13f, 0.11f, 0.08f, 1.0f),
            new Color(0.58f, 0.48f, 0.28f, 0.72f),
            12,
            8,
            5));

        mapTitleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        mapTitleLabel.AddThemeFontSizeOverride("font_size", 20);
        _hintLabel?.AddThemeColorOverride("font_color", new Color(0.86f, 0.93f, 0.86f));
        _hintLabel?.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel?.AddThemeColorOverride("font_color", new Color(0.78f, 0.90f, 0.79f));
        _statusLabel?.AddThemeFontSizeOverride("font_size", 12);
        _coordsLabel?.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.80f));
        _coordsLabel?.AddThemeFontSizeOverride("font_size", 12);
    }
}
