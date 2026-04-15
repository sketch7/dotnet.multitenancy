namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

/// <summary>Represents an application tenant.</summary>
public record AppTenant : ITenant
{
	/// <inheritdoc />
	public required string Key { get; init; }

	/// <summary>Gets the tenant display name.</summary>
	public required string Name { get; init; }

	/// <summary>Gets the organization identifier e.g. <c>sketch7</c>.</summary>
	public required string Organization { get; init; }
}