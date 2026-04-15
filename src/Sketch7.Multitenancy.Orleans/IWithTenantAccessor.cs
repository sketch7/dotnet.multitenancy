namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Opt-in interface for grains that want <see cref="ITenantAccessor{TTenant}"/> populated by
/// <see cref="TenantGrainActivator{TTenant}"/> after grain construction.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public interface IWithTenantAccessor<TTenant>
	where TTenant : class, ITenant
{
	/// <summary>
	/// Gets the tenant accessor that will be populated once the grain instance is created.
	/// </summary>
	TenantAccessor<TTenant> TenantAccessor { get; }
}
