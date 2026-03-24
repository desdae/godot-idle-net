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
        _coordsLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/CoordsLabel");
        _statusLabel = GetNode<Label>("OuterMargin/RootColumn/StatusLabel");

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.11f, 0.08f, 0.06f, 0.92f),
            new Color(0.45f, 0.34f, 0.19f, 0.82f),
            18,
            10,
            8));

        mapTitleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        mapTitleLabel.AddThemeFontSizeOverride("font_size", 18);
        _hintLabel?.AddThemeColorOverride("font_color", new Color(0.95f, 0.91f, 0.82f));
        _hintLabel?.AddThemeFontSizeOverride("font_size", 11);
        _statusLabel?.AddThemeColorOverride("font_color", new Color(0.82f, 0.92f, 0.81f));
        _statusLabel?.AddThemeFontSizeOverride("font_size", 12);
        _coordsLabel?.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.80f));
        _coordsLabel?.AddThemeFontSizeOverride("font_size", 12);
    }
}
