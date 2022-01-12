using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Discounts
{
	/// <summary>
	/// Handles discounts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DiscountService : AutoService<Discount>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DiscountService() : base(Events.Discount)
        {
			// Example admin page install:
			InstallAdminPages("Discounts", "fa:fa-badge-dollar", new string[] { "id", "name" });

			Events.Discount.BeforeCreate.AddEventListener(async (Context context, Discount discount) =>
			{
				if (discount == null)
				{
					return discount;
				}

				if (discount.DiscountPercentage < 0 || discount.DiscountPercentage > 100)
				{
					throw new PublicException("Coupon discount percentage needs to be a value between 0-100", "bad_percentage");
				}

				return discount;
			});

			Events.Discount.BeforeUpdate.AddEventListener(async (Context context, Discount discount) =>
			{
				if (discount == null)
				{
					return discount;
				}

				if (discount.DiscountPercentage < 0 || discount.DiscountPercentage > 100)
				{
					throw new PublicException("Coupon discount percentage needs to be a value between 0-100", "bad_percentage");
				}

				return discount;
			});
		}
	}
    
}
