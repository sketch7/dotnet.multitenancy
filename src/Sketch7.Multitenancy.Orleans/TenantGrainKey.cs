using System.Text;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Represents a parsed tenant-scoped grain key with zero-allocation span-based parsing.
/// Composite keys follow the format: <c>tenant/{tenantKey}/{grainId}</c>.
/// </summary>
public readonly record struct TenantGrainKey
{
	private const string Prefix = "tenant/";
	private const char Separator = '/';

	/// <summary>Gets the tenant key segment.</summary>
	public string TenantKey { get; init; }

	/// <summary>Gets the grain-specific identifier segment.</summary>
	public string GrainKey { get; init; }

	private TenantGrainKey(string tenantKey, string grainKey)
	{
		TenantKey = tenantKey;
		GrainKey = grainKey;
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
		return $"{Prefix}{tenantKey}{Separator}{grainKey}";
	}

	/// <summary>
	/// Returns the composite key string in the format <c>tenant/{TenantKey}/{GrainKey}</c>.
	/// </summary>
	public override string ToString()
		=> $"{Prefix}{TenantKey}{Separator}{GrainKey}";

	/// <summary>
	/// Parses a composite grain key, throwing <see cref="FormatException"/> on failure.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <exception cref="FormatException">Thrown when the key is not in the expected format.</exception>
	public static TenantGrainKey Parse(ReadOnlySpan<char> compositeKey)
		=> TryParse(compositeKey, out var result)
			? result
			: throw new FormatException(
				$"Invalid tenant grain key format '{compositeKey}'. Expected: 'tenant/{{tenantKey}}/{{grainId}}'.");

	/// <summary>
	/// Attempts to parse a composite grain key from a <see cref="ReadOnlySpan{Char}"/>.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <param name="result">The parsed <see cref="TenantGrainKey"/>, or <c>default</c> on failure.</param>
	/// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
	public static bool TryParse(ReadOnlySpan<char> compositeKey, out TenantGrainKey result)
	{
		var prefix = Prefix.AsSpan();
		if (!compositeKey.StartsWith(prefix))
		{
			result = default;
			return false;
		}

		var rest = compositeKey[prefix.Length..];
		var separatorIndex = rest.IndexOf(Separator);
		if (separatorIndex <= 0 || separatorIndex >= rest.Length - 1)
		{
			result = default;
			return false;
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
		var separatorIndex = rest.IndexOf((byte)Separator);
		if (separatorIndex <= 0 || separatorIndex >= rest.Length - 1)
		{
			result = default;
			return false;
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