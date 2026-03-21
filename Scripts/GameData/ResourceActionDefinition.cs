namespace IdleNet;

public sealed class ResourceActionDefinition
{
    public required string Id { get; init; }

    public required string ButtonText { get; init; }

    public required string Verb { get; init; }

    public required string ItemId { get; init; }

    public required string SkillId { get; init; }

    public int MinSkillLevel { get; init; } = 1;
}
