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

> C# formatting, naming, nullability, guard clauses, extension patterns, and language features are defined in [instructions/csharp.instructions.md](instructions/csharp.instructions.md) and auto-applied to all `.cs` files.

- **Keyed proxy pattern:** `MultitenancyBuilder` auto-generates an unkeyed proxy per service type that resolves the keyed implementation for the current tenant — controllers stay unaware of multitenancy
- **Versioning:** single source of truth is `version` in [package.json](../package.json); `Directory.Build.props` reads it for NuGet metadata
- **Formatting:** always run `dotnet format` after changes; respect `.editorconfig`

### Orleans grain keys

Always use `TenantGrainKey.Create(tenantKey, grainKey)` — the format is `tenant/{tenantKey}/{grainId}`. `TryParse` returns a `TenantGrainKey` record struct (not `out string?` params); parsing failures in throwing overloads throw `FormatException`; prefer `TryParse()` in non-middleware code.

### Orleans Grain Best Practices

- Grain interfaces: extend `IGrainWithStringKey` + `ITenantGrain`; add `[AlwaysInterleave]` to read-only methods; add `[return: Immutable]` to methods returning collections/complex types
- Grain classes: implement `IWithTenantAccessor<TTenant>` with `TenantAccessor<TTenant>` property; use `[StorageProvider]` + `Grain<TState>` for persistence; mark as `sealed`
- State types: `[GenerateSerializer]` + `[Id(n)]` starting at `0` on every property; use `sealed class` (mutable state)

## Key Patterns to Reference

| Pattern                         | Exemplary file                                                                                                                                             |
| ------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Keyed DI + proxy generation     | [MultitenancyBuilder.cs](../src/Sketch7.Multitenancy/MultitenancyBuilder.cs)                                                                               |
| Minimal API / middleware wiring | [MultitenancyMiddleware.cs](../src/Sketch7.Multitenancy.AspNet/MultitenancyMiddleware.cs)                                                                  |
| Orleans grain activator         | [TenantGrainActivator.cs](../src/Sketch7.Multitenancy.Orleans/TenantGrainActivator.cs)                                                                     |
| C# 14 extension blocks          | [MultitenancyServiceCollectionExtensions.cs](../src/Sketch7.Multitenancy/MultitenancyServiceCollectionExtensions.cs)                                       |
| Record value objects            | [AppTenant.cs](../samples/Sketch7.Multitenancy.Sample.Api/Tenancy/AppTenant.cs)                                                                            |
| End-to-end registration         | [samples/.../Program.cs](../samples/Sketch7.Multitenancy.Sample.Api/Program.cs)                                                                            |
| xUnit + Shouldly test style     | [tests.instructions.md](instructions/tests.instructions.md), [MultitenancyBuilderTests.cs](../test/Sketch7.Multitenancy.Tests/MultitenancyBuilderTests.cs) |

## Testing

See [tests.instructions.md](instructions/tests.instructions.md) for naming, scope wiring, test doubles, and integration test patterns.

- Run `npm run test` to execute the full suite (excludes e2e)

## Common Pitfalls

- **Middleware order:** `.UseMultitenancy<TTenant>()` must come before any middleware that reads tenant services; placing it late causes `InvalidOperationException` when the proxy tries to resolve with no tenant set
- **`.ForTenants()` without a registry:** calling predicate-based registration without `.WithRegistry()` or `.WithTenants()` throws at startup — the builder validates eagerly
- **`RequestServices` vs root provider:** middleware reads `ITenantHttpResolver` from `context.RequestServices`, not the root `IServiceProvider`
- **Extension block + generic receiver + method generic:** SDK 10.0.x cannot resolve this combination; use traditional `this` extension method as fallback

## Release Workflow

See [docs/RELEASE-WORKFLOW.md](../docs/RELEASE-WORKFLOW.md). In short: bump `package.json` version → update `CHANGELOG.md` → PR → merge to `master` → CircleCI auto-packs + publishes + tags.
