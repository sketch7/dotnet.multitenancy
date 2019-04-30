//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Text.RegularExpressions;

//namespace Sketch7.Multitenancy.Sample.Api.Tenancy
//{
//	public interface IAppTenantResolver
//	{
//		/// <summary>
//		/// Resolve tenant by domain.
//		/// </summary>
//		/// <param name="domain"></param>
//		/// <returns></returns>
//		AppTenant ResolveByDomain(string domain);

//		/// <summary>
//		/// Resolve tenant by primary key, used primary for grains.
//		/// </summary>
//		/// <param name="primaryKey">Primary key to resolve tenant from, it has to be in the form of 'tenant/{tenantId}' e.g. 'tenant/sketch7/xyz'.</param>
//		/// <returns></returns>
//		AppTenant ResolveByPrimaryKey(string primaryKey);
//	}

//	public class AppTenantResolver : IAppTenantResolver
//	{
//		private const string TenantGroupKey = "tenant";
//		private readonly IAppTenantRegistry _tenantRegistry;
//		private readonly IBrandConfigService _brandConfig;
//		private readonly List<BrandResolverConfig> _brandDomainResolverConfigs;
//		private readonly ConcurrentDictionary<string, BrandRef> _domainBrandRef = new ConcurrentDictionary<string, BrandRef>();
//		private readonly Regex _regex = new Regex($@"tenant\/(?'{TenantGroupKey}'[\w@-]+)\/?(.*)", RegexOptions.Compiled);

//		public AppTenantResolver(
//			SharedContractOptions sharedContractOptions,
//			IAppTenantRegistry tenantRegistry,
//			IBrandConfigService brandConfig
//		)
//		{
//			_tenantRegistry = tenantRegistry;
//			_brandConfig = brandConfig;
//			_brandDomainResolverConfigs = BuildConfigResolver(sharedContractOptions.BrandResolverConfigs).ToList();
//		}

//		public AppTenant ResolveByDomain(string domain)
//		{
//			var brandRef = ResolveBrandRefByDomain(domain);
//			return brandRef == null ? null : _tenantRegistry.Get(brandRef.Key);
//		}

//		public AppTenant ResolveByPrimaryKey(string primaryKey)
//		{
//			if (string.IsNullOrEmpty(primaryKey))
//				throw new ArgumentException("Primary key must be defined.", nameof(primaryKey));

//			var matches = _regex.Match(primaryKey);
//			if (!matches.Success)
//				return null;

//			var brandId = matches.Groups[TenantGroupKey].Value;
//			return _tenantRegistry.Get(brandId);
//		}

//		private BrandRef ResolveBrandRefByDomain(string domain)
//		{
//			return _domainBrandRef.GetOrAdd(domain, _ =>
//			{
//				foreach (var resolverConfig in _brandDomainResolverConfigs)
//				{
//					if (resolverConfig.DomainPatterns == null)
//						continue;

//					foreach (var domainRegex in resolverConfig.DomainPatterns)
//					{
//						if (!domainRegex.IsMatch(domain))
//							continue;

//						var brandRef = new BrandRef
//						{
//							Key = resolverConfig.Brand,
//							Environment = resolverConfig.Env
//						};
//						return brandRef;
//					}
//				}
//				return null;
//			});
//		}

//		private static IEnumerable<BrandResolverConfig> BuildConfigResolver(IEnumerable<BrandResolverConfigRaw> tenantResolverConfigs)
//		{
//			if (tenantResolverConfigs == null)
//				yield break;

//			foreach (var resolverConfig in tenantResolverConfigs)
//			{
//				yield return new BrandResolverConfig
//				{
//					Brand = resolverConfig.Brand,
//					Env = resolverConfig.Env,
//					DomainPatterns = resolverConfig.DomainPatterns?.Select(pattern => new Regex(pattern, RegexOptions.Compiled)).ToList()
//				};
//			}
//		}
//	}
//}
