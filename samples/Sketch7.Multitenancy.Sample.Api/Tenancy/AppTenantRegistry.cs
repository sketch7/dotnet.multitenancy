namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

public static class OrganizationNames
{
	public const string Riot = "riot";
	public const string Blizzard = "blizzard";
	public const string Sketch7 = "sketch7";
}

public interface IAppTenantRegistry : ITenantRegistry<AppTenant>
{
	AppTenant? GetOrDefault(string tenant);
}

public class AppTenantRegistry : IAppTenantRegistry
{
	public static readonly AppTenant LeagueOfLegends = new()
	{
		Key = "lol",
		Name = "League of Legends",
		Organization = OrganizationNames.Riot
	};

	public static readonly AppTenant HeroesOfTheStorm = new()
	{
		Key = "hots",
		Name = "Heroes of the Storm",
		Organization = OrganizationNames.Blizzard
	};

	private readonly Dictionary<string, AppTenant> _tenants;

	public AppTenantRegistry()
	{
		var tenants = new[] { LeagueOfLegends, HeroesOfTheStorm };
		_tenants = tenants.ToDictionary(x => x.Key);
	}

	public AppTenant Get(string tenant)
	{
		var config = GetOrDefault(tenant);
		if (config == null)
			throw new KeyNotFoundException($"Tenant not found for '{tenant}'");
		return config;
	}

	public AppTenant? GetOrDefault(string tenant)
	{
		_tenants.TryGetValue(tenant, out var brand);
		return brand;
	}

	public IEnumerable<AppTenant> GetAll() => _tenants.Values;
}
