namespace Sketch7.Multitenancy;

/// <summary>
/// Provides non-generic access to the current tenant in the active scope.
/// </summary>
public interface ITenantAccessor
{
	/// <summary>
	/// Gets the current tenant, or <c>null</c> if not resolved yet.
	/// </summary>
	ITenant? Tenant { get; }
}

/// <summary>
/// Provides typed access to the current tenant in the active scope.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public interface ITenantAccessor<TTenant> : ITenantAccessor
	where TTenant : class, ITenant
{
	/// <summary>
	/// Gets the current tenant, or <c>null</c> if not resolved yet.
	/// </summary>
	new TTenant? Tenant { get; }
}

/// <summary>
/// Default mutable implementation of <see cref="ITenantAccessor{TTenant}"/>.
/// Set by multitenancy middleware during request processing.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public class TenantAccessor<TTenant> : ITenantAccessor<TTenant>
	where TTenant : class, ITenant
{
	/// <inheritdoc />
	public TTenant? Tenant { get; set; }

	ITenant? ITenantAccessor.Tenant => Tenant;
}