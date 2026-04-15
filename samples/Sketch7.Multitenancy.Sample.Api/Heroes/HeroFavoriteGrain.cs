using Orleans.Concurrency;
using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Persistent state for <see cref="HeroFavoriteGrain"/>.</summary>
[GenerateSerializer]
public sealed class HeroFavoriteGrainState
{
	/// <summary>Gets or sets the list of favored hero keys for this tenant.</summary>
	[Id(0)]
	public List<string> FavoriteHeroKeys { get; set; } = [];
}


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


/// <summary>
/// Tenant-scoped grain that stores a list of favorite hero keys per tenant.
/// The grain key follows the convention <c>{tenantKey}/favorites</c>.
/// </summary>
/// <remarks>
/// Tenant context is injected by <see cref="TenantGrainActivator{TTenant}"/> at activation time —
/// no per-call inline scope management is needed.
/// </remarks>
public sealed class HeroFavoriteGrain : Grain, IHeroFavoriteGrain, IWithTenantAccessor<AppTenant>
{
	private readonly IPersistentState<HeroFavoriteGrainState> _state;

	/// <summary>Initializes a new instance of <see cref="HeroFavoriteGrain"/>.</summary>
	public HeroFavoriteGrain(
		[PersistentState("favorites", "heroes")]
		IPersistentState<HeroFavoriteGrainState> state
	)
	{
		_state = state;
	}

	/// <inheritdoc />
	public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

	/// <inheritdoc />
	public Task<string> GetTenantKeyAsync()
		=> Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));

	/// <inheritdoc />
	public Task<List<string>> GetFavoritesAsync()
		=> Task.FromResult(_state.State.FavoriteHeroKeys);

	/// <inheritdoc />
	public async Task AddFavoriteAsync(string heroKey)
	{
		if (_state.State.FavoriteHeroKeys.Contains(heroKey))
			return;

		_state.State.FavoriteHeroKeys.Add(heroKey);
		await _state.WriteStateAsync();
	}

	/// <inheritdoc />
	public async Task RemoveFavoriteAsync(string heroKey)
	{
		if (!_state.State.FavoriteHeroKeys.Remove(heroKey))
			return;

		await _state.WriteStateAsync();
	}
}
