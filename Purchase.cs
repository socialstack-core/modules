using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Purchases
{
	
	/// <summary>
	/// A Purchase
	/// </summary>
	public partial class Purchase : VersionedContent<uint>
	{
		/// <summary>
		/// The total cost of this purchase
		/// </summary>
		public long TotalCostPence;

		/// <summary>
		/// The currency of this purchase
		/// </summary>
		public string Currency;

        /// <summary>
        /// The description of the purchase
        /// </summary>
        [DatabaseField(Length = 2000)]
		public string Description;

		/// <summary>
		/// Id of the purchase in the used third party service e.g. stripe PaymentIntent Id
		/// </summary>
		[DatabaseField(Length = 200)]
		public string ThirdPartyId;

		/// <summary>
		/// Is the payment for this purchase processed?
		/// </summary>
		public bool IsPaymentProcessed;

		/// <summary>
		/// Did this payment fail?
		/// </summary>
		public bool DidPaymentFail;

		/// <summary>
		/// Does this payment require action?
		/// </summary>
		public bool DoesPaymentRequireAction;
	}

}