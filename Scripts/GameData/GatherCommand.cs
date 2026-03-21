using Godot;

namespace IdleNet;

public enum WorkKind
{
    Gather,
    Explore,
    TownUpgrade,
    BuildingConstruction,
}

public sealed class GatherCommand
{
    public required WorkKind Kind { get; init; }

    public string? ResourceId { get; init; }

    public string? ResourceActionId { get; init; }

    public string? TownUpgradeId { get; init; }

    public string? BuildingId { get; init; }

    public int TargetLevel { get; init; }

    public required Vector2I Cell { get; init; }

    public int TotalAmount { get; init; }

    public int RemainingAmount { get; set; }

    public string Description { get; init; } = string.Empty;
}
