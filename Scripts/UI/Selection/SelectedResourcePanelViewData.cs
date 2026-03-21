using System.Collections.Generic;
using Godot;

namespace IdleNet;

public sealed class SelectedResourcePanelViewData
{
    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string TagText { get; init; } = string.Empty;

    public string IconGlyph { get; init; } = "?";

    public string ProgressionText { get; init; } = string.Empty;

    public string ProgressionTooltipText { get; init; } = string.Empty;

    public string ActionNoteText { get; init; } = string.Empty;

    public bool EmphasizeProgression { get; init; }

    public bool ShowTag { get; init; } = true;

    public bool ShowCancelAction { get; init; }

    public string CancelActionText { get; init; } = "Clear Queue";

    public Color AccentColor { get; init; } = new(0.58f, 0.78f, 0.50f);

    public IReadOnlyList<SelectionStatChipViewData> Stats { get; init; } = new List<SelectionStatChipViewData>();

    public SelectedResourcePanelActionViewData PrimaryAction { get; init; } = new();

    public SelectedResourcePanelActionViewData SecondaryAction { get; init; } = new() { Visible = false };
}
