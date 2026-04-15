using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
		var (scope, context) = BuildMiddlewareContext(new TestTenant { Key = "lol" });
		using var _ = scope;

		bool nextInvoked = false;
		await BuildMiddleware(next: req => { nextInvoked = true; return Task.CompletedTask; })
			.InvokeAsync(context);

		scope.ServiceProvider.GetRequiredService<ITenantAccessor<TestTenant>>()
			.Tenant!.Key.ShouldBe("lol");
		nextInvoked.ShouldBeTrue();
	}

	[Fact]
	public async Task Middleware_WhenTenantNotResolved_Returns400AndDoesNotCallNext()
	{
		var (scope, context) = BuildMiddlewareContext();
		using var _ = scope;

		bool nextInvoked = false;
		await BuildMiddleware(next: req => { nextInvoked = true; return Task.CompletedTask; })
			.InvokeAsync(context);

		context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
		nextInvoked.ShouldBeFalse();
	}

	[Fact]
	public async Task Middleware_UsesCustomInvalidTenantResponse()
	{
		var (scope, context) = BuildMiddlewareContext();
		using var _ = scope;

		var options = new MultitenancyMiddlewareOptions()
			.WithInvalidTenantResponse(() => new { code = "custom.error" });
		await BuildMiddleware(options: options).InvokeAsync(context);

		context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
		context.Response.ContentType?.ShouldContain("application/json");
	}

	// ---- Helpers ----

	private static (IServiceScope Scope, DefaultHttpContext Context) BuildMiddlewareContext(TestTenant? tenant = null)
	{
		var services = new ServiceCollection();
		services.AddMultitenancy<TestTenant>();
		services.AddScoped<ITenantHttpResolver<TestTenant>>(_ => new StaticTenantResolver<TestTenant>(tenant));
		var scope = services.BuildServiceProvider().CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Response.Body = new MemoryStream();
		return (scope, context);
	}

	private static MultitenancyMiddleware<TestTenant> BuildMiddleware(
		RequestDelegate? next = null,
		MultitenancyMiddlewareOptions? options = null)
		=> new(next ?? (_ => Task.CompletedTask), options ?? new MultitenancyMiddlewareOptions());
}

// ---- Test doubles ----

file sealed class StaticTenantResolver<TTenant> : ITenantHttpResolver<TTenant>
	where TTenant : class, ITenant
{
	private readonly TTenant? _tenant;

	public StaticTenantResolver(TTenant? tenant) => _tenant = tenant;

	public Task<TTenant?> Resolve(HttpContext httpContext) => Task.FromResult(_tenant);
}
