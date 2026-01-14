using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;


namespace Api.Payments
{
	
	/// <summary>
	/// A Subscription.
	/// </summary>
	[ListAs("Subscriptions", Explicit = true)]
	public partial class Subscription : VersionedContent<uint>
	{
		/// <summary>
		/// The datetime this subscription was last charged.
		/// </summary>
		public DateTime LastChargeUtc;

		/// <summary>
		/// The datetime this subscription will next be charged, if it is not paused or cancelled.
		/// </summary>
		public DateTime NextChargeUtc;

		/// <summary>
		/// Subscription tax jurisdiction.
		/// </summary>
		[JsonIgnore]
		public string TaxJurisdiction;

		/// <summary>
		/// True if this subscription will cancel on the next billing cycle.
		/// </summary>
		public bool WillCancel;

		/// <summary>
		/// ID of a coupon to reuse on this subscription.
		/// </summary>
		public uint CouponId;

		/// <summary>
		/// 0 = The default, it's in months.   (currently the only supported option)
		/// 1 = Quarters
		/// 2 = Years
		/// 3 = Weeks
		/// </summary>
		public uint TimeslotFrequency;

		/// <summary>
		/// The status of this subscription. 0=Not yet started, 1=Active, 2=Cancelled (by user), 3=Paused (by failed payment)
		/// </summary>
		public uint Status;

		/// <summary>
		/// The payment method to use when billing this subscription.
		/// </summary>
		public uint PaymentMethodId;
		
		/// <summary>
		/// If physical products and user chooses method, the delivery option.
		/// </summary>
		public uint DeliveryOptionId;
		
		/// <summary>
		/// If physical products, the target address.
		/// </summary>
		public uint DeliveryAddressId;

		/// <summary>
		/// The billing address to use.
		/// </summary>
		public uint BillingAddressId;

		/// <summary>
		/// The subscription locale. Currency is selected based on this.
		/// </summary>
		public uint LocaleId;
	}

}