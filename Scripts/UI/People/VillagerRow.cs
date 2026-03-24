using System;
using Godot;

namespace IdleNet;

public sealed class VillagerRowViewData
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string CurrentTask { get; init; } = string.Empty;
    public string LoadText { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public bool Selected { get; init; }
}

public partial class VillagerRow : PanelContainer
{
    public event Action<string, bool, bool, bool>? RowInvoked;

    private string _villagerId = string.Empty;
    private Button? _selectButton;
    private Label? _nameLabel;
    private Label? _roleLabel;
    private Label? _taskLabel;
    private Label? _loadLabel;
    private Label? _statusLabel;

    public override void _Ready()
    {
        _selectButton = GetNode<Button>("RowMargin/Row/SelectButton");
        _nameLabel = GetNode<Label>("RowMargin/Row/NameLabel");
        _roleLabel = GetNode<Label>("RowMargin/Row/RoleLabel");
        _taskLabel = GetNode<Label>("RowMargin/Row/TaskLabel");
        _loadLabel = GetNode<Label>("RowMargin/Row/LoadLabel");
        _statusLabel = GetNode<Label>("RowMargin/Row/StatusLabel");

        MouseFilter = MouseFilterEnum.Stop;
        _selectButton.Pressed += OnSelectPressed;
    }

    public void SetData(VillagerRowViewData data)
    {
        if (_selectButton is null || _nameLabel is null || _roleLabel is null || _taskLabel is null || _loadLabel is null || _statusLabel is null)
        {
            return;
        }

        _villagerId = data.Id;
        _selectButton.Text = data.Selected ? "[x]" : "[ ]";
        _nameLabel.Text = data.Name;
        _roleLabel.Text = data.Role;
        _taskLabel.Text = data.CurrentTask;
        _loadLabel.Text = data.LoadText;
        _statusLabel.Text = data.StatusText;
        TooltipText = data.Summary;

        Color border = data.Selected ? new Color(0.92f, 0.79f, 0.50f, 0.92f) : new Color(0.52f, 0.40f, 0.22f, 0.64f);
        Color background = data.Selected ? new Color(0.34f, 0.24f, 0.14f, 0.94f) : new Color(0.22f, 0.16f, 0.10f, 0.88f);
        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(background, border, 14, 8, 6));
        Modulate = data.Selected ? Colors.White : new Color(0.98f, 0.98f, 0.98f, 0.96f);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton ||
            mouseButton.ButtonIndex != MouseButton.Left ||
            !mouseButton.Pressed)
        {
            return;
        }

        bool additive = mouseButton.CtrlPressed || mouseButton.MetaPressed;
        bool range = mouseButton.ShiftPressed;
        RowInvoked?.Invoke(_villagerId, additive, range, false);
    }

    private void OnSelectPressed()
    {
        RowInvoked?.Invoke(_villagerId, true, false, true);
    }
}
