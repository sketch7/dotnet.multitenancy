using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Sketch7.Multitenancy.Orleans;
using System.Text;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Tests for <see cref="TenantOrleansResolver{TTenant}"/>.
/// </summary>
public class TenantOrleansResolverTests
{
	private static TenantOrleansResolver<TestTenant> BuildResolver()
		=> new(new TestTenantRegistry(), NullLogger<TenantOrleansResolver<TestTenant>>.Instance);

	private static IdSpan ToIdSpan(string key) => new(Encoding.UTF8.GetBytes(key));

	[Theory]
	[InlineData("tenant/lol/heroes", "lol")]
	[InlineData("tenant/hots/heroes", "hots")]
	[InlineData("tenant/lol/favorites", "lol")]
	public void Resolve_ReturnsTenant_ForValidCompositeKey(string primaryKey, string expectedTenantKey)
	{
		var resolver = BuildResolver();

		var tenant = resolver.Resolve(ToIdSpan(primaryKey));

		tenant.ShouldNotBeNull();
		tenant!.Key.ShouldBe(expectedTenantKey);
	}

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	[InlineData("lol/grain-1")]
	public void Resolve_ReturnsNull_WhenPrimaryKeyFormatInvalid(string invalidKey)
	{
		var resolver = BuildResolver();

		var tenant = resolver.Resolve(ToIdSpan(invalidKey));

		tenant.ShouldBeNull();
	}

	[Fact]
	public void Resolve_ExtractsTenantKeyOnly_FromCompositeKey()
	{
		// Arrange
		var resolver = BuildResolver();

		// Act — ensure grain key portion does not bleed into tenant lookup
		var tenant = resolver.Resolve(ToIdSpan("tenant/lol/deeply/nested/grain"));

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
		Should.Throw<KeyNotFoundException>(() => resolver.Resolve(ToIdSpan("tenant/unknown/grain")));
	}
}