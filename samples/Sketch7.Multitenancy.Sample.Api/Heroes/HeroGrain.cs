using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

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
/// Tenant-scoped grain that acts as a read-through cache for hero data.
/// The grain key follows the convention <c>{tenantKey}/heroes</c>.
/// </summary>
/// <remarks>
/// On first activation the grain loads heroes from the per-tenant <see cref="IHeroDataClient"/>
/// (resolved via DI scope so the multitenancy proxy selects the correct implementation),
/// persists the result to the <c>heroes</c> storage provider, and serves it from memory on
/// subsequent calls.
/// </remarks>
public sealed class HeroGrain : Grain, IHeroGrain, IWithTenantAccessor<AppTenant>
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IPersistentState<HeroGrainState> _state;

	/// <summary>Initializes a new instance of <see cref="HeroGrain"/>.</summary>
	public HeroGrain(
		IServiceScopeFactory scopeFactory,
		[PersistentState("heroes", "heroes")]
		IPersistentState<HeroGrainState> state
	)
	{
		_scopeFactory = scopeFactory;
		_state = state;
	}

	/// <inheritdoc />
	public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

	/// <inheritdoc />
	public Task<string> GetTenantKeyAsync()
		=> Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));

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

	/// <summary>
	/// Lazily populates <see cref="HeroGrainState.Heroes"/> if the state is empty.
	/// Creates a new DI scope, sets the scoped tenant accessor to the current tenant
	/// (which was injected by <see cref="TenantGrainCallFilter{TTenant}"/>), then resolves
	/// the per-tenant <see cref="IHeroDataClient"/> proxy and fetches all heroes.
	/// </summary>
	private async Task EnsureHeroesAsync()
	{
		if (_state.State.Heroes.Count > 0)
			return;

		using var scope = _scopeFactory.CreateScope();

		// Propagate the tenant into the new scope so the IHeroDataClient proxy can pick the right keyed impl.
		var accessor = scope.ServiceProvider.GetRequiredService<TenantAccessor<AppTenant>>();
		accessor.Tenant = TenantAccessor.Tenant;

		var dataClient = scope.ServiceProvider.GetRequiredService<IHeroDataClient>();
		_state.State.Heroes = await dataClient.GetAll();
		await _state.WriteStateAsync();
	}
}