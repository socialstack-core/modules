using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A Subscription.
	/// </summary>
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
		/// 0 = The default, it's in months.   (currently the only supported option)
		/// 1 = Quarters
		/// 2 = Years
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
		/// The subscription locale. Currency is selected based on this.
		/// </summary>
		public uint LocaleId;
	}

}