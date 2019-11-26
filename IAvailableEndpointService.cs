using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.AvailableEndpoints
{
	/// <summary>
	/// This optional service is for self-documentation and automated testing.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IAvailableEndpointService
	{
		/// <summary>
		/// Lists all available endpoints on this site.
		/// </summary>
		/// <returns></returns>
		List<Endpoint> List();
    }
}
