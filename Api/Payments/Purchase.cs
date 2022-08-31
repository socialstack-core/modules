using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Payments
{
	
	/// <summary>
	/// A purchase for a set of products which are attached as a set of ProductQuantities.
	/// </summary>
	public partial class Purchase : VersionedContent<uint>
	{
		/// <summary>
		/// 0 = Created, not completed.
		/// 200 = Fulfilled.
		/// 202 = Payment successful. Fulfilment not yet completed.
		/// 101 = Started submit to gateway. If a purchase is stuck in this state you MUST check if the gateway received the request at all.
		/// 102 = Pending at payment gateway.
		/// 500 = Failed (payment gateway rejection).
		/// 400 = Failed (user sourced fault).
		/// </summary>
		public uint Status;

		/// <summary>
		/// True if this purchase has multiple subscriptions attached to it which fulfil when the purchase does.
		/// The subscriptions are attached via a mapping called "Subscriptions".
		/// </summary>
		public bool MultiExecute;

		/// <summary>
		/// The locale the purchase will occur in. This is used to specify the actual price paid.
		/// </summary>
		public uint LocaleId;

		/// <summary>
		/// An ID provided by the payment gateway.
		/// </summary>
		public string PaymentGatewayInternalId;

		/// <summary>
		/// The gateway ID. Stripe is gateway=1.
		/// </summary>
		public uint PaymentGatewayId;

		/// <summary>
		/// The payment method to use. This is used to specify PaymentGatewayId.
		/// </summary>
		public uint PaymentMethodId;

		/// <summary>
		/// The currency the payment is being made in.
		/// </summary>
		public string CurrencyCode;

		/// <summary>
		/// The total cost in the currency codes native atomic unit.
		/// </summary>
		public ulong TotalCost;

		/// <summary>
		/// A field for identifying duplicate purchase requests. Used by the content type.
		/// </summary>
		public ulong ContentAntiDuplication;

		/// <summary>
		/// The content type that requested the payment. E.g. "Subscription" or "ShoppingCart".
		/// </summary>
		public string ContentType;

		/// <summary>
		/// The ID of the content type that requested the payment. E.g. an ID of a partiuclar subscription.
		/// </summary>
		public uint ContentId;
	}

}