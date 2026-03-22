namespace IdleNet;

public sealed class GameRulesDefinition
{
    public uint WorldSeed { get; init; }

    public int TileSize { get; init; }

    public double BaseStepDurationSeconds { get; init; }

    public double GatherDurationSeconds { get; init; }

    public double ExploreDurationSeconds { get; init; }

    public int ExploreBerryCost { get; init; }

    public string ExploreRequirementItemId { get; init; } = "berries";

    public int ExploreRequirementAmount { get; init; } = 50;

    public int BagCapacity { get; init; }

    public int StockpileCapacity { get; init; }

    public double RunningXpPerSecond { get; init; }

    public double RunningSpeedMultiplierPerLevel { get; init; }

    public double ExploringXpPerSecond { get; init; }

    public double ExploringSpeedMultiplierPerLevel { get; init; }

    public double BuildingXpPerSecond { get; init; }

    public int SkillXpBaseRequirement { get; init; }

    public double SkillXpGrowthFactor { get; init; }

    public int StartingRevealRadius { get; init; }

    public int DefaultQueuedGatherAmount { get; init; }
}
