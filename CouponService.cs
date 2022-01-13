using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System.Linq;
using System;

namespace Api.Coupons
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
			// Example admin page install:
			InstallAdminPages("Coupons", "fa:fa-barcode", new string[] { "id", "name" });

			Events.Coupon.BeforeCreate.AddEventListener(async (Context context, Coupon coupon) =>
			{
				if (coupon == null)
				{
					return coupon;
				}

				if (string.IsNullOrWhiteSpace(coupon.Code))
				{
					coupon.Code = RandomCouponCode(12);
				}

				var duplicateCoupon = await Where("Code=?").Bind(coupon.Code).First(context);

				if (duplicateCoupon != null)
                {
					throw new PublicException("Another coupon already has the same code.", "duplicate_code");
                }

				return coupon;
			});
		}

		public async Task<Coupon> RedeemCoupon(Context context, Coupon coupon)
        {
			if (await StartUpdate(context, coupon, DataOptions.IgnorePermissions))
			{
				coupon.IsRedeemed = true;

				coupon = await FinishUpdate(context, coupon);
			}

			return coupon;
        }

		private static string RandomCouponCode(int length)
		{
			Random random = new Random();

			const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}
    
}
