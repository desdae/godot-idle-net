namespace IdleNet;

public sealed class BuildingState
{
    public required string BuildingId { get; init; }

    public int CurrentLevel { get; private set; }

    public bool IsUnderConstruction { get; private set; }

    public int TargetLevel { get; private set; }

    public double ProgressSeconds { get; private set; }

    public double TotalSeconds { get; private set; }

    public bool IsBuilt => CurrentLevel > 0;

    public bool IsMaxLevel(BuildingDefinition definition) => CurrentLevel >= definition.MaxLevel;

    public bool CanUpgrade(BuildingDefinition definition) => IsBuilt && !IsMaxLevel(definition);

    public double ConstructionProgress => TotalSeconds <= 0.0 ? 0.0 : ProgressSeconds / TotalSeconds;

    public void BeginConstruction(int targetLevel, double totalSeconds)
    {
        IsUnderConstruction = true;
        TargetLevel = targetLevel;
        TotalSeconds = totalSeconds;
        ProgressSeconds = 0.0;
    }

    public void Advance(double delta)
    {
        if (!IsUnderConstruction)
        {
            return;
        }

        ProgressSeconds = System.Math.Min(TotalSeconds, ProgressSeconds + delta);
    }

    public void CompleteConstruction()
    {
        CurrentLevel = TargetLevel;
        IsUnderConstruction = false;
        TargetLevel = 0;
        ProgressSeconds = 0.0;
        TotalSeconds = 0.0;
    }

    public void CancelConstruction()
    {
        IsUnderConstruction = false;
        TargetLevel = 0;
        ProgressSeconds = 0.0;
        TotalSeconds = 0.0;
    }
}
