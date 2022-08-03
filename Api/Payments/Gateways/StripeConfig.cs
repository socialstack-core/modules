using Api.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}
