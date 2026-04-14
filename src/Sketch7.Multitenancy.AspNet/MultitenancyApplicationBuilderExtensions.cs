using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy.AspNet;

/// <summary>
/// Extension methods for integrating multitenancy into an ASP.NET Core pipeline.
/// </summary>
public static class MultitenancyApplicationBuilderExtensions
{
	/// <summary>
	/// Adds the multitenancy middleware to the request pipeline.
	/// Must be placed before any middleware that depends on the resolved tenant.
	/// </summary>
	/// <typeparam name="TTenant">The tenant type.</typeparam>
	/// <param name="app">The application builder.</param>
	/// <param name="options">Optional middleware options.</param>
	public static IApplicationBuilder UseMultitenancy<TTenant>(
		this IApplicationBuilder app,
		MultitenancyMiddlewareOptions? options = null)
		where TTenant : class, ITenant
		=> app.UseMiddleware<MultitenancyMiddleware<TTenant>>(options ?? new MultitenancyMiddlewareOptions());
}

/// <summary>
/// Extension methods for registering ASP.NET Core multitenancy services on <see cref="MultitenancyBuilder{TTenant}"/>.
/// </summary>
public static class AspNetMultitenancyBuilderExtensions
{
	/// <summary>
	/// Registers the HTTP-based tenant resolver.
	/// </summary>
	/// <typeparam name="TTenant">The tenant type.</typeparam>
	/// <typeparam name="TResolver">The resolver implementation type.</typeparam>
	/// <param name="builder">The multitenancy builder.</param>
	public static MultitenancyBuilder<TTenant> WithHttpResolver<TTenant, TResolver>(this MultitenancyBuilder<TTenant> builder)
		where TTenant : class, ITenant
		where TResolver : class, ITenantHttpResolver<TTenant>
	{
		builder.Services.AddScoped<ITenantHttpResolver<TTenant>, TResolver>();
		return builder;
	}
}
