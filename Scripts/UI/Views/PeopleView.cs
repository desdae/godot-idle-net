using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace IdleNet;

public sealed class CharacterSkillViewData
{
    public string IconGlyph { get; init; } = string.Empty;
    public Color IconColor { get; init; } = Colors.White;
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
    public int CurrentXp { get; init; }
    public int RequiredXp { get; init; }
}

public sealed class PeopleViewData
{
    public string Title { get; init; } = "Villager Management";
    public string Summary { get; init; } = string.Empty;
    public string VillagerSummary { get; init; } = string.Empty;
    public string RecruitCostText { get; init; } = string.Empty;
    public string RecruitHintText { get; init; } = string.Empty;
    public string RecruitButtonText { get; init; } = "Recruit Villager";
    public bool CanRecruit { get; init; }
    public string Footer { get; init; } = string.Empty;
    public IReadOnlyList<CharacterSkillViewData> Skills { get; init; } = Array.Empty<CharacterSkillViewData>();
    public IReadOnlyList<VillagerRowViewData> Villagers { get; init; } = Array.Empty<VillagerRowViewData>();
}

public partial class PeopleView : PanelContainer
{
    public event Action? RecruitRequested;
    public event Action<string, IReadOnlyList<string>>? BulkActionRequested;
    public event Action<int, string, string>? SelectionChanged;

    [Export]
    public PackedScene? VillagerRowScene { get; set; }

    [Export]
    public PackedScene? SelectedGroupInspectorScene { get; set; }

    private enum PeopleTab
    {
        Roster,
        Tasks,
        Groups,
    }

    private Label? _titleLabel;
    private Label? _summaryLabel;
    private Label? _villagerSummaryLabel;
    private Label? _recruitCostLabel;
    private Label? _recruitHintLabel;
    private Button? _recruitButton;
    private Button? _rosterTabButton;
    private Button? _tasksTabButton;
    private Button? _groupsTabButton;
    private HBoxContainer? _searchRow;
    private LineEdit? _searchField;
    private Button? _selectAllButton;
    private Button? _clearSelectionButton;
    private VillagerFilterBar? _filterBar;
    private Control? _rosterTab;
    private VBoxContainer? _rosterList;
    private ScrollContainer? _rosterScroll;
    private PanelContainer? _emptyRosterPanel;
    private Control? _tasksTab;
    private VBoxContainer? _tasksList;
    private Control? _groupsTab;
    private VBoxContainer? _groupsList;
    private VBoxContainer? _skillsList;
    private Label? _footerLabel;
    private SelectedGroupInspector? _selectedGroupInspector;
    private Control? _inspectorHost;

    private IReadOnlyList<VillagerRowViewData> _villagers = Array.Empty<VillagerRowViewData>();
    private IReadOnlyList<CharacterSkillViewData> _skills = Array.Empty<CharacterSkillViewData>();
    private readonly HashSet<string> _selectedVillagerIds = new();
    private string? _selectionAnchorId;
    private string _activeFilter = "all";
    private PeopleTab _activeTab = PeopleTab.Roster;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn/HeaderRow/TitleLabel");
        _summaryLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn/SummaryLabel");
        _villagerSummaryLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn/MetaRow/VillagerSummaryLabel");
        _recruitCostLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn/MetaRow/RecruitCostLabel");
        _recruitHintLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn/RecruitHintLabel");
        _recruitButton = GetNode<Button>("OuterMargin/RootColumn/HeaderPanel/HeaderMargin/HeaderColumn/HeaderRow/RecruitButton");
        _rosterTabButton = GetNode<Button>("OuterMargin/RootColumn/TabsRow/RosterTabButton");
        _tasksTabButton = GetNode<Button>("OuterMargin/RootColumn/TabsRow/TasksTabButton");
        _groupsTabButton = GetNode<Button>("OuterMargin/RootColumn/TabsRow/GroupsTabButton");
        _searchRow = GetNode<HBoxContainer>("OuterMargin/RootColumn/SearchRow");
        _searchField = GetNode<LineEdit>("OuterMargin/RootColumn/SearchRow/SearchField");
        _selectAllButton = GetNode<Button>("OuterMargin/RootColumn/SearchRow/SelectAllButton");
        _clearSelectionButton = GetNode<Button>("OuterMargin/RootColumn/SearchRow/ClearSelectionButton");
        _filterBar = GetNode<VillagerFilterBar>("OuterMargin/RootColumn/FilterBar");
        _rosterTab = GetNode<Control>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab");
        _rosterScroll = GetNode<ScrollContainer>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/RosterScroll");
        _rosterList = GetNode<VBoxContainer>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/RosterScroll/RosterList");
        _emptyRosterPanel = GetNode<PanelContainer>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/EmptyStatePanel");
        _tasksTab = GetNode<Control>("OuterMargin/RootColumn/ContentFrame/ContentHost/TasksTab");
        _tasksList = GetNode<VBoxContainer>("OuterMargin/RootColumn/ContentFrame/ContentHost/TasksTab/TasksScroll/TasksColumn/TaskSummaryList");
        _skillsList = GetNode<VBoxContainer>("OuterMargin/RootColumn/ContentFrame/ContentHost/TasksTab/TasksScroll/TasksColumn/SkillsPanel/SkillsMargin/SkillsColumn/SkillsList");
        _groupsTab = GetNode<Control>("OuterMargin/RootColumn/ContentFrame/ContentHost/GroupsTab");
        _groupsList = GetNode<VBoxContainer>("OuterMargin/RootColumn/ContentFrame/ContentHost/GroupsTab/GroupsScroll/GroupsColumn/GroupsList");
        _footerLabel = GetNode<Label>("OuterMargin/RootColumn/FooterPanel/FooterMargin/FooterLabel");
        _inspectorHost = GetNode<Control>("OuterMargin/RootColumn/InspectorHost");

        if (SelectedGroupInspectorScene is not null)
        {
            _selectedGroupInspector = SelectedGroupInspectorScene.Instantiate<SelectedGroupInspector>();
            _inspectorHost.AddChild(_selectedGroupInspector);
            _selectedGroupInspector.SetAnchorsPreset(LayoutPreset.FullRect);
            _selectedGroupInspector.OffsetLeft = 0.0f;
            _selectedGroupInspector.OffsetTop = 0.0f;
            _selectedGroupInspector.OffsetRight = 0.0f;
            _selectedGroupInspector.OffsetBottom = 0.0f;
            _selectedGroupInspector.ActionRequested += RequestBulkAction;
        }

        AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        GetNode<PanelContainer>("OuterMargin/RootColumn/HeaderPanel").AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.23f, 0.17f, 0.11f, 0.90f),
            new Color(0.62f, 0.47f, 0.27f, 0.76f),
            18,
            10,
            8));
        GetNode<PanelContainer>("OuterMargin/RootColumn/ContentFrame").AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.17f, 0.13f, 0.09f, 0.90f),
            new Color(0.52f, 0.40f, 0.23f, 0.70f),
            18,
            8,
            8));
        _emptyRosterPanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.20f, 0.15f, 0.10f, 0.88f),
            new Color(0.57f, 0.43f, 0.24f, 0.68f),
            16,
            10,
            10));
        GetNode<PanelContainer>("OuterMargin/RootColumn/FooterPanel").AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.19f, 0.14f, 0.09f, 0.90f),
            new Color(0.49f, 0.38f, 0.22f, 0.68f),
            14,
            8,
            6));

        _titleLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.80f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 20);
        _summaryLabel.AddThemeColorOverride("font_color", new Color(0.90f, 0.85f, 0.77f));
        _summaryLabel.AddThemeFontSizeOverride("font_size", 11);
        _summaryLabel.MaxLinesVisible = 1;
        _summaryLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _villagerSummaryLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.79f));
        _villagerSummaryLabel.AddThemeFontSizeOverride("font_size", 12);
        _recruitCostLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.67f));
        _recruitCostLabel.AddThemeFontSizeOverride("font_size", 12);
        _recruitHintLabel.AddThemeColorOverride("font_color", new Color(0.84f, 0.80f, 0.73f));
        _recruitHintLabel.AddThemeFontSizeOverride("font_size", 11);
        _recruitHintLabel.MaxLinesVisible = 1;
        _recruitHintLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _footerLabel.AddThemeColorOverride("font_color", new Color(0.84f, 0.80f, 0.74f));
        _footerLabel.AddThemeFontSizeOverride("font_size", 11);
        _footerLabel.MaxLinesVisible = 1;
        _footerLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        GetNode<Label>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/EmptyStatePanel/EmptyStateMargin/EmptyStateColumn/EmptyStateTitle")
            .AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        GetNode<Label>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/EmptyStatePanel/EmptyStateMargin/EmptyStateColumn/EmptyStateTitle")
            .AddThemeFontSizeOverride("font_size", 15);
        GetNode<Label>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/EmptyStatePanel/EmptyStateMargin/EmptyStateColumn/EmptyStateBody")
            .AddThemeColorOverride("font_color", new Color(0.88f, 0.84f, 0.76f));
        GetNode<Label>("OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/EmptyStatePanel/EmptyStateMargin/EmptyStateColumn/EmptyStateBody")
            .AddThemeFontSizeOverride("font_size", 12);

        SelectionPanelStyles.ApplyActionButtonStyle(_recruitButton, new Color(0.82f, 0.69f, 0.42f), true, new Vector2(144.0f, 34.0f), 12);
        SelectionPanelStyles.ApplyActionButtonStyle(_selectAllButton, new Color(0.70f, 0.58f, 0.34f), false, new Vector2(84.0f, 30.0f), 11);
        SelectionPanelStyles.ApplyActionButtonStyle(_clearSelectionButton, new Color(0.63f, 0.50f, 0.31f), false, new Vector2(88.0f, 30.0f), 11);

        _recruitButton.Pressed += () => RecruitRequested?.Invoke();
        _rosterTabButton.Pressed += () => SetTab(PeopleTab.Roster);
        _tasksTabButton.Pressed += () => SetTab(PeopleTab.Tasks);
        _groupsTabButton.Pressed += () => SetTab(PeopleTab.Groups);
        _searchField.TextChanged += _ => RefreshContent();
        _selectAllButton.Pressed += SelectAllVisibleVillagers;
        _clearSelectionButton.Pressed += ClearSelection;
        _filterBar.FilterChanged += filterId =>
        {
            _activeFilter = filterId;
            _filterBar.SetActiveFilter(filterId);
            RefreshContent();
        };

        StyleColumnHeaders();
        SetTab(PeopleTab.Roster);
    }

    public void SetData(PeopleViewData data)
    {
        if (_titleLabel is null || _summaryLabel is null || _villagerSummaryLabel is null || _recruitCostLabel is null ||
            _recruitHintLabel is null || _recruitButton is null || _footerLabel is null)
        {
            return;
        }

        _titleLabel.Text = data.Title;
        _summaryLabel.Text = data.Summary;
        _villagerSummaryLabel.Text = data.VillagerSummary;
        _recruitCostLabel.Text = data.RecruitCostText;
        _recruitHintLabel.Text = data.RecruitHintText;
        _recruitButton.Text = data.RecruitButtonText;
        _recruitButton.Disabled = !data.CanRecruit;
        _footerLabel.Text = data.Footer;

        _villagers = data.Villagers;
        _skills = data.Skills;

        _selectedVillagerIds.RemoveWhere(id => _villagers.All(v => v.Id != id));
        if (_selectionAnchorId is not null && _villagers.All(v => v.Id != _selectionAnchorId))
        {
            _selectionAnchorId = null;
        }

        RefreshContent();
    }

    public void RequestBulkAction(string actionId)
    {
        List<string> selectedIds = GetSelectedVillagerIds();
        if (selectedIds.Count == 0)
        {
            return;
        }

        BulkActionRequested?.Invoke(actionId, selectedIds);
    }

    private void RefreshContent()
    {
        if (_rosterTab is null || _tasksTab is null || _groupsTab is null || _rosterList is null || _tasksList is null || _groupsList is null || _skillsList is null || _rosterScroll is null || _emptyRosterPanel is null)
        {
            return;
        }

        IReadOnlyList<VillagerRowViewData> visibleVillagers = GetVisibleVillagers();
        _rosterTab.Visible = _activeTab == PeopleTab.Roster;
        _tasksTab.Visible = _activeTab == PeopleTab.Tasks;
        _groupsTab.Visible = _activeTab == PeopleTab.Groups;
        bool showRosterTools = _activeTab == PeopleTab.Roster;
        if (_searchRow is not null)
        {
            _searchRow.Visible = showRosterTools;
        }

        if (_filterBar is not null)
        {
            _filterBar.Visible = showRosterTools;
        }

        foreach (Node child in _rosterList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (VillagerRowViewData villager in visibleVillagers)
        {
            VillagerRow row = VillagerRowScene is null
                ? new VillagerRow()
                : VillagerRowScene.Instantiate<VillagerRow>();
            _rosterList.AddChild(row);
            row.SetData(new VillagerRowViewData
            {
                Id = villager.Id,
                Name = villager.Name,
                Role = villager.Role,
                CurrentTask = villager.CurrentTask,
                LoadText = villager.LoadText,
                StatusText = villager.StatusText,
                Summary = villager.Summary,
                Selected = _selectedVillagerIds.Contains(villager.Id),
            });
            row.RowInvoked += OnVillagerRowInvoked;
        }

        _rosterScroll.Visible = visibleVillagers.Count > 0;
        _emptyRosterPanel.Visible = visibleVillagers.Count == 0;

        RebuildTasksTab(visibleVillagers);
        RebuildGroupsTab(visibleVillagers);
        UpdateTabButtons();
        UpdateInspector();
    }

    private IReadOnlyList<VillagerRowViewData> GetVisibleVillagers()
    {
        IEnumerable<VillagerRowViewData> filtered = _villagers;
        string searchTerm = _searchField?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(searchTerm))
        {
            filtered = filtered.Where(v =>
                v.Name.ToLowerInvariant().Contains(searchTerm) ||
                v.Role.ToLowerInvariant().Contains(searchTerm) ||
                v.CurrentTask.ToLowerInvariant().Contains(searchTerm) ||
                v.StatusText.ToLowerInvariant().Contains(searchTerm));
        }

        filtered = _activeFilter switch
        {
            "idle" => filtered.Where(v => v.StatusText.Equals("Idle", StringComparison.OrdinalIgnoreCase)),
            "assigned" => filtered.Where(v => !v.StatusText.Equals("Idle", StringComparison.OrdinalIgnoreCase)),
            "builders" => filtered.Where(v => v.Role.Contains("Builder", StringComparison.OrdinalIgnoreCase)),
            "carrying" => filtered.Where(v => !v.LoadText.StartsWith("0/", StringComparison.Ordinal)),
            _ => filtered,
        };

        return filtered.ToList();
    }

    private void RebuildTasksTab(IReadOnlyList<VillagerRowViewData> visibleVillagers)
    {
        if (_tasksList is null || _skillsList is null)
        {
            return;
        }

        foreach (Node child in _tasksList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (Node child in _skillsList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (IGrouping<string, VillagerRowViewData> group in visibleVillagers.GroupBy(v => v.CurrentTask).OrderByDescending(g => g.Count()))
        {
            _tasksList.AddChild(CreateSummaryRow(group.Key, $"{group.Count()} villager(s)"));
        }

        if (_tasksList.GetChildCount() == 0)
        {
            _tasksList.AddChild(CreateSummaryRow("No active tasks", "No workers assigned"));
        }

        foreach (CharacterSkillViewData skill in _skills)
        {
            _skillsList.AddChild(CreateSummaryRow($"{skill.IconGlyph} {skill.Name}", $"Lv.{skill.Level}  XP {skill.CurrentXp}/{skill.RequiredXp}"));
        }
    }

    private void RebuildGroupsTab(IReadOnlyList<VillagerRowViewData> visibleVillagers)
    {
        if (_groupsList is null)
        {
            return;
        }

        foreach (Node child in _groupsList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (IGrouping<string, VillagerRowViewData> group in visibleVillagers.GroupBy(v => v.Role).OrderByDescending(g => g.Count()))
        {
            string assignments = string.Join(", ", group.Select(v => v.CurrentTask).Distinct().Take(3));
            _groupsList.AddChild(CreateSummaryRow(group.Key, $"{group.Count()} assigned  |  {assignments}"));
        }

        if (_groupsList.GetChildCount() == 0)
        {
            _groupsList.AddChild(CreateSummaryRow("No groups", "No workers available"));
        }
    }

    private void UpdateInspector()
    {
        if (_selectedGroupInspector is null || _inspectorHost is null)
        {
            return;
        }

        List<VillagerRowViewData> selectedVillagers = _villagers.Where(v => _selectedVillagerIds.Contains(v.Id)).ToList();
        if (selectedVillagers.Count == 0)
        {
            _inspectorHost.Visible = false;
            SelectionChanged?.Invoke(0, "No villagers selected", "Select one or more villagers to unlock bulk actions.");
            return;
        }

        string roles = selectedVillagers.Select(v => v.Role).Distinct().Count() == 1
            ? selectedVillagers[0].Role
            : "Mixed roles";
        string assignments = string.Join(", ", selectedVillagers.Select(v => v.CurrentTask).Distinct().Take(3));
        string summary = $"{selectedVillagers.Count} villager(s) selected";

        _inspectorHost.Visible = true;
        _selectedGroupInspector.SetData(summary, roles, assignments, true);
        SelectionChanged?.Invoke(selectedVillagers.Count, summary, $"{roles}  |  {assignments}");
    }

    private void SetTab(PeopleTab tab)
    {
        _activeTab = tab;
        RefreshContent();
    }

    private void UpdateTabButtons()
    {
        UpdateTabButton(_rosterTabButton, _activeTab == PeopleTab.Roster, new Color(0.56f, 0.74f, 0.92f));
        UpdateTabButton(_tasksTabButton, _activeTab == PeopleTab.Tasks, new Color(0.82f, 0.68f, 0.41f));
        UpdateTabButton(_groupsTabButton, _activeTab == PeopleTab.Groups, new Color(0.66f, 0.74f, 0.52f));
    }

    private static void UpdateTabButton(Button? button, bool active, Color accent)
    {
        if (button is null)
        {
            return;
        }

        SelectionPanelStyles.ApplyActionButtonStyle(button, accent, active, new Vector2(0.0f, 32.0f), 12);
    }

    private void SelectAllVisibleVillagers()
    {
        foreach (VillagerRowViewData villager in GetVisibleVillagers())
        {
            _selectedVillagerIds.Add(villager.Id);
        }

        RefreshContent();
    }

    private void ClearSelection()
    {
        _selectedVillagerIds.Clear();
        _selectionAnchorId = null;
        RefreshContent();
    }

    private void OnVillagerRowInvoked(string villagerId, bool additive, bool range, bool toggleOnly)
    {
        IReadOnlyList<VillagerRowViewData> visibleVillagers = GetVisibleVillagers();
        if (toggleOnly)
        {
            if (_selectedVillagerIds.Contains(villagerId))
            {
                _selectedVillagerIds.Remove(villagerId);
            }
            else
            {
                _selectedVillagerIds.Add(villagerId);
            }

            _selectionAnchorId = villagerId;
            RefreshContent();
            return;
        }

        if (range && _selectionAnchorId is not null)
        {
            int startIndex = visibleVillagers.ToList().FindIndex(v => v.Id == _selectionAnchorId);
            int endIndex = visibleVillagers.ToList().FindIndex(v => v.Id == villagerId);
            if (startIndex >= 0 && endIndex >= 0)
            {
                if (!additive)
                {
                    _selectedVillagerIds.Clear();
                }

                int lower = Math.Min(startIndex, endIndex);
                int upper = Math.Max(startIndex, endIndex);
                for (int index = lower; index <= upper; index++)
                {
                    _selectedVillagerIds.Add(visibleVillagers[index].Id);
                }

                RefreshContent();
                return;
            }
        }

        if (additive)
        {
            if (_selectedVillagerIds.Contains(villagerId))
            {
                _selectedVillagerIds.Remove(villagerId);
            }
            else
            {
                _selectedVillagerIds.Add(villagerId);
            }
        }
        else
        {
            _selectedVillagerIds.Clear();
            _selectedVillagerIds.Add(villagerId);
        }

        _selectionAnchorId = villagerId;
        RefreshContent();
    }

    private List<string> GetSelectedVillagerIds()
    {
        return _villagers.Where(v => _selectedVillagerIds.Contains(v.Id)).Select(v => v.Id).ToList();
    }

    private void StyleColumnHeaders()
    {
        string[] headerPaths =
        {
            "OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/HeaderRow/SelectHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/HeaderRow/NameHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/HeaderRow/RoleHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/HeaderRow/TaskHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/HeaderRow/LoadHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/RosterTab/HeaderRow/StatusHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/TasksTab/TasksScroll/TasksColumn/TaskSummaryHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/TasksTab/TasksScroll/TasksColumn/SkillsPanel/SkillsMargin/SkillsColumn/SkillsHeader",
            "OuterMargin/RootColumn/ContentFrame/ContentHost/GroupsTab/GroupsScroll/GroupsColumn/GroupsHeader",
        };

        foreach (string path in headerPaths)
        {
            if (GetNodeOrNull<Label>(path) is not { } label)
            {
                continue;
            }

            label.AddThemeColorOverride("font_color", new Color(0.95f, 0.87f, 0.69f));
            label.AddThemeFontSizeOverride("font_size", 12);
        }
    }

    private static Control CreateSummaryRow(string title, string value)
    {
        PanelContainer row = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.22f, 0.16f, 0.10f, 0.88f),
            new Color(0.56f, 0.43f, 0.24f, 0.68f),
            12,
            8,
            6));

        HBoxContainer content = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 8);

        Label titleLabel = new()
        {
            Text = title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        titleLabel.AddThemeFontSizeOverride("font_size", 12);

        Label valueLabel = new()
        {
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Right,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        valueLabel.AddThemeColorOverride("font_color", new Color(0.86f, 0.82f, 0.75f));
        valueLabel.AddThemeFontSizeOverride("font_size", 11);

        MarginContainer margin = new();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 4);

        content.AddChild(titleLabel);
        content.AddChild(valueLabel);
        margin.AddChild(content);
        row.AddChild(margin);
        return row;
    }
}
