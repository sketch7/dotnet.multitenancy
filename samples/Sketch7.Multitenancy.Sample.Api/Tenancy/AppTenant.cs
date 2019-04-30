using System.Diagnostics;

namespace Sketch7.Multitenancy.Sample.Api.Tenancy
{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class AppTenant : ITenant
	{
		protected string DebuggerDisplay => $"Key: '{Key}', Name: '{Name}', Organization: {Organization}";

		/// <inheritdoc />
		public string Key { get; set; }

		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the organization e.g. 'sketch7'.
		/// </summary>
		public string Organization { get; set; }
	}
}
