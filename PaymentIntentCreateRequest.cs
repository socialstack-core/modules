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
        public List<IdQuantity> Products;

        /// <summary>
        /// An internal reference for what the purchase relates to if needed.
        /// </summary>
        public string CustomReference;

        ///<summary>
        /// Email address to send reciepts to 
        ///</summary>
        public string RecieptEmail;
    }

    /// <summary>
    /// The required info for retrieving a specific payment method
    /// </summary>
    public class PaymentMethodRequest
    {
        ///<summary>
        /// Email address to send reciepts to 
        ///</summary>
        public string StripePaymentMethodId;
    }

    /// <summary>
    /// Id and a quantity
    /// </summary>
    public struct IdQuantity
    {
        /// <summary>
        /// Product ID
        /// </summary>
        public uint Id;
        /// <summary>
        /// Amount to purchase
        /// </summary>
        public uint Quantity;
    }
}
