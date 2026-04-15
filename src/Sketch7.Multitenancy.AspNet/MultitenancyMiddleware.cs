using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy.AspNet;

/// <summary>
/// ASP.NET Core middleware that resolves the current tenant and sets it on <see cref="ITenantAccessor{TTenant}"/>.
/// Returns a 400 Bad Request response when no valid tenant is found.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public class MultitenancyMiddleware<TTenant>
	where TTenant : class, ITenant
{
	private readonly RequestDelegate _next;
	private readonly MultitenancyMiddlewareOptions _options;

	/// <summary>
	/// Initializes a new instance of <see cref="MultitenancyMiddleware{TTenant}"/>.
	/// </summary>
	public MultitenancyMiddleware(RequestDelegate next, MultitenancyMiddlewareOptions options)
	{
		_next = next;
		_options = options;
	}

	/// <summary>
	/// Invokes the middleware.
	/// </summary>
	public async Task InvokeAsync(HttpContext httpContext)
	{
		var resolver = httpContext.RequestServices.GetRequiredService<ITenantHttpResolver<TTenant>>();
		var tenantAccessor = httpContext.RequestServices.GetRequiredService<TenantAccessor<TTenant>>();

		var tenant = await resolver.Resolve(httpContext);

		if (tenant == null)
		{
			httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
			httpContext.Response.ContentType = "application/json";
			await httpContext.Response.WriteAsJsonAsync(_options.InvalidTenantResponse());
			return;
		}

		tenantAccessor.Tenant = tenant;
		await _next(httpContext);
	}
}

/// <summary>
/// Options for <see cref="MultitenancyMiddleware{TTenant}"/>.
/// </summary>
public class MultitenancyMiddlewareOptions
{
	internal Func<object> InvalidTenantResponse { get; private set; } =
		static () => new { errorCode = "error.invalid:tenant" };

	/// <summary>
	/// Configures the response body returned when tenant resolution fails.
	/// </summary>
	/// <param name="factory">Factory producing the response object.</param>
	public MultitenancyMiddlewareOptions WithInvalidTenantResponse(Func<object> factory)
	{
		InvalidTenantResponse = factory;
		return this;
	}
}