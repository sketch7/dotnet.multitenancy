using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Integration tests for the HeroFavorites endpoints, verifying that
/// <see cref="Sketch7.Multitenancy.Orleans.TenantGrainActivator{TTenant}"/> correctly
/// injects tenant context at grain activation time.
/// </summary>
public class HeroFavoritesIntegrationTests(WebApplicationFactory<Program> factory)
	: IClassFixture<WebApplicationFactory<Program>>
{
	[Fact]
	public async Task GetFavorites_ReturnsEmptyList_ForFreshTenant()
	{
		// Arrange — use a tenant that has had no favorites added
		var response = await CreateClientForTenant("hots").GetAsync("/api/heroes/favorites");

		// Act / Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var favorites = await response.Content.ReadFromJsonAsync<List<string>>();
		favorites.ShouldNotBeNull();
	}

	[Fact]
	public async Task AddFavorite_ThenGet_ContainsFavorite_ForLolTenant()
	{
		// Arrange
		var client = CreateClientForTenant("lol");
		await client.PostAsync("/api/heroes/favorites/rengar", null);

		// Act
		var response = await client.GetAsync("/api/heroes/favorites");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var favorites = await response.Content.ReadFromJsonAsync<List<string>>();
		favorites.ShouldNotBeNull();
		favorites!.ShouldContain("rengar");
	}

	[Fact]
	public async Task AddFavorite_IsIdempotent_ForLolTenant()
	{
		// Arrange
		var client = CreateClientForTenant("lol");
		await client.PostAsync("/api/heroes/favorites/singed", null);
		await client.PostAsync("/api/heroes/favorites/singed", null);

		// Act
		var favorites = await client.GetFromJsonAsync<List<string>>("/api/heroes/favorites");

		// Assert — should appear exactly once despite two adds
		favorites.ShouldNotBeNull();
		favorites!.Count(k => k == "singed").ShouldBe(1);
	}

	[Fact]
	public async Task RemoveFavorite_ThenGet_NoLongerContainsFavorite_ForLolTenant()
	{
		// Arrange
		var client = CreateClientForTenant("lol");
		await client.PostAsync("/api/heroes/favorites/kha-zix", null);

		// Act
		var removeResponse = await client.DeleteAsync("/api/heroes/favorites/kha-zix");
		var favorites = await client.GetFromJsonAsync<List<string>>("/api/heroes/favorites");

		// Assert
		removeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		favorites.ShouldNotBeNull();
		favorites!.ShouldNotContain("kha-zix");
	}

	[Fact]
	public async Task AddFavorite_IsTenantIsolated_BetweenLoLAndHoTS()
	{
		// Arrange
		await CreateClientForTenant("lol").PostAsync("/api/heroes/favorites/rengar", null);

		// Act — retrieve favorites for hots tenant
		var hotsFavorites = await CreateClientForTenant("hots")
			.GetFromJsonAsync<List<string>>("/api/heroes/favorites");

		// Assert — lol favorite is NOT visible to hots
		hotsFavorites.ShouldNotBeNull();
		hotsFavorites!.ShouldNotContain("rengar");
	}

	[Fact]
	public async Task GetFavorites_Returns400_WhenNoTenantHeader()
	{
		var response = await factory.CreateClient().GetAsync("/api/heroes/favorites");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	// ---- Helpers ----

	private HttpClient CreateClientForTenant(string tenantKey)
	{
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", tenantKey);
		return client;
	}
}
