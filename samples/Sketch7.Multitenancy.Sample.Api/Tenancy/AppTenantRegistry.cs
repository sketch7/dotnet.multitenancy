namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

/// <summary>Well-known organization name constants.</summary>
public static class OrganizationNames
{
	/// <summary>Riot Games organization key.</summary>
	public const string Riot = "riot";

	/// <summary>Blizzard Entertainment organization key.</summary>
	public const string Blizzard = "blizzard";

	/// <summary>Sketch7 organization key.</summary>
	public const string Sketch7 = "sketch7";
}

/// <summary>Extends <see cref="ITenantRegistry{TTenant}"/> with optional lookup by key.</summary>
public interface IAppTenantRegistry : ITenantRegistry<AppTenant>
{
	/// <summary>Gets the tenant by key, or <c>null</c> if not found.</summary>
	AppTenant? GetOrDefault(string key);
}

/// <summary>In-memory registry of known <see cref="AppTenant"/> instances.</summary>
public sealed class AppTenantRegistry : IAppTenantRegistry
{
	/// <summary>The League of Legends tenant (Riot Games).</summary>
	public static readonly AppTenant LeagueOfLegends = new()
	{
		Key = "lol",
		Name = "League of Legends",
		Organization = OrganizationNames.Riot
	};

	/// <summary>The Heroes of the Storm tenant (Blizzard).</summary>
	public static readonly AppTenant HeroesOfTheStorm = new()
	{
		Key = "hots",
		Name = "Heroes of the Storm",
		Organization = OrganizationNames.Blizzard
	};

	private readonly Dictionary<string, AppTenant> _tenants;

	/// <summary>Initializes a new instance of <see cref="AppTenantRegistry"/>.</summary>
	public AppTenantRegistry()
	{
		AppTenant[] tenants = [LeagueOfLegends, HeroesOfTheStorm];
		_tenants = tenants.ToDictionary(x => x.Key);
	}

	/// <inheritdoc />
	public AppTenant Get(string tenant)
		=> GetOrDefault(tenant) ?? throw new KeyNotFoundException($"Tenant not found for '{tenant}'");

	/// <inheritdoc />
	public AppTenant? GetOrDefault(string key)
	{
		_tenants.TryGetValue(key, out var tenant);
		return tenant;
	}

	public IEnumerable<AppTenant> GetAll() => _tenants.Values;
}