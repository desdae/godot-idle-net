using Godot;

namespace IdleNet;

public partial class SelectionStatChip : PanelContainer
{
    private PanelContainer? _iconFrame;
    private Label? _iconLabel;
    private Label? _labelText;
    private Label? _valueText;

    public override void _Ready()
    {
        _iconFrame = GetNode<PanelContainer>("InnerMargin/Row/IconFrame");
        _iconLabel = GetNode<Label>("InnerMargin/Row/IconFrame/IconLabel");
        _labelText = GetNode<Label>("InnerMargin/Row/Copy/LabelText");
        _valueText = GetNode<Label>("InnerMargin/Row/Copy/ValueText");
    }

    public void SetData(SelectionStatChipViewData data)
    {
        if (_iconFrame is null || _iconLabel is null || _labelText is null || _valueText is null)
        {
            if (!IsInsideTree())
            {
                return;
            }

            _Ready();
        }

        PanelContainer iconFrame = _iconFrame!;
        Label iconLabel = _iconLabel!;
        Label labelText = _labelText!;
        Label valueText = _valueText!;

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateChipStyle(data.AccentColor));

        iconFrame.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateBadgeStyle(data.AccentColor));
        iconLabel.Text = data.IconGlyph;
        iconLabel.TooltipText = data.TooltipText;
        iconLabel.AddThemeColorOverride("font_color", data.AccentColor.Darkened(0.42f));
        iconLabel.AddThemeFontSizeOverride("font_size", 12);

        labelText.Text = data.Label;
        labelText.TooltipText = data.TooltipText;
        labelText.AddThemeColorOverride("font_color", new Color(0.86f, 0.77f, 0.62f));
        labelText.AddThemeFontSizeOverride("font_size", 9);

        valueText.Text = data.Value;
        valueText.TooltipText = data.TooltipText;
        valueText.AddThemeColorOverride("font_color", new Color(0.98f, 0.91f, 0.78f));
        valueText.AddThemeFontSizeOverride("font_size", 12);

        TooltipText = data.TooltipText;
    }
}
