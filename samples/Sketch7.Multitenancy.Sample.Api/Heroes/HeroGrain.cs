using Orleans.Providers;
using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.Heroes;

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
[StorageProvider(ProviderName = "heroes")]
public sealed class HeroGrain : Grain<HeroGrainState>, IHeroGrain, IHasTenantAccessor<AppTenant>
{
	private readonly IServiceScopeFactory _scopeFactory;

	/// <summary>Initializes a new instance of <see cref="HeroGrain"/>.</summary>
	public HeroGrain(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
	}

	/// <inheritdoc />
	public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

	/// <inheritdoc />
	public Task<string> GetTenantKeyAsync() =>
		Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));

	/// <inheritdoc />
	public async Task<List<Hero>> GetAllAsync()
	{
		await EnsureHeroesAsync();
		return State.Heroes;
	}

	/// <inheritdoc />
	public async Task<Hero?> GetByKeyAsync(string heroKey)
	{
		await EnsureHeroesAsync();
		return State.Heroes.Find(h => h.Key == heroKey);
	}

	/// <summary>
	/// Lazily populates <see cref="HeroGrainState.Heroes"/> if the state is empty.
	/// Creates a new DI scope, sets the scoped tenant accessor to the current tenant
	/// (which was injected by <see cref="TenantGrainCallFilter{TTenant}"/>), then resolves
	/// the per-tenant <see cref="IHeroDataClient"/> proxy and fetches all heroes.
	/// </summary>
	private async Task EnsureHeroesAsync()
	{
		if (State.Heroes.Count > 0)
			return;

		using var scope = _scopeFactory.CreateScope();

		// Propagate the tenant into the new scope so the IHeroDataClient proxy can pick the right keyed impl.
		var accessor = scope.ServiceProvider.GetRequiredService<TenantAccessor<AppTenant>>();
		accessor.Tenant = TenantAccessor.Tenant;

		var dataClient = scope.ServiceProvider.GetRequiredService<IHeroDataClient>();
		State.Heroes = await dataClient.GetAll();
		await WriteStateAsync();
	}
}