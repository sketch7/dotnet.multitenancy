using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;

namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Per-grain-type <see cref="IGrainActivator"/> that sets tenant context on
/// <see cref="IGrainContext.ActivationServices"/> <em>before</em> the grain constructor runs,
/// making constructor injection of tenant-aware services fully reliable.
/// </summary>
/// <remarks>
/// <para>
/// Supports two tenant injection patterns simultaneously:
/// <list type="bullet">
/// <item><description>
/// <b>Constructor injection</b> — tenant is set on <see cref="TenantAccessor{TTenant}"/> in
/// <see cref="IGrainContext.ActivationServices"/> before the grain instance is created, so tenant-aware
/// services injected via the constructor (e.g. resolved through the multitenancy proxy) already have the
/// tenant populated.
/// </description></item>
/// <item><description>
/// <b>Property accessor</b> — grains implementing <see cref="IWithTenantAccessor{TTenant}"/> also receive
/// the tenant via their <see cref="TenantAccessor{TTenant}"/> property immediately after construction.
/// </description></item>
/// </list>
/// </para>
/// One instance of this activator is created per grain type by <see cref="ConfigureTenantGrainActivator{TTenant}"/>.
/// </remarks>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class TenantGrainActivator<TTenant> : IGrainActivator
	where TTenant : class, ITenant
{
	private readonly ITenantOrleansResolver<TTenant> _resolver;
	private readonly ObjectFactory _factory;
	private readonly GrainConstructorArgumentFactory _argumentFactory;

	/// <summary>
	/// Initializes a new instance of <see cref="TenantGrainActivator{TTenant}"/> for the given grain class.
	/// Caches a compiled <see cref="ObjectFactory"/> for the grain type to avoid repeated reflection on hot paths.
	/// Uses <see cref="GrainConstructorArgumentFactory"/> to handle Orleans facet parameters (e.g.
	/// <see cref="PersistentStateAttribute"/>) the same way <see cref="DefaultGrainActivator"/> does.
	/// </summary>
	public TenantGrainActivator(
		IServiceProvider serviceProvider,
		ITenantOrleansResolver<TTenant> resolver,
		Type grainClass
	)
	{
		_resolver = resolver;
		_argumentFactory = new(serviceProvider, grainClass);
		_factory = ActivatorUtilities.CreateFactory(grainClass, _argumentFactory.ArgumentTypes);
	}

	/// <summary>
	/// Resolves the tenant from the grain key, sets it on <see cref="TenantAccessor{TTenant}"/>
	/// before construction, then creates the grain instance and populates
	/// <see cref="IWithTenantAccessor{TTenant}"/> if implemented.
	/// </summary>
	public object CreateInstance(IGrainContext context)
	{
		var tenant = _resolver.Resolve(context.GrainId.Key);

		// Set tenant BEFORE grain construction — guarantees constructor injection of tenant-aware services.
		if (context.ActivationServices.GetService<TenantAccessor<TTenant>>() is { } tenantAccessor)
			tenantAccessor.Tenant = tenant;

		// CreateArguments resolves Orleans facet parameters (e.g. IPersistentState<T> from [PersistentState]).
		// Regular DI parameters are resolved from context.ActivationServices by the ObjectFactory.
		var arguments = _argumentFactory.CreateArguments(context);
		var instance = _factory(context.ActivationServices, arguments);

		// Set on IWithTenantAccessor<TTenant> property accessor pattern, if implemented.
		if (instance is IWithTenantAccessor<TTenant> withTenantAccessor)
			withTenantAccessor.TenantAccessor.Tenant = tenant;

		return instance;
	}

	/// <inheritdoc/>
	public async ValueTask DisposeInstance(IGrainContext context, object instance)
	{
		switch (instance)
		{
			case IAsyncDisposable asyncDisposable:
				await asyncDisposable.DisposeAsync();
				break;
			case IDisposable disposable:
				disposable.Dispose();
				break;
		}
	}
}

/// <summary>
/// Registers <see cref="TenantGrainActivator{TTenant}"/> as the <see cref="IGrainActivator"/>
/// for every grain type implementing <see cref="ITenantGrain"/>.
/// Called once per grain type during silo startup via <see cref="IConfigureGrainTypeComponents"/>.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public sealed class ConfigureTenantGrainActivator<TTenant> : IConfigureGrainTypeComponents
	where TTenant : class, ITenant
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ITenantOrleansResolver<TTenant> _resolver;
	private readonly GrainClassMap _grainClassMap;

	/// <summary>
	/// Initializes a new instance of <see cref="ConfigureTenantGrainActivator{TTenant}"/>.
	/// </summary>
	public ConfigureTenantGrainActivator(
		IServiceProvider serviceProvider,
		ITenantOrleansResolver<TTenant> resolver,
		GrainClassMap grainClassMap
	)
	{
		_serviceProvider = serviceProvider;
		_resolver = resolver;
		_grainClassMap = grainClassMap;
	}

	/// <inheritdoc/>
	public void Configure(GrainType grainType, GrainProperties properties, GrainTypeSharedContext shared)
	{
		if (!_grainClassMap.TryGetGrainClass(grainType, out var grainClass))
			return;

		if (!typeof(ITenantGrain).IsAssignableFrom(grainClass))
			return;

		shared.SetComponent<IGrainActivator>(new TenantGrainActivator<TTenant>(_serviceProvider, _resolver, grainClass));
	}
}