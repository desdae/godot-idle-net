using System;
using Godot;

namespace IdleNet;

public partial class BuildingCard : PanelContainer
{
    public event Action<string, bool>? ActionRequested;

    private PanelContainer? _illustrationFrame;
    private Label? _illustrationGlyph;
    private Label? _titleLabel;
    private Label? _levelLabel;
    private Label? _descriptionLabel;
    private Label? _statusLabel;
    private Label? _timeLabel;
    private Label? _requirementsLabel;
    private Label? _effectLabel;
    private Label? _hintLabel;
    private VBoxContainer? _costsContainer;
    private ProgressBar? _progressBar;
    private Label? _progressLabel;
    private Button? _actionButton;
    private Tween? _hoverTween;

    private string _buildingId = string.Empty;
    private bool _isUpgradeAction;

    public override void _Ready()
    {
        _illustrationFrame = GetNode<PanelContainer>("Outer/Row/IllustrationFrame");
        _illustrationGlyph = GetNode<Label>("Outer/Row/IllustrationFrame/IllustrationGlyph");
        _titleLabel = GetNode<Label>("Outer/Row/Info/TitleRow/TitleLabel");
        _levelLabel = GetNode<Label>("Outer/Row/Info/TitleRow/LevelLabel");
        _descriptionLabel = GetNode<Label>("Outer/Row/Info/DescriptionLabel");
        _statusLabel = GetNode<Label>("Outer/Row/Info/MetaRow/StatusLabel");
        _timeLabel = GetNode<Label>("Outer/Row/Info/MetaRow/TimeLabel");
        _requirementsLabel = GetNode<Label>("Outer/Row/Info/RequirementsLabel");
        _effectLabel = GetNode<Label>("Outer/Row/Info/EffectLabel");
        _hintLabel = GetNode<Label>("Outer/Row/Info/HintLabel");
        _costsContainer = GetNode<VBoxContainer>("Outer/Row/Info/CostsContainer");
        _progressBar = GetNode<ProgressBar>("Outer/Row/Info/ConstructionProgress");
        _progressLabel = GetNode<Label>("Outer/Row/Info/ProgressLabel");
        _actionButton = GetNode<Button>("Outer/Row/ActionColumn/ActionButton");
        AddThemeStyleboxOverride("panel", CreateCardStyle(new Color(0.22f, 0.16f, 0.11f, 0.92f), new Color(0.69f, 0.55f, 0.32f, 0.88f)));
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        _actionButton.Pressed += OnActionPressed;
    }

    public void SetData(BuildingCardViewData data, PackedScene costRowScene)
    {
        _buildingId = data.BuildingId;
        _isUpgradeAction = data.IsUpgradeAction;

        if (_illustrationGlyph is null || _illustrationFrame is null || _titleLabel is null || _levelLabel is null ||
            _descriptionLabel is null || _statusLabel is null || _timeLabel is null || _requirementsLabel is null ||
            _effectLabel is null || _hintLabel is null || _costsContainer is null || _progressBar is null ||
            _progressLabel is null || _actionButton is null)
        {
            return;
        }

        _illustrationGlyph.Text = data.IconGlyph;
        _illustrationGlyph.AddThemeColorOverride("font_color", data.AccentColor.Lightened(0.35f));
        _illustrationGlyph.AddThemeFontSizeOverride("font_size", 44);
        _illustrationFrame.AddThemeStyleboxOverride("panel", CreateCardStyle(data.AccentColor.Darkened(0.65f), data.AccentColor));

        _titleLabel.Text = data.DisplayName;
        _levelLabel.Text = data.LevelText;
        _descriptionLabel.Text = data.Description;
        _statusLabel.Text = data.StatusText;
        _timeLabel.Text = data.TimeText;
        _requirementsLabel.Text = data.RequirementText;
        _effectLabel.Text = $"{data.OutputSummary}\n{data.BenefitSummary}";
        _hintLabel.Text = data.ActionHintText;

        _titleLabel.AddThemeColorOverride("font_color", new Color(0.27f, 0.17f, 0.08f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        _levelLabel.AddThemeColorOverride("font_color", data.AccentColor.Darkened(0.2f));
        _levelLabel.AddThemeFontSizeOverride("font_size", 16);
        _descriptionLabel.AddThemeColorOverride("font_color", new Color(0.28f, 0.19f, 0.11f));
        _statusLabel.AddThemeColorOverride("font_color", data.IsUnderConstruction ? new Color(0.55f, 0.32f, 0.12f) : new Color(0.34f, 0.23f, 0.14f));
        _timeLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.25f, 0.16f));
        _requirementsLabel.AddThemeColorOverride("font_color", data.IsUnlocked ? new Color(0.33f, 0.24f, 0.15f) : new Color(0.63f, 0.26f, 0.22f));
        _effectLabel.AddThemeColorOverride("font_color", new Color(0.29f, 0.19f, 0.12f));
        _hintLabel.AddThemeColorOverride("font_color", data.CanAfford ? new Color(0.36f, 0.28f, 0.16f) : new Color(0.60f, 0.26f, 0.21f));

        foreach (Node child in _costsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (TownCostViewData cost in data.Costs)
        {
            CostRow row = costRowScene.Instantiate<CostRow>();
            row.SetData(cost);
            _costsContainer.AddChild(row);
        }

        _progressBar.Visible = data.IsUnderConstruction;
        _progressLabel.Visible = data.IsUnderConstruction;
        _progressBar.Value = data.Progress * 100.0f;
        _progressBar.AddThemeStyleboxOverride("background", CreateBarBackgroundStyle());
        _progressBar.AddThemeStyleboxOverride("fill", CreateBarFillStyle(data.AccentColor));
        _progressLabel.Text = data.ProgressText;
        _progressLabel.AddThemeColorOverride("font_color", new Color(0.36f, 0.24f, 0.12f));

        _actionButton.Text = data.PrimaryButtonText;
        _actionButton.Disabled = !data.CanAct;
        ApplyButtonStyle(_actionButton, data);

        Modulate = data.IsUnlocked ? Colors.White : new Color(0.72f, 0.72f, 0.72f, 0.86f);
    }

    private void OnActionPressed()
    {
        ActionRequested?.Invoke(_buildingId, _isUpgradeAction);
    }

    private void OnMouseEntered()
    {
        AnimateHover(new Vector2(1.01f, 1.01f), new Color(1.03f, 1.03f, 1.03f, 1.0f));
    }

    private void OnMouseExited()
    {
        AnimateHover(Vector2.One, Colors.White);
    }

    private void AnimateHover(Vector2 targetScale, Color targetModulate)
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.SetParallel(true);
        _hoverTween.TweenProperty(this, "scale", targetScale, 0.16);
        _hoverTween.TweenProperty(this, "modulate", targetModulate, 0.16);
    }

    private static StyleBoxFlat CreateCardStyle(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18,
            ShadowColor = new Color(0.08f, 0.05f, 0.03f, 0.28f),
            ShadowSize = 10,
            ShadowOffset = new Vector2(0.0f, 4.0f),
            ContentMarginLeft = 14,
            ContentMarginTop = 14,
            ContentMarginRight = 14,
            ContentMarginBottom = 14,
        };
    }

    private static StyleBoxFlat CreateBarBackgroundStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.28f, 0.21f, 0.13f, 0.88f),
            BorderColor = new Color(0.56f, 0.40f, 0.20f, 0.84f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };
    }

    private static StyleBoxFlat CreateBarFillStyle(Color accent)
    {
        return new StyleBoxFlat
        {
            BgColor = accent,
            BorderColor = accent.Lightened(0.25f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
        };
    }

    private static void ApplyButtonStyle(Button button, BuildingCardViewData data)
    {
        Color baseColor = data.CanAct ? data.AccentColor : new Color(0.42f, 0.36f, 0.31f);
        Color textColor = data.CanAct ? new Color(0.18f, 0.10f, 0.04f) : new Color(0.54f, 0.49f, 0.46f);
        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.44f, 0.41f, 0.39f));
        button.AddThemeFontSizeOverride("font_size", 20);

        button.AddThemeStyleboxOverride("normal", CreateCardStyle(baseColor.Lightened(0.15f), baseColor.Lightened(0.35f)));
        button.AddThemeStyleboxOverride("hover", CreateCardStyle(baseColor.Lightened(0.24f), baseColor.Lightened(0.48f)));
        button.AddThemeStyleboxOverride("pressed", CreateCardStyle(baseColor.Darkened(0.12f), baseColor.Lightened(0.22f)));
        button.AddThemeStyleboxOverride("disabled", CreateCardStyle(new Color(0.49f, 0.44f, 0.40f, 0.58f), new Color(0.52f, 0.48f, 0.42f, 0.40f)));
        button.CustomMinimumSize = new Vector2(168.0f, 54.0f);
    }
}
