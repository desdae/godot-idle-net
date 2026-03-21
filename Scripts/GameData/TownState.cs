using System.Collections.Generic;

namespace IdleNet;

public sealed class TownState
{
    private readonly Dictionary<string, int> _storedCounts = new();
    private readonly Dictionary<string, BuildingState> _buildingStates = new();

    public TownState(IEnumerable<ItemDefinition> items, IEnumerable<BuildingDefinition> buildings, int stockpileCapacity)
    {
        BaseStockpileCapacity = stockpileCapacity;
        StockpileCapacity = stockpileCapacity;

        foreach (ItemDefinition item in items)
        {
            _storedCounts[item.Id] = 0;
        }

        foreach (BuildingDefinition building in buildings)
        {
            _buildingStates[building.Id] = new BuildingState
            {
                BuildingId = building.Id,
            };
        }
    }

    public int BaseStockpileCapacity { get; }

    public int StockpileCapacity { get; private set; }

    public int StockpileLevel { get; private set; } = 1;

    public int Gold { get; private set; }

    public BuildingState GetBuildingState(string buildingId)
    {
        return _buildingStates[buildingId];
    }

    public IEnumerable<BuildingState> GetBuildingStates()
    {
        return _buildingStates.Values;
    }

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

    public int PreviewNextStockpileCapacity(double multiplier, int roundTo)
    {
        double scaledCapacity = StockpileCapacity * multiplier;
        int roundedCapacity = (int)System.Math.Round(scaledCapacity / roundTo, System.MidpointRounding.AwayFromZero) * roundTo;
        return System.Math.Max(StockpileCapacity + roundTo, roundedCapacity);
    }

    public int UpgradeStockpile(double multiplier, int roundTo)
    {
        StockpileLevel++;
        StockpileCapacity = PreviewNextStockpileCapacity(multiplier, roundTo);
        return StockpileCapacity;
    }

    public bool CanAfford(IEnumerable<BuildingCostDefinition> costs)
    {
        foreach (BuildingCostDefinition cost in costs)
        {
            if (GetStoredCount(cost.ItemId) < cost.Amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool TryConsumeCosts(IEnumerable<BuildingCostDefinition> costs)
    {
        if (!CanAfford(costs))
        {
            return false;
        }

        foreach (BuildingCostDefinition cost in costs)
        {
            _storedCounts[cost.ItemId] -= cost.Amount;
        }

        return true;
    }
}
