using Microsoft.Extensions.DependencyInjection;

namespace Sketch7.Multitenancy;

/// <summary>
/// Fluent builder for configuring multitenancy services.
/// </summary>
/// <typeparam name="TTenant">The tenant type.</typeparam>
public class MultitenancyBuilder<TTenant>
	where TTenant : class, ITenant
{
	private readonly HashSet<Type> _registeredProxies = [];
	private IEnumerable<TTenant>? _tenants;

	/// <summary>
	/// Gets the underlying <see cref="IServiceCollection"/>.
	/// </summary>
	public IServiceCollection Services { get; }

	internal MultitenancyBuilder(IServiceCollection services)
	{
		Services = services;
	}

	/// <summary>
	/// Registers the tenant registry type.
	/// Use <see cref="WithRegistry{TRegistry}(TRegistry)"/> or <see cref="WithTenants(IEnumerable{TTenant})"/>
	/// to provide tenant enumeration for predicate-based configuration.
	/// </summary>
	/// <typeparam name="TRegistry">The registry implementation type.</typeparam>
	public MultitenancyBuilder<TTenant> WithRegistry<TRegistry>()
		where TRegistry : class, ITenantRegistry<TTenant>
	{
		Services.AddSingleton<TRegistry>();
		Services.AddSingleton<ITenantRegistry<TTenant>>(sp => sp.GetRequiredService<TRegistry>());
		return this;
	}

	/// <summary>
	/// Registers a pre-created registry instance and provides tenant enumeration for predicate-based configuration.
	/// </summary>
	/// <typeparam name="TRegistry">The registry type.</typeparam>
	/// <param name="registry">The registry instance.</param>
	public MultitenancyBuilder<TTenant> WithRegistry<TRegistry>(TRegistry registry)
		where TRegistry : class, ITenantRegistry<TTenant>
	{
		_tenants = registry.GetAll();
		Services.AddSingleton(registry);
		Services.AddSingleton<ITenantRegistry<TTenant>>(registry);
		return this;
	}

	/// <summary>
	/// Sets the tenant list directly for predicate-based per-tenant configuration.
	/// </summary>
	/// <param name="tenants">The tenants to enumerate.</param>
	public MultitenancyBuilder<TTenant> WithTenants(IEnumerable<TTenant> tenants)
	{
		_tenants = tenants;
		return this;
	}

	/// <summary>
	/// Groups per-tenant service registrations under a fluent <see cref="TenantServicesBuilder{TTenant}"/>.
	/// </summary>
	/// <param name="configure">Action that configures per-tenant service registrations.</param>
	public MultitenancyBuilder<TTenant> WithServices(Action<TenantServicesBuilder<TTenant>> configure)
	{
		var tsb = new TenantServicesBuilder<TTenant>(this);
		configure(tsb);
		return this;
	}

	/// <summary>
	/// Configures services for a specific tenant identified by key.
	/// </summary>
	/// <param name="key">The tenant key.</param>
	/// <param name="configure">Action to configure tenant-specific services.</param>
	internal MultitenancyBuilder<TTenant> ForTenant(string key, Action<IServiceCollection> configure)
	{
		var tenantServices = new ServiceCollection();
		configure(tenantServices);
		RegisterKeyedServices(key, tenantServices);
		return this;
	}

	/// <summary>
	/// Configures services for all tenants matching the given predicate.
	/// Requires tenants to be available via <see cref="WithRegistry{TRegistry}(TRegistry)"/> or <see cref="WithTenants"/>.
	/// </summary>
	/// <param name="predicate">Filter to select matching tenants.</param>
	/// <param name="configure">Action to configure services for matching tenants.</param>
	internal MultitenancyBuilder<TTenant> ForTenants(Func<TTenant, bool> predicate, Action<IServiceCollection> configure)
	{
		if (_tenants == null)
			throw new InvalidOperationException(
				"Tenants must be provided via WithRegistry(registry) or WithTenants(tenants) before using predicate-based ForTenants.");

		foreach (var tenant in _tenants.Where(predicate))
			ForTenant(tenant.Key, configure);

		return this;
	}

	/// <summary>
	/// Configures shared services applied to all tenants.
	/// </summary>
	/// <param name="configure">Action to configure services for all tenants.</param>
	internal MultitenancyBuilder<TTenant> ForAllTenants(Action<IServiceCollection> configure)
	{
		if (_tenants == null)
			throw new InvalidOperationException(
				"Tenants must be provided via WithRegistry(registry) or WithTenants(tenants) before using ForAllTenants.");

		foreach (var tenant in _tenants)
			ForTenant(tenant.Key, configure);

		return this;
	}

	private void RegisterKeyedServices(string tenantKey, IServiceCollection tenantServices)
	{
		foreach (var descriptor in tenantServices)
		{
			var keyedDescriptor = CreateKeyedDescriptor(descriptor, tenantKey);
			Services.Add(keyedDescriptor);

			// Register an unkeyed proxy that resolves the correct keyed service based on the current tenant.
			// Only register the proxy once per service type.
			if (_registeredProxies.Add(descriptor.ServiceType))
				RegisterProxy(descriptor.ServiceType, descriptor.Lifetime);
		}
	}

	private static ServiceDescriptor CreateKeyedDescriptor(ServiceDescriptor descriptor, string tenantKey)
	{
		if (descriptor.ImplementationType != null)
			return new(descriptor.ServiceType, tenantKey, descriptor.ImplementationType, descriptor.Lifetime);

		if (descriptor.ImplementationFactory != null)
			return new(descriptor.ServiceType, tenantKey,
				(sp, _) => descriptor.ImplementationFactory(sp), descriptor.Lifetime);

		if (descriptor.ImplementationInstance != null)
			return new(descriptor.ServiceType, tenantKey, descriptor.ImplementationInstance);

		throw new InvalidOperationException($"Cannot create keyed descriptor for service type '{descriptor.ServiceType}'.");
	}

	private void RegisterProxy(Type serviceType, ServiceLifetime lifetime)
	{
		// Proxy must be at most Scoped — even when the underlying service is Singleton —
		// because it reads ITenantAccessor which is inherently scope-bound.
		// The underlying keyed singleton is still resolved (and shared) correctly through the scoped proxy.
		var proxyLifetime = lifetime == ServiceLifetime.Singleton
			? ServiceLifetime.Scoped
			: lifetime;

		var proxyDescriptor = ServiceDescriptor.Describe(
			serviceType,
			sp =>
			{
				var accessor = sp.GetRequiredService<ITenantAccessor>();
				var tenantKey = accessor.Tenant?.Key
					?? throw new InvalidOperationException(
						$"No tenant is set in {nameof(ITenantAccessor)}. " +
						"Ensure multitenancy middleware runs before services are resolved.");
				return ((IKeyedServiceProvider)sp).GetRequiredKeyedService(serviceType, tenantKey);
			},
			proxyLifetime);

		Services.Add(proxyDescriptor);
	}
}