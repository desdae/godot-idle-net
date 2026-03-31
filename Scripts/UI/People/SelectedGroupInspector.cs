using System;
using System.Collections.Generic;
using Godot;

namespace IdleNet;

public partial class SelectedGroupInspector : PanelContainer
{
    public event Action<string>? ActionRequested;

    private Label? _titleLabel;
    private Label? _summaryLabel;
    private Label? _rolesLabel;
    private Label? _assignmentsLabel;
    private HFlowContainer? _actionFlow;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("OuterMargin/RootColumn/TitleLabel");
        _summaryLabel = GetNode<Label>("OuterMargin/RootColumn/SummaryLabel");
        _rolesLabel = GetNode<Label>("OuterMargin/RootColumn/DetailsGrid/RolesValue");
        _assignmentsLabel = GetNode<Label>("OuterMargin/RootColumn/DetailsGrid/AssignmentsValue");
        _actionFlow = GetNode<HFlowContainer>("OuterMargin/RootColumn/ActionFlow");

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.18f, 0.13f, 0.09f, 0.94f),
            new Color(0.58f, 0.44f, 0.24f, 0.76f),
            18,
            8,
            8));

        _titleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        _summaryLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.88f, 0.80f));
        _summaryLabel.AddThemeFontSizeOverride("font_size", 12);
        _rolesLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.90f, 0.78f));
        _assignmentsLabel.AddThemeColorOverride("font_color", new Color(0.89f, 0.85f, 0.77f));

        RegisterButton("chop", "OuterMargin/RootColumn/ActionFlow/ChopButton", new Color(0.53f, 0.78f, 0.45f));
        RegisterButton("mine", "OuterMargin/RootColumn/ActionFlow/MineButton", new Color(0.72f, 0.76f, 0.82f));
        RegisterButton("build", "OuterMargin/RootColumn/ActionFlow/BuildButton", new Color(0.82f, 0.64f, 0.41f));
        RegisterButton("clear", "OuterMargin/RootColumn/ActionFlow/ClearButton", new Color(0.63f, 0.50f, 0.31f));
    }

    public void SetData(string summary, string roles, string assignments, bool hasSelection)
    {
        if (_summaryLabel is null || _rolesLabel is null || _assignmentsLabel is null || _actionFlow is null)
        {
            return;
        }

        _summaryLabel.Text = summary;
        _rolesLabel.Text = roles;
        _assignmentsLabel.Text = assignments;
        _actionFlow.Visible = hasSelection;

        foreach (Button button in _actionFlow.GetChildren())
        {
            button.Disabled = !hasSelection;
        }
    }

    private void RegisterButton(string actionId, string path, Color accent)
    {
        Button button = GetNode<Button>(path);
        button.Pressed += () => ActionRequested?.Invoke(actionId);
        SelectionPanelStyles.ApplyActionButtonStyle(button, accent, false, new Vector2(68.0f, 28.0f), 11);
    }
}
