using Godot;

namespace IdleNet;

[GlobalClass]
public partial class BuildingCostDefinition : Resource
{
    [Export]
    public string ItemId { get; set; } = string.Empty;

    [Export]
    public int Amount { get; set; }
}
