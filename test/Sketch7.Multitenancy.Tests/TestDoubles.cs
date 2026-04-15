namespace Sketch7.Multitenancy.Tests;

// ---- Shared test doubles ----

public record TestTenant : ITenant
{
	public string Key { get; init; } = "test";
	public string Organization { get; init; } = string.Empty;
}

public interface ITestService { }
public sealed class TestServiceA : ITestService { }
public sealed class TestServiceB : ITestService { }

public sealed class TestTenantRegistry : ITenantRegistry<TestTenant>
{
	private readonly Dictionary<string, TestTenant> _tenants = new()
	{
		["lol"] = new TestTenant { Key = "lol", Organization = "riot" },
		["hots"] = new TestTenant { Key = "hots", Organization = "blizzard" },
	};

	public TestTenant Get(string key) => _tenants[key];
	public IEnumerable<TestTenant> GetAll() => _tenants.Values;
}