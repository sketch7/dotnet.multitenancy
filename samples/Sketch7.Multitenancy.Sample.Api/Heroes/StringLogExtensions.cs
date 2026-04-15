namespace Sketch7.Multitenancy.Sample.Api.Heroes;

internal static class StringLogExtensions
{
	extension(string value)
	{
		/// <summary>Removes newline characters to prevent log injection.</summary>
		internal string SanitizeForLog()
			=> value
				.Replace(Environment.NewLine, string.Empty)
				.Replace("\n", string.Empty)
				.Replace("\r", string.Empty);
	}
}