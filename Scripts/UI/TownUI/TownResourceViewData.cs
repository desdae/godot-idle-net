namespace IdleNet;

public sealed class TownResourceViewData
{
    public required string ItemId { get; init; }

    public required string DisplayName { get; init; }

    public required string IconGlyph { get; init; }

    public required int Amount { get; init; }

    public required int SellValue { get; init; }

    public required bool Selected { get; init; }
}
