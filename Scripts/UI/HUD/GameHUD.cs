using System;
using Godot;

namespace IdleNet;

public partial class GameHUD : Control
{
    private const float BaseHudWidth = 1280.0f;
    private const float BaseHudHeight = 720.0f;

    public event Action<HudSection>? SectionChanged;
    private bool _sectionInitialized;

    private TopResourceBar? _topResourceBar;
    private NavigationRail? _navigationRail;
    private OverviewPanel? _overviewPanel;
    private MapPanel? _mapPanel;
    private HBoxContainer? _mainRow;
    private float _uiScale = 1.0f;

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
        _navigationRail = GetNode<NavigationRail>("RootMargin/RootColumn/NavigationRail");
        _mainRow = GetNode<HBoxContainer>("RootMargin/RootColumn/MainRow");
        _overviewPanel = GetNode<OverviewPanel>("RootMargin/RootColumn/MainRow/OverviewPanel");
        _mapPanel = GetNode<MapPanel>("RootMargin/RootColumn/MainRow/MapPanel");

        _navigationRail.SectionSelected += ShowSection;
        Resized += UpdateResponsiveLayout;

        _overviewPanel?.SelectionView?.ShowEmptyState("No selection.", "Select a tile, resource, or villager.");
        ShowSection(HudSection.Selection);
        UpdateResponsiveLayout();
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
        UpdateResponsiveLayout();
        SectionChanged?.Invoke(section);
    }

    private void UpdateResponsiveLayout()
    {
        if (_overviewPanel is null || _mapPanel is null || _mainRow is null)
        {
            return;
        }

        float width = Size.X > 0.0f ? Size.X : GetViewportRect().Size.X;
        float height = Size.Y > 0.0f ? Size.Y : GetViewportRect().Size.Y;
        float scale = Mathf.Clamp(Mathf.Min(width / BaseHudWidth, height / BaseHudHeight), 0.74f, 1.0f);
        ApplyUiScale(scale);

        float desiredOverviewWidth = Mathf.Clamp(width * 0.34f, 320.0f, 460.0f);
        if (CurrentSection is HudSection.Town or HudSection.Buildings)
        {
            desiredOverviewWidth = Mathf.Clamp(width * 0.37f, 360.0f, 500.0f);
        }

        _overviewPanel.CustomMinimumSize = new Vector2(desiredOverviewWidth, 0.0f);
        _mainRow.AddThemeConstantOverride("separation", width < 1180.0f ? 14 : 18);
        _mapPanel.CustomMinimumSize = width < 980.0f ? new Vector2(0.0f, 360.0f) : Vector2.Zero;
    }

    private void ApplyUiScale(float scale)
    {
        if (Mathf.IsEqualApprox(_uiScale, scale))
        {
            return;
        }

        _uiScale = scale;
        Theme? theme = GetTheme();
        if (theme is not null)
        {
            theme.DefaultBaseScale = scale;
        }
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
