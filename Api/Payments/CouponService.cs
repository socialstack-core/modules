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
	/// Handles coupons.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CouponService : AutoService<Coupon>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CouponService() : base(Events.Coupon)
        {
			InstallAdminPages("Coupons", "fa:fa-rocket", new string[] { "id", "token" });
			
			Events.Coupon.BeforeCreate.AddEventListener((Context context, Coupon coupon) => {
				
				if(coupon == null)
				{
					return new ValueTask<Coupon>(coupon);
				}
				
				if(string.IsNullOrEmpty(coupon.Token)){
					// Generate default token text:
					coupon.Token = RandomToken.Generate(10);
				}
				
				return new ValueTask<Coupon>(coupon);
			});

			Events.Purchase.BeforeUpdate.AddEventListener(async (Context context, Purchase toUpdate, Purchase original) => {

				if (toUpdate == null)
				{
					return null;
				}

				// If this is a status update on a purchase which used a coupon, check if we need to reduce the max # of people.
				if (toUpdate.Status != original.Status && toUpdate.Status == 202 && toUpdate.CouponId != 0)
				{
					// Coupon can now be considered to have been used.
					var coupon = await Get(context, toUpdate.CouponId, DataOptions.IgnorePermissions);

					if (coupon != null && coupon.MaxNumberOfPeople > 0)
					{
						// Future TODO: Ask the data API to decrease by 1 for large cluster concurrency.
						var newMax = coupon.MaxNumberOfPeople - 1;

						if (newMax <= 0)
						{
							// Disable it:
							await Update(context, coupon, (Context c, Coupon cToUpdate, Coupon orig) =>
							{

								cToUpdate.MaxNumberOfPeople = 0;
								cToUpdate.Disabled = true;

							}, DataOptions.IgnorePermissions);
						}
						else
						{
							// Just decrease:
							await Update(context, coupon, (Context c, Coupon cToUpdate, Coupon orig) =>
							{

								cToUpdate.MaxNumberOfPeople = newMax;

							}, DataOptions.IgnorePermissions);
						}
					}
				}

				return toUpdate;
			});
			
		}
	}
    
}
