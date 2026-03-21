using Godot;

namespace IdleNet;

public partial class CostRow : PanelContainer
{
    private Label? _iconLabel;
    private Label? _nameLabel;
    private Label? _amountLabel;

    public override void _Ready()
    {
        _iconLabel = GetNode<Label>("HBox/IconLabel");
        _nameLabel = GetNode<Label>("HBox/NameLabel");
        _amountLabel = GetNode<Label>("HBox/AmountLabel");
        AddThemeStyleboxOverride("panel", CreateStyle(false));
    }

    public void SetData(TownCostViewData data)
    {
        if (_iconLabel is null || _nameLabel is null || _amountLabel is null)
        {
            return;
        }

        _iconLabel.Text = data.IconGlyph;
        _nameLabel.Text = data.ItemName;
        _amountLabel.Text = data.Amount.ToString();
        _iconLabel.AddThemeColorOverride("font_color", data.Affordable ? new Color(0.87f, 0.80f, 0.62f) : new Color(0.55f, 0.44f, 0.38f));
        _nameLabel.AddThemeColorOverride("font_color", data.Affordable ? new Color(0.92f, 0.87f, 0.79f) : new Color(0.58f, 0.52f, 0.47f));
        _amountLabel.AddThemeColorOverride("font_color", data.Affordable ? new Color(0.98f, 0.88f, 0.64f) : new Color(0.78f, 0.40f, 0.34f));
        AddThemeStyleboxOverride("panel", CreateStyle(data.Affordable));
    }

    private static StyleBoxFlat CreateStyle(bool affordable)
    {
        return new StyleBoxFlat
        {
            BgColor = affordable ? new Color(0.23f, 0.18f, 0.12f, 0.78f) : new Color(0.19f, 0.14f, 0.12f, 0.62f),
            BorderColor = affordable ? new Color(0.54f, 0.40f, 0.20f, 0.74f) : new Color(0.34f, 0.26f, 0.22f, 0.52f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 8,
            ContentMarginTop = 4,
            ContentMarginRight = 8,
            ContentMarginBottom = 4,
        };
    }
}
