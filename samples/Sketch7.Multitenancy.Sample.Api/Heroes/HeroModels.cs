using System.Diagnostics;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Hero
{
	protected string DebuggerDisplay => $"Key: '{Key}', Name: '{Name}', Role: {Role}, Health: {Health}";
	public string Key { get; set; } = default!;
	public string Name { get; set; } = default!;
	public int Health { get; set; }
	public HeroRoleType Role { get; set; }
	public HashSet<string> Abilities { get; set; } = [];

	public override string ToString() => DebuggerDisplay;
}

public enum HeroRoleType
{
	Assassin = 1,
	Fighter = 2,
	Mage = 3,
	Support = 4,
	Tank = 5,
	Marksman = 6
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class HeroAbility
{
	protected string DebuggerDisplay => $"Id: '{Id}', HeroId: '{HeroId}', Name: '{Name}', Damage: {Damage}, DamageType: {DamageType}";

	public string Id { get; set; } = default!;
	public string HeroId { get; set; } = default!;
	public string Name { get; set; } = default!;
	public int Damage { get; set; }
	public DamageType DamageType { get; set; }
	public override string ToString() => DebuggerDisplay;
}

public enum DamageType
{
	None,
	AttackDamage,
	MagicDamage
}
