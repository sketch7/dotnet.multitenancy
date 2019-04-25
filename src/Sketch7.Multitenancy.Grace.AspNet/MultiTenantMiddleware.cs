using Grace.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Sketch7.Multitenancy.Grace.AspNet
{
	public class MultitenancyMiddleware<TTenant>
		where TTenant : class, ITenant
	{
		private readonly RequestDelegate _next;
		private readonly string _tenantInvalidJson;
		private readonly ITenantHttpResolver<TTenant> _tenantHttpResolver;

		public MultitenancyMiddleware(
			RequestDelegate next,
			MultitenancyMiddlewareOptions options,
			ITenantHttpResolver<TTenant> tenantHttpResolver
		)
		{
			_next = next;
			_tenantHttpResolver = tenantHttpResolver;
			_tenantInvalidJson = JsonConvert.SerializeObject(options.ConfigureInvalidTenant());
		}

		public async Task Invoke(
			HttpContext httpContext,
			IExportLocatorScope locatorScope
		)
		{
			var tenant = await _tenantHttpResolver.Resolve(httpContext);
			if (tenant == null)
			{
				httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				httpContext.Response.ContentType = "application/json";
				await httpContext.Response.WriteAsync(_tenantInvalidJson);
			}

			locatorScope.SetTenantContext(tenant);
			await _next(httpContext);
		}
	}


	public static class MultitenancyMiddlewareExtensions
	{
		public static IApplicationBuilder UseMultitenancy<TTenant>(this IApplicationBuilder builder, MultitenancyMiddlewareOptions options) where TTenant : class, ITenant
			=> builder.UseMiddleware<MultitenancyMiddleware<TTenant>>(options ?? new MultitenancyMiddlewareOptions());
	}

	public class MultitenancyMiddlewareOptions
	{
		public Func<object> ConfigureInvalidTenant { get; set; } = GetDefaultInvalidTenant;

		private static object GetDefaultInvalidTenant()
			=> new { errorCode = "error.invalid:tenant" };
	}
}