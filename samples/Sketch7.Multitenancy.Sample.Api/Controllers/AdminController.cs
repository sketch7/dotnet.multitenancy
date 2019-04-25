using Microsoft.AspNetCore.Mvc;

namespace Sketch7.Multitenancy.Sample.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AdminController : Controller
	{
		private readonly ITenant _tenant;

		public AdminController(ITenant tenant)
		{
			_tenant = tenant;
		}

		// GET api/admin/tenant
		[HttpGet("tenant")]
		public ITenant GetTenant() => _tenant;
	}
}
