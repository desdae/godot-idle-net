namespace IdleNet;

public sealed class TownCostViewData
{
    public required string ItemId { get; init; }

    public required string ItemName { get; init; }

    public required string IconGlyph { get; init; }

    public required int Amount { get; init; }

    public required bool Affordable { get; init; }
}
