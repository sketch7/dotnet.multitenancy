using Shouldly;
using Sketch7.Multitenancy.Orleans;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Tests for <see cref="TenantGrainKey"/> utility.
/// </summary>
public class TenantGrainKeyTests
{
	[Theory]
	[InlineData("lol", "grain-1", "lol/grain-1")]
	[InlineData("hots", "abc123", "hots/abc123")]
	[InlineData("tenant-x", "some/nested/path", "tenant-x/some/nested/path")]
	public void Create_ReturnsExpectedCompositeKey(string tenantKey, string grainKey, string expected)
		=> TenantGrainKey.Create(tenantKey, grainKey).ShouldBe(expected);

	[Theory]
	[InlineData("lol/grain-1", "lol")]
	[InlineData("hots/abc123", "hots")]
	[InlineData("tenant-x/some/path", "tenant-x")]
	public void GetTenantKey_ReturnsTenantPortion(string compositeKey, string expectedTenantKey)
		=> TenantGrainKey.GetTenantKey(compositeKey).ShouldBe(expectedTenantKey);

	[Theory]
	[InlineData("lol/grain-1", "grain-1")]
	[InlineData("hots/abc123", "abc123")]
	[InlineData("tenant-x/some/path", "some/path")]
	public void GetGrainKey_ReturnsGrainPortion(string compositeKey, string expectedGrainKey)
		=> TenantGrainKey.GetGrainKey(compositeKey).ShouldBe(expectedGrainKey);

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	public void GetTenantKey_ThrowsFormatException_WhenInvalid(string invalidKey)
		=> Should.Throw<FormatException>(() => TenantGrainKey.GetTenantKey(invalidKey));

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	public void GetGrainKey_ThrowsFormatException_WhenInvalid(string invalidKey)
		=> Should.Throw<FormatException>(() => TenantGrainKey.GetGrainKey(invalidKey));

	[Fact]
	public void TryParse_ReturnsTrue_ForValidKey()
	{
		var result = TenantGrainKey.TryParse("lol/grain-1", out var tenantKey, out var grainKey);

		result.ShouldBeTrue();
		tenantKey.ShouldBe("lol");
		grainKey.ShouldBe("grain-1");
	}

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	public void TryParse_ReturnsFalse_ForInvalidKey(string invalidKey)
	{
		var result = TenantGrainKey.TryParse(invalidKey, out var tenantKey, out var grainKey);

		result.ShouldBeFalse();
		tenantKey.ShouldBeNull();
		grainKey.ShouldBeNull();
	}

	[Fact]
	public void Create_ThrowsArgumentException_WhenTenantKeyIsEmpty()
		=> Should.Throw<ArgumentException>(() => TenantGrainKey.Create("", "grain-1"));

	[Fact]
	public void Create_ThrowsArgumentException_WhenGrainKeyIsEmpty()
		=> Should.Throw<ArgumentException>(() => TenantGrainKey.Create("lol", ""));
}