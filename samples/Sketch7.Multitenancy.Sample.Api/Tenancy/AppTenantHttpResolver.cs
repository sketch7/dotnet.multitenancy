using Sketch7.Multitenancy.AspNet;

namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

public class AppTenantHttpResolver(
	IAppTenantRegistry tenantRegistry
) : ITenantHttpResolver<AppTenant>
{
	public ValueTask<AppTenant?> Resolve(HttpContext httpContext)
		=> httpContext.Request.Headers.TryGetValue("X-SSV-Tenant", out var tenantValue)
			? new(tenantRegistry.GetOrDefault(tenantValue.ToString()))
			: new ValueTask<AppTenant?>();
}