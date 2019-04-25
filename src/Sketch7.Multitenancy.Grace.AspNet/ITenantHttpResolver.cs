using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Sketch7.Multitenancy.Grace.AspNet
{
	// todo: move to Sketch7.Multitenancy.AspNet
	public interface ITenantHttpResolver<TTenant>
		where TTenant : class, ITenant
	{
		Task<TTenant> Resolve(HttpContext httpContext);
	}

}
