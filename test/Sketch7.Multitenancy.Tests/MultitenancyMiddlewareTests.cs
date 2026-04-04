using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Sketch7.Multitenancy;
using Sketch7.Multitenancy.AspNet;
using Xunit;

namespace Sketch7.Multitenancy.Tests;

/// <summary>
/// Tests for <see cref="MultitenancyMiddleware{TTenant}"/>.
/// </summary>
public class MultitenancyMiddlewareTests
{
	[Fact]
	public async Task Middleware_SetsTenantAccessor_WhenTenantResolved()
	{
		// Arrange
		var tenant = new TestTenant { Key = "lol" };
		var resolver = new StaticTenantResolver<TestTenant>(tenant);

		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>();
		services.AddScoped<ITenantHttpResolver<TestTenant>>(_ => resolver);

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

		bool nextInvoked = false;
		var middleware = new MultitenancyMiddleware<TestTenant>(
			_ => { nextInvoked = true; return Task.CompletedTask; },
			new MultitenancyMiddlewareOptions());

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		var accessor = scope.ServiceProvider.GetRequiredService<ITenantAccessor<TestTenant>>();
		accessor.Tenant.ShouldNotBeNull();
		accessor.Tenant!.Key.ShouldBe("lol");
		nextInvoked.ShouldBeTrue();
	}

	[Fact]
	public async Task Middleware_Returns400_WhenTenantNotResolved()
	{
		// Arrange
		var resolver = new StaticTenantResolver<TestTenant>(null);

		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>();
		services.AddScoped<ITenantHttpResolver<TestTenant>>(_ => resolver);

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Response.Body = new System.IO.MemoryStream();

		bool nextInvoked = false;
		var middleware = new MultitenancyMiddleware<TestTenant>(
			_ => { nextInvoked = true; return Task.CompletedTask; },
			new MultitenancyMiddlewareOptions());

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
		nextInvoked.ShouldBeFalse();
	}

	[Fact]
	public async Task Middleware_DoesNotCallNext_WhenTenantNotResolved()
	{
		// Arrange
		var resolver = new StaticTenantResolver<TestTenant>(null);

		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>();
		services.AddScoped<ITenantHttpResolver<TestTenant>>(_ => resolver);

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Response.Body = new System.IO.MemoryStream();

		int nextCallCount = 0;
		var middleware = new MultitenancyMiddleware<TestTenant>(
			_ => { nextCallCount++; return Task.CompletedTask; },
			new MultitenancyMiddlewareOptions());

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		nextCallCount.ShouldBe(0);
	}

	[Fact]
	public async Task Middleware_UsesCustomInvalidTenantResponse()
	{
		// Arrange
		var resolver = new StaticTenantResolver<TestTenant>(null);

		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>();
		services.AddScoped<ITenantHttpResolver<TestTenant>>(_ => resolver);

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Response.Body = new System.IO.MemoryStream();

		var options = new MultitenancyMiddlewareOptions()
			.WithInvalidTenantResponse(() => new { code = "custom.error" });

		var middleware = new MultitenancyMiddleware<TestTenant>(
			_ => Task.CompletedTask,
			options);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
		context.Response.ContentType?.ShouldContain("application/json");
	}
}

// ---- Test doubles ----

/// <summary>
/// A simple resolver that always returns the provided tenant.
/// </summary>
file class StaticTenantResolver<TTenant> : ITenantHttpResolver<TTenant>
	where TTenant : class, ITenant
{
	private readonly TTenant? _tenant;

	public StaticTenantResolver(TTenant? tenant) => _tenant = tenant;

	public Task<TTenant?> Resolve(HttpContext httpContext) => Task.FromResult(_tenant);
}
