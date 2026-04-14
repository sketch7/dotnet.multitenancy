# To do

- [ ] Orleans
  - [ ] support Grain activation tenancy
- [ ] TenantHttpResolver // order matters - first resolve exit
  - [ ] AddEndpointResolver(opts => opts.Name = "tenant");
  - [ ] AddHeaderResolver(opts => opts.Name = "X-SSV-Tenant");
  - [ ] AddDomainResolver(opts => opts.Domains = ...);
  - [ ] Add<HeaderResolver>()
  - [ ] UseUnresolvedHandler<TenantUnresolvedHandler>()
- [ ] Singleton Key
- [ ] refactor asPerTenant