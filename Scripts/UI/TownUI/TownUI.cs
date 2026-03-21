using System;
using System.Collections.Generic;
using Godot;

namespace IdleNet;

public partial class TownUI : Control
{
    public event Action? CloseRequested;
    public event Action? StockpileUpgradeRequested;
    public event Action<string>? SellResourceSelected;
    public event Action<int>? SellPercentChanged;
    public event Action? SellRequested;
    public event Action<string>? BuildRequested;
    public event Action<string>? UpgradeRequested;
    public event Action<TownBuildingFilter>? FilterChanged;

    [Export]
    public PackedScene? BuildingCardScene { get; set; }

    [Export]
    public PackedScene? CostRowScene { get; set; }

    private Label? _titleLabel;
    private Label? _goldLabel;
    private Label? _stockpileLabel;
    private ProgressBar? _stockpileBar;
    private Label? _stockpileUpgradeLabel;
    private Button? _stockpileUpgradeButton;
    private VBoxContainer? _resourceList;
    private Label? _sellPromptLabel;
    private Label? _sellValueLabel;
    private HSlider? _sellSlider;
    private Button? _sellButton;
    private Button? _filterAllButton;
    private Button? _filterAvailableButton;
    private Button? _filterExistingButton;
    private VBoxContainer? _buildingList;
    private Label? _ledgerLabel;
    private Button? _closeButton;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/HeaderPanel/HeaderRow/TitleLabel");
		_goldLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/HeaderPanel/HeaderRow/GoldLabel");
		_stockpileLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/StockpileLabel");
		_stockpileBar = GetNode<ProgressBar>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/StockpileBar");
		_stockpileUpgradeLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/StockpileUpgradeLabel");
		_stockpileUpgradeButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/StockpileUpgradeButton");
		_resourceList = GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel/ResourceColumn/ResourceList");
		_sellPromptLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellPromptLabel");
		_sellValueLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellSummaryRow/SellValueLabel");
		_sellSlider = GetNode<HSlider>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellSlider");
		_sellButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel/SellColumn/SellButton");
		_filterAllButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel/BuildColumn/FilterRow/AllButton");
		_filterAvailableButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel/BuildColumn/FilterRow/AvailableButton");
		_filterExistingButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel/BuildColumn/FilterRow/ExistingButton");
		_buildingList = GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel/BuildColumn/BuildingScroll/BuildingList");
		_ledgerLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/LedgerPanel/LedgerMargin/LedgerLabel");
		_closeButton = GetNode<Button>("Frame/OuterMargin/RootColumn/CloseButton");

        ApplyRootStyle();

        _stockpileUpgradeButton!.Pressed += () => StockpileUpgradeRequested?.Invoke();
        _sellSlider!.ValueChanged += value => SellPercentChanged?.Invoke(Mathf.RoundToInt((float)value));
        _sellButton!.Pressed += () => SellRequested?.Invoke();
        _filterAllButton!.Pressed += () => FilterChanged?.Invoke(TownBuildingFilter.All);
        _filterAvailableButton!.Pressed += () => FilterChanged?.Invoke(TownBuildingFilter.Available);
        _filterExistingButton!.Pressed += () => FilterChanged?.Invoke(TownBuildingFilter.Existing);
        _closeButton!.Pressed += RequestClose;
    }

    public void SetData(TownViewData data)
    {
        if (_titleLabel is null || _goldLabel is null || _stockpileLabel is null || _stockpileBar is null ||
            _stockpileUpgradeLabel is null || _stockpileUpgradeButton is null || _resourceList is null ||
            _sellPromptLabel is null || _sellValueLabel is null || _sellSlider is null || _sellButton is null ||
            _buildingList is null || _ledgerLabel is null || _filterAllButton is null || _filterAvailableButton is null ||
            _filterExistingButton is null || BuildingCardScene is null || CostRowScene is null)
        {
            return;
        }

        _titleLabel.Text = data.SettlementTitle;
        _goldLabel.Text = $"o Gold {data.Gold}";
        _stockpileLabel.Text = data.StockpileSummary;
        _stockpileBar.MaxValue = data.StockpileCapacity;
        _stockpileBar.Value = data.StockpileCurrent;
        _stockpileUpgradeLabel.Text = data.StockpileUpgradeSummary;
        _stockpileUpgradeButton.Disabled = !data.CanUpgradeStockpile;
        _sellPromptLabel.Text = data.SellPrompt;
        _sellValueLabel.Text = data.SellAmountText;
        if (!Mathf.IsEqualApprox((float)_sellSlider.Value, data.SellPercent))
        {
            _sellSlider.Value = data.SellPercent;
        }

        _sellButton.Disabled = !data.CanSell;
        RebuildResourceRows(data.Resources);
        RebuildBuildingCards(data.Buildings, BuildingCardScene, CostRowScene);
        _ledgerLabel.Text = data.LedgerText;
        UpdateFilterButtonState(data.ActiveFilter);
    }

    public void RequestSellResourceSelection(string itemId)
    {
        SellResourceSelected?.Invoke(itemId);
    }

    public void RequestSellPercent(int percent)
    {
        if (_sellSlider is not null)
        {
            _sellSlider.Value = percent;
            return;
        }

        SellPercentChanged?.Invoke(percent);
    }

    public void RequestClose()
    {
        CloseRequested?.Invoke();
    }

    private void RebuildResourceRows(IReadOnlyList<TownResourceViewData> resources)
    {
        if (_resourceList is null)
        {
            return;
        }

        foreach (Node child in _resourceList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (TownResourceViewData resource in resources)
        {
            Button row = new()
            {
                Text = $"{resource.IconGlyph}  {resource.DisplayName}   {resource.Amount}   ({resource.SellValue}g)",
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0.0f, 42.0f),
            };
            ApplyResourceButtonStyle(row, resource.Selected);
            row.Pressed += () => SellResourceSelected?.Invoke(resource.ItemId);
            _resourceList.AddChild(row);
        }
    }

    private void RebuildBuildingCards(IReadOnlyList<BuildingCardViewData> buildings, PackedScene buildingCardScene, PackedScene costRowScene)
    {
        if (_buildingList is null)
        {
            return;
        }

        foreach (Node child in _buildingList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (BuildingCardViewData building in buildings)
        {
            BuildingCard card = buildingCardScene.Instantiate<BuildingCard>();
            card.SetData(building, costRowScene);
            card.ActionRequested += OnBuildingCardActionRequested;
            _buildingList.AddChild(card);
        }
    }

    private void OnBuildingCardActionRequested(string buildingId, bool upgrade)
    {
        if (upgrade)
        {
            UpgradeRequested?.Invoke(buildingId);
            return;
        }

        BuildRequested?.Invoke(buildingId);
    }

    private void ApplyRootStyle()
    {
        PanelContainer frame = GetNode<PanelContainer>("Frame");
        PanelContainer headerPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/HeaderPanel");
        PanelContainer resourcePanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/ResourcePanel");
        PanelContainer sellPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/SellPanel");
        PanelContainer buildPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/BuildPanel");
        PanelContainer ledgerPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/MainColumn/LedgerPanel");

        frame.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.16f, 0.11f, 0.08f, 0.97f), new Color(0.61f, 0.46f, 0.24f, 0.92f), 24, 2));
        headerPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.28f, 0.18f, 0.12f, 0.92f), new Color(0.72f, 0.57f, 0.31f, 0.88f), 18, 1));
        resourcePanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.23f, 0.16f, 0.11f, 0.88f), new Color(0.56f, 0.42f, 0.22f, 0.74f), 18, 1));
        sellPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.22f, 0.15f, 0.10f, 0.86f), new Color(0.55f, 0.41f, 0.21f, 0.72f), 18, 1));
        buildPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.20f, 0.14f, 0.10f, 0.86f), new Color(0.54f, 0.40f, 0.22f, 0.74f), 18, 1));
        ledgerPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.21f, 0.15f, 0.10f, 0.82f), new Color(0.48f, 0.36f, 0.18f, 0.62f), 18, 1));

        _titleLabel?.AddThemeColorOverride("font_color", new Color(0.98f, 0.91f, 0.77f));
        _titleLabel?.AddThemeColorOverride("font_shadow_color", new Color(0.10f, 0.06f, 0.03f, 0.92f));
        _titleLabel?.AddThemeConstantOverride("shadow_offset_x", 2);
        _titleLabel?.AddThemeConstantOverride("shadow_offset_y", 2);
        _titleLabel?.AddThemeFontSizeOverride("font_size", 28);

        _goldLabel?.AddThemeColorOverride("font_color", new Color(0.98f, 0.82f, 0.45f));
        _goldLabel?.AddThemeFontSizeOverride("font_size", 20);
        _stockpileLabel?.AddThemeColorOverride("font_color", new Color(0.96f, 0.89f, 0.76f));
        _stockpileLabel?.AddThemeFontSizeOverride("font_size", 18);
        _stockpileUpgradeLabel?.AddThemeColorOverride("font_color", new Color(0.90f, 0.84f, 0.74f));
        _stockpileUpgradeLabel?.AddThemeFontSizeOverride("font_size", 15);
        _sellPromptLabel?.AddThemeColorOverride("font_color", new Color(0.95f, 0.89f, 0.78f));
        _sellPromptLabel?.AddThemeFontSizeOverride("font_size", 18);
        _sellValueLabel?.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.64f));
        _sellValueLabel?.AddThemeFontSizeOverride("font_size", 18);
        _ledgerLabel?.AddThemeColorOverride("font_color", new Color(0.90f, 0.84f, 0.75f));
        _ledgerLabel?.AddThemeFontSizeOverride("font_size", 16);

        if (_stockpileBar is not null)
        {
            _stockpileBar.AddThemeStyleboxOverride("background", CreateBarBackgroundStyle());
            _stockpileBar.AddThemeStyleboxOverride("fill", CreateBarFillStyle());
        }

        if (_sellSlider is not null)
        {
            _sellSlider.AddThemeStyleboxOverride("slider", CreateSliderTrackStyle());
            _sellSlider.AddThemeStyleboxOverride("grabber_area", CreateSliderFillStyle());
            _sellSlider.AddThemeStyleboxOverride("grabber_area_highlight", CreateSliderFillStyle());
            _sellSlider.AddThemeIconOverride("grabber", CreateSliderKnobTexture(new Color(0.88f, 0.79f, 0.51f)));
            _sellSlider.AddThemeIconOverride("grabber_highlight", CreateSliderKnobTexture(new Color(0.98f, 0.89f, 0.66f)));
        }

        ApplyButtonStyle(_stockpileUpgradeButton, false);
        ApplyButtonStyle(_sellButton, true);
        ApplyButtonStyle(_filterAllButton, false);
        ApplyButtonStyle(_filterAvailableButton, false);
        ApplyButtonStyle(_filterExistingButton, false);
        ApplyButtonStyle(_closeButton, false);
    }

    private void UpdateFilterButtonState(TownBuildingFilter activeFilter)
    {
        UpdateFilterButton(_filterAllButton, activeFilter == TownBuildingFilter.All);
        UpdateFilterButton(_filterAvailableButton, activeFilter == TownBuildingFilter.Available);
        UpdateFilterButton(_filterExistingButton, activeFilter == TownBuildingFilter.Existing);
    }

    private static void UpdateFilterButton(Button? button, bool active)
    {
        if (button is null)
        {
            return;
        }

        button.Modulate = active ? Colors.White : new Color(0.84f, 0.79f, 0.72f, 0.82f);
        button.Scale = active ? new Vector2(1.02f, 1.02f) : Vector2.One;
    }

    private static void ApplyResourceButtonStyle(Button button, bool selected)
    {
        Color baseColor = selected ? new Color(0.74f, 0.63f, 0.43f, 0.96f) : new Color(0.24f, 0.18f, 0.12f, 0.82f);
        Color borderColor = selected ? new Color(0.96f, 0.86f, 0.62f, 0.96f) : new Color(0.55f, 0.43f, 0.24f, 0.72f);
        Color textColor = selected ? new Color(0.20f, 0.11f, 0.05f) : new Color(0.92f, 0.86f, 0.76f);

        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeStyleboxOverride("normal", CreateFancyPanelStyle(baseColor, borderColor, 12, 1));
        button.AddThemeStyleboxOverride("hover", CreateFancyPanelStyle(baseColor.Lightened(0.08f), borderColor.Lightened(0.08f), 12, 1));
        button.AddThemeStyleboxOverride("pressed", CreateFancyPanelStyle(baseColor.Darkened(0.08f), borderColor, 12, 1));
    }

    private static void ApplyButtonStyle(Button? button, bool emphasized)
    {
        if (button is null)
        {
            return;
        }

        Color baseColor = emphasized ? new Color(0.82f, 0.67f, 0.36f, 0.96f) : new Color(0.32f, 0.22f, 0.14f, 0.92f);
        Color borderColor = emphasized ? new Color(0.98f, 0.88f, 0.62f, 0.96f) : new Color(0.64f, 0.50f, 0.28f, 0.84f);
        Color textColor = emphasized ? new Color(0.20f, 0.11f, 0.05f) : new Color(0.94f, 0.88f, 0.78f);

        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.51f, 0.48f));
        button.AddThemeFontSizeOverride("font_size", emphasized ? 20 : 17);
        button.AddThemeStyleboxOverride("normal", CreateFancyPanelStyle(baseColor, borderColor, 14, 1));
        button.AddThemeStyleboxOverride("hover", CreateFancyPanelStyle(baseColor.Lightened(0.10f), borderColor.Lightened(0.10f), 14, 1));
        button.AddThemeStyleboxOverride("pressed", CreateFancyPanelStyle(baseColor.Darkened(0.10f), borderColor, 14, 1));
        button.AddThemeStyleboxOverride("disabled", CreateFancyPanelStyle(new Color(0.31f, 0.26f, 0.22f, 0.56f), new Color(0.42f, 0.37f, 0.32f, 0.40f), 14, 1));
    }

    private static StyleBoxFlat CreateFancyPanelStyle(Color background, Color border, int radius, int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ShadowColor = new Color(0.08f, 0.05f, 0.03f, 0.22f),
            ShadowSize = 8,
            ShadowOffset = new Vector2(0.0f, 3.0f),
            ContentMarginLeft = 10,
            ContentMarginTop = 8,
            ContentMarginRight = 10,
            ContentMarginBottom = 8,
        };
    }

    private static StyleBoxFlat CreateBarBackgroundStyle() => CreateFancyPanelStyle(new Color(0.17f, 0.12f, 0.08f, 0.90f), new Color(0.54f, 0.42f, 0.22f, 0.82f), 10, 1);

    private static StyleBoxFlat CreateBarFillStyle() => CreateFancyPanelStyle(new Color(0.21f, 0.58f, 0.36f, 0.98f), new Color(0.83f, 0.92f, 0.72f, 0.82f), 8, 1);

    private static StyleBoxFlat CreateSliderTrackStyle() => CreateFancyPanelStyle(new Color(0.16f, 0.12f, 0.08f, 0.88f), new Color(0.48f, 0.36f, 0.18f, 0.82f), 8, 1);

    private static StyleBoxFlat CreateSliderFillStyle() => CreateFancyPanelStyle(new Color(0.42f, 0.57f, 0.28f, 0.84f), new Color(0.82f, 0.91f, 0.64f, 0.82f), 8, 1);

    private static Texture2D CreateSliderKnobTexture(Color color)
    {
        Image image = Image.CreateEmpty(18, 18, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        Vector2 center = new(9.0f, 9.0f);
        for (int y = 0; y < 18; y++)
        {
            for (int x = 0; x < 18; x++)
            {
                Vector2 point = new(x + 0.5f, y + 0.5f);
                float distance = point.DistanceTo(center);
                if (distance <= 7.4f)
                {
                    image.SetPixel(x, y, color);
                }
                else if (distance <= 8.5f)
                {
                    image.SetPixel(x, y, new Color(0.26f, 0.17f, 0.07f, 0.95f));
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}
