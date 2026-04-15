using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy.AspNet;

/// <summary>
/// Extension members for integrating multitenancy into an ASP.NET Core pipeline.
/// </summary>
public static class MultitenancyApplicationBuilderExtensions
{
	extension(IApplicationBuilder app)
	{
		/// <summary>
		/// Adds the multitenancy middleware to the request pipeline.
		/// Must be placed before any middleware that depends on the resolved tenant.
		/// </summary>
		/// <typeparam name="TTenant">The tenant type.</typeparam>
		/// <param name="options">Optional middleware options.</param>
		public IApplicationBuilder UseMultitenancy<TTenant>(
			MultitenancyMiddlewareOptions? options = null)
			where TTenant : class, ITenant
			=> app.UseMiddleware<MultitenancyMiddleware<TTenant>>(options ?? new MultitenancyMiddlewareOptions());
	}
}

/// <summary>
/// Extension members for registering ASP.NET Core multitenancy services on <see cref="MultitenancyBuilder{TTenant}"/>.
/// </summary>
public static class AspNetMultitenancyBuilderExtensions
{
	extension<TTenant>(MultitenancyBuilder<TTenant> builder) where TTenant : class, ITenant
	{
		/// <summary>
		/// Registers the HTTP-based tenant resolver.
		/// </summary>
		/// <typeparam name="TResolver">The resolver implementation type.</typeparam>
		public MultitenancyBuilder<TTenant> WithHttpResolver<TResolver>()
			where TResolver : class, ITenantHttpResolver<TTenant>
		{
			builder.Services.AddScoped<ITenantHttpResolver<TTenant>, TResolver>();
			return builder;
		}
	}
}