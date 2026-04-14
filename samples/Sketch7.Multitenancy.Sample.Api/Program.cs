using Microsoft.Extensions.Hosting;
using Sketch7.Multitenancy;
using Sketch7.Multitenancy.AspNet;
using Sketch7.Multitenancy.Sample.Api.Heroes;
using Sketch7.Multitenancy.Sample.Api.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Create the tenant registry early so we can use it for predicate-based per-tenant registration
var tenantRegistry = new AppTenantRegistry();

builder.Services
	.AddSingleton<IAppTenantRegistry>(tenantRegistry)
	.AddSingleton<IDataClientManager, DataClientManager>();

builder.Services
	.AddMultitenancy<AppTenant>()
	.WithHttpResolver<AppTenant, AppTenantHttpResolver>()
	.WithTenants(tenantRegistry.GetAll())
	.ForTenants(t => t.Organization == OrganizationNames.Riot,
		s => s.AddScoped<IHeroDataClient, MockLoLHeroDataClient>())
	.ForTenants(t => t.Organization == OrganizationNames.Blizzard,
		s => s.AddScoped<IHeroDataClient, MockHotsHeroDataClient>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMultitenancy<AppTenant>();
app.MapControllers();

app.Run();
