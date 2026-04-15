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
	/// Resolves the tenant from a grain composite primary key in the format <c>{tenantKey}/{grainKey}</c>.
	/// </summary>
	/// <param name="primaryKey">The grain composite primary key.</param>
	/// <returns>The resolved tenant, or <c>null</c> if the key format is invalid.</returns>
	TTenant? Resolve(string primaryKey);
}
