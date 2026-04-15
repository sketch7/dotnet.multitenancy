using Orleans.Concurrency;
using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Heroes;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api.HeroType;

/// <summary>Represents a hero type category (e.g. Assassin, Tank, Support).</summary>
[GenerateSerializer]
public record HeroType
{
	/// <summary>Gets the unique hero type key.</summary>
	[Id(0)]
	public string Key { get; init; } = null!;

	/// <summary>Gets the display name.</summary>
	[Id(1)]
	public string Name { get; init; } = null!;

	/// <summary>Gets the optional description.</summary>
	[Id(2)]
	public string? Description { get; init; }
}

/// <summary>Persistent state for <see cref="HeroTypeGrain"/>.</summary>
[GenerateSerializer]
public sealed class HeroTypeGrainState
{
	/// <summary>Gets or sets the cached hero type list for this tenant.</summary>
	[Id(0)]
	public List<HeroType> HeroTypes { get; set; } = [];
}

/// <summary>
/// Tenant-scoped grain that provides hero type data for a single tenant.
/// Primary key format: <c>{tenantKey}/hero-types</c> — see <see cref="TenantGrainKey"/>.
/// </summary>
public interface IHeroTypeGrain : IGrainWithStringKey, ITenantGrain
{
	/// <summary>Gets all hero types for this tenant.</summary>
	[AlwaysInterleave]
	[return: Immutable]
	Task<List<HeroType>> GetAllAsync();
}

/// <summary>
/// Tenant-scoped grain that acts as a read-through cache for hero type data.
/// The grain key follows the convention <c>{tenantKey}/hero-types</c>.
/// </summary>
/// <remarks>
/// On first activation the grain loads hero types from the per-tenant <see cref="IHeroDataClient"/>
/// (resolved via DI scope so the multitenancy proxy selects the correct implementation),
/// persists the result to the <c>heroes</c> storage provider, and serves it from memory on
/// subsequent calls.
/// </remarks>
public sealed class HeroTypeGrain : Grain, IHeroTypeGrain, IWithTenantAccessor<AppTenant>
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IPersistentState<HeroTypeGrainState> _state;

	/// <summary>Initializes a new instance of <see cref="HeroTypeGrain"/>.</summary>
	public HeroTypeGrain(
		IServiceScopeFactory scopeFactory,
		[PersistentState("hero-types", "heroes")]
		IPersistentState<HeroTypeGrainState> state
	)
	{
		_scopeFactory = scopeFactory;
		_state = state;
	}

	/// <inheritdoc />
	public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

	/// <inheritdoc />
	public async Task<List<HeroType>> GetAllAsync()
	{
		await EnsureHeroTypesAsync();
		return _state.State.HeroTypes;
	}

	private async Task EnsureHeroTypesAsync()
	{
		if (_state.State.HeroTypes.Count > 0)
			return;

		using var scope = _scopeFactory.CreateScope();

		// Propagate the tenant into the new scope so the IHeroDataClient proxy can pick the right keyed impl.
		var accessor = scope.ServiceProvider.GetRequiredService<TenantAccessor<AppTenant>>();
		accessor.Tenant = TenantAccessor.Tenant;

		var dataClient = scope.ServiceProvider.GetRequiredService<IHeroDataClient>();
		_state.State.HeroTypes = await dataClient.GetAllHeroTypes();
		await _state.WriteStateAsync();
	}
}
