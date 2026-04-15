using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Sketch7.Multitenancy.Sample.Api.Tenancy;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Integration tests for the sample API demonstrating per-tenant service resolution.
/// </summary>
public class SampleApiIntegrationTests(WebApplicationFactory<Program> factory)
	: IClassFixture<WebApplicationFactory<Program>>
{
	[Fact]
	public async Task GetTenant_Returns_LoL_Tenant_ForLolHeader()
	{
		var response = await CreateClientForTenant("lol").GetAsync("/api/tenants/current");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var tenant = await response.Content.ReadFromJsonAsync<AppTenant>();
		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe("lol");
		tenant.Organization.ShouldBe(OrganizationNames.Riot);
	}

	[Fact]
	public async Task GetTenant_Returns_HoTS_Tenant_ForHotsHeader()
	{
		var response = await CreateClientForTenant("hots").GetAsync("/api/tenants/current");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var tenant = await response.Content.ReadFromJsonAsync<AppTenant>();
		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe("hots");
		tenant.Organization.ShouldBe(OrganizationNames.Blizzard);
	}

	[Fact]
	public async Task GetTenant_Returns400_WhenNoTenantHeader()
	{
		var response = await factory.CreateClient().GetAsync("/api/tenants/current");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetHeroes_Returns_LoLHeroes_ForLolTenant()
	{
		var response = await CreateClientForTenant("lol").GetAsync("/api/heroes");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var heroes = await response.Content.ReadFromJsonAsync<List<HeroDto>>();
		heroes.ShouldNotBeNull();
		heroes!.Count.ShouldBeGreaterThan(0);
		heroes.ShouldContain(h => h.Key == "rengar");
	}

	[Fact]
	public async Task GetHeroes_Returns_HotsHeroes_ForHotsTenant()
	{
		var response = await CreateClientForTenant("hots").GetAsync("/api/heroes");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var heroes = await response.Content.ReadFromJsonAsync<List<HeroDto>>();
		heroes.ShouldNotBeNull();
		heroes!.Count.ShouldBeGreaterThan(0);
		heroes.ShouldContain(h => h.Key == "maiev");
		heroes.ShouldNotContain(h => h.Key == "rengar");
	}

	// ---- Helpers ----

	private HttpClient CreateClientForTenant(string tenantKey)
	{
		var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", tenantKey);
		return client;
	}

	private record HeroDto(string Key, string Name);
}