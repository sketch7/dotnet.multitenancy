# Dotnet Multitenancy
[![CI](https://github.com/sketch7/dotnet.multitenancy/actions/workflows/ci.yml/badge.svg)](https://github.com/sketch7/dotnet.multitenancy/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Sketch7.Multitenancy.svg)](https://www.nuget.org/packages/Sketch7.Multitenancy)

Multi-tenancy library for .NET 10 (C# 14) using native Microsoft DI keyed services — no third-party containers required.

## Features

- Tenant resolution per HTTP request via a simple resolver interface
- Per-tenant service registration using native Microsoft DI keyed services — no third-party containers
- Transparent unkeyed proxy — controllers and handlers stay unaware of multitenancy; inject `IMyService` and get the right tenant implementation automatically
- Fluent builder with by-key, predicate, and all-tenants registration
- Microsoft Orleans support — tenant-scoped grain keys and a call filter that propagates tenant context

## Packages

| Package                        | Description                                                                         |
| ------------------------------ | ----------------------------------------------------------------------------------- |
| `Sketch7.Multitenancy`         | Core abstractions and builder (`ITenant`, `ITenantAccessor`, `MultitenancyBuilder`) |
| `Sketch7.Multitenancy.AspNet`  | ASP.NET Core middleware and HTTP resolver                                           |
| `Sketch7.Multitenancy.Orleans` | Orleans grain call filter and tenant grain key helpers                              |

---

## Getting Started

### 1. Define your tenant

Implement `ITenant` — the only requirement is a string `Key`. Use a `record` for immutability:

```csharp
public record AppTenant : ITenant
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Organization { get; init; }
}
```

### 2. Create a tenant registry

```csharp
public sealed class AppTenantRegistry : IAppTenantRegistry
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
    .AddSingleton<AppTenantRegistry>(tenantRegistry);

builder.Services
    .AddMultitenancy<AppTenant>(opts => opts
        .WithRegistry(tenantRegistry)
        .WithHttpResolver<AppTenant, AppTenantHttpResolver>()
        .WithServices(tsb => tsb
            // Register different IHeroDataClient implementations per tenant group
            .For(t => t.Organization == "riot", s => s
                .AddScoped<IHeroDataClient, LoLHeroDataClient>())
            .For(t => t.Organization == "blizzard", s => s
                .AddScoped<IHeroDataClient, HotsHeroDataClient>())
        )
    );
```

### 4. Implement the HTTP resolver

The resolver extracts the tenant identifier from the incoming request (header, host, route, etc.):

```csharp
public sealed class AppTenantHttpResolver : ITenantHttpResolver<AppTenant>
{
    private readonly AppTenantRegistry _registry;

    public AppTenantHttpResolver(AppTenantRegistry registry)
        => _registry = registry;

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

When tenant resolution fails the middleware returns `400 Bad Request` with `{"errorCode":"error.tenant.invalid"}`.

---

## Usage

### Inject tenant-specific services

Because `MultitenancyBuilder` registers unkeyed proxies, you inject the interface as normal — the right implementation for the current tenant is resolved automatically:

```csharp
app.MapGet("/heroes", async (IHeroDataClient client) => // resolves to LoL or HoTS implementation
    TypedResults.Ok(await client.GetAll()));
```

### Inject the current tenant directly

```csharp
app.MapGet("/tenant", (ITenantAccessor<AppTenant> tenantAccessor) =>
    TypedResults.Ok(tenantAccessor.Tenant?.Name ?? "unknown"));
```

### Configure per-tenant services

All per-tenant registrations live inside `WithServices`. You can mix by-key, predicate, and all-tenants registrations in any order:

```csharp
builder.Services
    .AddMultitenancy<AppTenant>(opts => opts
        .WithRegistry(tenantRegistry)   // makes tenants available for predicates
        .WithServices(tsb => tsb
            // by exact key
            .For("lol", s => s.AddScoped<IHeroDataClient, LoLHeroDataClient>())
            .For("hots", s => s.AddScoped<IHeroDataClient, HotsHeroDataClient>())
            // by predicate (requires WithRegistry or WithTenants)
            .For(t => t.Organization == "riot", s => s
                .AddScoped<IHeroDataClient, LoLHeroDataClient>())
            // same service for every tenant
            .ForAll(s => s.AddScoped<IAuditLogger, DefaultAuditLogger>())
        )
    );
```

### Customize the invalid-tenant response

```csharp
app.UseMultitenancy<AppTenant>(new MultitenancyMiddlewareOptions()
    .WithInvalidTenantResponse(() => new { error = "tenant_not_found", status = 400 }));
```

---

## Microsoft Orleans Integration

### 1. Configure the silo

Choose a propagation strategy when configuring the silo:

```csharp
// Option A — once per grain activation (recommended)
siloBuilder.UseMultitenancy<AppTenant>().WithGrainActivator();

// Option B — on every incoming grain call
siloBuilder.UseMultitenancy<AppTenant>().WithIncomingCallFilter();
```

| Strategy                   | When tenant is set       | Overhead                                        |
| -------------------------- | ------------------------ | ----------------------------------------------- |
| `WithGrainActivator()`     | Once at grain activation | Minimal — set once for the grain lifetime       |
| `WithIncomingCallFilter()` | Before every grain call  | Per-call — useful when tenant must be refreshed |

`WithGrainActivator()` supports two grain authoring styles — see sections 3a and 3b below.

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

### 3a. Grain with constructor injection (recommended)

`WithGrainActivator()` sets `TenantAccessor<T>` in the grain's `ActivationServices` scope **before** the
grain instance is constructed. Tenant-aware services (e.g. `IHeroDataClient` resolved via the multitenancy
proxy) can therefore be injected directly into the constructor — no scope factories or property accessors needed.

```csharp
public sealed class HeroGrain : Grain, IHeroGrain
{
    private readonly IHeroDataClient _heroDataClient;
    private readonly IPersistentState<HeroGrainState> _state;

    public HeroGrain(
        IHeroDataClient heroDataClient,
        [PersistentState("heroes", "heroes")]
        IPersistentState<HeroGrainState> state
    )
    {
        _heroDataClient = heroDataClient;
        _state = state;
    }

    public Task<string> GetTenantKeyAsync()
        => Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));

    public async Task<List<Hero>> GetAllAsync()
    {
        if (_state.State.Heroes.Count == 0)
        {
            _state.State.Heroes = await _heroDataClient.GetAll();
            await _state.WriteStateAsync();
        }
        return _state.State.Heroes;
    }
}
```

### 3b. Grain with property accessor (`IWithTenantAccessor`)

Alternatively, implement `IWithTenantAccessor<T>`. The activator sets `TenantAccessor.Tenant` after grain
construction via a lifecycle callback. Use this when you need to read the tenant object directly inside grain
methods (e.g. to branch on tenant properties):

```csharp
public sealed class HeroTypeGrain : Grain, IHeroTypeGrain, IWithTenantAccessor<AppTenant>
{
    private readonly IHeroDataClient _heroDataClient;
    private readonly IPersistentState<HeroTypeGrainState> _state;

    public HeroTypeGrain(
        IHeroDataClient heroDataClient,
        [PersistentState("hero-types", "heroes")]
        IPersistentState<HeroTypeGrainState> state
    )
    {
        _heroDataClient = heroDataClient;
        _state = state;
    }

    public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

    public Task<string> GetTenantKeyAsync()
        => Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));

    public async Task<List<HeroType>> GetAllAsync()
    {
        if (_state.State.HeroTypes.Count == 0)
        {
            _state.State.HeroTypes = await _heroDataClient.GetAllHeroTypes();
            await _state.WriteStateAsync();
        }
        return _state.State.HeroTypes;
    }
}
```

### 4. Define the grain interface (Orleans best practices)

```csharp
public interface IHeroGrain : IGrainWithStringKey, ITenantGrain
{
    [AlwaysInterleave]
    [return: Immutable]
    Task<List<Hero>> GetAllAsync();

    [AlwaysInterleave]
    [return: Immutable]
    Task<Hero?> GetByKeyAsync(string heroKey);
}
```
