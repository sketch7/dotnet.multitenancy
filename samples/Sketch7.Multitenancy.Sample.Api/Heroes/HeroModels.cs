namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Represents a hero entity shared across grains and API responses.</summary>
[GenerateSerializer]
public record Hero
{
	/// <summary>Gets the unique hero key.</summary>
	[Id(0)]
	public string Key { get; init; } = null!;

	/// <summary>Gets the display name.</summary>
	[Id(1)]
	public string Name { get; init; } = null!;

	/// <summary>Gets the base health value.</summary>
	[Id(2)]
	public int Health { get; init; }

	/// <summary>Gets the hero difficulty classification.</summary>
	[Id(3)]
	public HeroDifficulty Difficulty { get; init; }

	/// <summary>Gets the set of ability keys for this hero.</summary>
	[Id(4)]
	public HashSet<string> Abilities { get; init; } = [];
}

/// <summary>Hero difficulty classification.</summary>
public enum HeroDifficulty
{
	Easy = 1,
	Medium = 2,
	Hard = 3,
	VeryHard = 4
}
