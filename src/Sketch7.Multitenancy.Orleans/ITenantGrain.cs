namespace Sketch7.Multitenancy.Orleans;

/// <summary>
/// Marks an Orleans grain as tenant-aware.
/// The grain primary key is expected to be prefixed with the tenant key in the format <c>{tenantKey}/{grainKey}</c>.
/// </summary>
public interface ITenantGrain : IGrain
{
}