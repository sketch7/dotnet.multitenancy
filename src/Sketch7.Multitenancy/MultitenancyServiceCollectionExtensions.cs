using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy;

/// <summary>
/// Extension methods for registering multitenancy services on <see cref="IServiceCollection"/>.
/// </summary>
public static class MultitenancyServiceCollectionExtensions
{
	/// <summary>
	/// Adds core multitenancy services and returns a <see cref="MultitenancyBuilder{TTenant}"/>
	/// for fluent per-tenant configuration.
	/// </summary>
	/// <typeparam name="TTenant">The tenant type.</typeparam>
	/// <param name="services">The service collection.</param>
	public static MultitenancyBuilder<TTenant> AddMultitenancy<TTenant>(this IServiceCollection services)
		where TTenant : class, ITenant
	{
		services.AddScoped<TenantAccessor<TTenant>>();
		services.AddScoped<ITenantAccessor<TTenant>>(sp => sp.GetRequiredService<TenantAccessor<TTenant>>());

		return new MultitenancyBuilder<TTenant>(services);
	}

	/// <summary>
	/// Adds core multitenancy services with a registry and returns a <see cref="MultitenancyBuilder{TTenant}"/>
	/// for fluent per-tenant configuration.
	/// </summary>
	/// <typeparam name="TTenant">The tenant type.</typeparam>
	/// <typeparam name="TRegistry">The registry implementation type.</typeparam>
	/// <param name="services">The service collection.</param>
	public static MultitenancyBuilder<TTenant> AddMultitenancy<TTenant, TRegistry>(this IServiceCollection services)
		where TTenant : class, ITenant
		where TRegistry : class, ITenantRegistry<TTenant>
	{
		services.AddScoped<TenantAccessor<TTenant>>();
		services.AddScoped<ITenantAccessor<TTenant>>(sp => sp.GetRequiredService<TenantAccessor<TTenant>>());

		var builder = new MultitenancyBuilder<TTenant>(services);
		builder.WithRegistry<TRegistry>();
		return builder;
	}
}
