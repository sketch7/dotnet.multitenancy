namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Marks an Orleans grain as tenant-aware.
/// The grain primary key is expected to be prefixed with the tenant key in the format <c>{tenantKey}/{grainKey}</c>.
/// Implementations should expose the tenant key via a grain method or extract it from the primary key.
/// </summary>
public interface ITenantGrain : IGrain
{
	/// <summary>
	/// Gets the tenant key for this grain instance.
	/// </summary>
	/// <returns>The tenant key.</returns>
	Task<string> GetTenantKeyAsync();
}
