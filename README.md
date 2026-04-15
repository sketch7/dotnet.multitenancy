# Dotnet Multitenancy
[![CI](https://github.com/sketch7/dotnet.multitenancy/actions/workflows/ci.yml/badge.svg)](https://github.com/sketch7/dotnet.multitenancy/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Sketch7.Multitenancy.svg)](https://www.nuget.org/packages/Sketch7.Multitenancy)

Multi-tenancy library for .NET 10 (C# 14) using native Microsoft DI keyed services — no third-party containers required.

## Features

- Tenant resolution per HTTP request via a simple resolver interface
- Per-tenant service registration using native Microsoft DI keyed services — no third-party containers
- Transparent unkeyed proxy — controllers and handlers stay unaware of multitenancy; inject `IMyService` and get the right tenant implementation automatically
- Fluent builder with by-key, predicate, and all-tenants registration
- Microsoft Orleans support — tenant-scoped grain keys and a grain activator that propagates tenant context once per grain activation

## Packages

| Package                        | Description                                                                         |
| ------------------------------ | ----------------------------------------------------------------------------------- |
| `Sketch7.Multitenancy`         | Core abstractions and builder (`ITenant`, `ITenantAccessor`, `MultitenancyBuilder`) |
| `Sketch7.Multitenancy.AspNet`  | ASP.NET Core middleware and HTTP resolver                                           |
| `Sketch7.Multitenancy.Orleans` | Orleans grain activator and tenant grain key helpers                                |

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

```csharp
siloBuilder.UseMultitenancy<AppTenant>();
```

Registers `ITenantOrleansResolver<TTenant>` and `TenantGrainActivator<TTenant>` — tenant context is set once per grain activation.

### 2. Grain keys

Grain keys follow the `{tenantKey}/{grainKey}` convention:

```csharp
string key = TenantGrainKey.Create("lol", "hero-42"); // "lol/hero-42"
string tenantKey = TenantGrainKey.GetTenantKey(key);  // "lol"
string grainKey  = TenantGrainKey.GetGrainKey(key);   // "hero-42"
```

### 3. Grain authoring

Two patterns are supported:

**Constructor injection (recommended)** — tenant context is set in `ActivationServices` before the grain is constructed, so tenant-aware services resolve correctly via the multitenancy proxy:

```csharp
public sealed class HeroGrain : Grain, IHeroGrain
{
    public HeroGrain(IHeroDataClient heroDataClient, ...) { ... }

    public Task<string> GetTenantKeyAsync()
        => Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));
}
```

**Property accessor** — implement `IWithTenantAccessor<T>` to receive the `AppTenant` object directly inside grain methods:

```csharp
public sealed class HeroTypeGrain : Grain, IHeroTypeGrain, IWithTenantAccessor<AppTenant>
{
    public TenantAccessor<AppTenant> TenantAccessor { get; } = new();

    public Task<string> GetTenantKeyAsync()
        => Task.FromResult(TenantGrainKey.GetTenantKey(this.GetPrimaryKeyString()));
}
```

### 4. Grain interface

```csharp
public interface IHeroGrain : IGrainWithStringKey, ITenantGrain
{
    [AlwaysInterleave]
    [return: Immutable]
    Task<List<Hero>> GetAllAsync();
}
```
