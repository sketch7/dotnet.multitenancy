using Microsoft.AspNetCore.Http;

namespace Sketch7.Multitenancy.AspNet;

/// <summary>
/// Resolves the current tenant from an HTTP request context.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public interface ITenantHttpResolver<TTenant>
	where TTenant : class, ITenant
{
	/// <summary>
	/// Resolves the tenant from the given HTTP context.
	/// Returns <c>null</c> if the tenant could not be determined.
	/// </summary>
	/// <param name="httpContext">The current HTTP context.</param>
	ValueTask<TTenant?> Resolve(HttpContext httpContext);
}