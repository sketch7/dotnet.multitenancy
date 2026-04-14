using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Tenant;

public static class TenantEndpoints
{
	public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
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
