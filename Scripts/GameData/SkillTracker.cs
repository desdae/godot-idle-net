using System.Collections.Generic;

namespace IdleNet;

public sealed class SkillTracker
{
    private readonly Dictionary<string, int> _xpBySkillId = new();
    private readonly GameRulesDefinition _rules;

    public SkillTracker(IEnumerable<SkillDefinition> skills, GameRulesDefinition rules)
    {
        _rules = rules;

        foreach (SkillDefinition skill in skills)
        {
            _xpBySkillId[skill.Id] = 0;
        }
    }

    public void AddXp(string skillId, int amount = 1)
    {
        _xpBySkillId[skillId] += amount;
    }

    public int GetXp(string skillId)
    {
        return _xpBySkillId[skillId];
    }

    public int GetLevel(string skillId)
    {
        int xp = _xpBySkillId[skillId];
        int level = 1;
        int xpRequiredForNextLevel = GetXpRequiredForLevel(level);

        while (xp >= xpRequiredForNextLevel)
        {
            xp -= xpRequiredForNextLevel;
            level++;
            xpRequiredForNextLevel = GetXpRequiredForLevel(level);
        }

        return level;
    }

    public int GetXpIntoCurrentLevel(string skillId)
    {
        int xp = _xpBySkillId[skillId];
        int level = 1;
        int xpRequiredForNextLevel = GetXpRequiredForLevel(level);

        while (xp >= xpRequiredForNextLevel)
        {
            xp -= xpRequiredForNextLevel;
            level++;
            xpRequiredForNextLevel = GetXpRequiredForLevel(level);
        }

        return xp;
    }

    public int GetXpForNextLevel(string skillId)
    {
        return GetXpRequiredForLevel(GetLevel(skillId));
    }

    private int GetXpRequiredForLevel(int currentLevel)
    {
        double scaledRequirement = _rules.SkillXpBaseRequirement * System.Math.Pow(_rules.SkillXpGrowthFactor, currentLevel - 1);
        return System.Math.Max(1, (int)System.Math.Ceiling(scaledRequirement));
    }
}
