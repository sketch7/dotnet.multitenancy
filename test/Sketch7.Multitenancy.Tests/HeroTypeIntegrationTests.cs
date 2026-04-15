using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Integration tests for the HeroType endpoints, verifying that per-tenant
/// <see cref="Sketch7.Multitenancy.Sample.Api.Heroes.IHeroDataClient"/> data is served
/// correctly through the grain cache.
/// </summary>
public class HeroTypeIntegrationTests(WebApplicationFactory<Program> factory)
	: IClassFixture<WebApplicationFactory<Program>>
{
	[Fact]
	public async Task GetHeroTypes_ReturnsOk_WithTypes_ForLolTenant()
	{
		var response = await CreateClientForTenant("lol").GetAsync("/api/hero-types");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var types = await response.Content.ReadFromJsonAsync<List<HeroTypeDto>>();
		types.ShouldNotBeNull();
		types!.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetHeroTypes_ReturnsLolTypes_ForLolTenant()
	{
		var response = await CreateClientForTenant("lol").GetAsync("/api/hero-types");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var types = await response.Content.ReadFromJsonAsync<List<HeroTypeDto>>();
		types.ShouldNotBeNull();
		// LoL mock data includes "assassin" type key
		types!.ShouldContain(t => t.Key == "assassin");
	}

	[Fact]
	public async Task GetHeroTypes_ReturnsHotsTypes_ForHotsTenant()
	{
		var response = await CreateClientForTenant("hots").GetAsync("/api/hero-types");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var types = await response.Content.ReadFromJsonAsync<List<HeroTypeDto>>();
		types.ShouldNotBeNull();
		// HotS mock data includes "melee-assassin" type key
		types!.ShouldContain(t => t.Key == "melee-assassin");
		types.ShouldNotContain(t => t.Key == "assassin");
	}

	[Fact]
	public async Task GetHeroTypes_IsTenantIsolated_BetweenLoLAndHoTS()
	{
		var lolTypes = await CreateClientForTenant("lol")
			.GetFromJsonAsync<List<HeroTypeDto>>("/api/hero-types");
		var hotsTypes = await CreateClientForTenant("hots")
			.GetFromJsonAsync<List<HeroTypeDto>>("/api/hero-types");

		lolTypes.ShouldNotBeNull();
		hotsTypes.ShouldNotBeNull();

		// LoL-only type is not present in HoTS
		lolTypes!.ShouldContain(t => t.Key == "assassin");
		hotsTypes!.ShouldNotContain(t => t.Key == "assassin");

		// HoTS-only type is not present in LoL
		hotsTypes.ShouldContain(t => t.Key == "melee-assassin");
		lolTypes.ShouldNotContain(t => t.Key == "melee-assassin");
	}

	[Fact]
	public async Task GetHeroTypes_Returns400_WhenNoTenantHeader()
	{
		var response = await factory.CreateClient().GetAsync("/api/hero-types");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	// ---- Helpers ----

	private HttpClient CreateClientForTenant(string tenantKey)
	{
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", tenantKey);
		return client;
	}

	private record HeroTypeDto(string Key, string Name, string? Description);
}
