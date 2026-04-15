namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

/// <summary>Represents a tenant configuration entry.</summary>
public record AppTenantConfig
{
	/// <summary>Gets the tenant id e.g. <c>lol</c>.</summary>
	public string Id { get; init; } = null!;

	/// <summary>Gets the tenant name e.g. <c>League of Legends</c>.</summary>
	public string Name { get; init; } = null!;

	/// <summary>Gets whether the tenant is disabled.</summary>
	public bool IsDisabled { get; init; }

	/// <summary>Gets the organization e.g. <c>riot</c>.</summary>
	public string Organization { get; init; } = null!;
}