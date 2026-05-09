using Microsoft.AspNetCore.Http.HttpResults;
using Sketch7.Multitenancy.Orleans;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Registers hero-related API endpoints.</summary>
public static class HeroesEndpoints
{
	extension(IEndpointRouteBuilder app)
	{
		/// <summary>Maps all hero endpoints under <c>/api/heroes</c>.</summary>
		public IEndpointRouteBuilder MapHeroesEndpoints()
		{
			var group = app.MapGroup("/api/heroes")
				.WithTags("Heroes");

			group.MapGet("/", async (IGrainFactory grainFactory, ITenantAccessor tenantAccessor) =>
			{
				var grain = GetHeroGrain(grainFactory, tenantAccessor);
				var heroes = await grain.GetAllAsync();
				return TypedResults.Ok(heroes);
			})
			.WithName("GetAllHeroes")
			.WithSummary("Get all heroes")
			.WithDescription("Returns all heroes for the current tenant.");

			group.MapGet("/{key}", async Task<Results<Ok<Hero?>, NotFound>> (
				string key, IGrainFactory grainFactory, ITenantAccessor tenantAccessor
			) =>
			{
				var grain = GetHeroGrain(grainFactory, tenantAccessor);
				var hero = await grain.GetByKeyAsync(key);
				return hero is null
					? TypedResults.NotFound()
					: TypedResults.Ok<Hero?>(hero);
			})
			.WithName("GetHeroByKey")
			.WithSummary("Get hero by key")
			.WithDescription("Returns a single hero by its key for the current tenant.");

			return app;
		}
	}

	private static IHeroGrain GetHeroGrain(IGrainFactory grainFactory, ITenantAccessor tenantAccessor)
	{
		// Tenant is guaranteed non-null here: middleware returns 400 before reaching this handler.
		var grainKey = TenantGrainKey.Create(tenantAccessor.Tenant!.Key, "heroes");
		return grainFactory.GetGrain<IHeroGrain>(grainKey);
	}
}