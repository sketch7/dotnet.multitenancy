using Microsoft.Extensions.Logging;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Default <see cref="ITenantOrleansResolver{TTenant}"/> that parses the composite grain key
/// and delegates to <see cref="ITenantRegistry{TTenant}"/> for tenant lookup.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class TenantOrleansResolver<TTenant> : ITenantOrleansResolver<TTenant>
	where TTenant : class, ITenant
{
	private readonly ITenantRegistry<TTenant> _registry;
	private readonly ILogger _logger;

	/// <summary>
	/// Initializes a new instance of <see cref="TenantOrleansResolver{TTenant}"/>.
	/// </summary>
	public TenantOrleansResolver(
		ITenantRegistry<TTenant> registry,
		ILogger<TenantOrleansResolver<TTenant>> logger
	)
	{
		_registry = registry;
		_logger = logger;
	}

	/// <inheritdoc />
	public TTenant? Resolve(in IdSpan grainKey)
	{
		if (!TenantGrainKey.TryParse(grainKey, out var key))
		{
			_logger.LogWarning("Could not extract tenant key from grain key '{GrainKey}'.", grainKey);
			return null;
		}

		return _registry.Get(key.TenantKey);
	}
}
