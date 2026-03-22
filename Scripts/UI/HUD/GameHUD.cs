using System;
using Godot;

namespace IdleNet;

public partial class GameHUD : Control
{
    public event Action<HudSection>? SectionChanged;
    private bool _sectionInitialized;

    private NavigationRail? _navigationRail;
    private SelectionView? _selectionView;
    private QueueView? _queueView;
    private TownUI? _townUi;
    private PeopleView? _peopleView;
    private Label? _hintLabel;
    private Label? _coordsLabel;
    private Label? _statusLabel;

    public SelectedResourcePanel? SelectedResourcePanel => _selectionView?.SelectedPanel;
    public QueuePanel? QueuePanel => _queueView?.QueuePanel;
    public TownUI? TownUI => _townUi;
    public PeopleView? PeopleView => _peopleView;
    public Label? HintLabel => _hintLabel;
    public Label? CoordsLabel => _coordsLabel;
    public Label? StatusLabel => _statusLabel;
    public HudSection CurrentSection { get; private set; } = HudSection.Selection;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _navigationRail = GetNode<NavigationRail>("RootMargin/RootColumn/MiddleRow/LeftDock/NavigationRail");
        _selectionView = GetNode<SelectionView>("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame/ContentHost/SelectionView");
        _queueView = GetNode<QueueView>("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame/ContentHost/QueueView");
        _townUi = GetNode<TownUI>("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame/ContentHost/TownUI");
        _peopleView = GetNode<PeopleView>("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame/ContentHost/PeopleView");
        _hintLabel = GetNode<Label>("RootMargin/RootColumn/TopRow/HintPanel/HintMargin/HintLabel");
        _coordsLabel = GetNode<Label>("RootMargin/RootColumn/TopRow/CoordsPanel/CoordsLabel");
        _statusLabel = GetNode<Label>("RootMargin/RootColumn/BottomRow/StatusLabel");

        ApplyMousePassThrough();
        ApplyStyles();
        _navigationRail.SectionSelected += ShowSection;
        _selectionView.ShowEmptyState("Selection Ledger", "Choose a resource, town, or villager to inspect work details.");
        ShowSection(HudSection.Selection);
    }

    public void ShowSection(HudSection section)
    {
        if (_sectionInitialized && CurrentSection == section)
        {
            return;
        }

        CurrentSection = section;
        _sectionInitialized = true;
        _navigationRail?.SetActiveSection(section);

        if (_selectionView is not null)
        {
            _selectionView.Visible = section == HudSection.Selection;
        }

        if (_queueView is not null)
        {
            _queueView.Visible = section == HudSection.Queue;
        }

        if (_townUi is not null)
        {
            bool showTown = section == HudSection.Town || section == HudSection.Buildings;
            _townUi.Visible = showTown;
            _townUi.SetPanelMode(section == HudSection.Buildings ? TownPanelMode.Buildings : TownPanelMode.Overview);
        }

        if (_peopleView is not null)
        {
            _peopleView.Visible = section == HudSection.People;
        }

        SectionChanged?.Invoke(section);
    }

    private void ApplyStyles()
    {
        PanelContainer hintPanel = GetNode<PanelContainer>("RootMargin/RootColumn/TopRow/HintPanel");
        PanelContainer coordsPanel = GetNode<PanelContainer>("RootMargin/RootColumn/TopRow/CoordsPanel");
        PanelContainer contentFrame = GetNode<PanelContainer>("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame");

        hintPanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.15f, 0.11f, 0.08f, 0.92f),
            new Color(0.50f, 0.38f, 0.22f, 0.82f),
            16,
            10,
            8));
        coordsPanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.15f, 0.11f, 0.08f, 0.92f),
            new Color(0.50f, 0.38f, 0.22f, 0.82f),
            16,
            10,
            8));
        contentFrame.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.10f, 0.08f, 0.06f, 0.24f),
            new Color(0.44f, 0.33f, 0.18f, 0.54f),
            22,
            8,
            8));

        _hintLabel?.AddThemeColorOverride("font_color", new Color(0.94f, 0.91f, 0.81f));
        _hintLabel?.AddThemeFontSizeOverride("font_size", 13);
        _coordsLabel?.AddThemeColorOverride("font_color", new Color(0.94f, 0.91f, 0.81f));
        _coordsLabel?.AddThemeFontSizeOverride("font_size", 14);
        _statusLabel?.AddThemeColorOverride("font_color", new Color(0.94f, 0.96f, 0.88f));
        _statusLabel?.AddThemeColorOverride("font_shadow_color", new Color(0.06f, 0.08f, 0.05f, 0.80f));
        _statusLabel?.AddThemeConstantOverride("shadow_offset_x", 2);
        _statusLabel?.AddThemeConstantOverride("shadow_offset_y", 2);
        _statusLabel?.AddThemeFontSizeOverride("font_size", 13);
    }

    private void ApplyMousePassThrough()
    {
        SetMouseFilterForPath("RootMargin", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow/HintPanel", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow/HintPanel/HintMargin", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow/HintPanel/HintMargin/HintLabel", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow/TopSpacer", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow/CoordsPanel", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/TopRow/CoordsPanel/CoordsLabel", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MiddleRow", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MiddleRow/LeftDock", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MiddleRow/LeftDock/ContentFrame/ContentHost", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MiddleRow/WorldSpacer", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/BottomRow", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/BottomRow/BottomSpacer", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/BottomRow/StatusLabel", MouseFilterEnum.Ignore);
    }

    private void SetMouseFilterForPath(string path, MouseFilterEnum mouseFilter)
    {
        if (GetNodeOrNull<Control>(path) is { } control)
        {
            control.MouseFilter = mouseFilter;
        }
    }
}
