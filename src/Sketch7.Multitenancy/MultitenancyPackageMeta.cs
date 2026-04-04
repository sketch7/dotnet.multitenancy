using System.Reflection;

namespace Sketch7.Multitenancy;

public class MultitenancyPackageMeta
{
	/// <summary>
	/// Get the package assembly.
	/// </summary>
	public static readonly Assembly PackageAssembly = typeof(MultitenancyPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static readonly Version? PackageVersion = PackageAssembly.GetName().Version;
}
