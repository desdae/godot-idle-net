using System.Collections.Generic;

namespace IdleNet;

public sealed class TownViewData
{
    public required string SettlementTitle { get; init; }

    public required int Gold { get; init; }

    public required int StockpileCurrent { get; init; }

    public required int StockpileCapacity { get; init; }

    public required string StockpileSummary { get; init; }

    public required string StockpileUpgradeSummary { get; init; }

    public required bool CanUpgradeStockpile { get; init; }

    public required IReadOnlyList<TownResourceViewData> Resources { get; init; }

    public required string SellPrompt { get; init; }

    public required int SellPercent { get; init; }

    public required string SellAmountText { get; init; }

    public required bool CanSell { get; init; }

    public required IReadOnlyList<BuildingCardViewData> Buildings { get; init; }

    public required TownBuildingFilter ActiveFilter { get; init; }

    public required string LedgerText { get; init; }
}
