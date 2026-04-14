using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Sketch7.Multitenancy;
using Sketch7.Multitenancy.Sample.Api.Tenancy;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Integration tests for the sample API demonstrating per-tenant service resolution.
/// </summary>
public class SampleApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public SampleApiIntegrationTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetTenant_Returns_LoL_Tenant_ForLolHeader()
	{
		// Arrange
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "lol");

		// Act
		var response = await client.GetAsync("/api/admin/tenant");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var tenant = await response.Content.ReadFromJsonAsync<AppTenant>();
		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe("lol");
		tenant.Organization.ShouldBe(OrganizationNames.Riot);
	}

	[Fact]
	public async Task GetTenant_Returns_HoTS_Tenant_ForHotsHeader()
	{
		// Arrange
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "hots");

		// Act
		var response = await client.GetAsync("/api/admin/tenant");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var tenant = await response.Content.ReadFromJsonAsync<AppTenant>();
		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe("hots");
		tenant.Organization.ShouldBe(OrganizationNames.Blizzard);
	}

	[Fact]
	public async Task GetTenant_Returns400_WhenNoTenantHeader()
	{
		// Arrange
		var client = _factory.CreateClient();
		// No X-SSV-Tenant header

		// Act
		var response = await client.GetAsync("/api/admin/tenant");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetHeroes_Returns_LoLHeroes_ForLolTenant()
	{
		// Arrange
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "lol");

		// Act
		var response = await client.GetAsync("/api/heroes");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var heroes = await response.Content.ReadFromJsonAsync<List<HeroDto>>();
		heroes.ShouldNotBeNull();
		heroes!.Count.ShouldBeGreaterThan(0);

		// LoL data has Rengar, Kha'zix, Singed
		heroes.ShouldContain(h => h.Key == "rengar");
	}

	[Fact]
	public async Task GetHeroes_Returns_HotsHeroes_ForHotsTenant()
	{
		// Arrange
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "hots");

		// Act
		var response = await client.GetAsync("/api/heroes");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var heroes = await response.Content.ReadFromJsonAsync<List<HeroDto>>();
		heroes.ShouldNotBeNull();
		heroes!.Count.ShouldBeGreaterThan(0);

		// HotS data has Maiev, Alexstrasza, etc.
		heroes.ShouldContain(h => h.Key == "maiev");
		heroes.ShouldNotContain(h => h.Key == "rengar");
	}

	private record HeroDto(string Key, string Name);
}
