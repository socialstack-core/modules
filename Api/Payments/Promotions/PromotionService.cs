using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Payments
{
	/// <summary>
	/// Handles promotions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PromotionService : AutoService<Promotion>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PromotionService() : base(Events.Promotion)
        {
			// Example admin page install:
			InstallAdminPages("Promotions", "fa:fa-percent", [ "id", "name" ], null, "ecommerce");
		}
	}
    
}
