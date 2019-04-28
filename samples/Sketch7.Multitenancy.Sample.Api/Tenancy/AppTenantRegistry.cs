using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sketch7.Multitenancy.Sample.Api.Tenancy
{
	public static class OrganizationNames
	{
		public const string Riot = "riot";
		public const string Blizzard = "blizzard";
		public const string Sketch7 = "sketch7";
	}

	public interface IAppTenantRegistry : ITenantRegistry<AppTenant>
	{
		//AppTenant GetOrDefault(string brandId);
		//AppTenant Add(TenantConfig brand);
		//AppTenant AddAll(Dictionary<string, TenantConfig> brands);
		AppTenant GetOrDefault(string tenant);
	}

	public class AppTenantRegistry : IAppTenantRegistry
	{
		private const string TenantGroupKey = "tenant";
		private readonly Regex _regex = new Regex($@"tenant\/(?'{TenantGroupKey}'[\w@-]+)\/?(.*)", RegexOptions.Compiled);

		public static AppTenant LeagueOfLegends = new AppTenant
		{
			Key = "lol",
			Name = "League of Legends",
			Organization = OrganizationNames.Riot
		};

		public static readonly AppTenant HeroesOfTheStorm = new AppTenant
		{
			Key = "hots",
			Name = "Heroes of the Storm",
			Organization = OrganizationNames.Blizzard
		};

		private readonly Dictionary<string, AppTenant> _tenants;

		public AppTenantRegistry()
		{
			var tenants = new HashSet<AppTenant>
			{
				LeagueOfLegends,
				HeroesOfTheStorm
			};

			_tenants = tenants.ToDictionary(x => x.Key);
		}

		public AppTenant Get(string tenant)
		{
			var config = GetOrDefault(tenant);
			if (config == null)
				throw new KeyNotFoundException($"Tenant not found for '{tenant}'");
			return config;
		}

		public AppTenant GetOrDefault(string tenant)
		{
			_tenants.TryGetValue(tenant, out var brand);
			return brand;
		}

		public ITenant GetByPrimaryKey(string primaryKey)
		{
			if (string.IsNullOrEmpty(primaryKey))
				throw new ArgumentException("Primary key must be defined.", nameof(primaryKey));

			var matches = _regex.Match(primaryKey);
			if (!matches.Success)
				return null;

			var tenantKey = matches.Groups[TenantGroupKey].Value;
			return Get(tenantKey);
		}

		public IEnumerable<AppTenant> GetAll() => _tenants.Values;
	}
}
