# Sketch7.dotnet.multitenancy — Workspace Guidelines

Modular multitenancy library for .NET 10 (C# 14) using native Microsoft DI keyed services (no third-party containers).
See [README.md](../README.md) and [CHANGELOG.md](../CHANGELOG.md) for library overview and history.

## Build & Test

```sh
npm run build    # dotnet build dotnet.multitenancy.slnx -c Release
npm run test     # dotnet test across all projects in test/, excludes e2e
npm run pack     # dotnet pack → ./artifacts/
```

Solution file: `dotnet.multitenancy.slnx`

## Architecture

Three published packages + sample app:

| Package                        | Location                                                                                | Role                                                                         |
| ------------------------------ | --------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| `Sketch7.Multitenancy`         | [src/Sketch7.Multitenancy/](../src/Sketch7.Multitenancy/)                               | Core: `ITenant`, `ITenantRegistry`, `ITenantAccessor`, `MultitenancyBuilder` |
| `Sketch7.Multitenancy.AspNet`  | [src/Sketch7.Multitenancy.AspNet/](../src/Sketch7.Multitenancy.AspNet/)                 | Middleware + `ITenantHttpResolver`                                           |
| `Sketch7.Multitenancy.Orleans` | [src/Sketch7.Multitenancy.Orleans/](../src/Sketch7.Multitenancy.Orleans/)               | Orleans grain call filter + `TenantGrainKey`                                 |
| Sample API                     | [samples/Sketch7.Multitenancy.Sample.Api/](../samples/Sketch7.Multitenancy.Sample.Api/) | End-to-end usage demos                                                       |

## Conventions

- **Target framework:** `net10.0`; C# 14 (`<LangVersion>latest</LangVersion>`)
- **Nullable:** enabled globally; no `!` suppressions without comment; use `??` throw patterns
- **Never leave warnings:** handle every warning; no `#pragma warning disable`
- **XML docs:** required on all public members (`<GenerateDocumentationFile>true`)
- **Generic constraint:** always `where TTenant : class, ITenant`
- **Fluent builders:** return `this` for chaining; see [MultitenancyBuilder.cs](../src/Sketch7.Multitenancy/MultitenancyBuilder.cs)
- **Keyed proxy pattern:** `MultitenancyBuilder` auto-generates an unkeyed proxy per service type that resolves the keyed implementation for the current tenant — controllers stay unaware of multitenancy
- **Versioning:** single source of truth is `version` in [package.json](../package.json); `Directory.Build.props` reads it for NuGet metadata
- **Formatting:** always run `dotnet format` after changes; respect `.editorconfig`

### Extension Method Namespaces

Place extension methods in the namespace of the **extended type**, not the containing project's namespace. This ensures the extensions are discovered automatically without extra `using` directives.

```csharp
// ✅ CORRECT — MultitenancyBuilder<T> lives in Sketch7.Multitenancy → same namespace
// File is physically in Sketch7.Multitenancy.AspNet project but uses the correct namespace
namespace Sketch7.Multitenancy;

public static class AspNetMultitenancyBuilderExtensions
{
    extension<TTenant>(MultitenancyBuilder<TTenant> builder) where TTenant : class, ITenant { ... }
}

// ❌ WRONG — namespace follows the project folder, forcing a redundant using
namespace Sketch7.Multitenancy.AspNet;

public static class AspNetMultitenancyBuilderExtensions { ... }
```

> When the namespace intentionally differs from the folder/project structure, add a `#pragma warning disable IDE0130` at the top of the file with a comment explaining the intent:
>
> ```csharp
> #pragma warning disable IDE0130 // Namespace intentionally matches extended type not folder
> ```
>
> Extensions on framework/third-party types (`IServiceCollection`, `IApplicationBuilder`, `ISiloBuilder`) should still use the package's own namespace (e.g., `Sketch7.Multitenancy.AspNet`) since we cannot use Microsoft's namespace.

### C# 14 Extension Blocks

Always use C# 14 `extension(...)` blocks instead of traditional `this` extension methods:

```csharp
// ✅ CORRECT — C# 14 extension block
public static class MyExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMyService<TImpl>() where TImpl : class
        {
            services.AddScoped<TImpl>();
            return services;
        }
    }
}

// ❌ WRONG — Traditional this extension method
public static IServiceCollection AddMyService<TImpl>(this IServiceCollection services) ...
```

> **Known SDK limitation (10.0.x):** Extension blocks with _both_ a generic receiver type (`extension<T>(Builder<T> b)`) _and_ method-level generic type parameters do not resolve correctly. In that case, fall back to the traditional `this` extension method. Example: `WithHttpResolver<TTenant, TResolver>()` on `MultitenancyBuilder<TTenant>`.

### Value Object / Model Types

- Use `record` for immutable data/domain models (e.g. `AppTenant`, `Hero`)
- Use `sealed class` for mutable stateful objects (e.g. `TenantAccessor<T>`, `HeroGrainState`)
- Use `sealed` modifier on concrete implementations that are not meant to be subclassed
- Use `init` setters on record properties; use object initializer syntax

### Early Exit / Guard Clauses

Prefer early returns and guard clauses over nested `if` blocks, when it avoids nesting without duplicating logic:

```csharp
// ✅ CORRECT — guard at the top, happy path flows unindented
public void Process(string key)
{
    if (key is null)
      return;
    if (!IsValid(key))
    return;

    DoWork(key);
}

// ❌ WRONG — unnecessary nesting
public void Process(string key)
{
    if (key is not null)
    {
        if (IsValid(key))
        {
            DoWork(key);
        }
    }
}
```

### Exception Throwing

```csharp
// ✅ Null or empty guard
ArgumentException.ThrowIfNullOrWhiteSpace(tenantKey);

// ✅ Null-coalescing throw
public AppTenant Get(string key) =>
    GetOrDefault(key) ?? throw new KeyNotFoundException($"Tenant '{key}' not found.");
```

### Orleans grain keys

Always use `TenantGrainKey.Create(tenantKey, grainKey)` — the format is `{tenantKey}/{grainKey}`. Parsing failures throw `FormatException`; prefer `TryParse()` in non-middleware code.

### Orleans Grain Best Practices

- Grain interfaces: extend `IGrainWithStringKey` + `ITenantGrain`; add `[AlwaysInterleave]` to read-only methods; add `[return: Immutable]` to methods returning collections/complex types
- Grain classes: implement `IHasTenantAccessor<TTenant>` with `TenantAccessor<TTenant>` property; use `[StorageProvider]` + `Grain<TState>` for persistence; mark as `sealed`
- State types: `[GenerateSerializer]` + `[Id(n)]` starting at `0` on every property; use `sealed class` (mutable state)

## Key Patterns to Reference

| Pattern                         | Exemplary file                                                                                                       |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Keyed DI + proxy generation     | [MultitenancyBuilder.cs](../src/Sketch7.Multitenancy/MultitenancyBuilder.cs)                                         |
| Minimal API / middleware wiring | [MultitenancyMiddleware.cs](../src/Sketch7.Multitenancy.AspNet/MultitenancyMiddleware.cs)                            |
| Orleans call filter             | [TenantGrainCallFilter.cs](../src/Sketch7.Multitenancy.Orleans/TenantGrainCallFilter.cs)                             |
| C# 14 extension blocks          | [MultitenancyServiceCollectionExtensions.cs](../src/Sketch7.Multitenancy/MultitenancyServiceCollectionExtensions.cs) |
| Record value objects            | [AppTenant.cs](../samples/Sketch7.Multitenancy.Sample.Api/Tenancy/AppTenant.cs)                                      |
| End-to-end registration         | [samples/.../Program.cs](../samples/Sketch7.Multitenancy.Sample.Api/Program.cs)                                      |
| xUnit + Shouldly test style     | [MultitenancyBuilderTests.cs](../test/Sketch7.Multitenancy.Tests/MultitenancyBuilderTests.cs)                        |

## Testing

- Framework: **xUnit** with **Shouldly** assertions; integration tests via `Microsoft.AspNetCore.Mvc.Testing`
- Each test creates a fresh `ServiceProvider` scope and sets the `ITenantAccessor` explicitly — never share scope across tests
- Run `npm run test` to execute the full suite

## Common Pitfalls

- **Middleware order:** `.UseMultitenancy<TTenant>()` must come before any middleware that reads tenant services; placing it late causes `InvalidOperationException` when the proxy tries to resolve with no tenant set
- **`.ForTenants()` without a registry:** calling predicate-based registration without `.WithRegistry()` or `.WithTenants()` throws at startup — the builder validates eagerly
- **`RequestServices` vs root provider:** middleware reads `ITenantHttpResolver` from `context.RequestServices`, not the root `IServiceProvider`
- **Extension block + generic receiver + method generic:** SDK 10.0.x cannot resolve this combination; use traditional `this` extension method as fallback

## Release Workflow

See [docs/RELEASE-WORKFLOW.md](../docs/RELEASE-WORKFLOW.md). In short: bump `package.json` version → update `CHANGELOG.md` → PR → merge to `master` → CircleCI auto-packs + publishes + tags.
