using Godot;
using Godot.Collections;

namespace IdleNet;

[GlobalClass]
public partial class BuildingDefinition : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Export]
    public Texture2D? Illustration { get; set; }

    [Export]
    public string IconGlyph { get; set; } = string.Empty;

    [Export]
    public string AccentColorHex { get; set; } = "#C89B62";

    [Export]
    public int MaxLevel { get; set; } = 1;

    [Export]
    public string RequiredSkillId { get; set; } = string.Empty;

    [Export]
    public int RequiredSkillLevel { get; set; } = 1;

    [Export]
    public Array<BuildingLevelDefinition> Levels { get; set; } = new();

    public Color AccentColor => new(AccentColorHex);

    public BuildingLevelDefinition GetLevelDefinition(int targetLevel)
    {
        foreach (BuildingLevelDefinition level in Levels)
        {
            if (level.Level == targetLevel)
            {
                return level;
            }
        }

        return Levels[Mathf.Clamp(targetLevel - 1, 0, Levels.Count - 1)];
    }
}
