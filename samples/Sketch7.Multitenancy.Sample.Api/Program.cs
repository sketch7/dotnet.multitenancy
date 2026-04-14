using Sketch7.Multitenancy;
using Sketch7.Multitenancy.AspNet;
using Sketch7.Multitenancy.Orleans;
using Sketch7.Multitenancy.Sample.Api.Heroes;
using Sketch7.Multitenancy.Sample.Api.Tenancy;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Create the tenant registry early so we can use it for predicate-based per-tenant registration
var tenantRegistry = new AppTenantRegistry();

builder.Services
	.AddSingleton<IAppTenantRegistry>(tenantRegistry)
	.AddSingleton<ITenantRegistry<AppTenant>>(tenantRegistry)
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

// Configure Orleans silo (co-hosted in same process as the ASP.NET Core app).
// When Aspire provides a "redis" connection string, use Redis for clustering and grain persistence.
// Otherwise fall back to localhost clustering and in-memory storage (e.g. integration tests).
builder.Host.UseOrleans(silo =>
{
	var redisConnectionString = builder.Configuration.GetConnectionString("redis");

	if (!string.IsNullOrEmpty(redisConnectionString))
	{
		silo.UseRedisClustering(redisConnectionString);
		silo.AddRedisGrainStorage("heroes", options =>
			options.ConfigurationOptions = ConfigurationOptions.Parse(redisConnectionString));
	}
	else
	{
		silo.UseLocalhostClustering();
		silo.AddMemoryGrainStorage("heroes");
	}

	silo.UseMultitenancy<AppTenant>();
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMultitenancy<AppTenant>();
app.MapControllers();

app.Run();
