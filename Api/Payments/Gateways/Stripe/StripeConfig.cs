using Api.Configuration;

namespace Api.Payments
{
    /// <summary>
	/// Configuration for Stripe
    /// </summary>
    public class StripeConfig : Config
    {
        /// <summary>
		/// Your stripe publishable API Key
		/// </summary>
        [Frontend]
        public string PublishableKey { get; set; } = "";

		/// <summary>
		/// Your stripe shortcode used to easily identify payment type and pass to 3rd paries 
		/// </summary>
		public string ShortCode { get; set; } = "SPE";

		/// <summary>
		/// Your stripe secret API Key
		/// </summary>
		public string SecretKey { get; set; } = "";

        /// <summary>
		/// Your stripe payment webhook endpoint secret
		/// </summary>
        public string PaymentEndpointSecret { get; set; } = "";

        /// <summary>
		/// Your stripe invoice webhook endpoint secret
		/// </summary>
        public string InvoiceEndpointSecret { get; set; } = "";
		
        /// <summary>
		/// Your stripe customer webhook endpoint secret
		/// </summary>
        public string SubscriptionEndpointSecret { get; set; } = "";

        /// <summary>
        /// Expose additional logging to trace payments when testing
        /// </summary>
        public bool VerboseLogging {get;set;} = false;

		/// <summary>
		/// Flag to expose if stripe is enabled 
		/// </summary>
		[Frontend]
		public bool IsEnabled { get; set; } = false;

		/// <summary>
		/// Flag to indicate if we use our own hosted form or the merchants iframe 
		/// This is related to the level of PCI complianace
		/// </summary>
		[Frontend]
		public bool OwnFormEnabled { get; set; } = false;

		/// <summary>
		/// Flag to indicate if we use of merchants hosted page
		/// This is related to the level of PCI complianace
		/// </summary>
		[Frontend]
		public bool HostedPageEnabled { get; set; } = false;

		/// <summary>
		/// Flag to expose if stripe is able to save card details for reuse 
		/// </summary>
		[Frontend]
		public bool CanSaveCards { get; set; } = true;

	}
}
