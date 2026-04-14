using Orleans.Concurrency;
using Sketch7.Multitenancy.Orleans;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>
/// Tenant-scoped grain that provides hero data for a single tenant.
/// Primary key format: <c>{tenantKey}/heroes</c> — see <see cref="TenantGrainKey"/>.
/// </summary>
public interface IHeroGrain : IGrainWithStringKey, ITenantGrain
{
	/// <summary>Gets all heroes for this tenant.</summary>
	[AlwaysInterleave]
	[return: Immutable]
	Task<List<Hero>> GetAllAsync();

	/// <summary>Gets a hero by key, or <c>null</c> if not found.</summary>
	[AlwaysInterleave]
	[return: Immutable]
	Task<Hero?> GetByKeyAsync(string heroKey);
}