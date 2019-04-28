using Grace.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sketch7.Multitenancy.Grace.AspNet;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

namespace Sketch7.Multitenancy.Sample.Api
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddSingleton<IAppTenantRegistry, AppTenantRegistry>()
				.AddSingleton<ITenantHttpResolver<AppTenant>, AppTenantHttpResolver>()
				.AddMvc()
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
		}

		public void ConfigureContainer(IInjectionScope scope)
		{
			scope.Configure(c =>
			{
				c.ExportTenant<AppTenant>();

				//var tenantRegistry = scope.Locate<IAppTenantRegistry>();
				//c.PopulateFrom(services => services.AddCors(o => o.AddPolicy(CorsPolicyNames.Api, builder =>
				//{
				//	builder.SetIsOriginAllowed(host =>
				//		{
				//			var brand = tenantRegistry.ResolveByDomain(host);
				//			return brand != null;
				//		})
				//		.AllowAnyMethod()
				//		.AllowAnyHeader()
				//		.AllowCredentials();
				//})));
			});

			scope.ForTenants<AppTenant, IAppTenantRegistry>(tcb =>
			{
				tcb.ForTenant(tenant => tenant.Organization == OrganizationNames.Blizzard,
					tc => tc.PopulateFrom(s => s.AddAppBlizzardServices()));

				tcb.ForTenant(tenant => tenant.Organization == OrganizationNames.Riot,
					tc => tc.PopulateFrom(s => s.AddAppRiotServices()));
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			//app.UseCors(CorsPolicyNames.Api);
			app.UseMultitenancy<AppTenant>();
			app.UseMvc();
		}
	}
}
