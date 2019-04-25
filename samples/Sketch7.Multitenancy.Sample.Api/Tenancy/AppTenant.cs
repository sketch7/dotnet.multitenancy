namespace Sketch7.Multitenancy.Sample.Api.Tenancy
{
	public class AppTenant : ITenant
	{
		/// <inheritdoc />
		public string Key { get; set; }

		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the organization e.g. 'sketch7'.
		/// </summary>
		public string Organization { get; set; }
	}
}
