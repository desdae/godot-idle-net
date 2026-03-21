using Godot;
using Godot.Collections;

namespace IdleNet;

[GlobalClass]
public partial class BuildingLevelDefinition : Resource
{
    [Export]
    public int Level { get; set; } = 1;

    [Export]
    public double DurationSeconds { get; set; }

    [Export]
    public string OutputSummary { get; set; } = string.Empty;

    [Export]
    public string BenefitSummary { get; set; } = string.Empty;

    [Export]
    public Array<BuildingCostDefinition> Costs { get; set; } = new();
}
