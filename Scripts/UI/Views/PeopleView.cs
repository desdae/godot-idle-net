using System.Collections.Generic;
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
    public string Title { get; init; } = "Wayfarer";
    public string Summary { get; init; } = string.Empty;
    public string Footer { get; init; } = string.Empty;
    public IReadOnlyList<CharacterSkillViewData> Skills { get; init; } = new List<CharacterSkillViewData>();
}

public partial class PeopleView : PanelContainer
{
    private Label? _titleLabel;
    private Label? _summaryLabel;
    private Label? _footerLabel;
    private VBoxContainer? _skillsList;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/TitleLabel");
        _summaryLabel = GetNode<Label>("OuterMargin/RootColumn/SummaryPanel/SummaryMargin/SummaryLabel");
        _footerLabel = GetNode<Label>("OuterMargin/RootColumn/FooterPanel/FooterMargin/FooterLabel");
        _skillsList = GetNode<VBoxContainer>("OuterMargin/RootColumn/SkillsPanel/SkillsMargin/SkillsColumn/SkillsScroll/SkillsList");

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateFrameStyle(new Color(0.58f, 0.74f, 0.92f)));
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.80f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 21);
        _summaryLabel.AddThemeColorOverride("font_color", new Color(0.90f, 0.85f, 0.77f));
        _summaryLabel.AddThemeFontSizeOverride("font_size", 13);
        _footerLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.78f, 0.70f));
        _footerLabel.AddThemeFontSizeOverride("font_size", 12);
    }

    public void SetData(PeopleViewData data)
    {
        if (_titleLabel is null || _summaryLabel is null || _footerLabel is null || _skillsList is null)
        {
            return;
        }

        _titleLabel.Text = data.Title;
        _summaryLabel.Text = data.Summary;
        _footerLabel.Text = data.Footer;

        foreach (Node child in _skillsList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (CharacterSkillViewData skill in data.Skills)
        {
            _skillsList.AddChild(CreateSkillRow(skill));
        }
    }

    private static Control CreateSkillRow(CharacterSkillViewData skill)
    {
        PanelContainer row = new()
        {
            CustomMinimumSize = new Vector2(0.0f, 42.0f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.22f, 0.15f, 0.10f, 0.86f),
            new Color(0.54f, 0.41f, 0.23f, 0.68f),
            12,
            8,
            6));

        MarginContainer margin = new();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 4);

        VBoxContainer root = new();
        root.AddThemeConstantOverride("separation", 2);

        HBoxContainer top = new();
        top.AddThemeConstantOverride("separation", 6);

        Label icon = new()
        {
            Text = skill.IconGlyph,
            CustomMinimumSize = new Vector2(16.0f, 16.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        icon.AddThemeColorOverride("font_color", skill.IconColor);

        Label name = new()
        {
            Text = skill.Name,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        name.AddThemeColorOverride("font_color", new Color(0.96f, 0.91f, 0.80f));
        name.AddThemeFontSizeOverride("font_size", 13);

        Label level = new()
        {
            Text = $"Lv.{skill.Level}",
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize = new Vector2(58.0f, 0.0f),
        };
        level.AddThemeColorOverride("font_color", new Color(0.91f, 0.85f, 0.74f));
        level.AddThemeFontSizeOverride("font_size", 12);

        Label xp = new()
        {
            Text = $"{skill.CurrentXp}/{skill.RequiredXp}",
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize = new Vector2(52.0f, 0.0f),
        };
        xp.AddThemeColorOverride("font_color", new Color(0.80f, 0.84f, 0.80f));
        xp.AddThemeFontSizeOverride("font_size", 11);

        ProgressBar progress = new()
        {
            MinValue = 0,
            MaxValue = skill.RequiredXp,
            Value = skill.CurrentXp,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0.0f, 5.0f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        progress.AddThemeStyleboxOverride("background", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.17f, 0.12f, 0.09f, 0.92f),
            new Color(0.41f, 0.31f, 0.18f, 0.62f),
            6,
            2,
            2));
        progress.AddThemeStyleboxOverride("fill", SelectionPanelStyles.CreateInsetStyle(
            skill.IconColor.Darkened(0.15f),
            skill.IconColor.Lightened(0.20f),
            6,
            2,
            1));

        top.AddChild(icon);
        top.AddChild(name);
        top.AddChild(level);
        top.AddChild(xp);
        root.AddChild(top);
        root.AddChild(progress);
        margin.AddChild(root);
        row.AddChild(margin);
        return row;
    }
}
