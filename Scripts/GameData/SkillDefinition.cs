using Godot;

namespace IdleNet;

public sealed class SkillDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string IconGlyph { get; init; }

    public required string IconColorHex { get; init; }

    public Color IconColor => new(IconColorHex);
}
