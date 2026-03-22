using System;
using Godot;

namespace IdleNet;

public partial class LeftSidebar : Control
{
    public event Action? QueueToggleRequested;

    private MarginContainer? _outerMargin;
    private VBoxContainer? _rootColumn;
    private Button? _queueToggleButton;
    private SelectedResourcePanel? _selectedResourcePanel;
    private QueuePanel? _queuePanel;

    public SelectedResourcePanel? SelectedResourcePanel => _selectedResourcePanel;

    public QueuePanel? QueuePanel => _queuePanel;

    public bool IsQueueVisible => _queuePanel?.Visible == true;

    public override void _Ready()
    {
        _outerMargin = GetNode<MarginContainer>("OuterMargin");
        _rootColumn = GetNode<VBoxContainer>("OuterMargin/RootColumn");
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Pass;
        _queueToggleButton = GetNode<Button>("OuterMargin/RootColumn/TopRow/QueueToggleButton");
        _selectedResourcePanel = GetNode<SelectedResourcePanel>("OuterMargin/RootColumn/SelectedResourcePanel");
        _queuePanel = GetNode<QueuePanel>("OuterMargin/RootColumn/QueuePanel");

        _outerMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _rootColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rootColumn.SizeFlagsVertical = SizeFlags.ExpandFill;

        _queueToggleButton.Pressed += () => QueueToggleRequested?.Invoke();
        SelectionPanelStyles.ApplyActionButtonStyle(_queueToggleButton, new Color(0.63f, 0.50f, 0.30f), false, new Vector2(112.0f, 30.0f), 12);
        SetQueueVisible(false);
    }

    public void SetQueueVisible(bool visible)
    {
        if (_queuePanel is null || _queueToggleButton is null)
        {
            return;
        }

        _queuePanel.Visible = visible;
        _queueToggleButton.Text = visible ? "Hide Queue" : "Show Queue";
    }
}
