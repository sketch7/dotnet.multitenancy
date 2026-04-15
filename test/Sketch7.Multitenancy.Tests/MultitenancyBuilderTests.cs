using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Tests for <see cref="MultitenancyBuilder{TTenant}"/> with keyed service registration.
/// </summary>
public class MultitenancyBuilderTests
{
	[Fact]
	public void WithServices_For_RegistersKeyedAndProxyService()
	{
		var provider = BuildTwoTenantProvider();

		provider.GetRequiredKeyedService<ITestService>("tenant-a").ShouldBeOfType<TestServiceA>();
		provider.GetRequiredKeyedService<ITestService>("tenant-b").ShouldBeOfType<TestServiceB>();
	}

	[Fact]
	public void WithServices_For_ProxyResolvesCorrectServiceForCurrentTenant()
	{
		using var scope = BuildTwoTenantProvider().CreateScopeForTenant(new TestTenant { Key = "tenant-a" });

		scope.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
	}

	[Fact]
	public void WithServices_For_ProxyResolvesCorrectServiceForDifferentTenants()
	{
		var provider = BuildTwoTenantProvider();

		using var scopeA = provider.CreateScopeForTenant(new TestTenant { Key = "tenant-a" });
		using var scopeB = provider.CreateScopeForTenant(new TestTenant { Key = "tenant-b" });

		scopeA.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
		scopeB.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceB>();
	}

	[Fact]
	public void WithServices_For_SingletonTenantServicesAreSupported()
	{
		var provider = BuildProvider(opts => opts
			.WithServices(tsb => tsb
				.For("tenant-a", s => s.AddSingleton<ITestService, TestServiceA>())
				.For("tenant-b", s => s.AddSingleton<ITestService, TestServiceB>())));

		using var scopeA1 = provider.CreateScopeForTenant(new TestTenant { Key = "tenant-a" });
		using var scopeA2 = provider.CreateScopeForTenant(new TestTenant { Key = "tenant-a" });
		using var scopeB = provider.CreateScopeForTenant(new TestTenant { Key = "tenant-b" });

		var serviceA1 = scopeA1.ServiceProvider.GetRequiredService<ITestService>();
		var serviceA2 = scopeA2.ServiceProvider.GetRequiredService<ITestService>();

		serviceA1.ShouldBeOfType<TestServiceA>();
		scopeB.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceB>();
		serviceA1.ShouldBeSameAs(serviceA2); // singleton: same instance across scopes
	}

	[Fact]
	public void Proxy_ThrowsWhenNoTenantIsSet()
	{
		var provider = BuildProvider(opts => opts
			.WithServices(tsb => tsb
				.For("tenant-a", s => s.AddScoped<ITestService, TestServiceA>())));

		using var scope = provider.CreateScope();

		Should.Throw<InvalidOperationException>(() =>
			scope.ServiceProvider.GetRequiredService<ITestService>());
	}

	[Fact]
	public void WithServices_ForPredicate_RegistersMatchingTenants()
	{
		var tenants = new[]
		{
			new TestTenant { Key = "riot-lol",      Organization = "riot" },
			new TestTenant { Key = "blizzard-hots", Organization = "blizzard" },
			new TestTenant { Key = "riot-tft",      Organization = "riot" },
		};

		var provider = BuildProvider(opts => opts
			.WithTenants(tenants)
			.WithServices(tsb => tsb
				.For(t => t.Organization == "riot", s => s.AddScoped<ITestService, TestServiceA>())
				.For(t => t.Organization == "blizzard", s => s.AddScoped<ITestService, TestServiceB>())));

		using var scopeLol = provider.CreateScopeForTenant(tenants[0]);
		using var scopeHots = provider.CreateScopeForTenant(tenants[1]);
		using var scopeTft = provider.CreateScopeForTenant(tenants[2]);

		scopeLol.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
		scopeHots.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceB>();
		scopeTft.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
	}

	[Fact]
	public void WithServices_ForPredicate_ThrowsWhenNoTenantsProvided()
	{
		var services = new ServiceCollection();
		var builder = services.AddMultitenancy<TestTenant>();

		Should.Throw<InvalidOperationException>(() =>
			builder.WithServices(tsb => tsb.For(t => true, s => s.AddScoped<ITestService, TestServiceA>())));
	}

	[Fact]
	public void WithServices_ForAll_RegistersServicesForAllTenants()
	{
		var tenants = new[]
		{
			new TestTenant { Key = "tenant-a", Organization = "org-a" },
			new TestTenant { Key = "tenant-b", Organization = "org-b" },
		};

		var provider = BuildProvider(opts => opts
			.WithTenants(tenants)
			.WithServices(tsb => tsb.ForAll(s => s.AddScoped<ITestService, TestServiceA>())));

		using var scopeA = provider.CreateScopeForTenant(tenants[0]);
		using var scopeB = provider.CreateScopeForTenant(tenants[1]);

		scopeA.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
		scopeB.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
	}

	[Fact]
	public void WithRegistry_RegistersBothTypedAndInterfaceRegistry()
	{
		var provider = BuildProvider(opts => opts.WithRegistry<TestTenantRegistry>());

		var typedRegistry = provider.GetRequiredService<TestTenantRegistry>();
		var interfaceRegistry = provider.GetRequiredService<ITenantRegistry<TestTenant>>();

		typedRegistry.ShouldNotBeNull();
		interfaceRegistry.ShouldBeSameAs(typedRegistry);
	}

	[Fact]
	public void WithRegistryInstance_ExposesTenantsForPredicateFiltering()
	{
		var registry = new TestTenantRegistry();
		var provider = BuildProvider(opts => opts
			.WithRegistry(registry)
			.WithServices(tsb => tsb
				.For(t => t.Organization == "riot", s => s.AddScoped<ITestService, TestServiceA>())));

		using var scope = provider.CreateScopeForTenant(registry.Get("lol"));

		scope.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
	}

	// ---- Helpers ----

	private static ServiceProvider BuildProvider(Action<MultitenancyBuilder<TestTenant>>? configure = null)
	{
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>(configure);
		return services.BuildServiceProvider();
	}

	private static ServiceProvider BuildTwoTenantProvider()
		=> BuildProvider(opts => opts
			.WithServices(tsb => tsb
				.For("tenant-a", s => s.AddScoped<ITestService, TestServiceA>())
				.For("tenant-b", s => s.AddScoped<ITestService, TestServiceB>())));
}