namespace IdleNet;

using System.Collections.Generic;

public sealed class ResourceDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string IconGlyph { get; init; } = "?";

    public string CategoryTag { get; init; } = "RESOURCE";

    public string PanelSubtitle { get; init; } = string.Empty;

    public required string GatherButtonText { get; init; }

    public required string GatherVerb { get; init; }

    public required string ItemId { get; init; }

    public required string SkillId { get; init; }

    public List<ResourceActionDefinition> AlternateActions { get; init; } = new();
}
