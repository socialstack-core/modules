using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Payments
{
    /// <summary>Handles coupon endpoints.</summary>
    [Route("v1/coupon")]
	public partial class CouponController : AutoController<Coupon>
    {
		/// <summary>
		/// Checks a coupon code and if it's not disabled, returns its info.
		/// </summary>
		/// <param name="couponCode"></param>
		/// <exception cref="PublicException"></exception>
		[HttpGet("check/{couponCode}")]
		public virtual async ValueTask CheckCoupon([FromRoute] string couponCode)
		{
			var context = await Request.GetContext();

			// Get a coupon code:
			var coupon = await _service.Where("Token=?", DataOptions.IgnorePermissions).Bind(couponCode).First(context);

			if (coupon == null)
			{
				throw new PublicException("That coupon code was not found", "not_found");
			}

			if (coupon.Disabled || (coupon.ExpiryDateUtc.HasValue && coupon.ExpiryDateUtc.Value < System.DateTime.UtcNow))
			{
				throw new PublicException("Unfortunately that coupon code is no longer valid", "not_valid");
			}

			await OutputJson(context, coupon, "minSpendPrice,discountAmount");
		}

	}
}