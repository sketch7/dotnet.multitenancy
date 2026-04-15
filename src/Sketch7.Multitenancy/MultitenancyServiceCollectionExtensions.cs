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
		/// <param name="configure">Optional inline configuration callback.</param>
		public MultitenancyBuilder<TTenant> AddMultitenancy<TTenant>(Action<MultitenancyBuilder<TTenant>>? configure = null)
			where TTenant : class, ITenant
		{
			services.AddScoped<TenantAccessor<TTenant>>();
			services.AddScoped<ITenantAccessor<TTenant>>(sp => sp.GetRequiredService<TenantAccessor<TTenant>>());
			services.AddScoped<ITenantAccessor>(sp => sp.GetRequiredService<TenantAccessor<TTenant>>());

			var builder = new MultitenancyBuilder<TTenant>(services);
			configure?.Invoke(builder);
			return builder;
		}
	}
}