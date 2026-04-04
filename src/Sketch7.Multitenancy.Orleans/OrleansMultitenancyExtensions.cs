using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Extension methods for configuring multitenancy with Orleans.
/// </summary>
public static class OrleansMultitenancyExtensions
{
	/// <summary>
	/// Configures Orleans with multitenancy support, registering the tenant grain call filter.
	/// </summary>
	/// <typeparam name="TTenant">The tenant type.</typeparam>
	/// <param name="builder">The silo builder.</param>
	public static ISiloBuilder UseMultitenancy<TTenant>(this ISiloBuilder builder)
		where TTenant : class, ITenant
	{
		builder.ConfigureServices(services =>
			services.AddSingleton<IIncomingGrainCallFilter, TenantGrainCallFilter<TTenant>>());
		return builder;
	}
}
