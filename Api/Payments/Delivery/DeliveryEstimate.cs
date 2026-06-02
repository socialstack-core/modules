using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.PasswordResetRequests;
using System;
using Api.Addresses;

namespace Api.Payments
{
	/// <summary>
	/// The set of options for the user.
	/// </summary>
	public partial struct DeliveryEstimates
    {
		/// <summary>
		/// The cart being estimated.
		/// </summary>
		public ShoppingCart Cart;

		/// <summary>
		/// The set of pricing info for everything in the cart.
		/// </summary>
		public ProductQuantityPricing Pricing;

		/// <summary>
		/// Tax calc to use.
		/// </summary>
		public TaxCalculator TaxCalculator;

		/// <summary>
		/// Info used for pricing the delivery.
		/// </summary>
		public DeliveryPricingInfo DeliveryPricing;

		/// <summary>
		/// Applicable tax jurisdiction.
		/// </summary>
		public string TaxJurisdiction;

		/// <summary>
		/// The target address.
		/// </summary>
		public Address Target;

		/// <summary>
		/// The set of options for the user.
		/// </summary>
		public List<DeliveryEstimate> Options;
	}
    
	/// <summary>
	/// A singular delivery estimate.
	/// </summary>
	public partial class DeliveryEstimate
	{
        /// <summary>
        /// Delivery code
        /// </summary>
        public string DeliveryCode;

        /// <summary>
        /// Requested delivery date
        /// </summary>
        public DateOnly RequestedDeliveryDate;

		/// <summary>
		/// The set of usually 1 deliveries in this estimation.
		/// </summary>
		public DeliveryInfo[] Deliveries;
		
		/// <summary>
		/// The price in whole pennies/ cents etc.
		/// </summary>
		public uint Price;

		/// <summary>
		/// The applied tax apportioning ratio.
		/// </summary>
		public double TaxApportionment;

		/// <summary>
		/// The price in whole pennies/ cents etc (less apportioned tax).
		/// </summary>
		public uint PriceLessTax;

		/// <summary>
		/// The currency that the price is in.
		/// </summary>
		public string Currency;
	}
	
	/// <summary>
	/// Information about a delivery itself.
	/// </summary>
	public partial class DeliveryInfo
	{
		/// <summary>
		/// Can be e.g. "Royal Mail 24h".
		/// </summary>
		public string DeliveryName;
		
		/// <summary>
		/// Other general notes about this delivery.
		/// </summary>
		public string DeliveryNotes;
		
		/// <summary>
		/// Currently expected to be present only if a delivery option is split.
		/// This is the set of products in this particular delivery.
		/// </summary>
		public List<DeliveryProduct> Products;
		
		/// <summary>
		/// The date. The time component is only used if the timeWindowLength is non-zero.
		/// </summary>
		public DateTime SlotStartUtc;
		
		/// <summary>
		/// A length, in minutes, of the time slot. For example, the slot start may represent 2pm and this is 30, indicating that  
		/// the delivery is expected between 2 - 2.30pm. Zero if it is not time slotted.
		/// </summary>
		public uint TimeWindowLength;
		
	}
	
	/// <summary>
	/// A product from the basket being delivered.
	/// </summary>
	public partial class DeliveryProduct
	{
		/// <summary>
		/// Line item cost (inc tax).
		/// </summary>
		public ulong Total;

		/// <summary>
		/// Line item cost (less tax).
		/// </summary>
		public ulong TotalLessTax;

		/// <summary>
		/// A product ID, initially from the basket.
		/// </summary>
		public uint ProductId;
		
		/// <summary>
		/// The quantity of the product.
		/// </summary>
		public uint Quantity;
		
		/// <summary>
		/// "Due to weight restrictions this is being delivered separately" etc.
		/// </summary>
		public string Notes;
		
	}
}
