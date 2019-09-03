using Microsoft.AspNetCore.Http;
using Sketch7.Multitenancy.Grace.AspNet;
using System.Threading.Tasks;

namespace Sketch7.Multitenancy.Sample.Api.Tenancy
{
	public class AppTenantHttpResolver : ITenantHttpResolver<AppTenant>
	{
		private readonly IAppTenantRegistry _tenantRegistry;

		public AppTenantHttpResolver(IAppTenantRegistry tenantRegistry)
		{
			_tenantRegistry = tenantRegistry;
		}

		public Task<AppTenant> Resolve(HttpContext httpContext)
			// todo: move to lib - configurable Header + Allow from route + domain resolving
			=> httpContext.Request.Headers.TryGetValue("X-SSV-Tenant", out var tenantValue)
				? Task.FromResult(_tenantRegistry.GetOrDefault(tenantValue))
				: Task.FromResult<AppTenant>(null);
	}
}
