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
	[InlineData("lol", "grain-1", "tenant/lol/grain-1")]
	[InlineData("hots", "abc123", "tenant/hots/abc123")]
	[InlineData("tenant-x", "some/nested/path", "tenant/tenant-x/some/nested/path")]
	public void Create_ReturnsExpectedCompositeKey(string tenantKey, string grainKey, string expected)
		=> TenantGrainKey.Create(tenantKey, grainKey).ShouldBe(expected);

	[Theory]
	[InlineData("lol", "tenant/lol")]
	[InlineData("hots", "tenant/hots")]
	public void Create_TenantOnly_ReturnsExpectedKey(string tenantKey, string expected)
		=> TenantGrainKey.Create(tenantKey).ShouldBe(expected);

	[Fact]
	public void Create_TenantOnly_ThrowsArgumentException_WhenTenantKeyIsEmpty()
		=> Should.Throw<ArgumentException>(() => TenantGrainKey.Create(""));

	[Theory]
	[InlineData("tenant/lol/grain-1", "lol")]
	[InlineData("tenant/hots/abc123", "hots")]
	[InlineData("tenant/tenant-x/some/path", "tenant-x")]
	[InlineData("tenant/lol", "lol")]
	public void GetTenantKey_ReturnsTenantPortion(string compositeKey, string expectedTenantKey)
		=> TenantGrainKey.GetTenantKey(compositeKey).ShouldBe(expectedTenantKey);

	[Theory]
	[InlineData("tenant/lol/grain-1", "grain-1")]
	[InlineData("tenant/hots/abc123", "abc123")]
	[InlineData("tenant/tenant-x/some/path", "some/path")]
	public void GetGrainKey_ReturnsGrainPortion(string compositeKey, string expectedGrainKey)
		=> TenantGrainKey.GetGrainKey(compositeKey).ShouldBe(expectedGrainKey);

	[Fact]
	public void GetGrainKey_ReturnsEmptyString_ForTenantOnlyKey()
		=> TenantGrainKey.GetGrainKey("tenant/lol").ShouldBe(string.Empty);

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	[InlineData("lol/grain-1")]
	[InlineData("tenant/")]
	[InlineData("tenant//")]
	public void GetTenantKey_ThrowsFormatException_WhenInvalid(string invalidKey)
		=> Should.Throw<FormatException>(() => TenantGrainKey.GetTenantKey(invalidKey));

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	[InlineData("lol/grain-1")]
	[InlineData("tenant/")]
	[InlineData("tenant//")]
	public void GetGrainKey_ThrowsFormatException_WhenInvalid(string invalidKey)
		=> Should.Throw<FormatException>(() => TenantGrainKey.GetGrainKey(invalidKey));

	[Fact]
	public void TryParse_ReturnsTrue_ForValidKey()
	{
		var result = TenantGrainKey.TryParse("tenant/lol/grain-1", out var parsed);

		result.ShouldBeTrue();
		parsed.TenantKey.ShouldBe("lol");
		parsed.GrainKey.ShouldBe("grain-1");
	}

	[Fact]
	public void TryParse_Span_ReturnsTrue_ForValidKey()
	{
		var result = TenantGrainKey.TryParse("tenant/lol/grain-1".AsSpan(), out var parsed);

		result.ShouldBeTrue();
		parsed.TenantKey.ShouldBe("lol");
		parsed.GrainKey.ShouldBe("grain-1");
	}

	[Fact]
	public void TryParse_ReturnsTrue_ForTenantOnlyKey()
	{
		var result = TenantGrainKey.TryParse("tenant/lol", out var parsed);

		result.ShouldBeTrue();
		parsed.TenantKey.ShouldBe("lol");
		parsed.GrainKey.ShouldBe(string.Empty);
		parsed.IsTenantOnly.ShouldBeTrue();
	}

	[Fact]
	public void TryParse_Span_ReturnsTrue_ForTenantOnlyKey()
	{
		var result = TenantGrainKey.TryParse("tenant/lol".AsSpan(), out var parsed);

		result.ShouldBeTrue();
		parsed.TenantKey.ShouldBe("lol");
		parsed.GrainKey.ShouldBe(string.Empty);
		parsed.IsTenantOnly.ShouldBeTrue();
	}

	[Fact]
	public void TryParse_Utf8_ReturnsTrue_ForTenantOnlyKey()
	{
		var result = TenantGrainKey.TryParse("tenant/lol"u8, out var parsed);

		result.ShouldBeTrue();
		parsed.TenantKey.ShouldBe("lol");
		parsed.GrainKey.ShouldBe(string.Empty);
		parsed.IsTenantOnly.ShouldBeTrue();
	}

	[Theory]
	[InlineData("no-separator")]
	[InlineData("")]
	[InlineData("/")]
	[InlineData("lol/grain-1")]
	[InlineData("tenant/")]
	[InlineData("tenant//")]
	public void TryParse_ReturnsFalse_ForInvalidKey(string invalidKey)
	{
		var result = TenantGrainKey.TryParse(invalidKey, out var parsed);

		result.ShouldBeFalse();
		parsed.ShouldBe(default);
	}

	[Fact]
	public void ToString_ProducesCompositeKey()
	{
		TenantGrainKey.TryParse("tenant/lol/heroes", out var parsed);
		parsed.ToString().ShouldBe("tenant/lol/heroes");
	}

	[Fact]
	public void ToString_TenantOnly_ProducesKeyWithoutGrainSegment()
	{
		TenantGrainKey.TryParse("tenant/lol", out var parsed);
		parsed.ToString().ShouldBe("tenant/lol");
	}

	[Fact]
	public void IsTenantOnly_ReturnsTrue_ForTenantOnlyKey()
	{
		TenantGrainKey.TryParse("tenant/lol", out var parsed);
		parsed.IsTenantOnly.ShouldBeTrue();
	}

	[Fact]
	public void IsTenantOnly_ReturnsFalse_ForKeyWithGrainSegment()
	{
		TenantGrainKey.TryParse("tenant/lol/heroes", out var parsed);
		parsed.IsTenantOnly.ShouldBeFalse();
	}

	[Fact]
	public void Create_ThrowsArgumentException_WhenTenantKeyIsEmpty()
		=> Should.Throw<ArgumentException>(() => TenantGrainKey.Create("", "grain-1"));

	[Fact]
	public void Create_ThrowsArgumentException_WhenGrainKeyIsEmpty()
		=> Should.Throw<ArgumentException>(() => TenantGrainKey.Create("lol", ""));
}