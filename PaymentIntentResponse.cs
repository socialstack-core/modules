using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.PaymentGateways
{
    /// <summary>
    /// Response from the API when creating a new payment intent
    /// </summary>
    public class PaymentIntentResponse
    {
        /// <summary>
        /// Stripe client secret
        /// </summary>
        public string ClientSecret;

        /// <summary>
        /// The purchase
        /// </summary>
        public uint PurchaseId;
    }

    /// <summary>
    /// Response from the API when creating a new setup intent
    /// </summary>
    public class SetupIntentResponse
    {
        /// <summary>
        /// Stripe client secret
        /// </summary>
        public string ClientSecret;
    }

    /// <summary>
    /// Response from the API when getting payment methods
    /// </summary>
    public class PaymentMethodsResponse
    {
        /// <summary>
        /// List of payment methods already existing
        /// </summary>
        public StripeList<PaymentMethod> PaymentMethods;
    }

    /// <summary>
    /// Response from the API when getting a specific payment method
    /// </summary>
    public class PaymentMethodResponse
    {
        /// <summary>
        /// A specific existing stripe payment method
        /// </summary>
        public PaymentMethod PaymentMethod;
    }
}
