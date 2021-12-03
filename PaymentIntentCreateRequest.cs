using Api.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.PaymentGateways
{
    /// <summary>
    /// The required info for creating a payment intent
    /// </summary>
    public class PaymentIntentCreateRequest
    {
        /// <summary>
        /// The products the customer plans to buy
        /// </summary>
        public List<Product> Products;
    }
}
