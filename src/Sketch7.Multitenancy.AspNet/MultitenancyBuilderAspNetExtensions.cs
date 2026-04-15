#pragma warning disable IDE0130 // Namespace intentionally matches extended type (Sketch7.Multitenancy) not folder
using Microsoft.Extensions.DependencyInjection;
using Sketch7.Multitenancy.AspNet;

// ReSharper disable once CheckNamespace
namespace Sketch7.Multitenancy;

/// <summary>
/// Extension members for registering ASP.NET Core multitenancy services on <see cref="MultitenancyBuilder{TTenant}"/>.
/// </summary>
public static class AspNetMultitenancyBuilderExtensions
{
	extension<TTenant>(MultitenancyBuilder<TTenant> builder) where TTenant : class, ITenant
	{
		/// <summary>
		/// Registers the HTTP-based tenant resolver.
		/// </summary>
		/// <typeparam name="TResolver">The resolver implementation type.</typeparam>
		public MultitenancyBuilder<TTenant> WithHttpResolver<TResolver>()
			where TResolver : class, ITenantHttpResolver<TTenant>
		{
			builder.Services.AddScoped<ITenantHttpResolver<TTenant>, TResolver>();
			return builder;
		}
	}
}
