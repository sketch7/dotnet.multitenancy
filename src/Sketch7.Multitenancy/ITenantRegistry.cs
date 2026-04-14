namespace Sketch7.Multitenancy;

/// <summary>
/// Tenants registry interface.
/// </summary>
/// <typeparam name="TTenant">Tenant type.</typeparam>
public interface ITenantRegistry<out TTenant>
	where TTenant : class, ITenant
{
	/// <summary>
	/// Get tenant by key.
	/// </summary>
	/// <param name="key">Key to get tenant.</param>
	TTenant Get(string key);

	/// <summary>
	/// Gets all available tenants.
	/// </summary>
	IEnumerable<TTenant> GetAll();
}