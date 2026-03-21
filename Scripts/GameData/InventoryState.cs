using System.Collections.Generic;

namespace IdleNet;

public sealed class InventoryState
{
    private readonly Dictionary<string, int> _storedCounts = new();
    private readonly Dictionary<string, int> _bagCounts = new();

    public InventoryState(IEnumerable<ItemDefinition> items, int bagCapacity, int stockpileCapacity)
    {
        BagCapacity = bagCapacity;
        StockpileCapacity = stockpileCapacity;

        foreach (ItemDefinition item in items)
        {
            _storedCounts[item.Id] = 0;
            _bagCounts[item.Id] = 0;
        }
    }

    public int BagCapacity { get; }

    public int StockpileCapacity { get; }

    public int Gold { get; private set; }

    public void AddToBag(string itemId, int amount = 1)
    {
        _bagCounts[itemId] += amount;
    }

    public int UnloadBagToStorage()
    {
        int moved = 0;

        foreach (string itemId in _bagCounts.Keys)
        {
            if (GetRemainingStorage() <= 0)
            {
                break;
            }

            int amountToMove = System.Math.Min(_bagCounts[itemId], GetRemainingStorage());
            _storedCounts[itemId] += amountToMove;
            _bagCounts[itemId] -= amountToMove;
            moved += amountToMove;
        }

        return moved;
    }

    public int GetStoredCount(string itemId) => _storedCounts[itemId];

    public int GetBagCount(string itemId) => _bagCounts[itemId];

    public int GetBagCount()
    {
        int total = 0;
        foreach (int value in _bagCounts.Values)
        {
            total += value;
        }

        return total;
    }

    public bool IsBagFull() => GetBagCount() >= BagCapacity;

    public int GetStoredCountTotal()
    {
        int total = 0;
        foreach (int value in _storedCounts.Values)
        {
            total += value;
        }

        return total;
    }

    public int GetRemainingStorage() => StockpileCapacity - GetStoredCountTotal();

    public bool HasStorageSpace() => GetRemainingStorage() > 0;

    public int SellStored(string itemId, int amount, int sellPriceCoins)
    {
        int soldAmount = System.Math.Min(amount, _storedCounts[itemId]);
        _storedCounts[itemId] -= soldAmount;
        Gold += soldAmount * sellPriceCoins;
        return soldAmount;
    }
}
