namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Resolves a <typeparamref name="TTenant"/> from an Orleans grain primary key.
/// Implement and register this interface to customize tenant resolution strategy.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public interface ITenantOrleansResolver<TTenant>
	where TTenant : class, ITenant
{
	/// <summary>
	/// Resolves the tenant from an Orleans grain key span in the format <c>tenant/{tenantKey}/{grainId}</c>.
	/// </summary>
	/// <param name="grainKey">The Orleans grain key span.</param>
	/// <returns>The resolved tenant, or <c>null</c> if the key format is invalid.</returns>
	TTenant? Resolve(in IdSpan grainKey);
}
