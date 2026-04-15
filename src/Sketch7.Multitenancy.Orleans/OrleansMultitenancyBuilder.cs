using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Builder for configuring the Orleans multitenancy propagation strategy.
/// Obtained via <see cref="OrleansMultitenancyExtensions.UseMultitenancy{TTenant}"/>.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class OrleansMultitenancyBuilder<TTenant>
	where TTenant : class, ITenant
{
	private readonly ISiloBuilder _builder;

	internal OrleansMultitenancyBuilder(ISiloBuilder builder)
	{
		_builder = builder;
	}

	/// <summary>
	/// Registers <see cref="TenantGrainActivator{TTenant}"/> to inject tenant context once per grain activation.
	/// Context is set exactly once when the grain instance is created and remains for the grain's lifetime.
	/// </summary>
	/// <returns>The same builder for chaining.</returns>
	public OrleansMultitenancyBuilder<TTenant> WithGrainActivator()
	{
		_builder.ConfigureServices(services =>
			services.AddSingleton<IConfigureGrainContextProvider, TenantGrainActivator<TTenant>>());
		return this;
	}
}
