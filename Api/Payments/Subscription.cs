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
		/// The timeslot index that this subscription was last charged. Usually in months. 
		/// Null indicates it has not been charged yet.
		/// This is defined as "The number of months that have occurred since 2020".
		/// </summary>
		public uint ChargedTimeslotId;

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