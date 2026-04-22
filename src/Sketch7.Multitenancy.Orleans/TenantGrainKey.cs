using System.Text;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Represents a parsed tenant-scoped grain key with span-based parsing.
/// Keys follow the format: <c>tenant/{tenantKey}/{grainId}</c> for grain-specific keys,
/// or <c>tenant/{tenantKey}</c> for tenant-only keys (one grain instance per tenant).
/// </summary>
public readonly record struct TenantGrainKey
{
	private const string Prefix = "tenant/";
	private const char Separator = '/';
	// ReadOnlySpan<char> property from a literal is stored in the PE's read-only section —
	// avoids dereferencing the heap string object on every call to TryParse(ReadOnlySpan<char>).
	private static ReadOnlySpan<char> PrefixSpan => "tenant/";

	/// <summary>Gets the tenant key segment.</summary>
	public string TenantKey { get; init; }

	/// <summary>Gets the grain-specific identifier segment. <see cref="string.Empty"/> for tenant-only keys.</summary>
	public string GrainKey { get; init; }

	/// <summary>Gets a value indicating whether this is a tenant-only key with no grain segment.</summary>
	public bool IsTenantOnly => GrainKey is { Length: 0 };

	private TenantGrainKey(string tenantKey, string grainKey)
	{
		TenantKey = tenantKey;
		GrainKey = grainKey;
	}

	/// <summary>
	/// Creates the tenant-only primary key string for a grain with one instance per tenant.
	/// </summary>
	/// <param name="tenantKey">The tenant key.</param>
	/// <returns>A key string in the format <c>tenant/{tenantKey}</c>.</returns>
	public static string Create(string tenantKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(tenantKey);
		return string.Concat(Prefix, tenantKey);
	}

	/// <summary>
	/// Creates the composite primary key string for a tenant-scoped grain.
	/// </summary>
	/// <param name="tenantKey">The tenant key.</param>
	/// <param name="grainKey">The grain-specific identifier.</param>
	/// <returns>A composite key string in the format <c>tenant/{tenantKey}/{grainId}</c>.</returns>
	public static string Create(string tenantKey, string grainKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(tenantKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(grainKey);
		return string.Concat(Prefix, tenantKey, "/", grainKey);
	}

	/// <summary>
	/// Returns the key string. For tenant-only keys: <c>tenant/{TenantKey}</c>.
	/// For grain-specific keys: <c>tenant/{TenantKey}/{GrainKey}</c>.
	/// </summary>
	public override string ToString()
		=> IsTenantOnly
			? string.Concat(Prefix, TenantKey)
			: string.Concat(Prefix, TenantKey, "/", GrainKey);

	/// <summary>
	/// Parses a composite grain key, throwing <see cref="FormatException"/> on failure.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <exception cref="FormatException">Thrown when the key is not in the expected format.</exception>
	public static TenantGrainKey Parse(ReadOnlySpan<char> compositeKey)
		=> TryParse(compositeKey, out var result)
			? result
			: throw new FormatException(
				$"Invalid tenant grain key format '{compositeKey}'. Expected: 'tenant/{{tenantKey}}' or 'tenant/{{tenantKey}}/{{grainId}}'.");

	/// <summary>
	/// Attempts to parse a composite grain key from a <see cref="ReadOnlySpan{Char}"/>.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <param name="result">The parsed <see cref="TenantGrainKey"/>, or <c>default</c> on failure.</param>
	/// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
	public static bool TryParse(ReadOnlySpan<char> compositeKey, out TenantGrainKey result)
	{
		if (!compositeKey.StartsWith(PrefixSpan))
		{
			result = default;
			return false;
		}

		var rest = compositeKey[Prefix.Length..];
		if (rest.IsEmpty)
		{
			result = default;
			return false;
		}

		var separatorIndex = rest.IndexOf(Separator);
		if (separatorIndex == 0 || separatorIndex == rest.Length - 1)
		{
			// Empty tenant segment (tenant//foo) or trailing slash (tenant/lol/) — invalid.
			result = default;
			return false;
		}

		if (separatorIndex < 0)
		{
			// No second separator — tenant-only key: tenant/{tenantKey}
			result = new(rest.ToString(), string.Empty);
			return true;
		}

		result = new(rest[..separatorIndex].ToString(), rest[(separatorIndex + 1)..].ToString());
		return true;
	}

	/// <summary>
	/// Attempts to parse a composite grain key from a <see cref="string"/>.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <param name="result">The parsed <see cref="TenantGrainKey"/>, or <c>default</c> on failure.</param>
	/// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
	public static bool TryParse(string compositeKey, out TenantGrainKey result)
		=> TryParse(compositeKey.AsSpan(), out result);

	/// <summary>
	/// Attempts to parse a composite grain key from a UTF-8 byte span (e.g. from <see cref="IdSpan.AsSpan"/>).
	/// Avoids string allocation when parsing directly from grain identity.
	/// </summary>
	/// <param name="utf8Key">The UTF-8 encoded composite grain primary key.</param>
	/// <param name="result">The parsed <see cref="TenantGrainKey"/>, or <c>default</c> on failure.</param>
	/// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
	public static bool TryParse(ReadOnlySpan<byte> utf8Key, out TenantGrainKey result)
	{
		ReadOnlySpan<byte> prefix = "tenant/"u8;
		if (!utf8Key.StartsWith(prefix))
		{
			result = default;
			return false;
		}

		var rest = utf8Key[prefix.Length..];
		if (rest.IsEmpty)
		{
			result = default;
			return false;
		}

		var separatorIndex = rest.IndexOf((byte)Separator);
		if (separatorIndex == 0 || separatorIndex == rest.Length - 1)
		{
			// Empty tenant segment or trailing slash — invalid.
			result = default;
			return false;
		}

		if (separatorIndex < 0)
		{
			// No second separator — tenant-only key: tenant/{tenantKey}
			result = new(Encoding.UTF8.GetString(rest), string.Empty);
			return true;
		}

		result = new(
			Encoding.UTF8.GetString(rest[..separatorIndex]),
			Encoding.UTF8.GetString(rest[(separatorIndex + 1)..])
		);
		return true;
	}

	/// <summary>
	/// Attempts to parse a composite grain key from an Orleans <see cref="IdSpan"/> without allocating a string.
	/// </summary>
	/// <param name="grainId">The Orleans grain key span.</param>
	/// <param name="result">The parsed <see cref="TenantGrainKey"/>, or <c>default</c> on failure.</param>
	/// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
	public static bool TryParse(in IdSpan grainId, out TenantGrainKey result)
		=> TryParse(grainId.AsSpan(), out result);

	/// <summary>
	/// Extracts the tenant key portion from a composite grain key.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <exception cref="FormatException">Thrown when the key is not in the expected format.</exception>
	public static string GetTenantKey(string compositeKey)
		=> Parse(compositeKey.AsSpan()).TenantKey;

	/// <summary>
	/// Extracts the grain-specific key portion from a composite grain key.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <exception cref="FormatException">Thrown when the key is not in the expected format.</exception>
	public static string GetGrainKey(string compositeKey)
		=> Parse(compositeKey.AsSpan()).GrainKey;
}