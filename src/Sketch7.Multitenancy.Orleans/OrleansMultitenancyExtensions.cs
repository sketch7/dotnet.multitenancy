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
		/// Configures Orleans with multitenancy support, registering the tenant grain call filter.
		/// </summary>
		/// <typeparam name="TTenant">The tenant type.</typeparam>
		public ISiloBuilder UseMultitenancy<TTenant>()
			where TTenant : class, ITenant
		{
			builder.ConfigureServices(services =>
				services.AddSingleton<IIncomingGrainCallFilter, TenantGrainCallFilter<TTenant>>());
			return builder;
		}
	}
}