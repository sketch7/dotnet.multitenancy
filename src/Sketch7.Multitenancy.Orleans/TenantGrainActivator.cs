using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Configures tenant context on grain instances at activation time by implementing
/// <see cref="IConfigureGrainContextProvider"/> and <see cref="IConfigureGrainContext"/>.
/// Runs once per grain instance creation.
/// </summary>
/// <remarks>
/// <para>
/// Supports two tenant injection patterns simultaneously:
/// <list type="bullet">
/// <item><description>
/// <b>Constructor injection</b> — tenant is set synchronously on <see cref="TenantAccessor{TTenant}"/> in
/// <see cref="IGrainContext.ActivationServices"/> before the grain instance is created, so tenant-aware
/// services injected via the constructor (e.g. resolved through the multitenancy proxy) already have the
/// tenant populated.
/// </description></item>
/// <item><description>
/// <b>Property accessor</b> — grains implementing <see cref="IWithTenantAccessor{TTenant}"/> also receive
/// the tenant via their <see cref="TenantAccessor{TTenant}"/> property, set in a lifecycle callback after
/// the grain instance is constructed but before state is loaded.
/// </description></item>
/// </list>
/// </para>
/// All other grain types are skipped silently.
/// </remarks>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class TenantGrainActivator<TTenant> : IConfigureGrainContextProvider, IConfigureGrainContext
	where TTenant : class, ITenant
{
	private readonly ITenantOrleansResolver<TTenant> _resolver;
	private readonly GrainClassMap _grainClassMap;

	/// <summary>
	/// Initializes a new instance of <see cref="TenantGrainActivator{TTenant}"/>.
	/// </summary>
	public TenantGrainActivator(
		ITenantOrleansResolver<TTenant> resolver,
		GrainClassMap grainClassMap
	)
	{
		_resolver = resolver;
		_grainClassMap = grainClassMap;
	}

	/// <summary>
	/// Returns <see langword="this"/> as the configurator only for grain types that implement <see cref="ITenantGrain"/>.
	/// <see cref="GrainClassMap"/> resolves the CLR type from the Orleans grain type registry, avoiding assembly scanning.
	/// </summary>
	public bool TryGetConfigurator(GrainType grainType, GrainProperties properties, out IConfigureGrainContext configurator)
	{
		if (!_grainClassMap.TryGetGrainClass(grainType, out var grainClass)
			|| !typeof(ITenantGrain).IsAssignableFrom(grainClass))
		{
			configurator = null!;
			return false;
		}

		configurator = this;
		return true;
	}

	/// <summary>
	/// Sets tenant context via two mechanisms:
	/// first synchronously in <see cref="IGrainContext.ActivationServices"/> to support constructor injection,
	/// then via a lifecycle subscription to set <see cref="IWithTenantAccessor{TTenant}.TenantAccessor"/>
	/// after grain construction for the property accessor pattern.
	/// </summary>
	public void Configure(IGrainContext context)
	{
		var tenant = _resolver.Resolve(context.GrainId.Key);

		// Set tenant in ActivationServices before grain construction — enables constructor injection.
		if (context.ActivationServices.GetService<TenantAccessor<TTenant>>() is { } tenantAccessor)
			tenantAccessor.Tenant = tenant;

		// Set on IWithTenantAccessor<TTenant> after grain construction — property accessor pattern.
		context.ObservableLifecycle.Subscribe(
			nameof(TenantGrainActivator<>),
			GrainLifecycleStage.SetupState - 1,
			_ =>
			{
				if (context.GrainInstance is IWithTenantAccessor<TTenant> withTenantAccessor)
					withTenantAccessor.TenantAccessor.Tenant = tenant;
				return Task.CompletedTask;
			});
	}
}
