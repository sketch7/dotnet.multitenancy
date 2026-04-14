using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy;

/// <summary>
/// Extension members for registering multitenancy services on <see cref="IServiceCollection"/>.
/// </summary>
public static class MultitenancyServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds core multitenancy services and returns a <see cref="MultitenancyBuilder{TTenant}"/>
		/// for fluent per-tenant configuration.
		/// </summary>
		/// <typeparam name="TTenant">The tenant type.</typeparam>
		public MultitenancyBuilder<TTenant> AddMultitenancy<TTenant>()
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
		public MultitenancyBuilder<TTenant> AddMultitenancy<TTenant, TRegistry>()
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
}