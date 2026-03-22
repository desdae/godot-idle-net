using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace IdleNet;

public partial class TownUI : Control
{
    public event Action? CloseRequested;
    public event Action? OpenWorksRequested;
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
    private PanelContainer? _commercePanel;
    private PanelContainer? _overviewPanel;
    private PanelContainer? _buildPanel;
    private PanelContainer? _footerPanel;
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
    private Label? _structuresValueLabel;
    private Label? _projectsValueLabel;
    private Label? _builderValueLabel;
    private Label? _overviewSummaryLabel;
    private Label? _overviewHintLabel;
    private Button? _openWorksButton;
    private Label? _ledgerLabel;
    private Button? _closeButton;

    private readonly List<BuildingCard> _buildingCards = new();
    private string? _selectedBuildingId;
    private string _resourceSignature = string.Empty;
    private string _buildingSignature = string.Empty;
    private TownPanelMode _currentMode = TownPanelMode.Overview;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderRow/TitleLabel");
        _goldLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderRow/GoldPanel/GoldLabel");
        _commercePanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel");
        _overviewPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel");
        _buildPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel");
        _footerPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/FooterPanel");
        _stockpileLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/StockpileLabel");
        _stockpileBar = GetNode<ProgressBar>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/StockpileBar");
        _stockpileUpgradeLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/StockpileMetaRow/StockpileUpgradeLabel");
        _stockpileUpgradeButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/StockpileMetaRow/StockpileUpgradeButton");
        _resourceList = GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/ResourceColumn/ResourceList");
        _sellPromptLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellPromptLabel");
        _sellValueLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellSummaryRow/SellValueLabel");
        _sellSlider = GetNode<HSlider>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellSlider");
        _sellButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellButton");
        _filterAllButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildTopRow/FilterFrame/FilterMargin/FilterRow/AllButton");
        _filterAvailableButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildTopRow/FilterFrame/FilterMargin/FilterRow/AvailableButton");
        _filterExistingButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildTopRow/FilterFrame/FilterMargin/FilterRow/ExistingButton");
        _buildingList = GetNode<VBoxContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildingList");
        _structuresValueLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewStatsRow/StructuresChip/StructuresValueLabel");
        _projectsValueLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewStatsRow/ProjectsChip/ProjectsValueLabel");
        _builderValueLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewStatsRow/BuilderChip/BuilderValueLabel");
        _overviewSummaryLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewSummaryPanel/OverviewSummaryMargin/OverviewSummaryColumn/OverviewSummaryLabel");
        _overviewHintLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewSummaryPanel/OverviewSummaryMargin/OverviewSummaryColumn/OverviewHintLabel");
        _openWorksButton = GetNode<Button>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewHeaderRow/OpenWorksButton");
        _ledgerLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/FooterPanel/FooterMargin/FooterRow/LedgerLabel");
        _closeButton = GetNode<Button>("Frame/OuterMargin/RootColumn/FooterPanel/FooterMargin/FooterRow/CloseButton");

        ApplyRootStyle();
        ApplyPanelMode();

        _stockpileUpgradeButton!.Pressed += () => StockpileUpgradeRequested?.Invoke();
        _sellSlider!.ValueChanged += value => SellPercentChanged?.Invoke(Mathf.RoundToInt((float)value));
        _sellButton!.Pressed += () => SellRequested?.Invoke();
        _openWorksButton!.Pressed += () => OpenWorksRequested?.Invoke();
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
            _filterExistingButton is null || _structuresValueLabel is null || _projectsValueLabel is null ||
            _builderValueLabel is null || _overviewSummaryLabel is null || _overviewHintLabel is null ||
            BuildingCardScene is null || CostRowScene is null)
        {
            return;
        }

        _titleLabel.Text = _currentMode == TownPanelMode.Buildings ? "Town Works" : data.SettlementTitle;
        _goldLabel.Text = $"o {data.Gold}";
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
        _structuresValueLabel.Text = $"Structures {data.BuiltBuildings}/{data.TotalBuildings}";
        _projectsValueLabel.Text = data.ActiveProjects > 0 ? $"Projects {data.ActiveProjects}" : "Projects idle";
        _builderValueLabel.Text = $"Builder Lv.{data.BuilderLevel}";
        _overviewSummaryLabel.Text = data.WorksSummary;
        _overviewHintLabel.Text = data.WorksHint;

        string nextResourceSignature = BuildResourceSignature(data.Resources);
        if (nextResourceSignature != _resourceSignature)
        {
            RebuildResourceRows(data.Resources);
            _resourceSignature = nextResourceSignature;
        }

        string nextBuildingSignature = BuildBuildingSignature(data.Buildings);
        if (nextBuildingSignature != _buildingSignature)
        {
            RebuildBuildingCards(data.Buildings, BuildingCardScene, CostRowScene);
            _buildingSignature = nextBuildingSignature;
        }

        _ledgerLabel.Text = _currentMode == TownPanelMode.Buildings
            ? "Contracts, upgrades, and current worksites."
            : data.LedgerText;
        _ledgerLabel.TooltipText = data.LedgerText.Replace("  •  ", "\n");
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

    public void RequestOpenWorks()
    {
        OpenWorksRequested?.Invoke();
    }

    public void SetPanelMode(TownPanelMode mode)
    {
        _currentMode = mode;
        if (IsInsideTree())
        {
            ApplyPanelMode();
        }
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
                Text = $"{resource.IconGlyph}  {resource.DisplayName}  {resource.Amount}  ({resource.SellValue}g)",
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0.0f, 34.0f),
                TooltipText = $"{resource.DisplayName}: {resource.Amount} stored, sells for {resource.SellValue} gold each.",
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

        _buildingCards.Clear();

        bool foundSelected = false;
        foreach (BuildingCardViewData building in buildings)
        {
            BuildingCard card = buildingCardScene.Instantiate<BuildingCard>();
            bool isSelected = _selectedBuildingId == building.BuildingId;
            foundSelected |= isSelected;
            _buildingList.AddChild(card);
            card.SetData(building, costRowScene, isSelected);
            card.ActionRequested += OnBuildingCardActionRequested;
            card.Selected += OnBuildingCardSelected;
            _buildingCards.Add(card);
        }

        if (_buildingCards.Count == 0)
        {
            _selectedBuildingId = null;
            return;
        }

        if (!foundSelected)
        {
            _selectedBuildingId = _buildingCards[0].BuildingId;
        }

        ApplyBuildingSelection();
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

    private void OnBuildingCardSelected(string buildingId)
    {
        _selectedBuildingId = buildingId;
        ApplyBuildingSelection();
    }

    private void ApplyBuildingSelection()
    {
        foreach (BuildingCard card in _buildingCards)
        {
            card.SetSelected(card.BuildingId == _selectedBuildingId);
        }
    }

    private void ApplyRootStyle()
    {
        PanelContainer frame = GetNode<PanelContainer>("Frame");
        PanelContainer headerPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/HeaderPanel");
        PanelContainer crestFrame = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderRow/CrestFrame");
        PanelContainer goldPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderRow/GoldPanel");
        PanelContainer commercePanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel");
        PanelContainer commerceDivider = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/CommerceDivider");
        PanelContainer overviewPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel");
        PanelContainer structuresChip = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewStatsRow/StructuresChip");
        PanelContainer projectsChip = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewStatsRow/ProjectsChip");
        PanelContainer builderChip = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewStatsRow/BuilderChip");
        PanelContainer overviewSummaryPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewSummaryPanel");
        PanelContainer buildPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel");
        PanelContainer filterFrame = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildTopRow/FilterFrame");
        PanelContainer footerPanel = GetNode<PanelContainer>("Frame/OuterMargin/RootColumn/FooterPanel");
        Label crestGlyph = GetNode<Label>("Frame/OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderRow/CrestFrame/CrestGlyph");
        Label resourceHeadingLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/ResourceColumn/ResourceHeadingLabel");
        Label sellHeadingLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/CommercePanel/CommerceMargin/CommerceColumn/CommerceRow/SellColumn/SellHeadingLabel");
        Label overviewHeadingLabel = GetNode<Label>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/OverviewPanel/OverviewMargin/OverviewColumn/OverviewHeaderRow/OverviewHeadingLabel");
        SectionHeader buildHeader = GetNode<SectionHeader>("Frame/OuterMargin/RootColumn/BodyScroll/BodyMargin/BodyColumn/BuildPanel/BuildMargin/BuildColumn/BuildTopRow/BuildHeader");

        frame.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.14f, 0.10f, 0.07f, 0.98f), new Color(0.64f, 0.48f, 0.25f, 0.94f), 24, 2, 9, 7));
        headerPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.30f, 0.18f, 0.11f, 0.96f), new Color(0.79f, 0.60f, 0.32f, 0.86f), 18, 1, 8, 6));
        crestFrame.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.24f, 0.16f, 0.09f, 0.94f), new Color(0.86f, 0.70f, 0.38f, 0.88f), 14));
        goldPanel.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.22f, 0.15f, 0.08f, 0.95f), new Color(0.92f, 0.74f, 0.38f, 0.92f), 16));
        commercePanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.24f, 0.16f, 0.10f, 0.90f), new Color(0.61f, 0.45f, 0.24f, 0.76f), 18, 1, 8, 8));
        commerceDivider.AddThemeStyleboxOverride("panel", CreateDividerStyle());
        overviewPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.22f, 0.15f, 0.10f, 0.90f), new Color(0.60f, 0.45f, 0.25f, 0.76f), 18, 1, 8, 8));
        structuresChip.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.27f, 0.19f, 0.12f, 0.86f), new Color(0.70f, 0.58f, 0.31f, 0.82f), 12));
        projectsChip.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.27f, 0.19f, 0.12f, 0.86f), new Color(0.70f, 0.58f, 0.31f, 0.82f), 12));
        builderChip.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.27f, 0.19f, 0.12f, 0.86f), new Color(0.70f, 0.58f, 0.31f, 0.82f), 12));
        overviewSummaryPanel.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.18f, 0.13f, 0.09f, 0.88f), new Color(0.52f, 0.40f, 0.22f, 0.70f), 14));
        buildPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.20f, 0.14f, 0.10f, 0.90f), new Color(0.60f, 0.45f, 0.26f, 0.78f), 18, 1, 8, 8));
        filterFrame.AddThemeStyleboxOverride("panel", CreateInsetStyle(new Color(0.22f, 0.16f, 0.10f, 0.85f), new Color(0.57f, 0.42f, 0.23f, 0.70f), 14));
        footerPanel.AddThemeStyleboxOverride("panel", CreateFancyPanelStyle(new Color(0.18f, 0.13f, 0.09f, 0.88f), new Color(0.49f, 0.36f, 0.20f, 0.70f), 14, 1, 6, 4));

        crestGlyph.AddThemeColorOverride("font_color", new Color(0.77f, 0.96f, 0.67f));
        crestGlyph.AddThemeFontSizeOverride("font_size", 20);

        buildHeader.SetTitle("Build & Upgrade");

        _titleLabel?.AddThemeColorOverride("font_color", new Color(0.98f, 0.91f, 0.79f));
        _titleLabel?.AddThemeColorOverride("font_shadow_color", new Color(0.08f, 0.05f, 0.03f, 0.88f));
        _titleLabel?.AddThemeConstantOverride("shadow_offset_x", 2);
        _titleLabel?.AddThemeConstantOverride("shadow_offset_y", 2);
        _titleLabel?.AddThemeFontSizeOverride("font_size", 23);

        _goldLabel?.AddThemeColorOverride("font_color", new Color(0.99f, 0.84f, 0.46f));
        _goldLabel?.AddThemeFontSizeOverride("font_size", 18);

        ApplySmallHeadingStyle(resourceHeadingLabel);
        ApplySmallHeadingStyle(sellHeadingLabel);
        ApplySmallHeadingStyle(overviewHeadingLabel);

        _stockpileLabel?.AddThemeColorOverride("font_color", new Color(0.97f, 0.90f, 0.77f));
        _stockpileLabel?.AddThemeFontSizeOverride("font_size", 17);
        _stockpileUpgradeLabel?.AddThemeColorOverride("font_color", new Color(0.89f, 0.83f, 0.74f));
        _stockpileUpgradeLabel?.AddThemeFontSizeOverride("font_size", 12);
        _sellPromptLabel?.AddThemeColorOverride("font_color", new Color(0.95f, 0.89f, 0.78f));
        _sellPromptLabel?.AddThemeFontSizeOverride("font_size", 13);
        _sellValueLabel?.AddThemeColorOverride("font_color", new Color(0.99f, 0.88f, 0.64f));
        _sellValueLabel?.AddThemeFontSizeOverride("font_size", 14);
        _structuresValueLabel?.AddThemeColorOverride("font_color", new Color(0.96f, 0.90f, 0.78f));
        _structuresValueLabel?.AddThemeFontSizeOverride("font_size", 12);
        _projectsValueLabel?.AddThemeColorOverride("font_color", new Color(0.96f, 0.90f, 0.78f));
        _projectsValueLabel?.AddThemeFontSizeOverride("font_size", 12);
        _builderValueLabel?.AddThemeColorOverride("font_color", new Color(0.96f, 0.90f, 0.78f));
        _builderValueLabel?.AddThemeFontSizeOverride("font_size", 12);
        _overviewSummaryLabel?.AddThemeColorOverride("font_color", new Color(0.95f, 0.89f, 0.78f));
        _overviewSummaryLabel?.AddThemeFontSizeOverride("font_size", 13);
        _overviewHintLabel?.AddThemeColorOverride("font_color", new Color(0.82f, 0.78f, 0.72f));
        _overviewHintLabel?.AddThemeFontSizeOverride("font_size", 12);
        _ledgerLabel?.AddThemeColorOverride("font_color", new Color(0.88f, 0.82f, 0.74f));
        _ledgerLabel?.AddThemeFontSizeOverride("font_size", 12);

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

        ApplyButtonStyle(_stockpileUpgradeButton, false, new Vector2(136.0f, 28.0f), 12);
        ApplyButtonStyle(_openWorksButton, false, new Vector2(122.0f, 28.0f), 12);
        ApplyButtonStyle(_sellButton, true, new Vector2(0.0f, 36.0f), 17);
        ApplyButtonStyle(_filterAllButton, false, new Vector2(0.0f, 26.0f), 12);
        ApplyButtonStyle(_filterAvailableButton, false, new Vector2(0.0f, 26.0f), 12);
        ApplyButtonStyle(_filterExistingButton, false, new Vector2(0.0f, 26.0f), 12);
        ApplyButtonStyle(_closeButton, false, new Vector2(128.0f, 26.0f), 13);
    }

    private void ApplyPanelMode()
    {
        if (_titleLabel is null || _commercePanel is null || _overviewPanel is null || _buildPanel is null || _footerPanel is null || _closeButton is null || _ledgerLabel is null)
        {
            return;
        }

        bool showCommerce = _currentMode == TownPanelMode.Overview;
        bool showOverview = _currentMode == TownPanelMode.Overview;
        bool showBuild = _currentMode == TownPanelMode.Buildings;

        _commercePanel.Visible = showCommerce;
        _overviewPanel.Visible = showOverview;
        _buildPanel.Visible = showBuild;
        _footerPanel.Visible = true;

        switch (_currentMode)
        {
            case TownPanelMode.Buildings:
                _titleLabel.Text = "Town Works";
                _ledgerLabel.Text = "Contracts, upgrades, and current worksites.";
                _closeButton.Text = "Close Works";
                break;
            default:
                _titleLabel.Text = "Starter Town";
                _closeButton.Text = "Close Ledger";
                break;
        }
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

        button.Modulate = active ? Colors.White : new Color(0.82f, 0.78f, 0.72f, 0.85f);
        button.Scale = active ? new Vector2(1.02f, 1.02f) : Vector2.One;
    }

    private static void ApplySmallHeadingStyle(Label label)
    {
        label.AddThemeColorOverride("font_color", new Color(0.94f, 0.80f, 0.47f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.09f, 0.05f, 0.03f, 0.72f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        label.AddThemeFontSizeOverride("font_size", 15);
    }

    private static void ApplyResourceButtonStyle(Button button, bool selected)
    {
        Color baseColor = selected ? new Color(0.76f, 0.63f, 0.42f, 0.94f) : new Color(0.30f, 0.21f, 0.14f, 0.82f);
        Color borderColor = selected ? new Color(0.96f, 0.86f, 0.61f, 0.94f) : new Color(0.58f, 0.44f, 0.24f, 0.72f);
        Color textColor = selected ? new Color(0.20f, 0.11f, 0.05f) : new Color(0.93f, 0.86f, 0.75f);

        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeStyleboxOverride("normal", CreateFancyPanelStyle(baseColor, borderColor, 10, 1, 5, 3));
        button.AddThemeStyleboxOverride("hover", CreateFancyPanelStyle(baseColor.Lightened(0.06f), borderColor.Lightened(0.06f), 10, 1, 5, 3));
        button.AddThemeStyleboxOverride("pressed", CreateFancyPanelStyle(baseColor.Darkened(0.08f), borderColor, 10, 1, 5, 3));
    }

    private static void ApplyButtonStyle(Button? button, bool emphasized, Vector2 minimumSize, int fontSize)
    {
        if (button is null)
        {
            return;
        }

        Color baseColor = emphasized ? new Color(0.82f, 0.67f, 0.36f, 0.96f) : new Color(0.34f, 0.24f, 0.15f, 0.92f);
        Color borderColor = emphasized ? new Color(0.98f, 0.88f, 0.62f, 0.96f) : new Color(0.63f, 0.49f, 0.28f, 0.84f);
        Color textColor = emphasized ? new Color(0.20f, 0.11f, 0.05f) : new Color(0.94f, 0.88f, 0.78f);

        button.AddThemeColorOverride("font_color", textColor);
        button.AddThemeColorOverride("font_hover_color", textColor);
        button.AddThemeColorOverride("font_pressed_color", textColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.56f, 0.52f, 0.48f));
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeStyleboxOverride("normal", CreateFancyPanelStyle(baseColor, borderColor, 12, 1, 6, 5));
        button.AddThemeStyleboxOverride("hover", CreateFancyPanelStyle(baseColor.Lightened(0.10f), borderColor.Lightened(0.10f), 12, 1, 6, 5));
        button.AddThemeStyleboxOverride("pressed", CreateFancyPanelStyle(baseColor.Darkened(0.10f), borderColor, 12, 1, 6, 5));
        button.AddThemeStyleboxOverride("disabled", CreateFancyPanelStyle(new Color(0.31f, 0.26f, 0.22f, 0.56f), new Color(0.42f, 0.37f, 0.32f, 0.40f), 12, 1, 6, 5));
        button.CustomMinimumSize = minimumSize;
    }

    private static StyleBoxFlat CreateFancyPanelStyle(Color background, Color border, int radius, int borderWidth, int horizontalPadding, int verticalPadding)
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
            ShadowColor = new Color(0.08f, 0.05f, 0.03f, 0.18f),
            ShadowSize = 7,
            ShadowOffset = new Vector2(0.0f, 2.0f),
            ContentMarginLeft = horizontalPadding,
            ContentMarginTop = verticalPadding,
            ContentMarginRight = horizontalPadding,
            ContentMarginBottom = verticalPadding,
        };
    }

    private static StyleBoxFlat CreateInsetStyle(Color background, Color border, int radius)
    {
        return CreateFancyPanelStyle(background, border, radius, 1, 6, 4);
    }

    private static StyleBoxFlat CreateDividerStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.59f, 0.44f, 0.23f, 0.42f),
            BorderColor = new Color(0.80f, 0.66f, 0.36f, 0.20f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
        };
    }

    private static StyleBoxFlat CreateBarBackgroundStyle() => CreateFancyPanelStyle(new Color(0.18f, 0.12f, 0.08f, 0.92f), new Color(0.56f, 0.43f, 0.24f, 0.82f), 10, 1, 4, 2);

    private static StyleBoxFlat CreateBarFillStyle() => CreateFancyPanelStyle(new Color(0.22f, 0.60f, 0.37f, 0.98f), new Color(0.84f, 0.93f, 0.74f, 0.82f), 8, 1, 3, 1);

    private static StyleBoxFlat CreateSliderTrackStyle() => CreateFancyPanelStyle(new Color(0.18f, 0.13f, 0.08f, 0.88f), new Color(0.49f, 0.37f, 0.19f, 0.82f), 8, 1, 3, 2);

    private static StyleBoxFlat CreateSliderFillStyle() => CreateFancyPanelStyle(new Color(0.42f, 0.57f, 0.28f, 0.84f), new Color(0.82f, 0.91f, 0.64f, 0.82f), 8, 1, 3, 2);

    private static Texture2D CreateSliderKnobTexture(Color color)
    {
        Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        Vector2 center = new(8.0f, 8.0f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Vector2 point = new(x + 0.5f, y + 0.5f);
                float distance = point.DistanceTo(center);
                if (distance <= 6.4f)
                {
                    image.SetPixel(x, y, color);
                }
                else if (distance <= 7.5f)
                {
                    image.SetPixel(x, y, new Color(0.26f, 0.17f, 0.07f, 0.95f));
                }
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static string BuildResourceSignature(IReadOnlyList<TownResourceViewData> resources)
    {
        StringBuilder builder = new();
        foreach (TownResourceViewData resource in resources)
        {
            builder.Append(resource.ItemId)
                .Append(':')
                .Append(resource.Amount)
                .Append(':')
                .Append(resource.Selected ? '1' : '0')
                .Append('|');
        }

        return builder.ToString();
    }

    private static string BuildBuildingSignature(IReadOnlyList<BuildingCardViewData> buildings)
    {
        StringBuilder builder = new();
        foreach (BuildingCardViewData building in buildings)
        {
            builder.Append(building.BuildingId)
                .Append(':')
                .Append(building.CurrentLevel)
                .Append(':')
                .Append(building.StatusText)
                .Append(':')
                .Append(building.CanAct ? '1' : '0')
                .Append(':')
                .Append(building.Progress.ToString("0.000"))
                .Append(':')
                .Append(string.Join(",", building.Costs.Select(cost => $"{cost.ItemId}-{cost.Amount}-{(cost.Affordable ? 1 : 0)}")))
                .Append('|');
        }

        return builder.ToString();
    }
}
