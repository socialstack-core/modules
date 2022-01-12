using System;
using Api.Database;
using Api.Discounts;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Coupons
{

	/// <summary>
	/// A Coupon
	/// </summary>
	[HasVirtualField("Discount", typeof(Discount), "DiscountId")]
	public partial class Coupon : VersionedContent<uint>
	{
		/// <summary>
		/// The discount this coupon code is for.
		/// </summary>
		public uint DiscountId;

		/// <summary>
		/// The code the customer can use to activate the coupon
		/// </summary>
		[DatabaseField(Length = 40)]
		public string Code;
	
		/// <summary>
		/// Has the coupon been redeemed?
		/// </summary>
		public bool IsRedeemed;
	}

}