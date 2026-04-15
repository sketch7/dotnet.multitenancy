namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Orleans incoming grain call filter that propagates tenant context to
/// <see cref="ITenantAccessor{TTenant}"/> for tenant-aware grains.
/// Runs on every incoming grain call. For one-time activation-time injection, use
/// <see cref="TenantGrainActivator{TTenant}"/> instead.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public class TenantGrainCallFilter<TTenant> : IIncomingGrainCallFilter
	where TTenant : class, ITenant
{
	private readonly ITenantOrleansResolver<TTenant> _resolver;

	/// <summary>
	/// Initializes a new instance of <see cref="TenantGrainCallFilter{TTenant}"/>.
	/// </summary>
	public TenantGrainCallFilter(ITenantOrleansResolver<TTenant> resolver)
	{
		_resolver = resolver;
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

		var tenant = _resolver.Resolve(addressable.GetPrimaryKeyString());
		if (tenant is null)
			return;

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