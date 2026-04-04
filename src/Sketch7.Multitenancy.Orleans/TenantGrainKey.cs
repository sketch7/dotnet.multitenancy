namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Provides helpers for creating and parsing tenant-scoped grain keys.
/// Grain keys follow the convention: <c>{tenantKey}/{grainKey}</c>.
/// </summary>
public static class TenantGrainKey
{
	private const char Separator = '/';

	/// <summary>
	/// Creates a tenant-scoped grain key from a tenant key and a grain-specific key.
	/// </summary>
	/// <param name="tenantKey">The tenant key.</param>
	/// <param name="grainKey">The grain-specific identifier.</param>
	public static string Create(string tenantKey, string grainKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(tenantKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(grainKey);
		return $"{tenantKey}{Separator}{grainKey}";
	}

	/// <summary>
	/// Extracts the tenant key from a composite grain key.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <exception cref="FormatException">Thrown when the key is not in the expected format.</exception>
	public static string GetTenantKey(string compositeKey)
	{
		var separatorIndex = compositeKey.IndexOf(Separator);
		if (separatorIndex <= 0)
			throw new FormatException(
				$"Invalid tenant grain key format '{compositeKey}'. Expected format: '{{tenantKey}}/{{grainKey}}'.");
		return compositeKey[..separatorIndex];
	}

	/// <summary>
	/// Extracts the grain-specific key portion from a composite grain key.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <exception cref="FormatException">Thrown when the key is not in the expected format.</exception>
	public static string GetGrainKey(string compositeKey)
	{
		var separatorIndex = compositeKey.IndexOf(Separator);
		if (separatorIndex <= 0 || separatorIndex >= compositeKey.Length - 1)
			throw new FormatException(
				$"Invalid tenant grain key format '{compositeKey}'. Expected format: '{{tenantKey}}/{{grainKey}}'.");
		return compositeKey[(separatorIndex + 1)..];
	}

	/// <summary>
	/// Attempts to parse a composite grain key into its tenant and grain parts.
	/// </summary>
	/// <param name="compositeKey">The composite grain primary key.</param>
	/// <param name="tenantKey">The extracted tenant key, or <c>null</c> on failure.</param>
	/// <param name="grainKey">The extracted grain key, or <c>null</c> on failure.</param>
	/// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
	public static bool TryParse(string compositeKey, out string? tenantKey, out string? grainKey)
	{
		var separatorIndex = compositeKey.IndexOf(Separator);
		if (separatorIndex <= 0 || separatorIndex >= compositeKey.Length - 1)
		{
			tenantKey = null;
			grainKey = null;
			return false;
		}

		tenantKey = compositeKey[..separatorIndex];
		grainKey = compositeKey[(separatorIndex + 1)..];
		return true;
	}
}
