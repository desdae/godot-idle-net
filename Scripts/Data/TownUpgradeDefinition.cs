using Godot;
using Godot.Collections;

namespace IdleNet;

[GlobalClass]
public partial class TownUpgradeDefinition : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public double DurationSeconds { get; set; }

    [Export]
    public double CapacityMultiplier { get; set; } = 1.0;

    [Export]
    public int CapacityRoundTo { get; set; } = 10;

    [Export]
    public Array<BuildingCostDefinition> Costs { get; set; } = new();
}
