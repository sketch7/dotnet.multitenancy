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

	/// <summary>Gets the hero role classification.</summary>
	[Id(3)]
	public HeroRoleType Role { get; init; }

	/// <summary>Gets the set of ability keys for this hero.</summary>
	[Id(4)]
	public HashSet<string> Abilities { get; init; } = [];
}

/// <summary>Hero role classification.</summary>
public enum HeroRoleType
{
	Assassin = 1,
	Fighter = 2,
	Mage = 3,
	Support = 4,
	Tank = 5,
	Marksman = 6
}

/// <summary>Represents an individual hero ability.</summary>
public record HeroAbility
{
	/// <summary>Gets the ability identifier.</summary>
	public string Id { get; init; } = null!;

	/// <summary>Gets the owning hero identifier.</summary>
	public string HeroId { get; init; } = null!;

	/// <summary>Gets the ability display name.</summary>
	public string Name { get; init; } = null!;

	/// <summary>Gets the base damage value.</summary>
	public int Damage { get; init; }

	/// <summary>Gets the damage type.</summary>
	public DamageType DamageType { get; init; }
}

/// <summary>Damage type classification for hero abilities.</summary>
public enum DamageType
{
	None,
	AttackDamage,
	MagicDamage
}