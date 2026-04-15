namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Marks an Orleans grain as tenant-aware.
/// The grain primary key is expected to follow the format <c>tenant/{tenantKey}/{grainId}</c>.
/// </summary>
public interface ITenantGrain : IGrain
{
}