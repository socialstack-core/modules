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
			Events.DeliveryOption.BeforeCreate.AddEventListener(async (Context context, DeliveryOption option) =>
			{
				//Add an anonymous key to the address
				DeliveryOption matchingOption = null;
				while(option.AnonKey == null || matchingOption != null)
				{
					option.AnonKey = RandomToken.Generate(16);
					matchingOption = await Where("AnonKey = ?", DataOptions.IgnorePermissions).Bind(option.AnonKey).First(context);
				}

				return option;
			});
		}
		
	}
    
}
