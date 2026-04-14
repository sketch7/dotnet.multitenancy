using System.Diagnostics;

namespace Sketch7.Multitenancy.Sample.Api.Tenancy;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class AppTenantConfig
{
	protected string DebuggerDisplay => $"Id: '{Id}', Name: '{Name}', IsDisabled: {IsDisabled}, Organization: {Organization}";

	/// <summary>
	/// Gets or sets the tenant id e.g. 'lol'.
	/// </summary>
	public string Id { get; set; } = default!;

	/// <summary>
	/// Gets or sets the tenant name e.g. 'League of Legends'.
	/// </summary>
	public string Name { get; set; } = default!;

	/// <summary>
	/// Gets or sets whether the tenant is disabled.
	/// </summary>
	public bool IsDisabled { get; set; }

	/// <summary>
	/// Gets or sets the organization e.g. 'riot'.
	/// </summary>
	public string Organization { get; set; } = default!;
}
