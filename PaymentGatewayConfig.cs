using Api.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.PaymentGateways
{
    /// <summary>
	/// Configuration for payment gateways
    /// </summary>
    public class PaymentGatewayConfig : Config
    {
        /// <summary>
		/// Your stripe publishable API Key
		/// </summary>
        [Frontend]
        public string StripePublishableKey { get; set; } = "";

        /// <summary>
		/// Your stripe secret API Key
		/// </summary>
        public string StripeSecretKey { get; set; } = "";

        /// <summary>
		/// Your stripe payment webhook endpoint secret
		/// </summary>
        public string StripePaymentEndpointSecret { get; set; } = "";

        /// <summary>
		/// Your stripe invoice webhook endpoint secret
		/// </summary>
        public string StripeInvoiceEndpointSecret { get; set; } = "";

        /// <summary>
		/// Your stripe customer webhook endpoint secret
		/// </summary>
        public string StripeCustomerEndpointSecret { get; set; } = "";

        /// <summary>
		/// Your payment currency;
		/// </summary>
        public string PaymentCurrency { get; set; } = "gbp";

        /// <summary>
        /// Should a stripe customer be automatically created for the user after the user has been created
        /// </summary>
        public bool CreateStripeCustomerAfterUserCreate { get; set;} = false;
    }
}
