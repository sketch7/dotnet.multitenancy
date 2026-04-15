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
		/// Configures Orleans with multitenancy support and returns a builder to select the propagation strategy.
		/// </summary>
		/// <typeparam name="TTenant">The tenant type.</typeparam>
		/// <returns>
		/// An <see cref="OrleansMultitenancyBuilder{TTenant}"/> — chain
		/// <c>.WithIncomingCallFilter()</c> or <c>.WithGrainActivator()</c> to complete configuration.
		/// </returns>
		public OrleansMultitenancyBuilder<TTenant> UseMultitenancy<TTenant>()
			where TTenant : class, ITenant
		{
			builder.ConfigureServices(services =>
				services.AddSingleton<ITenantOrleansResolver<TTenant>, TenantOrleansResolver<TTenant>>());
			return new OrleansMultitenancyBuilder<TTenant>(builder);
		}
	}
}