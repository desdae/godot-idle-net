using System;
using System.Collections.Generic;
using Godot;

namespace IdleNet;

public partial class BulkActionBar : PanelContainer
{
    public event Action<string>? ActionRequested;

    private Label? _summaryLabel;
    private Label? _detailLabel;
    private readonly Dictionary<string, Button> _buttons = new();

    public override void _Ready()
    {
        _summaryLabel = GetNode<Label>("OuterMargin/RootColumn/SummaryLabel");
        _detailLabel = GetNode<Label>("OuterMargin/RootColumn/DetailLabel");

        RegisterButton("chop", "OuterMargin/RootColumn/ActionsFlow/ChopButton", new Color(0.53f, 0.78f, 0.45f));
        RegisterButton("mine", "OuterMargin/RootColumn/ActionsFlow/MineButton", new Color(0.72f, 0.76f, 0.82f));
        RegisterButton("gather", "OuterMargin/RootColumn/ActionsFlow/GatherButton", new Color(0.86f, 0.53f, 0.69f));
        RegisterButton("build", "OuterMargin/RootColumn/ActionsFlow/BuildButton", new Color(0.82f, 0.63f, 0.40f));
        RegisterButton("prioritize", "OuterMargin/RootColumn/ActionsFlow/PrioritizeButton", new Color(0.93f, 0.77f, 0.43f));
        RegisterButton("clear", "OuterMargin/RootColumn/ActionsFlow/ClearButton", new Color(0.63f, 0.50f, 0.31f));

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.12f, 0.09f, 0.06f, 0.96f),
            new Color(0.52f, 0.40f, 0.22f, 0.90f),
            18,
            10,
            8));
        _summaryLabel?.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        _summaryLabel?.AddThemeFontSizeOverride("font_size", 14);
        _detailLabel?.AddThemeColorOverride("font_color", new Color(0.83f, 0.80f, 0.73f));
        _detailLabel?.AddThemeFontSizeOverride("font_size", 11);

        Visible = false;
    }

    public void SetData(bool visible, string summary, string detail)
    {
        Visible = visible;
        if (_summaryLabel is not null)
        {
            _summaryLabel.Text = summary;
        }

        if (_detailLabel is not null)
        {
            _detailLabel.Text = detail;
        }
    }

    private void RegisterButton(string actionId, string path, Color accent)
    {
        Button button = GetNode<Button>(path);
        button.Pressed += () => ActionRequested?.Invoke(actionId);
        SelectionPanelStyles.ApplyActionButtonStyle(button, accent, false, new Vector2(72.0f, 28.0f), 11);
        _buttons[actionId] = button;
    }
}
