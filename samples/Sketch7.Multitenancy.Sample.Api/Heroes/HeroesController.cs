using Microsoft.AspNetCore.Mvc;
using Sketch7.Multitenancy;
using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

[Route("api/[controller]")]
[ApiController]
public class HeroesController : ControllerBase
{
	private readonly IGrainFactory _grainFactory;
	private readonly ITenantAccessor<AppTenant> _tenantAccessor;

	public HeroesController(IGrainFactory grainFactory, ITenantAccessor<AppTenant> tenantAccessor)
	{
		_grainFactory = grainFactory;
		_tenantAccessor = tenantAccessor;
	}

	// GET api/heroes
	[HttpGet]
	public Task<List<Hero>> GetAll()
	{
		var grain = GetHeroGrain();
		return grain.GetAllAsync();
	}

	// GET api/heroes/{key}
	[HttpGet("{key}")]
	public Task<Hero?> GetByKey(string key)
	{
		var grain = GetHeroGrain();
		return grain.GetByKeyAsync(key);
	}

	private IHeroGrain GetHeroGrain()
	{
		// Tenant is guaranteed non-null here: middleware returns 400 before reaching this action.
		var grainKey = TenantGrainKey.Create(_tenantAccessor.Tenant!.Key, "heroes");
		return _grainFactory.GetGrain<IHeroGrain>(grainKey);
	}
}
