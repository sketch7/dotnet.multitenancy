# Sketch7.dotnet.multitenancy — Workspace Guidelines

Modular multitenancy library for .NET 10 using native Microsoft DI keyed services (no third-party containers).
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

- **Target framework:** `net10.0`; C# latest (`<LangVersion>latest</LangVersion>`)
- **Nullable:** enabled globally; no `!` suppressions without comment
- **XML docs:** required on all public members (`<GenerateDocumentationFile>true`)
- **Generic constraint:** always `where TTenant : class, ITenant`
- **Fluent builders:** return `this` for chaining; see [MultitenancyBuilder.cs](../src/Sketch7.Multitenancy/MultitenancyBuilder.cs)
- **Keyed proxy pattern:** `MultitenancyBuilder` auto-generates an unkeyed proxy per service type that resolves the keyed implementation for the current tenant — controllers stay unaware of multitenancy
- **Versioning:** single source of truth is `version` in [package.json](../package.json); `Directory.Build.props` reads it for NuGet metadata

### Orleans grain keys

Always use `TenantGrainKey.Create(tenantKey, grainKey)` — the format is `{tenantKey}/{grainKey}`. Parsing failures throw `FormatException`; prefer `TryParse()` in non-middleware code.

## Key Patterns to Reference

| Pattern                         | Exemplary file                                                                                |
| ------------------------------- | --------------------------------------------------------------------------------------------- |
| Keyed DI + proxy generation     | [MultitenancyBuilder.cs](../src/Sketch7.Multitenancy/MultitenancyBuilder.cs)                  |
| Minimal API / middleware wiring | [MultitenancyMiddleware.cs](../src/Sketch7.Multitenancy.AspNet/MultitenancyMiddleware.cs)     |
| Orleans call filter             | [TenantGrainCallFilter.cs](../src/Sketch7.Multitenancy.Orleans/TenantGrainCallFilter.cs)      |
| End-to-end registration         | [samples/.../Program.cs](../samples/Sketch7.Multitenancy.Sample.Api/Program.cs)               |
| xUnit + Shouldly test style     | [MultitenancyBuilderTests.cs](../test/Sketch7.Multitenancy.Tests/MultitenancyBuilderTests.cs) |

## Testing

- Framework: **xUnit** with **Shouldly** assertions; integration tests via `Microsoft.AspNetCore.Mvc.Testing`
- Each test creates a fresh `ServiceProvider` scope and sets the `ITenantAccessor` explicitly — never share scope across tests
- Run `npm run test` to execute the full suite

## Common Pitfalls

- **Middleware order:** `.UseMultitenancy<TTenant>()` must come before any middleware that reads tenant services; placing it late causes `InvalidOperationException` when the proxy tries to resolve with no tenant set
- **`.ForTenants()` without a registry:** calling predicate-based registration without `.WithRegistry()` or `.WithTenants()` throws at startup — the builder validates eagerly
- **`RequestServices` vs root provider:** middleware reads `ITenantHttpResolver` from `context.RequestServices`, not the root `IServiceProvider`

## Release Workflow

See [docs/RELEASE-WORKFLOW.md](../docs/RELEASE-WORKFLOW.md). In short: bump `package.json` version → update `CHANGELOG.md` → PR → merge to `master` → CircleCI auto-packs + publishes + tags.
