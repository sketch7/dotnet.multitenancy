using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sketch7.Multitenancy.E2ETests;

/// <summary>
/// Shared Aspire fixture: starts the full distributed application (Redis + sample-api)
/// once per test class and tears it down afterwards.
/// Requires Docker to be running.
/// </summary>
public sealed class AspireFixture : IAsyncLifetime
{
	private DistributedApplication? _app;

	public DistributedApplication App => _app!;

	public async Task InitializeAsync()
	{
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.Sketch7_Multitenancy_AppHost>();

		_app = await appHost.BuildAsync();

		var resourceNotifications = _app.Services
			.GetRequiredService<ResourceNotificationService>();

		await _app.StartAsync();

		// Wait until the API is ready to serve requests
		await resourceNotifications
			.WaitForResourceAsync("sample-api", KnownResourceStates.Running)
			.WaitAsync(TimeSpan.FromSeconds(90));
	}

	public async Task DisposeAsync()
	{
		if (_app is not null)
			await _app.DisposeAsync();
	}
}

/// <summary>
/// End-to-end tests covering the full Request → API → HeroGrain → IHeroDataClient flow
/// with Redis-backed Orleans clustering and grain storage provisioned by Aspire.
/// </summary>
/// <remarks>Requires Docker. Excluded from the standard test run — use <c>dotnet test test/Sketch7.Multitenancy.E2ETests/</c>.</remarks>
[Trait("Category", "e2e")]
public class HeroGrainE2ETests : IClassFixture<AspireFixture>
{
	private readonly DistributedApplication _app;

	public HeroGrainE2ETests(AspireFixture fixture)
	{
		_app = fixture.App;
	}

	[Fact]
	public async Task GetHeroes_LoLTenant_ReturnsLoLHeroes()
	{
		using var client = _app.CreateHttpClient("sample-api");
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "lol");

		var heroes = await client.GetFromJsonAsync<List<HeroDto>>("/api/heroes");

		heroes.ShouldNotBeNull();
		heroes.ShouldContain(h => h.Key == "rengar");
		heroes.ShouldContain(h => h.Key == "singed");
		heroes.ShouldNotContain(h => h.Key == "maiev");
	}

	[Fact]
	public async Task GetHeroes_HotsTenant_ReturnsHotsHeroes()
	{
		using var client = _app.CreateHttpClient("sample-api");
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "hots");

		var heroes = await client.GetFromJsonAsync<List<HeroDto>>("/api/heroes");

		heroes.ShouldNotBeNull();
		heroes.ShouldContain(h => h.Key == "maiev");
		heroes.ShouldContain(h => h.Key == "malthael");
		heroes.ShouldNotContain(h => h.Key == "rengar");
	}

	[Fact]
	public async Task GetHeroByKey_LoLTenant_ReturnsCorrectHero()
	{
		using var client = _app.CreateHttpClient("sample-api");
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "lol");

		var hero = await client.GetFromJsonAsync<HeroDto>("/api/heroes/rengar");

		hero.ShouldNotBeNull();
		hero!.Key.ShouldBe("rengar");
		hero.Name.ShouldBe("Rengar");
	}

	[Fact]
	public async Task GetTenant_LoLTenant_ReturnsTenantInfo()
	{
		using var client = _app.CreateHttpClient("sample-api");
		client.DefaultRequestHeaders.Add("X-SSV-Tenant", "lol");

		var response = await client.GetAsync("/api/tenants/current");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe("lol");
	}

	[Fact]
	public async Task GetHeroes_NoTenantHeader_Returns400()
	{
		using var client = _app.CreateHttpClient("sample-api");
		// No X-SSV-Tenant header — multitenancy middleware should reject

		var response = await client.GetAsync("/api/heroes");

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	private record HeroDto(string Key, string Name);

	private record TenantDto(string Key, string Name);
}