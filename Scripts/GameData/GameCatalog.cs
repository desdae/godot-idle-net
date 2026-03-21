using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace IdleNet;

public static class GameCatalog
{
	public static GameRulesDefinition Rules { get; }

	public static IReadOnlyList<SkillDefinition> Skills { get; }

	public static IReadOnlyList<ItemDefinition> Items { get; }

	public static IReadOnlyList<ResourceDefinition> Resources { get; }

	private static readonly Dictionary<string, SkillDefinition> SkillsById;
	private static readonly Dictionary<string, ItemDefinition> ItemsById;
	private static readonly Dictionary<string, ResourceDefinition> ResourcesById;

	static GameCatalog()
	{
		Rules = LoadObject<GameRulesDefinition>("res://Data/rules.json");
		Skills = LoadList<SkillDefinition>("res://Data/skills.json");
		Items = LoadList<ItemDefinition>("res://Data/items.json");
		Resources = LoadList<ResourceDefinition>("res://Data/resources.json");

		SkillsById = CreateMap(Skills, skill => skill.Id);
		ItemsById = CreateMap(Items, item => item.Id);
		ResourcesById = CreateMap(Resources, resource => resource.Id);
	}

	public static SkillDefinition Woodcutting => GetSkill("woodcutting");

	public static SkillDefinition Mining => GetSkill("mining");

	public static SkillDefinition Foraging => GetSkill("foraging");

	public static SkillDefinition Exploring => GetSkill("exploring");

	public static SkillDefinition Running => GetSkill("running");

	public static ItemDefinition Sticks => GetItem("sticks");

	public static ItemDefinition Stones => GetItem("stones");

	public static ItemDefinition Berries => GetItem("berries");

	public static ResourceDefinition Tree => GetResource("tree");

	public static ResourceDefinition Stone => GetResource("stone");

	public static ResourceDefinition BerryBush => GetResource("berries");

	public static SkillDefinition GetSkill(string id) => SkillsById[id];

	public static ItemDefinition GetItem(string id) => ItemsById[id];

	public static ResourceDefinition GetResource(string id) => ResourcesById[id];

	private static T LoadObject<T>(string resourcePath)
	{
		string json = FileAccess.GetFileAsString(resourcePath);
		T? value = JsonSerializer.Deserialize<T>(json, CreateJsonOptions());
		if (value is null)
		{
			throw new JsonException($"Failed to deserialize data file: {resourcePath}");
		}

		return value;
	}

	private static IReadOnlyList<T> LoadList<T>(string resourcePath)
	{
		string json = FileAccess.GetFileAsString(resourcePath);
		List<T>? value = JsonSerializer.Deserialize<List<T>>(json, CreateJsonOptions());
		if (value is null)
		{
			throw new JsonException($"Failed to deserialize data file: {resourcePath}");
		}

		return value;
	}

	private static JsonSerializerOptions CreateJsonOptions()
	{
		return new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		};
	}

	private static Dictionary<string, T> CreateMap<T>(IReadOnlyList<T> entries, System.Func<T, string> idSelector)
	{
		Dictionary<string, T> map = new();
		foreach (T entry in entries)
		{
			map[idSelector(entry)] = entry;
		}

		return map;
	}
}
