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
	public void ForTenant_RegistersKeyedAndProxyService()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.ForTenant("tenant-a", s => s.AddScoped<ITestService, TestServiceA>())
			.ForTenant("tenant-b", s => s.AddScoped<ITestService, TestServiceB>());

		// Act - build the provider
		var provider = services.BuildServiceProvider();

		// Assert - keyed services should be registered
		var serviceA = provider.GetRequiredKeyedService<ITestService>("tenant-a");
		var serviceB = provider.GetRequiredKeyedService<ITestService>("tenant-b");

		serviceA.ShouldBeOfType<TestServiceA>();
		serviceB.ShouldBeOfType<TestServiceB>();
	}

	[Fact]
	public void ForTenant_ProxyResolvesCorrectServiceForCurrentTenant()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.ForTenant("tenant-a", s => s.AddScoped<ITestService, TestServiceA>())
			.ForTenant("tenant-b", s => s.AddScoped<ITestService, TestServiceB>());

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var accessor = scope.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>();
		accessor.Tenant = new TestTenant { Key = "tenant-a" };

		// Act
		var service = scope.ServiceProvider.GetRequiredService<ITestService>();

		// Assert
		service.ShouldBeOfType<TestServiceA>();
	}

	[Fact]
	public void ForTenant_ProxyResolvesCorrectServiceForDifferentTenants()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.ForTenant("tenant-a", s => s.AddScoped<ITestService, TestServiceA>())
			.ForTenant("tenant-b", s => s.AddScoped<ITestService, TestServiceB>());

		var provider = services.BuildServiceProvider();

		// Simulate two separate requests with different tenants
		using var scopeA = provider.CreateScope();
		scopeA.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>().Tenant = new TestTenant { Key = "tenant-a" };

		using var scopeB = provider.CreateScope();
		scopeB.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>().Tenant = new TestTenant { Key = "tenant-b" };

		// Act
		var serviceA = scopeA.ServiceProvider.GetRequiredService<ITestService>();
		var serviceB = scopeB.ServiceProvider.GetRequiredService<ITestService>();

		// Assert
		serviceA.ShouldBeOfType<TestServiceA>();
		serviceB.ShouldBeOfType<TestServiceB>();
	}

	[Fact]
	public void Proxy_ThrowsWhenNoTenantIsSet()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.ForTenant("tenant-a", s => s.AddScoped<ITestService, TestServiceA>());

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		// Deliberately NOT setting tenant on accessor

		// Act & Assert
		Should.Throw<InvalidOperationException>(() =>
			scope.ServiceProvider.GetRequiredService<ITestService>());
	}

	[Fact]
	public void ForTenants_Predicate_RegistersMatchingTenants()
	{
		// Arrange
		var tenants = new[]
		{
			new TestTenant { Key = "riot-lol", Organization = "riot" },
			new TestTenant { Key = "blizzard-hots", Organization = "blizzard" },
			new TestTenant { Key = "riot-tft", Organization = "riot" },
		};

		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.WithTenants(tenants)
			.ForTenants(t => t.Organization == "riot",
				s => s.AddScoped<ITestService, TestServiceA>())
			.ForTenants(t => t.Organization == "blizzard",
				s => s.AddScoped<ITestService, TestServiceB>());

		var provider = services.BuildServiceProvider();

		// Assert - riot tenants get TestServiceA
		using var scopeLol = provider.CreateScope();
		scopeLol.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>().Tenant = tenants[0];
		scopeLol.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();

		// Assert - blizzard tenant gets TestServiceB
		using var scopeHots = provider.CreateScope();
		scopeHots.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>().Tenant = tenants[1];
		scopeHots.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceB>();

		// Assert - second riot tenant also gets TestServiceA
		using var scopeTft = provider.CreateScope();
		scopeTft.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>().Tenant = tenants[2];
		scopeTft.ServiceProvider.GetRequiredService<ITestService>().ShouldBeOfType<TestServiceA>();
	}

	[Fact]
	public void ForTenants_Predicate_ThrowsWhenNoTenantsProvided()
	{
		// Arrange
		var services = new ServiceCollection();
		var builder = services.AddMultitenancy<TestTenant>();

		// Act & Assert - no tenants set via WithTenants
		Should.Throw<InvalidOperationException>(() =>
			builder.ForTenants(t => true, s => s.AddScoped<ITestService, TestServiceA>()));
	}

	[Fact]
	public void WithRegistry_RegistersBothTypedAndInterfaceRegistry()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.WithRegistry<TestTenantRegistry>();

		var provider = services.BuildServiceProvider();

		// Act
		var typedRegistry = provider.GetRequiredService<TestTenantRegistry>();
		var interfaceRegistry = provider.GetRequiredService<ITenantRegistry<TestTenant>>();

		// Assert
		typedRegistry.ShouldNotBeNull();
		interfaceRegistry.ShouldNotBeNull();
		interfaceRegistry.ShouldBeSameAs(typedRegistry);
	}

	[Fact]
	public void WithRegistryInstance_ExposesTenantsForPredicateFiltering()
	{
		// Arrange
		var registry = new TestTenantRegistry();
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>()
			.WithRegistry(registry)
			.ForTenants(t => t.Organization == "riot",
				s => s.AddScoped<ITestService, TestServiceA>());

		var provider = services.BuildServiceProvider();

		// Act - riot tenant should get TestServiceA
		using var scope = provider.CreateScope();
		scope.ServiceProvider.GetRequiredService<TenantAccessor<TestTenant>>()
			.Tenant = registry.Get("lol");

		var service = scope.ServiceProvider.GetRequiredService<ITestService>();

		// Assert
		service.ShouldBeOfType<TestServiceA>();
	}
}

// ---- Test doubles ----

public class TestTenant : ITenant
{
	public string Key { get; set; } = "test";
	public string Organization { get; set; } = string.Empty;
}

public interface ITestService { }
public class TestServiceA : ITestService { }
public class TestServiceB : ITestService { }

public class TestTenantRegistry : ITenantRegistry<TestTenant>
{
	private readonly Dictionary<string, TestTenant> _tenants = new()
	{
		["lol"] = new TestTenant { Key = "lol", Organization = "riot" },
		["hots"] = new TestTenant { Key = "hots", Organization = "blizzard" },
	};

	public TestTenant Get(string key) => _tenants[key];
	public IEnumerable<TestTenant> GetAll() => _tenants.Values;
}
