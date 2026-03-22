using System.Collections.Generic;

namespace IdleNet;

public sealed class FrontierTileState
{
    private readonly Dictionary<string, int> _requiredAmounts = new();
    private readonly Dictionary<string, int> _stagedAmounts = new();

    public bool RequirementsGenerated { get; private set; }

    public IReadOnlyDictionary<string, int> RequiredAmounts => _requiredAmounts;

    public IReadOnlyDictionary<string, int> StagedAmounts => _stagedAmounts;

    public void SetRequirements(IEnumerable<ExplorationRequirementEntry> requirements)
    {
        _requiredAmounts.Clear();
        _stagedAmounts.Clear();

        foreach (ExplorationRequirementEntry requirement in requirements)
        {
            _requiredAmounts[requirement.ItemId] = requirement.RequiredAmount;
            _stagedAmounts[requirement.ItemId] = 0;
        }

        RequirementsGenerated = true;
    }

    public int GetRequiredAmount(string itemId)
    {
        return _requiredAmounts.TryGetValue(itemId, out int amount) ? amount : 0;
    }

    public int GetStagedAmount(string itemId)
    {
        return _stagedAmounts.TryGetValue(itemId, out int amount) ? amount : 0;
    }

    public int GetMissingAmount(string itemId)
    {
        return System.Math.Max(0, GetRequiredAmount(itemId) - GetStagedAmount(itemId));
    }

    public int AddStaged(string itemId, int amount)
    {
        if (!_requiredAmounts.ContainsKey(itemId))
        {
            return 0;
        }

        int moved = System.Math.Min(amount, GetMissingAmount(itemId));
        _stagedAmounts[itemId] += moved;
        return moved;
    }

    public bool HasRequirements()
    {
        return _requiredAmounts.Count > 0;
    }

    public bool IsReadyToExplore()
    {
        if (!RequirementsGenerated || _requiredAmounts.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<string, int> pair in _requiredAmounts)
        {
            if (GetStagedAmount(pair.Key) < pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    public void ConsumeRequirements()
    {
        foreach (KeyValuePair<string, int> pair in _requiredAmounts)
        {
            int staged = GetStagedAmount(pair.Key);
            _stagedAmounts[pair.Key] = System.Math.Max(0, staged - pair.Value);
        }
    }
}
