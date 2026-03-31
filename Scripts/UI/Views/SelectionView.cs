using Godot;

namespace IdleNet;

public partial class SelectionView : Control
{
    private ScrollContainer? _selectedScroll;
    private PanelContainer? _emptyStatePanel;
    private Label? _emptyTitleLabel;
    private Label? _emptyBodyLabel;
    private SelectedResourcePanel? _selectedPanel;

    public SelectedResourcePanel? SelectedPanel => _selectedPanel;

    public override void _Ready()
    {
        _selectedScroll = GetNode<ScrollContainer>("RootColumn/SelectedScroll");
        _emptyStatePanel = GetNode<PanelContainer>("RootColumn/EmptyStatePanel");
        _emptyTitleLabel = GetNode<Label>("RootColumn/EmptyStatePanel/EmptyStateMargin/EmptyStateColumn/EmptyTitleLabel");
        _emptyBodyLabel = GetNode<Label>("RootColumn/EmptyStatePanel/EmptyStateMargin/EmptyStateColumn/EmptyBodyLabel");
        _selectedPanel = GetNode<SelectedResourcePanel>("RootColumn/SelectedScroll/SelectedResourcePanel");

        _emptyStatePanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.16f, 0.11f, 0.08f, 0.92f),
            new Color(0.49f, 0.38f, 0.22f, 0.82f),
            20,
            16,
            16));
        _emptyTitleLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.79f));
        _emptyTitleLabel.AddThemeFontSizeOverride("font_size", 21);
        _emptyBodyLabel.AddThemeColorOverride("font_color", new Color(0.87f, 0.82f, 0.74f));
        _emptyBodyLabel.AddThemeFontSizeOverride("font_size", 13);
    }

    public void ShowSelectionCard()
    {
        if (_selectedScroll is null || _selectedPanel is null || _emptyStatePanel is null)
        {
            return;
        }

        _emptyStatePanel.Visible = false;
        _selectedScroll.Visible = true;
        _selectedScroll.ScrollVertical = 0;
        _selectedPanel.Visible = true;
    }

    public void ShowEmptyState(string title, string body)
    {
        if (_selectedScroll is null || _selectedPanel is null || _emptyStatePanel is null || _emptyTitleLabel is null || _emptyBodyLabel is null)
        {
            return;
        }

        _selectedPanel.HidePanel();
        _selectedScroll.Visible = false;
        _emptyStatePanel.Visible = true;
        _emptyTitleLabel.Text = title;
        _emptyBodyLabel.Text = body;
    }
}
