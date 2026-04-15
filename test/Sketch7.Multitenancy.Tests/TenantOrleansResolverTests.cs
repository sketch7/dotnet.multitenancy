using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Sketch7.Multitenancy.Orleans;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Tests for <see cref="TenantOrleansResolver{TTenant}"/>.
/// </summary>
public class TenantOrleansResolverTests
{
	private static TenantOrleansResolver<TestTenant> BuildResolver()
		=> new(new TestTenantRegistry(), NullLogger<TenantOrleansResolver<TestTenant>>.Instance);

	[Theory]
	[InlineData("lol/heroes", "lol")]
	[InlineData("hots/heroes", "hots")]
	[InlineData("lol/favorites", "lol")]
	public void Resolve_ReturnsTenant_ForValidCompositeKey(string primaryKey, string expectedTenantKey)
	{
		var resolver = BuildResolver();

		var tenant = resolver.Resolve(primaryKey);

		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe(expectedTenantKey);
	}

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	public void Resolve_ReturnsNull_WhenPrimaryKeyFormatInvalid(string invalidKey)
	{
		var resolver = BuildResolver();

		var tenant = resolver.Resolve(invalidKey);

		tenant.ShouldBeNull();
	}

	[Fact]
	public void Resolve_ExtractsTenantKeyOnly_FromCompositeKey()
	{
		// Arrange
		var resolver = BuildResolver();

		// Act — ensure grain key portion does not bleed into tenant lookup
		var tenant = resolver.Resolve("lol/deeply/nested/grain");

		// Assert
		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe("lol");
	}

	[Fact]
	public void Resolve_PropagatesException_WhenTenantNotFoundInRegistry()
	{
		// Arrange
		var resolver = BuildResolver();

		// Act / Assert — registry.Get throws KeyNotFoundException for unknown tenants
		Should.Throw<KeyNotFoundException>(() => resolver.Resolve("unknown/grain"));
	}
}
