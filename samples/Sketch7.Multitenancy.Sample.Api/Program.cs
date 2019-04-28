using Grace.AspNetCore.Hosting;
using Grace.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Sketch7.Multitenancy.Sample.Api
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseGrace(new InjectionScopeConfiguration
				{
					Behaviors =
					{
						AllowInstanceAndFactoryToReturnNull = true
					}
				})
				.UseUrls("http://localhost:5001/")
				.UseStartup<Startup>();
	}
}
