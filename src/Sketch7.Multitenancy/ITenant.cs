namespace Sketch7.Multitenancy
{
	/// <summary>
	/// Interface to mark as Tenant.
	/// </summary>
	public interface ITenant
	{
		/// <summary>
		/// Gets the tenant key.
		/// </summary>
		string Key { get; }
	}
}
