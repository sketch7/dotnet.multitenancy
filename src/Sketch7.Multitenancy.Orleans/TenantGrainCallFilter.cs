using Microsoft.Extensions.Logging;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Orleans incoming grain call filter that propagates tenant context to
/// <see cref="ITenantAccessor{TTenant}"/> for tenant-aware grains.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public class TenantGrainCallFilter<TTenant> : IIncomingGrainCallFilter
	where TTenant : class, ITenant
{
	private readonly ITenantRegistry<TTenant> _registry;
	private readonly ILogger _logger;

	/// <summary>
	/// Initializes a new instance of <see cref="TenantGrainCallFilter{TTenant}"/>.
	/// </summary>
	public TenantGrainCallFilter(
		ITenantRegistry<TTenant> registry,
		ILogger<TenantGrainCallFilter<TTenant>> logger
	)
	{
		_registry = registry;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task Invoke(IIncomingGrainCallContext context)
	{
		SetTenantContext(context);
		await context.Invoke();
	}

	private void SetTenantContext(IIncomingGrainCallContext context)
	{
		if (context.Grain is not (ITenantGrain and IAddressable addressable))
			return;

		var primaryKey = addressable.GetPrimaryKeyString();

		if (!TenantGrainKey.TryParse(primaryKey, out var tenantKey, out _) || tenantKey == null)
		{
			_logger.LogWarning(
				"Grain {GrainType} has an invalid primary key format for tenant extraction: '{PrimaryKey}'.",
				context.Grain.GetType().Name,
				primaryKey
			);
			return;
		}

		var tenant = _registry.Get(tenantKey);
		if (tenant == null)
			_logger.LogWarning(
				"Grain {GrainType} has tenant key '{TenantKey}' which was not found in the registry.",
				context.Grain.GetType().Name,
				tenantKey
			);

		if (context.Grain is IWithTenantAccessor<TTenant> withTenantAccessor)
			withTenantAccessor.TenantAccessor.Tenant = tenant;
	}
}

/// <summary>
/// Opt-in interface for grains that want the <see cref="ITenantAccessor{TTenant}"/> populated by the call filter.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public interface IWithTenantAccessor<TTenant>
	where TTenant : class, ITenant
{
	/// <summary>
	/// Gets the tenant accessor that will be populated before each grain call.
	/// </summary>
	TenantAccessor<TTenant> TenantAccessor { get; }
}