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
    private Label? _sectionKickerLabel;
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
        const string headerRoot = "OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn";
        const string contentRoot = "OuterMargin/RootColumn/ContentFrame/ContentMargin/ContentHost";

        _sectionKickerLabel = GetNode<Label>($"{headerRoot}/SectionKickerLabel");
        _panelTitleLabel = GetNode<Label>($"{headerRoot}/PanelTitleLabel");
        _panelSubtitleLabel = GetNode<Label>($"{headerRoot}/PanelSubtitleLabel");
        _townSummaryScroll = GetNode<ScrollContainer>($"{contentRoot}/TownSummaryScroll");
        _overviewBodyLabel = GetNode<Label>($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/OverviewCard/CardMargin/CardColumn/BodyLabel");
        _housingBodyLabel = GetNode<Label>($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/HousingCard/CardMargin/CardColumn/BodyLabel");
        _productionBodyLabel = GetNode<Label>($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/ProductionCard/CardMargin/CardColumn/BodyLabel");
        _alertsList = GetNode<VBoxContainer>($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/AlertsCard/CardMargin/CardColumn/AlertsList");

        _selectionView = GetNode<SelectionView>($"{contentRoot}/SelectionView");
        _queueView = GetNode<QueueView>($"{contentRoot}/QueueView");
        _townUi = GetNode<TownUI>($"{contentRoot}/TownUI");
        _peopleView = GetNode<PeopleView>($"{contentRoot}/PeopleView");

        AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        GetNode<PanelContainer>("OuterMargin/RootColumn/HeaderPanel").AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.08f, 0.11f, 0.08f, 1.0f),
            new Color(0.34f, 0.46f, 0.32f, 0.76f),
            24,
            12,
            12));
        GetNode<PanelContainer>("OuterMargin/RootColumn/ContentFrame").AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.12f, 0.09f, 0.07f, 1.0f),
            new Color(0.48f, 0.39f, 0.24f, 0.76f),
            24,
            10,
            10));

        _sectionKickerLabel?.AddThemeColorOverride("font_color", new Color(0.76f, 0.91f, 0.77f));
        _sectionKickerLabel?.AddThemeFontSizeOverride("font_size", 10);
        _panelTitleLabel?.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        _panelTitleLabel?.AddThemeFontSizeOverride("font_size", 24);
        _panelSubtitleLabel?.AddThemeColorOverride("font_color", new Color(0.84f, 0.88f, 0.82f));
        _panelSubtitleLabel?.AddThemeFontSizeOverride("font_size", 13);
        if (_panelSubtitleLabel is not null)
        {
            _panelSubtitleLabel.MaxLinesVisible = 1;
            _panelSubtitleLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        }

        ApplyCardStyle($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/OverviewCard");
        ApplyCardStyle($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/HousingCard");
        ApplyCardStyle($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/ProductionCard");
        ApplyCardStyle($"{contentRoot}/TownSummaryScroll/TownSummaryColumn/AlertsCard");

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
