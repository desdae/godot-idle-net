using System.Collections.Generic;
using Godot;

namespace IdleNet;

public sealed class BuildingCardViewData
{
    public required string BuildingId { get; init; }

    public required string DisplayName { get; init; }

    public required string Description { get; init; }

    public required string IconGlyph { get; init; }

    public required Color AccentColor { get; init; }

    public required int CurrentLevel { get; init; }

    public required int MaxLevel { get; init; }

    public required bool IsBuilt { get; init; }

    public required bool IsUnlocked { get; init; }

    public required bool IsUnderConstruction { get; init; }

    public required bool IsMaxLevel { get; init; }

    public required bool CanAfford { get; init; }

    public required bool CanAct { get; init; }

    public required string StatusText { get; init; }

    public required string RequirementText { get; init; }

    public required string TimeText { get; init; }

    public required string OutputSummary { get; init; }

    public required string BenefitSummary { get; init; }

    public required string PrimaryButtonText { get; init; }

    public required string LevelText { get; init; }

    public required string ActionHintText { get; init; }

    public required float Progress { get; init; }

    public required string ProgressText { get; init; }

    public required bool IsUpgradeAction { get; init; }

    public Texture2D? Illustration { get; init; }

    public IReadOnlyList<TownCostViewData> Costs { get; init; } = new List<TownCostViewData>();
}
