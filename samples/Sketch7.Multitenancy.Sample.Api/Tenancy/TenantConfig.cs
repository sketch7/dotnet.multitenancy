namespace Sketch7.Multitenancy.Sample.Api.Tenancy
{
	public class TenantConfig
	{
		protected string DebuggerDisplay => $"Id: '{Id}', Name: '{Name}', IsDisabled: {IsDisabled}, Organization: {Organization}";

		/// <summary>
		/// Gets or sets the brand id e.g. 'loki'.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the brand name e.g. 'Loki'.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets whether the brand is disabled or not.
		/// </summary>
		public bool IsDisabled { get; set; }

		/// <summary>
		/// Gets or sets the organization e.g. 'cpm'.
		/// </summary>
		public string Organization { get; set; }
	}
}