using Orleans.Concurrency;
using Sketch7.Multitenancy.Orleans;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>
/// Tenant-scoped grain that persists a list of favorite hero keys per tenant.
/// Primary key format: <c>{tenantKey}/favorites</c> — see <see cref="TenantGrainKey"/>.
/// </summary>
public interface IHeroFavoriteGrain : IGrainWithStringKey, ITenantGrain
{
	/// <summary>Gets all favorite hero keys for this tenant.</summary>
	[AlwaysInterleave]
	[return: Immutable]
	Task<List<string>> GetFavoritesAsync();

	/// <summary>Adds a hero key to the favorites list. No-op if already present.</summary>
	Task AddFavoriteAsync(string heroKey);

	/// <summary>Removes a hero key from the favorites list. No-op if not present.</summary>
	Task RemoveFavoriteAsync(string heroKey);
}
