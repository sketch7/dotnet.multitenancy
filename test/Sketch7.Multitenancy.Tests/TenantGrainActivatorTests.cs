using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Runtime;
using Shouldly;
using Sketch7.Multitenancy.Orleans;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Tests for <see cref="TenantGrainActivator{TTenant}"/>.
/// </summary>
public class TenantGrainActivatorTests
{
	[Fact]
	public void CreateInstance_WhenResolvingTenantAwareProxyServiceInConstructor_SetsTenantBeforeConstruction()
	{
		// Arrange
		using var scope = BuildActivationScope(services =>
		{
			services.AddMultitenancy<TestTenant>(builder => builder
				.WithServices(tsb => tsb
					.For("lol", s => s.AddScoped<IActivationProbeService, ActivationProbeService>())
					.For("hots", s => s.AddScoped<IActivationProbeService, ActivationProbeService>())));
		});
		var activator = BuildActivator(scope.ServiceProvider, typeof(ConstructorInjectionGrain));
		var context = new FakeGrainContext(scope.ServiceProvider, GrainId.Create("test-grain", "tenant/lol/heroes"));

		// Act
		var instance = activator.CreateInstance(context).ShouldBeOfType<ConstructorInjectionGrain>();

		// Assert
		instance.ServiceTenantKeyAtConstruction.ShouldBe("lol");
	}

	[Fact]
	public void CreateInstance_WhenConstructorHasFacetAndTenantAwareServices_ResolvesBothSuccessfully()
	{
		// Arrange
		using var scope = BuildActivationScope(services =>
		{
			services.AddMultitenancy<TestTenant>(builder => builder
				.WithServices(tsb => tsb
					.For("lol", s => s.AddScoped<IActivationProbeService, ActivationProbeService>())
					.For("hots", s => s.AddScoped<IActivationProbeService, ActivationProbeService>())));
			services.AddSingleton<IAttributeToFactoryMapper<PersistentStateAttribute>, PersistentStateAttributeMapper>();
			services.AddSingleton<IPersistentStateFactory, FakePersistentStateFactory>();
		});
		var activator = BuildActivator(scope.ServiceProvider, typeof(FacetAndConstructorInjectionGrain));
		var context = new FakeGrainContext(scope.ServiceProvider, GrainId.Create("test-grain", "tenant/hots/hero-types"));

		// Act
		var instance = activator.CreateInstance(context).ShouldBeOfType<FacetAndConstructorInjectionGrain>();

		// Assert
		instance.ServiceTenantKeyAtConstruction.ShouldBe("hots");
		instance.PersistentState.ShouldBeOfType<FakePersistentState<ActivationFacetState>>();
	}

	// ---- Helpers ----

	private static IServiceScope BuildActivationScope(Action<IServiceCollection> configure)
	{
		var services = new ServiceCollection();
		configure(services);
		return services.BuildServiceProvider().CreateScope();
	}

	private static TenantGrainActivator<TestTenant> BuildActivator(IServiceProvider serviceProvider, Type grainType)
	{
		var resolver = new TenantOrleansResolver<TestTenant>(new TestTenantRegistry(), NullLogger<TenantOrleansResolver<TestTenant>>.Instance);
		return new TenantGrainActivator<TestTenant>(serviceProvider, resolver, grainType);
	}
}

// ---- Test doubles ----

file interface IActivationProbeService
{
	string? TenantKeyAtConstruction { get; }
}

file sealed class ActivationProbeService(ITenantAccessor<TestTenant> tenantAccessor) : IActivationProbeService
{
	public string? TenantKeyAtConstruction { get; } = tenantAccessor.Tenant?.Key;
}

file sealed class ConstructorInjectionGrain(IActivationProbeService service)
{
	public string? ServiceTenantKeyAtConstruction { get; } = service.TenantKeyAtConstruction;
}

file sealed class FacetAndConstructorInjectionGrain(
	IActivationProbeService service,
	[PersistentState("activation-state", "heroes")]
	IPersistentState<ActivationFacetState> state
)
{
	public string? ServiceTenantKeyAtConstruction { get; } = service.TenantKeyAtConstruction;
	public IPersistentState<ActivationFacetState> PersistentState { get; } = state;
}

file sealed class ActivationFacetState;

file sealed class FakePersistentStateFactory : IPersistentStateFactory
{
	public IPersistentState<TState> Create<TState>(IGrainContext context, IPersistentStateConfiguration configuration)
		=> new FakePersistentState<TState>();
}

file sealed class FakePersistentState<TState> : IPersistentState<TState>
{
	private TState? _state;

	public string Etag { get; set; } = string.Empty;
	public bool RecordExists => false;
	public TState State
	{
		get => _state ?? throw new InvalidOperationException("State has not been initialized.");
		set => _state = value;
	}

	public Task ClearStateAsync() => Task.CompletedTask;
	public Task WriteStateAsync() => Task.CompletedTask;
	public Task ReadStateAsync() => Task.CompletedTask;
}

file sealed class FakeGrainContext(IServiceProvider activationServices, GrainId grainId) : IGrainContext
{
	private readonly Dictionary<Type, object?> _components = [];

	public GrainReference GrainReference => throw new NotSupportedException();
	public GrainId GrainId { get; } = grainId;
	public object GrainInstance { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
	public ActivationId ActivationId => default;
	public GrainAddress Address => throw new NotSupportedException();
	public IServiceProvider ActivationServices { get; } = activationServices;
	public IGrainLifecycle ObservableLifecycle => throw new NotSupportedException();
	public IWorkItemScheduler Scheduler => throw new NotSupportedException();
	public Task Deactivated => Task.CompletedTask;

	public void SetComponent<TComponent>(TComponent? value) where TComponent : class
		=> _components[typeof(TComponent)] = value;

	public void ReceiveMessage(object message) => throw new NotSupportedException();
	public void Activate(Dictionary<string, object>? requestContext, CancellationToken cancellationToken) => throw new NotSupportedException();
	public void Deactivate(DeactivationReason deactivationReason, CancellationToken cancellationToken) => throw new NotSupportedException();
	public void Rehydrate(IRehydrationContext context) => throw new NotSupportedException();
	public void Migrate(Dictionary<string, object>? requestContext, CancellationToken cancellationToken) => throw new NotSupportedException();
	public object GetTarget() => throw new NotSupportedException();
	public object? GetComponent(Type type) => _components.GetValueOrDefault(type);
	public bool Equals(IGrainContext? other) => ReferenceEquals(this, other);
}
