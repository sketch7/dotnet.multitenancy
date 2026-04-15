using Orleans.Metadata;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Configures tenant context on grain instances at activation time by implementing
/// <see cref="IConfigureGrainContextProvider"/> and <see cref="IConfigureGrainContext"/>.
/// Runs once per grain instance creation — unlike <see cref="TenantGrainCallFilter{TTenant}"/>
/// which runs on every incoming grain call.
/// </summary>
/// <remarks>
/// Grains must implement both <see cref="ITenantGrain"/> and <see cref="IWithTenantAccessor{TTenant}"/>
/// to receive tenant injection; all other grain types are skipped silently.
/// </remarks>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class TenantGrainActivator<TTenant> : IConfigureGrainContextProvider, IConfigureGrainContext
	where TTenant : class, ITenant
{
	private readonly ITenantOrleansResolver<TTenant> _resolver;

	/// <summary>
	/// Initializes a new instance of <see cref="TenantGrainActivator{TTenant}"/>.
	/// </summary>
	public TenantGrainActivator(ITenantOrleansResolver<TTenant> resolver)
	{
		_resolver = resolver;
	}

	/// <summary>
	/// Returns <see langword="this"/> as the configurator for all grain types.
	/// Filtering by grain type is deferred to <see cref="Configure"/> to avoid per-type metadata lookups.
	/// </summary>
	public bool TryGetConfigurator(GrainType grainType, GrainProperties properties, out IConfigureGrainContext configurator)
	{
		configurator = this;
		return true;
	}

	/// <summary>
	/// Subscribes to the grain lifecycle so that tenant context is injected after the
	/// grain instance has been created but before state is loaded or the grain activates.
	/// </summary>
	/// <remarks>
	/// <see cref="IConfigureGrainContext.Configure"/> is called before the grain instance
	/// object is created, so <see cref="IGrainContext.GrainInstance"/> is <see langword="null"/>
	/// at this point. Subscribing to <see cref="GrainLifecycleStage.SetupState"/> - 1 defers
	/// the work to after grain construction but before state is read from storage.
	/// </remarks>
	public void Configure(IGrainContext context)
	{
		context.ObservableLifecycle.Subscribe(
			nameof(TenantGrainActivator<>),
			GrainLifecycleStage.SetupState - 1,
			_ =>
			{
				if (context.GrainInstance is ITenantGrain and IWithTenantAccessor<TTenant> withTenantAccessor)
				{
					var tenant = _resolver.Resolve(context.GrainId.Key.ToString());
					if (tenant is not null)
						withTenantAccessor.TenantAccessor.Tenant = tenant;
				}
				return Task.CompletedTask;
			});
	}
}
