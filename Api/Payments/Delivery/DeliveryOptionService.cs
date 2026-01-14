using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.PasswordResetRequests;

namespace Api.Payments
{
	/// <summary>
	/// Handles stored delivery options to ensure what is displayed to the user is followed through.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DeliveryOptionService : AutoService<DeliveryOption, uint>
    {
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DeliveryOptionService() : base(Events.DeliveryOption)
        {
		}
		
	}
    
}
