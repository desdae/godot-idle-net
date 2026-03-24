using System;
using Godot;

namespace IdleNet;

public partial class GameHUD : Control
{
    public event Action<HudSection>? SectionChanged;
    private bool _sectionInitialized;

    private TopResourceBar? _topResourceBar;
    private NavigationRail? _navigationRail;
    private OverviewPanel? _overviewPanel;
    private MapPanel? _mapPanel;

    public TopResourceBar? TopResourceBar => _topResourceBar;
    public OverviewPanel? OverviewPanel => _overviewPanel;
    public MapPanel? MapPanel => _mapPanel;
    public PeopleView? PeopleView => _overviewPanel?.PeopleView;
    public SelectedResourcePanel? SelectedResourcePanel => _overviewPanel?.SelectionView?.SelectedPanel;
    public QueuePanel? QueuePanel => _overviewPanel?.QueuePanel;
    public TownUI? TownUI => _overviewPanel?.TownUI;
    public Label? HintLabel => _mapPanel?.MapToolbar?.HintLabel;
    public Label? CoordsLabel => _mapPanel?.MapToolbar?.CoordsLabel;
    public Label? StatusLabel => _mapPanel?.MapToolbar?.StatusLabel;
    public HudSection CurrentSection { get; private set; } = HudSection.Selection;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _topResourceBar = GetNode<TopResourceBar>("RootMargin/RootColumn/TopResourceBar");
        _navigationRail = GetNode<NavigationRail>("RootMargin/RootColumn/MainRow/NavigationRail");
        _overviewPanel = GetNode<OverviewPanel>("RootMargin/RootColumn/MainRow/OverviewPanel");
        _mapPanel = GetNode<MapPanel>("RootMargin/RootColumn/MainRow/MapPanel");

        _navigationRail.SectionSelected += ShowSection;

        _overviewPanel?.SelectionView?.ShowEmptyState("No selection.", "Select a tile, resource, or villager.");
        ShowSection(HudSection.Selection);
        ApplyMousePassThrough();
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
        _overviewPanel?.ShowContext(section);
        SectionChanged?.Invoke(section);
    }

    private void ApplyMousePassThrough()
    {
        SetMouseFilterForPath("RootMargin", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MainRow", MouseFilterEnum.Ignore);
        SetMouseFilterForPath("RootMargin/RootColumn/MainRow/MapPanel", MouseFilterEnum.Ignore);
    }

    private void SetMouseFilterForPath(string path, MouseFilterEnum mouseFilter)
    {
        if (GetNodeOrNull<Control>(path) is { } control)
        {
            control.MouseFilter = mouseFilter;
        }
    }
}
