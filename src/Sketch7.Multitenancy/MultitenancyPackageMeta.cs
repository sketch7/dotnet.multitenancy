using System;
using System.Reflection;

namespace Sketch7.Multitenancy
{
	public class MultitenancyPackageMeta
	{
		/// <summary>
		/// Get package assembly.
		/// </summary>
		public static Assembly Assembly = typeof(MultitenancyPackageMeta).Assembly;

		/// <summary>
		/// Gets the package version.
		/// </summary>
		public static Version Version = Assembly.GetName().Version;
	}
}
