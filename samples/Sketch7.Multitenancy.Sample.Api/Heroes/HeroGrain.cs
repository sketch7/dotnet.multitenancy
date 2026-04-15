using Orleans.Concurrency;
using Sketch7.Multitenancy.Orleans;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>
/// Persistent state for <see cref="HeroGrain"/>, stored in the <c>heroes</c> grain storage provider.
/// </summary>
[GenerateSerializer]
public sealed class HeroGrainState
{
	/// <summary>Gets or sets the cached hero list for this tenant.</summary>
	[Id(0)]
	public List<Hero> Heroes { get; set; } = [];
}

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

/// <summary>
/// Tenant-scoped grain that acts as a read-through cache for hero data.
/// The grain key follows the convention <c>{tenantKey}/heroes</c>.
/// </summary>
/// <remarks>
/// On first activation the grain loads heroes from the per-tenant <see cref="IHeroDataClient"/>,
/// which is injected via constructor and resolved through the multitenancy proxy using
/// <see cref="TenantGrainActivator{TTenant}"/> — no scope factory needed.
/// </remarks>
public sealed class HeroGrain : Grain, IHeroGrain
{
	private readonly IHeroDataClient _heroDataClient;
	private readonly IPersistentState<HeroGrainState> _state;

	/// <summary>Initializes a new instance of <see cref="HeroGrain"/>.</summary>
	public HeroGrain(
		IHeroDataClient heroDataClient,
		[PersistentState("heroes", "heroes")]
		IPersistentState<HeroGrainState> state
	)
	{
		_heroDataClient = heroDataClient;
		_state = state;
	}

	/// <inheritdoc />
	public async Task<List<Hero>> GetAllAsync()
	{
		await EnsureHeroesAsync();
		return _state.State.Heroes;
	}

	/// <inheritdoc />
	public async Task<Hero?> GetByKeyAsync(string heroKey)
	{
		await EnsureHeroesAsync();
		return _state.State.Heroes.Find(h => h.Key == heroKey);
	}

	private async Task EnsureHeroesAsync()
	{
		if (_state.State.Heroes.Count > 0)
			return;

		_state.State.Heroes = await _heroDataClient.GetAll();
		await _state.WriteStateAsync();
	}
}