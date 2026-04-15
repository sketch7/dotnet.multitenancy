using Microsoft.Extensions.DependencyInjection;
using Sketch7.Multitenancy;

namespace Sketch7.Multitenancy.Tests;

internal static class TestHelpers
{
	/// <summary>
	/// Sets the current tenant on <see cref="TenantAccessor{TTenant}"/> and returns the provider for chaining.
	/// </summary>
	internal static IServiceProvider SetTenant<TTenant>(this IServiceProvider sp, TTenant tenant)
		where TTenant : class, ITenant
	{
		sp.GetRequiredService<TenantAccessor<TTenant>>().Tenant = tenant;
		return sp;
	}

	/// <summary>
	/// Creates a new scope and sets the current tenant. Dispose the returned scope when done.
	/// </summary>
	internal static IServiceScope CreateScopeForTenant<TTenant>(this IServiceProvider provider, TTenant tenant)
		where TTenant : class, ITenant
	{
		var scope = provider.CreateScope();
		scope.ServiceProvider.SetTenant(tenant);
		return scope;
	}
}
