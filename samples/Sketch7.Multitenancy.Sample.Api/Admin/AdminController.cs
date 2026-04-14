using Microsoft.AspNetCore.Mvc;
using Sketch7.Multitenancy;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Admin;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
	private readonly ITenantAccessor<AppTenant> _tenantAccessor;

	public AdminController(ITenantAccessor<AppTenant> tenantAccessor)
	{
		_tenantAccessor = tenantAccessor;
	}

	// GET api/admin/tenant
	[HttpGet("tenant")]
	public AppTenant? GetTenant() => _tenantAccessor.Tenant;
}
