﻿using Sketch7.Multitenancy.Sample.Api.Heroes;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddAppBlizzardServices(this IServiceCollection services)
		{
			services.AddSingleton<IHeroDataClient, MockHotsHeroDataClient>();
			return services;
		}

		public static IServiceCollection AddAppRiotServices(this IServiceCollection services)
		{
			services.AddSingleton<IHeroDataClient, MockLoLHeroDataClient>();
			return services;
		}
	}
}