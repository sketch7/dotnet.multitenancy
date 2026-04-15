using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Extension members for configuring multitenancy with Orleans.
/// </summary>
public static class OrleansMultitenancyExtensions
{
	extension(ISiloBuilder builder)
	{
		/// <summary>
		/// Configures Orleans with multitenancy support, registering
		/// <see cref="ITenantOrleansResolver{TTenant}"/> and <see cref="TenantGrainActivator{TTenant}"/>.
		/// </summary>
		/// <typeparam name="TTenant">The tenant type.</typeparam>
		/// <returns>The same <see cref="ISiloBuilder"/> for chaining.</returns>
		public ISiloBuilder UseMultitenancy<TTenant>()
			where TTenant : class, ITenant
		{
			builder.ConfigureServices(services =>
			{
				services.AddSingleton<ITenantOrleansResolver<TTenant>, TenantOrleansResolver<TTenant>>();
				services.AddSingleton<IConfigureGrainContextProvider, TenantGrainActivator<TTenant>>();
			});
			return builder;
		}
	}
}
