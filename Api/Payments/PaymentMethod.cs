using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Payments
{
	
	/// <summary>
	/// A Payment method - this is generally a saved tokenised card.
	/// </summary>
	public partial class PaymentMethod : VersionedContent<uint>
	{
		/// <summary>
		/// The gateway that this payment method is on. Stripe=1 etc.
		/// </summary>
		public uint PaymentGatewayId;
		
		/// <summary>
		/// The token for this payment method on the gateway.
		/// </summary>
		[JsonIgnore]
		public string GatewayToken;
		
		/// <summary>
		/// The time when this method expires.
		/// </summary>
		public DateTime ExpiryUtc;
		
		/// <summary>
		/// 1=Tokenised card.
		/// </summary>
		public uint PaymentMethodTypeId = 1;
		
		/// <summary>
		/// A name of this payment method. This defaults to the last 4 digits of the card number but can be renamed.
		/// </summary>
		public string Name;
	}

}