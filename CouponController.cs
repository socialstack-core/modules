using Microsoft.AspNetCore.Mvc;

namespace Api.Coupons
{
    /// <summary>Handles coupon endpoints.</summary>
    [Route("v1/coupon")]
	public partial class CouponController : AutoController<Coupon>
    {
    }
}