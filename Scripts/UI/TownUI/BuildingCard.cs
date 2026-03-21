using System;
using System.Text;
using Godot;

namespace IdleNet;

public partial class BuildingCard : PanelContainer
{
    public event Action<string, bool>? ActionRequested;
    public event Action<string>? Selected;

    public string BuildingId => _buildingId;

    private PanelContainer? _illustrationFrame;
    private Label? _illustrationGlyph;
    private Label? _titleLabel;
    private Label? _levelLabel;
    private Label? _summaryLabel;
    private Label? _statusLabel;
    private Label? _timeLabel;
    private Label? _shortRequirementLabel;
    private VBoxContainer? _detailBlock;
    private Label? _detailLabel;
    private HFlowContainer? _costsContainer;
    private ProgressBar? _progressBar;
    private Label? _progressLabel;
    private Button? _actionButton;
    private Tween? _hoverTween;

    private string _buildingId = string.Empty;
    private bool _isUpgradeAction;
    private bool _isUnderConstruction;
    private bool _isSelected;
    private Color _accentColor = new(0.75f, 0.60f, 0.36f);
    private bool _canAct;

    public override void _Ready()
    {
        _illustrationFrame = GetNode<PanelContainer>("Outer/MainColumn/HeaderRow/IllustrationFrame");
        _illustrationGlyph = GetNode<Label>("Outer/MainColumn/HeaderRow/IllustrationFrame/IllustrationGlyph");
        _titleLabel = GetNode<Label>("Outer/MainColumn/HeaderRow/Info/TitleRow/TitleLabel");
        _levelLabel = GetNode<Label>("Outer/MainColumn/HeaderRow/Info/TitleRow/LevelLabel");
        _summaryLabel = GetNode<Label>("Outer/MainColumn/HeaderRow/Info/SummaryLabel");
        _statusLabel = GetNode<Label>("Outer/MainColumn/HeaderRow/Info/MetaRow/StatusLabel");
        _timeLabel = GetNode<Label>("Outer/MainColumn/HeaderRow/Info/MetaRow/TimeLabel");
        _shortRequirementLabel = GetNode<Label>("Outer/MainColumn/HeaderRow/Info/ShortRequirementLabel");
        _detailBlock = GetNode<VBoxContainer>("Outer/MainColumn/DetailBlock");
        _detailLabel = GetNode<Label>("Outer/MainColumn/DetailBlock/DetailLabel");
        _costsContainer = GetNode<HFlowContainer>("Outer/MainColumn/DetailBlock/CostsContainer");
        _progressBar = GetNode<ProgressBar>("Outer/MainColumn/DetailBlock/ConstructionProgress");
        _progressLabel = GetNode<Label>("Outer/MainColumn/DetailBlock/ProgressLabel");
        _actionButton = GetNode<Button>("Outer/MainColumn/HeaderRow/ActionButton");

        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", CreateCardStyle(new Color(0.79f, 0.69f, 0.53f, 0.95f), new Color(0.61f, 0.45f, 0.25f, 0.88f), false));
        GuiInput += OnGuiInput;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        _actionButton.Pressed += OnActionPressed;
    }

    public void SetData(BuildingCardViewData data, PackedScene costRowScene, bool selected)
    {
        if (_illustrationGlyph is null || _illustrationFrame is null || _titleLabel is null || _levelLabel is null ||
            _summaryLabel is null || _statusLabel is null || _timeLabel is null || _shortRequirementLabel is null ||
            _detailBlock is null || _detailLabel is null || _costsContainer is null || _progressBar is null ||
            _progressLabel is null || _actionButton is null)
        {
            if (!IsInsideTree())
            {
                return;
            }

            _Ready();
        }

        Label illustrationGlyph = _illustrationGlyph!;
        PanelContainer illustrationFrame = _illustrationFrame!;
        Label titleLabel = _titleLabel!;
        Label levelLabel = _levelLabel!;
        Label summaryLabel = _summaryLabel!;
        Label statusLabel = _statusLabel!;
        Label timeLabel = _timeLabel!;
        Label shortRequirementLabel = _shortRequirementLabel!;
        Label detailLabel = _detailLabel!;
        HFlowContainer costsContainer = _costsContainer!;
        ProgressBar progressBar = _progressBar!;
        Label progressLabel = _progressLabel!;
        Button actionButton = _actionButton!;

        _buildingId = data.BuildingId;
        _isUpgradeAction = data.IsUpgradeAction;
        _isUnderConstruction = data.IsUnderConstruction;
        _accentColor = data.AccentColor;
        _canAct = data.CanAct;

        illustrationGlyph.Text = data.IconGlyph;
        illustrationGlyph.AddThemeColorOverride("font_color", data.AccentColor.Darkened(0.20f));
        illustrationGlyph.AddThemeFontSizeOverride("font_size", 22);
        illustrationFrame.AddThemeStyleboxOverride("panel", CreateIllustrationStyle(data.AccentColor));

        titleLabel.Text = data.DisplayName;
        levelLabel.Text = data.LevelText;
        summaryLabel.Text = data.Description;
        statusLabel.Text = data.StatusText;
        timeLabel.Text = data.TimeText;

        titleLabel.AddThemeColorOverride("font_color", new Color(0.26f, 0.17f, 0.09f));
        titleLabel.AddThemeFontSizeOverride("font_size", 19);

        levelLabel.AddThemeColorOverride("font_color", new Color(0.31f, 0.20f, 0.10f));
        levelLabel.AddThemeFontSizeOverride("font_size", 13);

        summaryLabel.AddThemeColorOverride("font_color", new Color(0.33f, 0.23f, 0.15f));
        summaryLabel.AddThemeFontSizeOverride("font_size", 13);

        statusLabel.AddThemeColorOverride("font_color", data.IsUnderConstruction ? new Color(0.57f, 0.31f, 0.14f) : new Color(0.34f, 0.24f, 0.15f));
        statusLabel.AddThemeFontSizeOverride("font_size", 12);
        timeLabel.AddThemeColorOverride("font_color", new Color(0.38f, 0.26f, 0.16f));
        timeLabel.AddThemeFontSizeOverride("font_size", 12);
        bool showRequirementLine = !data.IsUnlocked;
        shortRequirementLabel.Visible = showRequirementLine;
        shortRequirementLabel.Text = showRequirementLine ? data.RequirementText : string.Empty;
        shortRequirementLabel.AddThemeColorOverride("font_color", new Color(0.67f, 0.27f, 0.23f));
        shortRequirementLabel.AddThemeFontSizeOverride("font_size", 12);
        detailLabel.Text = data.OutputSummary;
        detailLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.24f, 0.16f));
        detailLabel.AddThemeFontSizeOverride("font_size", 12);

        foreach (Node child in costsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (TownCostViewData cost in data.Costs)
        {
            CostRow row = costRowScene.Instantiate<CostRow>();
            costsContainer.AddChild(row);
            row.SetData(cost);
        }

        progressBar.Visible = data.IsUnderConstruction;
        progressLabel.Visible = data.IsUnderConstruction;
        progressBar.Value = data.Progress * 100.0f;
        progressBar.AddThemeStyleboxOverride("background", CreateBarBackgroundStyle());
        progressBar.AddThemeStyleboxOverride("fill", CreateBarFillStyle(data.AccentColor));
        progressLabel.Text = data.ProgressText;
        progressLabel.AddThemeColorOverride("font_color", new Color(0.42f, 0.27f, 0.14f));
        progressLabel.AddThemeFontSizeOverride("font_size", 12);

        actionButton.Text = data.PrimaryButtonText;
        actionButton.Disabled = !data.CanAct;
        actionButton.TooltipText = BuildTooltipText(data);
        ApplyButtonStyle(actionButton, data);

        TooltipText = BuildTooltipText(data);

        SetSelected(selected || data.IsUnderConstruction);
        Modulate = data.IsUnlocked ? Colors.White : new Color(0.86f, 0.84f, 0.82f, 0.88f);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (_detailBlock is not null)
        {
            _detailBlock.Visible = selected || _isUnderConstruction;
        }

        AddThemeStyleboxOverride("panel", CreateCardStyle(
            selected ? new Color(0.84f, 0.73f, 0.56f, 0.97f) : new Color(0.79f, 0.69f, 0.53f, 0.95f),
            selected ? _accentColor.Lightened(0.18f) : new Color(0.61f, 0.45f, 0.25f, 0.88f),
            selected));
    }

    private void OnActionPressed()
    {
        ActionRequested?.Invoke(_buildingId, _isUpgradeAction);
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton &&
            mouseButton.ButtonIndex == MouseButton.Left &&
            mouseButton.Pressed)
        {
            Selected?.Invoke(_buildingId);
            AcceptEvent();
        }
    }

    private void OnMouseEntered()
    {
        AnimateHover(_isSelected ? new Vector2(1.01f, 1.01f) : new Vector2(1.006f, 1.006f));
    }

    private void OnMouseExited()
    {
        AnimateHover(Vector2.One);
    }

    private void AnimateHover(Vector2 targetScale)
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(this, "scale", targetScale, 0.14);
    }

    private static string BuildTooltipText(BuildingCardViewData data)
    {
        StringBuilder builder = new();
        builder.AppendLine(data.DisplayName);
        builder.AppendLine(data.Description);
        builder.AppendLine(data.TimeText);
        builder.AppendLine(data.RequirementText);
        builder.AppendLine(data.OutputSummary);
        builder.AppendLine(data.BenefitSummary);
        if (data.Costs.Count > 0)
        {
            builder.Append("Costs: ");
            for (int index = 0; index < data.Costs.Count; index++)
            {
                TownCostViewData cost = data.Costs[index];
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(cost.ItemName)
                    .Append(' ')
                    .Append(cost.Amount);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static StyleBoxFlat CreateCardStyle(Color background, Color border, bool selected)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = selected ? 3 : 2,
            BorderWidthTop = selected ? 3 : 2,
            BorderWidthRight = selected ? 3 : 2,
            BorderWidthBottom = selected ? 3 : 2,
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            ShadowColor = new Color(0.08f, 0.05f, 0.03f, 0.22f),
            ShadowSize = selected ? 7 : 5,
            ShadowOffset = new Vector2(0.0f, 2.0f),
            ContentMarginLeft = 5,
            ContentMarginTop = 5,
            ContentMarginRight = 5,
            ContentMarginBottom = 5,
        };
    }

    private static StyleBoxFlat CreateIllustrationStyle(Color accent)
    {
        return new StyleBoxFlat
        {
            BgColor = accent.Lightened(0.33f),
            BorderColor = accent.Darkened(0.15f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ContentMarginLeft = 4,
            ContentMarginTop = 4,
            ContentMarginRight = 4,
            ContentMarginBottom = 4,
        };
    }

    private static StyleBoxFlat CreateBarBackgroundStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.37f, 0.28f, 0.18f, 0.78f),
            BorderColor = new Color(0.62f, 0.47f, 0.24f, 0.82f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };
    }

    private static StyleBoxFlat CreateBarFillStyle(Color accent)
    {
        return new StyleBoxFlat
        {
            BgColor = accent.Darkened(0.10f),
            BorderColor = accent.Lightened(0.15f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
        };
    }

    private static void ApplyButtonStyle(Button button, BuildingCardViewData data)
    {
        Color baseColor = data.CanAct ? data.AccentColor : new Color(0.49f, 0.43f, 0.39f);
        Color borderColor = data.CanAct ? data.AccentColor.Lightened(0.22f) : new Color(0.58f, 0.53f, 0.49f);
        Color textColor = data.CanAct ? new Color(0.18f, 0.10f, 0.04f) : new Color(0.54f, 0.50f, 0.47f);

        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.46f, 0.43f, 0.40f));
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeStyleboxOverride("normal", CreateCardStyle(baseColor.Lightened(0.12f), borderColor, false));
        button.AddThemeStyleboxOverride("hover", CreateCardStyle(baseColor.Lightened(0.20f), borderColor.Lightened(0.10f), false));
        button.AddThemeStyleboxOverride("pressed", CreateCardStyle(baseColor.Darkened(0.08f), borderColor, false));
        button.AddThemeStyleboxOverride("disabled", CreateCardStyle(new Color(0.56f, 0.51f, 0.47f, 0.56f), new Color(0.58f, 0.54f, 0.49f, 0.38f), false));
        button.CustomMinimumSize = new Vector2(112.0f, 36.0f);
    }
}
