[projectUri]: https://github.com/sketch7/dotnet.multitenancy
[changeLog]: ./CHANGELOG.md

# Dotnet Multitenancy
[![CircleCI](https://circleci.com/gh/sketch7/dotnet.multitenancy.svg?style=shield)](https://circleci.com/gh/sketch7/dotnet.multitenancy)

Multi-tenancy library for .NET 10 using native Microsoft DI keyed services — no third-party containers required.

**Quick links**

[Change logs][changeLog] | [Project Repository][projectUri]

## Features

- Tenant resolution per HTTP request via a simple resolver interface
- Per-tenant service registration using Microsoft DI keyed services
- Transparent unkeyed proxy — inject `IHeroDataClient` and get the right implementation for the current tenant automatically
- Predicate-based bulk registration across matching tenants
- Microsoft Orleans support — tenant-scoped grain keys and a call filter that propagates tenant context
- Aspire-ready (`ServiceDefaults` integration)

## Packages

| Package                        | Description                                                                         |
| ------------------------------ | ----------------------------------------------------------------------------------- |
| `Sketch7.Multitenancy`         | Core abstractions and builder (`ITenant`, `ITenantAccessor`, `MultitenancyBuilder`) |
| `Sketch7.Multitenancy.AspNet`  | ASP.NET Core middleware and HTTP resolver                                           |
| `Sketch7.Multitenancy.Orleans` | Orleans grain call filter and tenant grain key helpers                              |

---

## Getting Started

### 1. Define your tenant

Implement `ITenant` — the only requirement is a string `Key`:

```csharp
public class AppTenant : ITenant
{
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Organization { get; set; } = default!;
}
```

### 2. Create a tenant registry

```csharp
public class AppTenantRegistry : ITenantRegistry<AppTenant>
{
    private static readonly AppTenant[] _tenants =
    [
        new() { Key = "lol", Name = "League of Legends", Organization = "riot" },
        new() { Key = "hots", Name = "Heroes of the Storm", Organization = "blizzard" },
    ];

    public AppTenant Get(string key)
        => GetOrDefault(key) ?? throw new KeyNotFoundException($"Tenant '{key}' not found.");

    public AppTenant? GetOrDefault(string key)
        => _tenants.FirstOrDefault(t => t.Key == key);

    public IEnumerable<AppTenant> GetAll() => _tenants;
}
```

### 3. Register multitenancy services

```csharp
// Program.cs
var tenantRegistry = new AppTenantRegistry();

builder.Services
    .AddSingleton<AppTenantRegistry>(tenantRegistry)
    .AddMultitenancy<AppTenant>()
    .WithHttpResolver<AppTenant, AppTenantHttpResolver>()
    .WithTenants(tenantRegistry.GetAll())
    // Register different IHeroDataClient implementations per tenant group
    .ForTenants(t => t.Organization == "riot",
        s => s.AddScoped<IHeroDataClient, LoLHeroDataClient>())
    .ForTenants(t => t.Organization == "blizzard",
        s => s.AddScoped<IHeroDataClient, HotsHeroDataClient>());
```

### 4. Implement the HTTP resolver

The resolver extracts the tenant identifier from the incoming request (header, host, route, etc.):

```csharp
public class AppTenantHttpResolver : ITenantHttpResolver<AppTenant>
{
    private readonly AppTenantRegistry _registry;

    public AppTenantHttpResolver(AppTenantRegistry registry) => _registry = registry;

    public Task<AppTenant?> Resolve(HttpContext httpContext)
    {
        httpContext.Request.Headers.TryGetValue("X-Tenant", out var tenantKey);
        return Task.FromResult(_registry.GetOrDefault(tenantKey.ToString()));
    }
}
```

### 5. Add the middleware

Add `UseMultitenancy<T>()` **before** any middleware that requires the resolved tenant (e.g. auth, routing, controllers):

```csharp
app.UseMultitenancy<AppTenant>();
app.MapControllers();
```

When tenant resolution fails the middleware returns `400 Bad Request` with `{"errorCode":"error.invalid:tenant"}`.

---

## Usage

### Inject tenant-specific services

Because `MultitenancyBuilder` registers unkeyed proxies, you inject the interface as normal — the right implementation for the current tenant is resolved automatically:

```csharp
[ApiController, Route("api/[controller]")]
public class HeroesController : ControllerBase
{
    private readonly IHeroDataClient _client; // resolves to LoL or HoTS implementation

    public HeroesController(IHeroDataClient client) => _client = client;

    [HttpGet]
    public Task<List<Hero>> GetAll() => _client.GetAll();
}
```

### Inject the current tenant directly

```csharp
public class HeroesController : ControllerBase
{
    private readonly ITenantAccessor<AppTenant> _tenantAccessor;

    public HeroesController(ITenantAccessor<AppTenant> tenantAccessor)
        => _tenantAccessor = tenantAccessor;

    [HttpGet("tenant")]
    public ActionResult<string> GetTenant()
        => _tenantAccessor.Tenant?.Name ?? "unknown";
}
```

### Register services for a specific tenant by key

```csharp
builder.Services
    .AddMultitenancy<AppTenant>()
    .ForTenant("lol", s => s.AddScoped<IHeroDataClient, LoLHeroDataClient>())
    .ForTenant("hots", s => s.AddScoped<IHeroDataClient, HotsHeroDataClient>());
```

### Register the same services for all tenants

```csharp
builder.Services
    .AddMultitenancy<AppTenant>()
    .WithTenants(tenantRegistry.GetAll())
    .ForAllTenants(s => s.AddScoped<IAuditLogger, DefaultAuditLogger>());
```

### Customise the invalid-tenant response

```csharp
app.UseMultitenancy<AppTenant>(new MultitenancyMiddlewareOptions()
    .WithInvalidTenantResponse(() => new { error = "tenant_not_found", status = 400 }));
```

---

## Microsoft Orleans Integration

### 1. Configure the silo

```csharp
siloBuilder.UseMultitenancy<AppTenant>();
```

This registers `TenantGrainCallFilter<T>` as an incoming call filter that automatically populates `ITenantAccessor<T>` for every `ITenantGrain` call.

### 2. Create tenant-scoped grain keys

Grain keys follow the `{tenantKey}/{grainKey}` convention:

```csharp
// Create
string key = TenantGrainKey.Create("lol", "hero-42"); // "lol/hero-42"

// Parse
string tenantKey = TenantGrainKey.GetTenantKey("lol/hero-42"); // "lol"
string grainKey  = TenantGrainKey.GetGrainKey("lol/hero-42");  // "hero-42"

// Safe parse
if (TenantGrainKey.TryParse(compositeKey, out var tenant, out var grain))
{
    // use tenant, grain
}
```

### 3. Implement a tenant-aware grain

```csharp
public class HeroGrain : Grain, ITenantGrain, IHasTenantAccessor<AppTenant>
{
    public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

    public Task<string> GetTenantKeyAsync()
        => Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));
}
```

---

## Aspire Integration

The sample uses `Sketch7.Multitenancy.ServiceDefaults` which wires up OpenTelemetry traces/metrics and health checks via a single `AddServiceDefaults()` / `MapDefaultEndpoints()` call (standard Aspire pattern).

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);
var api = builder.AddProject<Projects.Sketch7_Multitenancy_Sample_Api>("api");
builder.Build().Run();
```

---

## Contributing

### Setup

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- NodeJS (for npm scripts)

### Commands

```bash
# build
npm run build
# or: dotnet build dotnet.multitenancy.slnx -c Release

# run tests
npm test

# pack NuGet packages
npm run pack

# publish dev packages
npm run publish:dev
```
