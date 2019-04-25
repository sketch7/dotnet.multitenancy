using System.Collections.Generic;

namespace Sketch7.Multitenancy
{
	/// <summary>
	/// Tenants registry interface.
	/// </summary>
	/// <typeparam name="TTenant"></typeparam>
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
}