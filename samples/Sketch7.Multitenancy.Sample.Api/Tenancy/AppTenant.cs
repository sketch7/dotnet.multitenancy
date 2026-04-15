namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

/// <summary>Represents an application tenant.</summary>
public record AppTenant : ITenant
{
	/// <inheritdoc />
	public string Key { get; init; } = null!;

	/// <summary>Gets the tenant display name.</summary>
	public string Name { get; init; } = null!;

	/// <summary>Gets the organization identifier e.g. <c>sketch7</c>.</summary>
	public string Organization { get; init; } = null!;
}