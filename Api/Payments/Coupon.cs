using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A Coupon
	/// </summary>
	public partial class Coupon : VersionedContent<uint>
	{
		/// <summary>
		/// The token.
		/// </summary>
        [DatabaseField(Length = 10)]
		public string Token;
		
		/// <summary>
		/// The number of people who can use this coupon before it expires.
		/// 0 indicates there is no limit.
		/// </summary>
		[Data("help", "Max number of people who can use this coupon before it expires. Zero indicates there is no limit.")]
		public uint MaxNumberOfPeople = 1;
		
		/// <summary>
		/// When this is set, the coupon can no longer be used. A coupon becomes disabled automatically when the max people is reached.
		/// </summary>
		public bool Disabled;
		
		/// <summary>
		/// Expiry date of the coupon. It can't be used after this date.
		/// </summary>
		public DateTime? ExpiryDateUtc;
		
		/// <summary>
		/// If set and there are subscriptions in the checkout, this time delay will be added to the next payment.
		/// When combined with discount percent, this can effectively create a free trial of a subscription.
		/// </summary>
		public uint SubscriptionDelayDays;
		
		/// <summary>
		/// a 0-100 overall discount percent.
		/// If subscriptions are present and there is a 100% discount, the card will still be authorised.
		/// </summary>
		public uint DiscountPercent;
		
		/// <summary>
		/// A price indicating a specific discount.
		/// </summary>
		[Localized]
		[Data("help", "Optional fixed amount discount. For example, £5 off if you spend £20 or more.")]
		[Data("type", "select")]
		[Data("contentType", "Price")]
		public uint DiscountFixedAmount;

		/// <summary>
		/// True if delivery becomes free, if delivery is applicable.
		/// </summary>
		public bool FreeDelivery;

		/// <summary>
		/// A price indicating minimum spend required for the coupon to be usable on the purchase.
		/// </summary>
		[Localized]
		[Data("help", "Optional minimum spend required for the coupon to be usable on the purchase.")]
		[Data("type", "select")]
		[Data("contentType", "Price")]
		public uint MinimumSpendAmount;
		
	}

}