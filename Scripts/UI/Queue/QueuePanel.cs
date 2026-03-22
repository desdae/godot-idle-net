using System.Collections.Generic;
using Godot;

namespace IdleNet;

public partial class QueuePanel : PanelContainer
{
    public event System.Action? ClearRequested;

    private Label? _titleLabel;
    private Label? _summaryLabel;
    private ScrollContainer? _scroll;
    private VBoxContainer? _entriesColumn;
    private VBoxContainer? _entriesList;
    private PanelContainer? _emptyPanel;
    private Label? _emptyLabel;
    private Button? _clearButton;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("OuterMargin/RootColumn/HeaderRow/TitleLabel");
        _summaryLabel = GetNode<Label>("OuterMargin/RootColumn/SummaryLabel");
        _scroll = GetNode<ScrollContainer>("OuterMargin/RootColumn/EntriesScroll");
        _entriesColumn = GetNode<VBoxContainer>("OuterMargin/RootColumn/EntriesScroll/EntriesColumn");
        _entriesList = GetNode<VBoxContainer>("OuterMargin/RootColumn/EntriesScroll/EntriesColumn/QueueEntriesList");
        _emptyPanel = GetNode<PanelContainer>("OuterMargin/RootColumn/EntriesScroll/EntriesColumn/EmptyPanel");
        _emptyLabel = GetNode<Label>("OuterMargin/RootColumn/EntriesScroll/EntriesColumn/EmptyPanel/EmptyMargin/EmptyLabel");
        _clearButton = GetNode<Button>("OuterMargin/RootColumn/FooterRow/ClearButton");

        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        _clearButton.Pressed += () => ClearRequested?.Invoke();

        ApplyStyles();
    }

    public void SetData(string summaryText, IReadOnlyList<string> entries, bool canClear)
    {
        if (_titleLabel is null || _summaryLabel is null || _entriesList is null || _emptyPanel is null || _emptyLabel is null || _clearButton is null)
        {
            if (!IsInsideTree())
            {
                return;
            }

            _Ready();
        }

        Label titleLabel = _titleLabel!;
        Label summaryLabel = _summaryLabel!;
        VBoxContainer entriesList = _entriesList!;
        PanelContainer emptyPanel = _emptyPanel!;
        Label emptyLabel = _emptyLabel!;
        Button clearButton = _clearButton!;

        titleLabel.Text = "Action Queue";
        summaryLabel.Text = summaryText;

        foreach (Node child in entriesList.GetChildren())
        {
            child.QueueFree();
        }

        bool hasEntries = entries.Count > 0;
        emptyPanel.Visible = !hasEntries;
        emptyLabel.Text = "No jobs queued. Select a tile to plan the next task.";

        if (hasEntries)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                string entry = entries[index];
                bool isCurrent = index == 0 && entry.StartsWith("Now:");
                PanelContainer row = CreateEntryRow(entry, isCurrent);
                entriesList.AddChild(row);
            }
        }

        clearButton.Disabled = !canClear;
    }

    private void ApplyStyles()
    {
        if (_titleLabel is null || _summaryLabel is null || _scroll is null || _entriesColumn is null || _emptyPanel is null || _emptyLabel is null || _clearButton is null)
        {
            return;
        }

        AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.16f, 0.11f, 0.08f, 0.95f),
            new Color(0.52f, 0.39f, 0.22f, 0.86f),
            18,
            8,
            8));

        _titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.88f, 0.74f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 17);

        _summaryLabel.AddThemeColorOverride("font_color", new Color(0.83f, 0.77f, 0.69f));
        _summaryLabel.AddThemeFontSizeOverride("font_size", 12);

        _entriesColumn.AddThemeConstantOverride("separation", 4);

        _emptyPanel.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            new Color(0.20f, 0.14f, 0.10f, 0.84f),
            new Color(0.42f, 0.32f, 0.19f, 0.54f),
            12,
            8,
            8));
        _emptyLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.73f, 0.67f));
        _emptyLabel.AddThemeFontSizeOverride("font_size", 12);

        SelectionPanelStyles.ApplyActionButtonStyle(_clearButton, new Color(0.59f, 0.45f, 0.26f), false, new Vector2(0.0f, 28.0f), 12);
    }

    private static PanelContainer CreateEntryRow(string entry, bool isCurrent)
    {
        PanelContainer row = new()
        {
            ClipContents = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0.0f, isCurrent ? 36.0f : 30.0f),
        };

        Color accent = isCurrent ? new Color(0.85f, 0.69f, 0.38f) : new Color(0.54f, 0.44f, 0.29f);
        row.AddThemeStyleboxOverride("panel", SelectionPanelStyles.CreateInsetStyle(
            isCurrent ? new Color(0.30f, 0.20f, 0.12f, 0.94f) : new Color(0.23f, 0.16f, 0.11f, 0.88f),
            accent,
            12,
            6,
            5));

        MarginContainer margin = new();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_bottom", 4);

        Label label = new()
        {
            Text = entry,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClipText = false,
        };
        label.AddThemeColorOverride("font_color", isCurrent ? new Color(0.98f, 0.93f, 0.81f) : new Color(0.89f, 0.84f, 0.76f));
        label.AddThemeFontSizeOverride("font_size", isCurrent ? 12 : 11);
        label.MaxLinesVisible = isCurrent ? 2 : 2;

        margin.AddChild(label);
        row.AddChild(margin);
        return row;
    }
}
