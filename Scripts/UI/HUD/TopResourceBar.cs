using System.Collections.Generic;
using Godot;

namespace IdleNet;

public sealed class ResourceStatViewData
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public Color AccentColor { get; init; } = new(0.80f, 0.67f, 0.40f);
}

public partial class TopResourceBar : PanelContainer
{
    private HBoxContainer? _statsRow;
    private readonly Dictionary<string, PanelContainer> _statCards = new();
    private readonly Dictionary<string, Label> _valueLabels = new();

    public override void _Ready()
    {
        _statsRow = GetNode<HBoxContainer>("OuterMargin/StatsRow");

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.12f, 0.09f, 0.06f, 0.96f),
            new Color(0.52f, 0.40f, 0.22f, 0.90f),
            20,
            10,
            8));
    }

    public void SetData(IReadOnlyList<ResourceStatViewData> stats)
    {
        if (_statsRow is null)
        {
            return;
        }

        HashSet<string> activeIds = new();
        foreach (ResourceStatViewData stat in stats)
        {
            activeIds.Add(stat.Id);
            PanelContainer card = EnsureCard(stat.Id);
            card.Visible = true;
            Label valueLabel = _valueLabels[stat.Id];
            Label labelLabel = card.GetNode<Label>("CardMargin/CardColumn/Label");
            valueLabel.Text = stat.Value;
            labelLabel.Text = stat.Label;

            card.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
                new Color(0.21f, 0.15f, 0.10f, 0.88f),
                stat.AccentColor.Lerp(new Color(0.94f, 0.83f, 0.58f, 0.86f), 0.38f),
                12,
                8,
                6));
            valueLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.80f));
            labelLabel.AddThemeColorOverride("font_color", stat.AccentColor.Lightened(0.25f));
        }

        foreach (KeyValuePair<string, PanelContainer> entry in _statCards)
        {
            entry.Value.Visible = activeIds.Contains(entry.Key);
        }
    }

    private PanelContainer EnsureCard(string statId)
    {
        if (_statsRow is null)
        {
            return new PanelContainer();
        }

        if (_statCards.TryGetValue(statId, out PanelContainer? existingCard))
        {
            return existingCard;
        }

        PanelContainer card = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(78.0f, 40.0f),
        };

        MarginContainer margin = new() { Name = "CardMargin" };
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 6);

        VBoxContainer column = new() { Name = "CardColumn" };
        column.AddThemeConstantOverride("separation", 0);

        Label valueLabel = new() { Name = "Value" };
        valueLabel.AddThemeFontSizeOverride("font_size", 15);

        Label labelLabel = new() { Name = "Label" };
        labelLabel.AddThemeFontSizeOverride("font_size", 10);
        labelLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        labelLabel.ClipText = true;

        column.AddChild(valueLabel);
        column.AddChild(labelLabel);
        margin.AddChild(column);
        card.AddChild(margin);
        _statsRow.AddChild(card);

        _statCards[statId] = card;
        _valueLabels[statId] = valueLabel;
        return card;
    }
}
