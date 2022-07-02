using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PaymentGateways
{
	
	/// <summary>
	/// A PaymentGateway
	/// </summary>
	public partial class PaymentGateway : VersionedContent<uint>
	{
		// Example fields. None are required:
		/*
        /// <summary>
        /// The name of the paymentGateway
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Name;
		
		/// <summary>
		/// The content of this paymentGateway.
		/// </summary>
		[Localized]
		public string BodyJson;

		/// <summary>
		/// The feature image ref
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;
		*/
		
	}

}