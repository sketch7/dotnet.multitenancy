using Grace.Data;
using Microsoft.Extensions.DependencyInjection;
using Sketch7.Multitenancy;
using Sketch7.Multitenancy.Grace;
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Grace.DependencyInjection
{
	public static class GraceExtensions
	{
		private const string TENANT_KEY = "tenant";

		public static ITenant GetTenantContext(this IExtraDataContainer scopeData)
			=> (ITenant)scopeData.GetExtraData(TENANT_KEY);

		public static TTenant GetTenantContext<TTenant>(this IExtraDataContainer scopeData) where TTenant : ITenant
			=> (TTenant)scopeData.GetTenantContext();

		public static ITenant SetTenantContext(this IExtraDataContainer scopeData, ITenant tenant)
			=> (ITenant)scopeData.SetExtraData(TENANT_KEY, tenant);
	}

	public static class GraceContainerExtensions
	{
		/// <summary>
		/// Locate a service for tenant specified.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="locator"></param>
		/// <param name="tenant">Tenant to get service for.</param>
		public static T LocateForTenant<T>(this ILocatorService locator, ITenant tenant)
			=> locator.Locate<T>(
				withKey: tenant.Key,
				extraData: new { tenant }
			);

		// todo: ask Grace to expose?
		public static IFluentExportStrategyConfiguration ConfigureLifetime(this IFluentExportStrategyConfiguration configuration, ServiceLifetime lifetime, bool asPerTenant)
		{
			switch (lifetime)
			{
				case ServiceLifetime.Singleton:
					if (asPerTenant)
						return configuration.Lifestyle.SingletonPerKey((scope, context) =>
						{
							var tenant = context.GetTenantContext() ?? scope.GetTenantContext();
							return tenant.Key;
						});
					else
						return configuration.Lifestyle.Singleton();
				case ServiceLifetime.Scoped:
					return configuration.Lifestyle.SingletonPerScope();
			}

			return configuration;
		}

		// todo: ask Grace to expose?
		public static IFluentExportInstanceConfiguration<T> ConfigureLifetime<T>(this IFluentExportInstanceConfiguration<T> configuration, ServiceLifetime lifecycleKind, bool asPerTenant)
		{
			switch (lifecycleKind)
			{
				case ServiceLifetime.Singleton:
					if (asPerTenant)
						return configuration.Lifestyle.SingletonPerKey((scope, context) =>
						{
							var tenant = context.GetTenantContext() ?? scope.GetTenantContext();
							return tenant.Key;
						});
					else
						return configuration.Lifestyle.Singleton();
				case ServiceLifetime.Scoped:
					return configuration.Lifestyle.SingletonPerScope();
			}

			return configuration;
		}

		public static void PopulateFrom(this IExportRegistrationBlock exportConfig, Action<IServiceCollection> configure, ServiceCollection services = null)
		{
			services = services ?? new ServiceCollection();
			configure(services);
			exportConfig.PopulateFrom(services);
		}

		// todo: move to Grace? (and remove his from private)
		public static void PopulateFrom(this IExportRegistrationBlock exportConfig, IEnumerable<ServiceDescriptor> descriptors)
		{
			foreach (var descriptor in descriptors)
			{
				if ((object)descriptor.ImplementationType != null)
					exportConfig.Export(descriptor.ImplementationType).As(descriptor.ServiceType).ConfigureLifetime(descriptor.Lifetime, asPerTenant: false);
				else if (descriptor.ImplementationFactory != null)
					exportConfig.ExportFactory(descriptor.ImplementationFactory).As(descriptor.ServiceType).ConfigureLifetime(descriptor.Lifetime, asPerTenant: false);
				else
					exportConfig.ExportInstance(descriptor.ImplementationInstance).As(descriptor.ServiceType).ConfigureLifetime(descriptor.Lifetime, asPerTenant: false);
			}
		}

		public static IExportRegistrationBlock ExportTenant(this IExportRegistrationBlock config)
		{
			config.ExportFactory<IExportLocatorScope, ITenant>(scope => scope.GetTenantContext());
			return config;
		}

		/// <summary>
		/// Exports the Tenant factory.
		/// </summary>
		/// <typeparam name="TTenant">Tenant type.</typeparam>
		/// <param name="config"></param>
		public static IExportRegistrationBlock ExportTenant<TTenant>(this IExportRegistrationBlock config)
			where TTenant : ITenant
		{
			config.ExportFactory<IExportLocatorScope, TTenant>(scope => scope.GetTenantContext<TTenant>());
			return config;
		}

		public static TenantContainerBuilder<TTenant> ForTenant<TTenant>(this IExportRegistrationBlock config, TTenant tenant)
			where TTenant : ITenant
			=> new TenantContainerBuilder<TTenant>(config, tenant);

		/// <summary>
		/// Export a specific type that requires a tenant.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <param name="config"></param>
		/// <returns></returns>
		public static IExportRegistrationBlock ExportPerTenantFactory<TInterface>(this IExportRegistrationBlock config)
			=> config.ExportPerTenantFactory(typeof(TInterface));

		public static IExportRegistrationBlock ExportPerTenantFactory(this IExportRegistrationBlock config, Type interfaceType)
		{
			config.ExportFactory<IExportLocatorScope, IInjectionContext, ITenant, object>((scope, injectionContext, tenantContext) =>
			{
				var tenant = tenantContext?.Key;

				if (tenant == null) throw new ArgumentNullException("tenant", "Tenant must be defined");
				return scope.Locate(withKey: tenant, type: interfaceType, extraData: injectionContext);
			}).As(interfaceType);
			return config;
		}

		/// <summary>
		/// Export a service/interface for all Tenants individually.
		/// </summary>
		/// <typeparam name="TInterface"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <param name="config"></param>
		/// <param name="tenants"></param>
		/// <param name="configure"></param>
		/// <returns></returns>
		public static IExportRegistrationBlock ExportForAllTenants<TInterface, TImplementation>(
			this IExportRegistrationBlock config, IEnumerable<ITenant> tenants,
			Action<IFluentExportStrategyConfiguration> configure = null)
			=> config.ExportForAllTenants(tenants, typeof(TInterface), typeof(TImplementation), configure);

		public static IExportRegistrationBlock ExportForAllTenants(this IExportRegistrationBlock config, IEnumerable<ITenant> tenants, Type interfaceType, Type implementationType, Action<IFluentExportStrategyConfiguration> configure = null)
		{
			foreach (var tenant in tenants)
			{
				var exportConfig = config.Export(implementationType).AsKeyed(interfaceType, tenant.Key);
				configure?.Invoke(exportConfig);
			}

			config.ExportPerTenantFactory(interfaceType);

			return config;
		}

		/// <summary>
		/// Create and configure a new registration block for tenants container configuration.
		/// </summary>
		/// <typeparam name="TTenant">Tenant Type.</typeparam>
		/// <param name="config"></param>
		/// <param name="tenants"></param>
		/// <param name="configure"></param>
		/// <returns></returns>
		public static IExportRegistrationBlock ForTenants<TTenant>(this IExportRegistrationBlock config,
			IEnumerable<TTenant> tenants,
			Action<TenantsContainerBuilder<TTenant>> configure
		) where TTenant : ITenant
		{
			var tenantsContainerBuilder = new TenantsContainerBuilder<TTenant>();
			configure(tenantsContainerBuilder);

			var configs = tenantsContainerBuilder.GetAll();

			foreach (var tenant in tenants)
			{
				foreach (var tenantConfig in configs)
				{
					if (tenantConfig.TenantFilter != null && !tenantConfig.TenantFilter(tenant))
						continue;

					tenantConfig.Configure(new TenantContainerBuilder<TTenant>(config, tenant));
				}
			}

			return config;
		}

		/// <summary>
		/// Configure injection scope for multi tenancy, this will resolve and use <see cref="ITenantRegistry&lt;TTenant&gt;"/> in order to obtain tenants.
		/// </summary>
		/// <typeparam name="TTenant">Tenant content model to use.</typeparam>
		/// <typeparam name="TTenantRegistry">Tenant registry to resolve tenants from.</typeparam>
		/// <param name="scope"></param>
		/// <param name="configure"></param>
		/// <returns></returns>
		public static IInjectionScope ForTenants<TTenant, TTenantRegistry>(this IInjectionScope scope,
			Action<TenantsContainerBuilder<TTenant>> configure
		) where TTenant : class, ITenant
			where TTenantRegistry : ITenantRegistry<TTenant>
		{
			var tenantRegistry = scope.Locate<TTenantRegistry>();
			var tenants = tenantRegistry.GetAll();

			scope.Configure(c =>
			{
				c.ExportTenant<TTenant>();
				c.ForTenants(tenants, configure);
			});
			return scope;
		}
	}
}