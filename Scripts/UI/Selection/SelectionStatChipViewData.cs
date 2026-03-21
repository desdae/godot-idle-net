using Godot;

namespace IdleNet;

public sealed class SelectionStatChipViewData
{
    public string IconGlyph { get; init; } = "?";

    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string TooltipText { get; init; } = string.Empty;

    public Color AccentColor { get; init; } = new(0.58f, 0.78f, 0.50f);
}
