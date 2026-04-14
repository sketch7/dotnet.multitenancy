---
name: orleans-best-practices
description: "Microsoft Orleans best practices for this codebase. Use when writing or reviewing grain interfaces, grain implementations, serialization attributes, concurrency attributes, grain key strategies, storage providers, or GrainFactory extension methods. Use for [Immutable], [return: Immutable], [GenerateSerializer], [AlwaysInterleave], [OneWay], [StatelessWorker], [Reentrant], [SharedTenant], ArcaneGrain base class selection, IPersistentState injection, PersistentState attribute, StorageProvider naming, and partial GrainExtensions patterns."
---

# Orleans Best Practices

## When to Use

- Writing a new grain interface or implementation
- Adding parameters or return values to grain methods
- Choosing a grain key type
- Selecting a grain base class
- Adding state persistence to a grain
- Registering grains across tenants

---

## 1. Serialization — `[Immutable]` and `[return: Immutable]`

### Rule

All collection and complex object parameters crossing grain boundaries **must** be decorated with `[Immutable]`. All methods returning collections or complex objects **must** use `[return: Immutable]`.

This tells Orleans the caller guarantees the object won't be mutated — Orleans skips copying the argument, avoiding large allocations on hot paths.

### On grain interfaces

```csharp
public interface IStoreOpWorkerGrain<TCrudModel> : IArcaneGrainContract, IGrainWithIntegerKey
    where TCrudModel : IArcaneEntity
{
    [return: Immutable]
    Task<List<TCrudModel>> CreateMany(
        [Immutable] List<TCrudModel> input,
        ActionSource actionSource,
        [Immutable] CrudContext? ctx = null);

    [return: Immutable]
    Task<List<TCrudModel>> UpdateMany(
        [Immutable] List<UpdateManyRequest<TCrudModel>> updates,
        ActionSource actionSource,
        [Immutable] CrudContext? ctx = null);
}
```

### Rules

- Use `[Immutable]` / `[return: Immutable]` on the **interface**, not the implementation
- The implementation omits the `[Immutable]` attribute; it receives the unwrapped type
- Scalar types (`string`, `int`, `bool`, `ActionSource`, enums) do **not** need `[Immutable]`
- `CrudContext?` always uses `[Immutable] CrudContext?` — **not** the `Immutable<T>` wrapper
- ❌ Avoid the `Immutable<T>` struct wrapper (`Immutable<CrudContext>`, `Immutable<TCrudModel>`) — it changes the public API type and leaks Orleans internals into the interface signature. Prefer the attribute form at all times

---

## 2. Serialization — `[GenerateSerializer]` and `[Id(n)]`

All types sent over grain calls, stored in grain state, or streamed through Orleans must have Orleans serialization attributes.

```csharp
[GenerateSerializer]
public class TransactionCoordinatorGrainState
{
    [Id(0)]
    public Dictionary<string, CrudGrainReference> GrainRefs { get; set; } = new();

    [Id(1)]
    public CrudTransactionStatus Status { get; set; } = CrudTransactionStatus.Inactive;
}

[GenerateSerializer]
public record ReactiveResult<TModel>(
    [property: Id(0)] TModel? Model,
    [property: Id(1)] TimeSpan? TimeToLive,
    [property: Id(2)] Guid VersionToken,
    [property: Id(3)] bool IsFaulted = false
);
```

### Rules

- `[Id(n)]` **must** start at `0` and be contiguous
- Never reuse or reorder `Id` values — this breaks deserialization of persisted state
- Use `[Alias("type-name")]` on grain interfaces to decouple the type name from the wire name (prevents breaking changes on rename)
- Records can use `[property: Id(n)]` on the primary constructor

---

## 3. Concurrency Attributes

### `[AlwaysInterleave]` — read-only, non-mutating methods

Bypasses the grain's single-threaded turn-based execution. Use for reads that do NOT mutate grain state:

```csharp
public interface ICrudGrain<TCrudModel> : ICrudGrain
{
    [AlwaysInterleave]
    ValueTask<Immutable<TCrudModel?>> Get(bool includeArchived, Immutable<CrudContext>? ctx = null);

    [AlwaysInterleave]
    Task<List<TResult>> GetAll();
}
```

### `[OneWay]` — fire-and-forget

No return value expected (Orleans won't wait for completion). Use for notification/side-effect methods:

```csharp
[OneWay]
Task SetOneWay(Immutable<TCrudModel> input, ArcaneMessageActionType actionType,
    ActionSource actionSource = ActionSource.Client, Immutable<CrudContext>? ctx = null);

[OneWay]
Task ActivateOneWay();  // defined in IArcaneGrainContract
```

### `[Reentrant]` — on the grain class, not the interface

Allows the grain to process new messages while awaiting async operations. Use on reactive/streaming grains:

```csharp
[Reentrant]
public abstract class ArcaneReactiveGrain<TState> : ArcaneGrain(...)
```

> ⚠️ Never use `[Reentrant]` on grains with mutable state that require strict ordering.

### `[StatelessWorker]` — pooled, stateless workers

Any number of instances can be created per silo. No per-grain state. Use for CPU/IO work that scales out:

```csharp
[StatelessWorker]              // unlimited instances
[StatelessWorker(1)]           // 1 instance per silo (bounded)
public class StoreOpWorkerGrain<TCrudModel> : ArcaneGrain, IStoreOpWorkerGrain<TCrudModel>
```

---

## 4. Grain Key Strategy

| Key Type | `IGrainWithXxxKey`     | When to Use                                                       |
| -------- | ---------------------- | ----------------------------------------------------------------- |
| Integer  | `IGrainWithIntegerKey` | Singleton or small cardinality; use `0` for the default singleton |
| String   | `IGrainWithStringKey`  | Per-entity (entity ID), per-session, or semantic composite keys   |
| GUID     | `IGrainWithGuidKey`    | Not used in this codebase                                         |

```csharp
// Singleton worker — always key 0
public interface IStoreOpWorkerGrain<TCrudModel> : IArcaneGrainContract, IGrainWithIntegerKey

// Per-entity grain — key = entity ID string
public interface ICrudGrain<TCrudModel> : ICrudGrain  // inherits IGrainWithStringKey

// Composite string key with semantic prefix
factory.GetGrain<IBrandClusterInitializerGrain>($"brand/{brandId}");
```

### Structured Key Pattern (`record struct` + `ParseKey`)

When a string key encodes multiple values (e.g. a service key, tenant, resource type), define a `record struct` to hold the structured data with a `Template` constant and a `Create` factory method. The grain parses its own key via `this.ParseKey<TKey>(template)` in the constructor.

```csharp
// Key type — define next to the grain, in the same file
public record struct TopicKey
{
    public static readonly string Template = "arcaneMessagingTopics/{serviceKey}";

    public static string Create(string serviceKey)
        => Template.FromTemplate(new Dictionary<string, object?> { ["serviceKey"] = serviceKey });

    public string ServiceKey { get; set; }
}

// Grain — parse itself using the same template
public class TopicGrain : ArcaneGrain, ITopicGrain
{
    private readonly TopicKey _keyData;

    public TopicGrain(...) : base(logger, loggingContext)
    {
        _keyData = this.ParseKey<TopicKey>(TopicKey.Template);
        // now use _keyData.ServiceKey, etc.
    }
}
```

### Rules

- `Template` uses `{paramName}` placeholders; `Create(...)` calls `FromTemplate` to fill them
- `ParseKey<T>` is called once in the constructor — never in hot paths
- Every grain with a structured key **must** have a corresponding `IGrainFactory` extension accessor (see section 8)

---

## 5. Base Class Selection

| Class         | State | Use When                                                                                    |
| ------------- | ----- | ------------------------------------------------------------------------------------------- |
| `ArcaneGrain` | None  | All grains — stateless workers, coordinators, and stateful grains via `IPersistentState<T>` |

Always extend `ArcaneGrain` and inject `IPersistentState<TState>` for persistence (see section 6). All grain classes provide: `ILogger Logger`, `ILoggingContext LoggingContext`, `string PrimaryKey`, `RegisterTimer(...)`, `OnArcaneActivate()` lifecycle hook.

> ❌ **Legacy** — `ArcaneGrain<TState>` (backed by `Grain<TState>`) + `[StorageProvider]` class attribute is the deprecated Orleans approach. Do not use for new grains.

---

## 6. Grain Persistence

### Preferred — `IPersistentState<TState>` constructor injection

Inject `IPersistentState<TState>` into the constructor and annotate the parameter with `[PersistentState("stateName", "providerName")]`. Extend `ArcaneGrain` (not `ArcaneGrain<TState>`).

```csharp
public class MyGrain : ArcaneGrain, IMyGrain
{
    private readonly IPersistentState<MyGrainState> _state;

    public MyGrain(
        ILogger<MyGrain> logger,
        ILoggingContext loggingContext,
        [PersistentState("myState", OrleansStoreNames.Crud)]
        IPersistentState<MyGrainState> state
    ) : base(logger, loggingContext)
    {
        _state = state;
    }

    // State is NOT yet loaded at constructor time — only after OnArcaneActivate()
    public Task<string> GetNameAsync() => Task.FromResult(_state.State.Name);

    public async Task SetNameAsync(string name)
    {
        _state.State.Name = name;
        await _state.WriteStateAsync();
    }
}
```

### Store name constants

| Constant                             | Purpose                                                                        |
| ------------------------------------ | ------------------------------------------------------------------------------ |
| `OrleansStoreNames.Crud`             | Persist CRUD entity state (CrudGrain, TransactionCoordinatorGrain, StoreGrain) |
| `OrleansStoreNames.GrainPersistence` | General-purpose grain state (auth tokens, health checks, service context)      |

### Dynamic storage name — `IPersistentStateFactory`

When the provider name comes from runtime configuration (e.g. per-service options), use `IPersistentStateFactory` instead of the `[PersistentState]` attribute:

```csharp
public class TopicGrain : ArcaneGrain, ITopicGrain
{
    private readonly IPersistentState<TopicGrainState> _store;

    public TopicGrain(
        ILogger<TopicGrain> logger,
        ILoggingContext loggingContext,
        IPersistentStateFactory persistentStateFactory,
        IGrainContext grainContext,
        IOptionsMonitor<ArcaneMessagingKafkaOptions> optionsMonitor
    ) : base(logger, loggingContext)
    {
        var options = optionsMonitor.Get(_keyData.ServiceKey);
        _store = persistentStateFactory.Create<TopicGrainState>(
            grainContext,
            new PersistentStateAttribute("topic", options.StoreName)
        );
    }
}
```

### Rules

- ✅ Use `IPersistentState<TState>` + `[PersistentState("name", OrleansStoreNames.Xxx)]` on the constructor parameter
- ✅ Only access `_state.State` **after** activation (state is not loaded in the constructor)
- ✅ Use `IPersistentStateFactory` when the storage provider name is determined at runtime
- ❌ Do **not** use `[StorageProvider(ProviderName = ...)]` on the grain class — this is the legacy `Grain<TState>` pattern and is deprecated in Orleans 10
- ❌ Do **not** extend `ArcaneGrain<TState>` for new grains — prefer `ArcaneGrain` + injected `IPersistentState<T>`

---

## 7. `[SharedTenant]` — Cross-Tenant Grains

By default all grains are tenant-scoped. Grains shared across all tenants (messaging infrastructure, cluster coordination, Kafka topics) must be decorated with `[SharedTenant]`:

```csharp
[SharedTenant]
public class StoreGrain : ArcaneGrain<StoreGrainState>, IStoreGrain

[SharedTenant]
public class ProducerGrain : ArcaneGrain, IProducerGrain

[SharedTenant]
public class TransactionCoordinatorGrain : ArcaneGrain<TransactionCoordinatorGrainState>, ITransactionCoordinatorGrain
```

> ⚠️ Never add `[SharedTenant]` to entity-level grains (CrudGrain, etc.) — they must remain tenant-isolated.

---

## 8. GrainFactory Extensions (Partial Class Pattern)

Every grain **must** have a typed extension accessor on `IGrainFactory`. This hides key construction behind a named method, prevents callers from hard-coding key strings, and enforces consistency.

All accessors live in `public static partial class GrainExtensions`, split by domain across multiple files. Always use C# 14 `extension` blocks — never the old `this` parameter style.

```csharp
// ✅ Correct — C# 14 extension block
// src/Arcane.Orleans.DataStore/GrainExtensions.cs
public static partial class GrainExtensions
{
    extension(IGrainFactory grainFactory)
    {
        // Entity grain — key is the entity ID
        public ICrudGrain<TCrudModel> GetCrudGrain<TCrudModel>(string id)
            where TCrudModel : ArcaneCrudModel
            => grainFactory.GetGrain<ICrudGrain<TCrudModel>>(id);

        // Singleton grain — always key 0
        public IStoreOpWorkerGrain<TCrudModel> GetStoreOpWorkerGrain<TCrudModel>()
            where TCrudModel : ArcaneCrudModel
            => grainFactory.GetGrain<IStoreOpWorkerGrain<TCrudModel>>(0);

        // Structured key grain — key built via the key type's Create()
        public ITopicGrain GetTopicGrain(string serviceKey)
            => grainFactory.GetGrain<ITopicGrain>(TopicKey.Create(serviceKey));
    }
}

// ❌ Old style — do not use
public static class GrainExtensions
{
    public static ITopicGrain GetTopicGrain(this IGrainFactory factory, string serviceKey)
        => factory.GetGrain<ITopicGrain>(TopicKey.Create(serviceKey));
}
```

Accessors may be co-located with the grain file (inline `partial class`) instead of a separate `GrainExtensions.cs` when the grain is self-contained:

```csharp
// src/Arcane.Messaging.Kafka/TopicGrain.cs
public static partial class GrainExtensions
{
    extension(IGrainFactory grainFactory)
    {
        public ITopicGrain GetTopicGrain(string serviceKey)
            => grainFactory.GetGrain<ITopicGrain>(TopicKey.Create(serviceKey));
    }
}
```

### Rules

- Every grain **must** have an extension accessor — callers must never call `GetGrain<T>(key)` directly
- Always use C# 14 `extension` blocks (not old `this` parameter style)
- One `partial class GrainExtensions` per feature file or domain file — never a monolithic file
- Singleton grains use key `0`; entity grains use the entity ID; structured-key grains use `KeyType.Create(...)`

---

## 9. `IArcaneGrainContract` — Required Interface Members

Every grain interface must extend `IArcaneGrainContract`, which provides:

```csharp
Task Activate();       // warm-up / pre-load

[OneWay]
Task ActivateOneWay(); // fire-and-forget warm-up
```

These are implemented in the `ArcaneGrain` base class — no need to implement them in concrete grains.

---

## Quick Reference Checklist

When writing a new grain:

- [ ] Interface extends `IArcaneGrainContract` + `IGrainWithXxxKey`
- [ ] Collections and complex types in parameters use `[Immutable]` or `Immutable<T>`
- [ ] Methods returning collections use `[return: Immutable]`
- [ ] Read-only methods use `[AlwaysInterleave]`
- [ ] Fire-and-forget methods use `[OneWay]` and return `Task` (no return value)
- [ ] State type has `[GenerateSerializer]` + `[Id(n)]` on every property
- [ ] Grain class uses correct base: `ArcaneGrain` (stateless) or `ArcaneGrain<TState>` (stateful)
- [ ] Stateful grains inject `IPersistentState<TState>` with `[PersistentState("name", OrleansStoreNames.Xxx)]` on the constructor parameter — not `[StorageProvider]` on the class
- [ ] Cross-tenant grains have `[SharedTenant]`
- [ ] GrainFactory accessor added to `public static partial class GrainExtensions` using C# 14 `extension` block
- [ ] Structured string keys use a `record struct` with `Template` + `Create()`; grain parses via `this.ParseKey<TKey>(Template)`
