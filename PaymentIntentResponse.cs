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
    }
}
