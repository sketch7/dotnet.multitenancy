using Microsoft.AspNetCore.Builder;

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