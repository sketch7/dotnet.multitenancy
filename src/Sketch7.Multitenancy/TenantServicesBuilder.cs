using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy;

/// <summary>
/// Fluent builder for configuring per-tenant service registrations.
/// Obtained via <see cref="MultitenancyBuilder{TTenant}.WithServices"/>.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class TenantServicesBuilder<TTenant>
	where TTenant : class, ITenant
{
	private readonly MultitenancyBuilder<TTenant> _builder;

	internal TenantServicesBuilder(MultitenancyBuilder<TTenant> builder)
	{
		_builder = builder;
	}

	/// <summary>
	/// Configures services for a specific tenant identified by key.
	/// </summary>
	/// <param name="key">The tenant key.</param>
	/// <param name="configure">Action to configure tenant-specific services.</param>
	public TenantServicesBuilder<TTenant> For(string key, Action<IServiceCollection> configure)
	{
		_builder.ForTenant(key, configure);
		return this;
	}

	/// <summary>
	/// Configures services for all tenants matching the given predicate.
	/// Requires tenants to be available via
	/// <see cref="MultitenancyBuilder{TTenant}.WithRegistry{TRegistry}(TRegistry)"/> or
	/// <see cref="MultitenancyBuilder{TTenant}.WithTenants"/>.
	/// </summary>
	/// <param name="predicate">Filter to select matching tenants.</param>
	/// <param name="configure">Action to configure services for matching tenants.</param>
	public TenantServicesBuilder<TTenant> For(Func<TTenant, bool> predicate, Action<IServiceCollection> configure)
	{
		_builder.ForTenants(predicate, configure);
		return this;
	}

	/// <summary>
	/// Configures shared services applied to all tenants.
	/// Requires tenants to be available via
	/// <see cref="MultitenancyBuilder{TTenant}.WithRegistry{TRegistry}(TRegistry)"/> or
	/// <see cref="MultitenancyBuilder{TTenant}.WithTenants"/>.
	/// </summary>
	/// <param name="configure">Action to configure services for all tenants.</param>
	public TenantServicesBuilder<TTenant> ForAll(Action<IServiceCollection> configure)
	{
		_builder.ForAllTenants(configure);
		return this;
	}
}
