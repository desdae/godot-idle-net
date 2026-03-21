using System.Collections.Generic;

namespace IdleNet;

public sealed class CharacterState
{
    private readonly Dictionary<string, int> _bagCounts = new();
    private readonly SkillTracker _skillTracker;

    public CharacterState(IEnumerable<ItemDefinition> items, IEnumerable<SkillDefinition> skills, int bagCapacity)
    {
        BagCapacity = bagCapacity;

        foreach (ItemDefinition item in items)
        {
            _bagCounts[item.Id] = 0;
        }

        _skillTracker = new SkillTracker(skills, GameCatalog.Rules);
    }

    public int BagCapacity { get; }

    public void AddToBag(string itemId, int amount = 1)
    {
        _bagCounts[itemId] += amount;
    }

    public int GetBagCount(string itemId)
    {
        return _bagCounts[itemId];
    }

    public int GetBagCount()
    {
        int total = 0;
        foreach (int value in _bagCounts.Values)
        {
            total += value;
        }

        return total;
    }

    public bool IsBagFull()
    {
        return GetBagCount() >= BagCapacity;
    }

    public void AddSkillXp(string skillId, int amount = 1)
    {
        _skillTracker.AddXp(skillId, amount);
    }

    public int GetSkillLevel(string skillId)
    {
        return _skillTracker.GetLevel(skillId);
    }

    public int GetSkillXp(string skillId)
    {
        return _skillTracker.GetXp(skillId);
    }

    public int GetSkillXpIntoCurrentLevel(string skillId)
    {
        return _skillTracker.GetXpIntoCurrentLevel(skillId);
    }

    public int GetSkillXpForNextLevel(string skillId)
    {
        return _skillTracker.GetXpForNextLevel(skillId);
    }

    public int UnloadToTown(TownState townState)
    {
        int moved = 0;

        foreach (string itemId in _bagCounts.Keys)
        {
            if (!townState.HasStorageSpace())
            {
                break;
            }

            int stored = townState.Store(itemId, _bagCounts[itemId]);
            _bagCounts[itemId] -= stored;
            moved += stored;
        }

        return moved;
    }
}
