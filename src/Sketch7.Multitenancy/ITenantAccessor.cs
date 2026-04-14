namespace Sketch7.Multitenancy;

/// <summary>
/// Provides access to the current tenant in the active scope.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public interface ITenantAccessor<TTenant>
	where TTenant : class, ITenant
{
	/// <summary>
	/// Gets the current tenant, or <c>null</c> if not resolved yet.
	/// </summary>
	TTenant? Tenant { get; }
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
}