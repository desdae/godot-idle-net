using System.Collections.Generic;

namespace IdleNet;

public sealed class TownState
{
    private readonly Dictionary<string, int> _storedCounts = new();

    public TownState(IEnumerable<ItemDefinition> items, int stockpileCapacity)
    {
        StockpileCapacity = stockpileCapacity;

        foreach (ItemDefinition item in items)
        {
            _storedCounts[item.Id] = 0;
        }
    }

    public int StockpileCapacity { get; }

    public int Gold { get; private set; }

    public int Store(string itemId, int amount)
    {
        int moved = System.Math.Min(amount, GetRemainingStorage());
        _storedCounts[itemId] += moved;
        return moved;
    }

    public int GetStoredCount(string itemId)
    {
        return _storedCounts[itemId];
    }

    public int GetStoredCountTotal()
    {
        int total = 0;
        foreach (int value in _storedCounts.Values)
        {
            total += value;
        }

        return total;
    }

    public int GetRemainingStorage()
    {
        return StockpileCapacity - GetStoredCountTotal();
    }

    public bool HasStorageSpace()
    {
        return GetRemainingStorage() > 0;
    }

    public int SellStored(string itemId, int amount, int sellPriceCoins)
    {
        int soldAmount = System.Math.Min(amount, _storedCounts[itemId]);
        _storedCounts[itemId] -= soldAmount;
        Gold += soldAmount * sellPriceCoins;
        return soldAmount;
    }

    public bool TryConsumeStored(string itemId, int amount)
    {
        if (_storedCounts[itemId] < amount)
        {
            return false;
        }

        _storedCounts[itemId] -= amount;
        return true;
    }
}
