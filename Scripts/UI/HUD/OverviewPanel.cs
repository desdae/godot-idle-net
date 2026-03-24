using System;
using System.Collections.Generic;
using Godot;

namespace IdleNet;

public sealed class OverviewPanelViewData
{
    public string OverviewSummary { get; init; } = string.Empty;
    public string HousingSummary { get; init; } = string.Empty;
    public string ProductionSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> Alerts { get; init; } = Array.Empty<string>();
}

public partial class OverviewPanel : PanelContainer
{
    private Label? _panelTitleLabel;
    private Label? _panelSubtitleLabel;
    private ScrollContainer? _townSummaryScroll;
    private Label? _overviewBodyLabel;
    private Label? _housingBodyLabel;
    private Label? _productionBodyLabel;
    private VBoxContainer? _alertsList;

    private SelectionView? _selectionView;
    private QueueView? _queueView;
    private TownUI? _townUi;
    private PeopleView? _peopleView;

    public SelectionView? SelectionView => _selectionView;
    public QueuePanel? QueuePanel => _queueView?.QueuePanel;
    public TownUI? TownUI => _townUi;
    public PeopleView? PeopleView => _peopleView;

    public override void _Ready()
    {
        _panelTitleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderColumn/PanelTitleLabel");
        _panelSubtitleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderColumn/PanelSubtitleLabel");
        _townSummaryScroll = GetNode<ScrollContainer>("OuterMargin/RootColumn/ContentHost/TownSummaryScroll");
        _overviewBodyLabel = GetNode<Label>("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/OverviewCard/CardMargin/CardColumn/BodyLabel");
        _housingBodyLabel = GetNode<Label>("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/HousingCard/CardMargin/CardColumn/BodyLabel");
        _productionBodyLabel = GetNode<Label>("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/ProductionCard/CardMargin/CardColumn/BodyLabel");
        _alertsList = GetNode<VBoxContainer>("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/AlertsCard/CardMargin/CardColumn/AlertsList");

        _selectionView = GetNode<SelectionView>("OuterMargin/RootColumn/ContentHost/SelectionView");
        _queueView = GetNode<QueueView>("OuterMargin/RootColumn/ContentHost/QueueView");
        _townUi = GetNode<TownUI>("OuterMargin/RootColumn/ContentHost/TownUI");
        _peopleView = GetNode<PeopleView>("OuterMargin/RootColumn/ContentHost/PeopleView");

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.13f, 0.10f, 0.07f, 0.96f),
            new Color(0.53f, 0.41f, 0.23f, 0.88f),
            24,
            10,
            10));

        _panelTitleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        _panelTitleLabel.AddThemeFontSizeOverride("font_size", 22);
        _panelSubtitleLabel.AddThemeColorOverride("font_color", new Color(0.84f, 0.80f, 0.73f));
        _panelSubtitleLabel.AddThemeFontSizeOverride("font_size", 12);
        _panelSubtitleLabel.MaxLinesVisible = 1;
        _panelSubtitleLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;

        ApplyCardStyle("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/OverviewCard");
        ApplyCardStyle("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/HousingCard");
        ApplyCardStyle("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/ProductionCard");
        ApplyCardStyle("OuterMargin/RootColumn/ContentHost/TownSummaryScroll/TownSummaryColumn/AlertsCard");

        ShowContext(HudSection.People);
    }

    public void SetData(OverviewPanelViewData data)
    {
        if (_overviewBodyLabel is null || _housingBodyLabel is null || _productionBodyLabel is null || _alertsList is null)
        {
            return;
        }

        _overviewBodyLabel.Text = data.OverviewSummary;
        _housingBodyLabel.Text = data.HousingSummary;
        _productionBodyLabel.Text = data.ProductionSummary;

        foreach (Node child in _alertsList.GetChildren())
        {
            child.QueueFree();
        }

        if (data.Alerts.Count == 0)
        {
            _alertsList.AddChild(CreateAlertRow("No urgent alerts. Production lines are steady."));
            return;
        }

        foreach (string alert in data.Alerts)
        {
            _alertsList.AddChild(CreateAlertRow(alert));
        }
    }

    public void ShowContext(HudSection section)
    {
        if (_selectionView is null || _queueView is null || _townUi is null || _peopleView is null || _townSummaryScroll is null || _panelTitleLabel is null || _panelSubtitleLabel is null)
        {
            return;
        }

        _townSummaryScroll.Visible = false;
        _selectionView.Visible = section == HudSection.Selection;
        _queueView.Visible = section == HudSection.Queue;
        _townUi.Visible = section == HudSection.Town || section == HudSection.Buildings;
        _peopleView.Visible = section == HudSection.People;

        switch (section)
        {
            case HudSection.Selection:
                _panelTitleLabel.Text = "Map";
                _panelSubtitleLabel.Text = "Select a tile, resource, or villager.";
                break;
            case HudSection.Queue:
                _panelTitleLabel.Text = "Queue";
                _panelSubtitleLabel.Text = "Track active work and priorities.";
                break;
            case HudSection.Town:
                _panelTitleLabel.Text = "Town";
                _panelSubtitleLabel.Text = "Housing, production, and alerts.";
                _townUi.SetPanelMode(TownPanelMode.Overview);
                break;
            case HudSection.Buildings:
                _panelTitleLabel.Text = "Build";
                _panelSubtitleLabel.Text = "Place new works and upgrade the town.";
                _townUi.SetPanelMode(TownPanelMode.Buildings);
                break;
            case HudSection.People:
                _panelTitleLabel.Text = "People";
                _panelSubtitleLabel.Text = "Manage the roster and issue group orders.";
                break;
            default:
                _panelTitleLabel.Text = "Map";
                _panelSubtitleLabel.Text = "Select a tile, resource, or villager.";
                break;
        }
    }

    private void ApplyCardStyle(string path)
    {
        PanelContainer card = GetNode<PanelContainer>(path);
        Label titleLabel = card.GetNode<Label>("CardMargin/CardColumn/TitleLabel");
        Label? bodyLabel = card.GetNodeOrNull<Label>("CardMargin/CardColumn/BodyLabel");

        card.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.22f, 0.16f, 0.10f, 0.88f),
            new Color(0.58f, 0.44f, 0.24f, 0.70f),
            16,
            8,
            8));
        titleLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.88f, 0.70f));
        titleLabel.AddThemeFontSizeOverride("font_size", 15);
        bodyLabel?.AddThemeColorOverride("font_color", new Color(0.90f, 0.85f, 0.77f));
        bodyLabel?.AddThemeFontSizeOverride("font_size", 12);
    }

    private static Control CreateAlertRow(string text)
    {
        PanelContainer row = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.20f, 0.14f, 0.09f, 0.86f),
            new Color(0.63f, 0.48f, 0.25f, 0.68f),
            12,
            8,
            6));

        Label label = new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeColorOverride("font_color", new Color(0.93f, 0.86f, 0.75f));
        label.AddThemeFontSizeOverride("font_size", 11);

        MarginContainer margin = new();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        margin.AddChild(label);
        row.AddChild(margin);
        return row;
    }
}
