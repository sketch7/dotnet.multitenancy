using System.Reflection;

namespace Sketch7.Multitenancy;

public class MultitenancyPackageMeta
{
	/// <summary>
	/// Get package assembly.
	/// </summary>
	public static readonly Assembly Assembly = typeof(MultitenancyPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static readonly Version? Version = Assembly.GetName().Version;
}
