using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Payments
{
	/// <summary>
	/// Handles prices.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PriceService : AutoService<Price>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PriceService() : base(Events.Price)
        {
			InstallAdminPages("Prices", "fa:fa-rocket", new string[] { "id", "name", "amount", "currencyCode" });

			Cache();
		}
	}
    
}
