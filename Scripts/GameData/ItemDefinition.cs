namespace IdleNet;

public sealed class ItemDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public int SellPriceCoins { get; init; }
}
