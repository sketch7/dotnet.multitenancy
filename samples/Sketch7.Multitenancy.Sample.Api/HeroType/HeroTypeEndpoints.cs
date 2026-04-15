using Sketch7.Multitenancy.Orleans;

namespace Sketch7.Multitenancy.Sample.Api.HeroType;

/// <summary>Registers hero-type API endpoints.</summary>
public static class HeroTypeEndpoints
{
	extension(IEndpointRouteBuilder app)
	{
		/// <summary>Maps all hero-type endpoints under <c>/api/hero-types</c>.</summary>
		public IEndpointRouteBuilder MapHeroTypeEndpoints()
		{
			var group = app.MapGroup("/api/hero-types")
				.WithTags("HeroTypes");

			group.MapGet("/", async (IGrainFactory grainFactory, ITenantAccessor tenantAccessor) =>
			{
				var grain = GetHeroTypeGrain(grainFactory, tenantAccessor);
				var heroTypes = await grain.GetAllAsync();
				return TypedResults.Ok(heroTypes);
			})
			.WithName("GetAllHeroTypes")
			.WithSummary("Get all hero types")
			.WithDescription("Returns all hero types for the current tenant.");

			return app;
		}
	}

	private static IHeroTypeGrain GetHeroTypeGrain(IGrainFactory grainFactory, ITenantAccessor tenantAccessor)
	{
		// Tenant is guaranteed non-null here: middleware returns 400 before reaching this handler.
		var grainKey = TenantGrainKey.Create(tenantAccessor.Tenant!.Key, "hero-types");
		return grainFactory.GetGrain<IHeroTypeGrain>(grainKey);
	}
}