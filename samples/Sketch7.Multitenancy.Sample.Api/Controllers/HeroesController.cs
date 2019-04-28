using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sketch7.Multitenancy.Sample.Api.Heroes;

namespace Sketch7.Multitenancy.Sample.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class HeroesController : Controller
	{
		private readonly IHeroDataClient _client;

		public HeroesController(IHeroDataClient client)
		{
			_client = client;
		}

		// GET api/heroes
		[HttpGet]
		public Task<List<Hero>> GetAll() => _client.GetAll();

		// GET api/heroes/{key}
		[HttpGet("{key}")]
		public Task<Hero> GetByKey(string key) => _client.GetByKey(key);
	}
}
