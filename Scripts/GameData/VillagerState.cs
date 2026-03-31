namespace IdleNet;

public sealed class VillagerState
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Role { get; set; } = "Laborer";

    public string Duty { get; set; } = "Idle";

    public string CurrentTask { get; set; } = "Awaiting orders";

    public string LoadText { get; set; } = "0/10";

    public string Status { get; set; } = "Idle";

    public string Summary { get; set; } = "Waiting for a first assignment.";
}
