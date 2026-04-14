using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Tenant;

/// <summary>Registers tenant-related API endpoints.</summary>
public static class TenantEndpoints
{
	extension(IEndpointRouteBuilder app)
	{
		/// <summary>Maps all tenant endpoints under <c>/api/tenants</c>.</summary>
		public IEndpointRouteBuilder MapTenantEndpoints()
		{
			var group = app.MapGroup("/api/tenants")
				.WithTags("Tenants");

			group.MapGet("/current", (ITenantAccessor<AppTenant> tenantAccessor) =>
				TypedResults.Ok(tenantAccessor.Tenant))
				.WithName("GetCurrentTenant")
				.WithSummary("Get current tenant")
				.WithDescription("Returns the tenant resolved from the current request.");

			return app;
		}
	}
}