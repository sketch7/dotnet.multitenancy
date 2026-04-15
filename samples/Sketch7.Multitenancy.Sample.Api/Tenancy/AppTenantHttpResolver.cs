using Sketch7.Multitenancy.AspNet;

namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

public class AppTenantHttpResolver(
	IAppTenantRegistry tenantRegistry
) : ITenantHttpResolver<AppTenant>
{
	public Task<AppTenant?> Resolve(HttpContext httpContext)
		=> httpContext.Request.Headers.TryGetValue("X-SSV-Tenant", out var tenantValue)
			? Task.FromResult(tenantRegistry.GetOrDefault(tenantValue.ToString()))
			: Task.FromResult<AppTenant?>(null);
}