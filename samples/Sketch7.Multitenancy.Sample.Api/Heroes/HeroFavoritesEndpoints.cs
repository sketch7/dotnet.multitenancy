using Microsoft.AspNetCore.Http.HttpResults;
using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Registers hero-favorites API endpoints.</summary>
public static class HeroFavoritesEndpoints
{
	extension(IEndpointRouteBuilder app)
	{
		/// <summary>Maps all hero-favorites endpoints under <c>/api/heroes/favorites</c>.</summary>
		public IEndpointRouteBuilder MapHeroFavoritesEndpoints()
		{
			var group = app.MapGroup("/api/heroes/favorites")
				.WithTags("Heroes");

			group.MapGet("/", async (IGrainFactory grainFactory, ITenantAccessor<AppTenant> tenantAccessor) =>
			{
				var grain = GetFavoritesGrain(grainFactory, tenantAccessor);
				var favorites = await grain.GetFavoritesAsync();
				return TypedResults.Ok(favorites);
			})
			.WithName("GetFavoriteHeroes")
			.WithSummary("Get favorite heroes")
			.WithDescription("Returns all favorite hero keys for the current tenant.");

			group.MapPost("/{heroKey}", async (
				string heroKey, IGrainFactory grainFactory, ITenantAccessor<AppTenant> tenantAccessor
			) =>
			{
				var grain = GetFavoritesGrain(grainFactory, tenantAccessor);
				await grain.AddFavoriteAsync(heroKey);
				return TypedResults.NoContent();
			})
			.WithName("AddFavoriteHero")
			.WithSummary("Add favorite hero")
			.WithDescription("Adds a hero to the favorites list for the current tenant. No-op if already present.");

			group.MapDelete("/{heroKey}", async (
				string heroKey, IGrainFactory grainFactory, ITenantAccessor<AppTenant> tenantAccessor
			) =>
			{
				var grain = GetFavoritesGrain(grainFactory, tenantAccessor);
				await grain.RemoveFavoriteAsync(heroKey);
				return TypedResults.NoContent();
			})
			.WithName("RemoveFavoriteHero")
			.WithSummary("Remove favorite hero")
			.WithDescription("Removes a hero from the favorites list for the current tenant. No-op if not present.");

			return app;
		}
	}

	private static IHeroFavoriteGrain GetFavoritesGrain(IGrainFactory grainFactory, ITenantAccessor<AppTenant> tenantAccessor)
	{
		// Tenant is guaranteed non-null here: middleware returns 400 before reaching this handler.
		var grainKey = TenantGrainKey.Create(tenantAccessor.Tenant!.Key, "favorites");
		return grainFactory.GetGrain<IHeroFavoriteGrain>(grainKey);
	}
}
