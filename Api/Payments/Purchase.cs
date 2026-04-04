using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Payments
{

	/// <summary>
	/// A purchase for a set of products which are attached as a set of ProductQuantities each with baked in prices.
	/// </summary>
	[HasVirtualField("DeliveryAddress", typeof(Addresses.Address), "DeliveryAddressId")]
	[HasVirtualField("BillingAddress", typeof(Addresses.Address), "BillingAddressId")]
	[HasVirtualField("DeliveryOption", typeof(DeliveryOption), "DeliveryOptionId")]
	public partial class Purchase : VersionedContent<uint>
	{
		/// <summary>
		/// 0 = Created, not completed.
		/// 200 = Fulfilled.
		/// 201 = BNPL ready for fulfilment.
		/// 202 = Paid and ready for fulfilment.
		/// 203 = Pending approval. Only incurred if your site specifically creates some form of approval mechanism.
		/// 250-299 = Custom site specific codes.
		/// 101 = Started submit to gateway. If a purchase is stuck in this state you MUST check if the gateway received the request at all.
		/// 102 = Pending at payment gateway.
		/// 103 = Passed to external page for processing
		/// 500 = Failed (payment gateway rejection).
		/// 400 = Failed (user sourced fault).
		/// 401 = Not approved.
		/// 402 = Cancelled
		/// 300 = Needs 3ds approval
		/// </summary>
		public uint Status;

		/// <summary>
		/// True if this is a BNPL purchase. 
		/// It jumps straight to status 202 (ready for fulfilment) despite being not actually paid.
		/// </summary>
		[JsonIgnore]
		public bool BuyNowPayLater;

		/// <summary>
		/// Set if a coupon was used.
		/// </summary>
		[JsonIgnore]
		[Permissions(WriteRule = "false", Roles="!admins")]
		public uint CouponId;

		/// <summary>
		/// True if the order has been modified during an approval phase.
		/// </summary>
		[JsonIgnore]
		public bool ModifiedByApproval;

		/// <summary>
		/// Exclude tax on this purchase.
		/// </summary>
		[JsonIgnore]
		public bool ExcludeTax;

		/// <summary>
		/// The tax jurisdiction of the purchase.
		/// </summary>
		[JsonIgnore]
		public string TaxJurisdiction;

		/// <summary>
		/// True if this purchase was only authorised, not actually executed.
		/// </summary>
		[Module(Hide = true)]
		public bool Authorise;

		/// <summary>
		/// True if this purchase has multiple subscriptions attached to it which fulfil when the purchase does.
		/// The subscriptions are attached via a mapping called "Subscriptions".
		/// </summary>
		[Module(Hide = true)]
		public bool MultiExecute;

		/// <summary>
		/// The locale the purchase will occur in. This is used to specify the actual price paid.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint LocaleId;

		/// <summary>
		/// An ID provided by the payment gateway.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public string PaymentGatewayInternalId;

		/// <summary>
		/// The gateway ID. Stripe is gateway=1.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint PaymentGatewayId;

		/// <summary>
		/// The payment method to use. This is used to specify PaymentGatewayId.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint PaymentMethodId;

		/// <summary>
		/// The currency the payment is being made in.
		/// </summary>
		[Module(Hide = true)]
		public string CurrencyCode;

		/// <summary>
		/// Total sum of products less any tax. If delivery is free, this equals TotalCostLessTax.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Data("readonly", true)]
		[Data("type", "price")]
		public ulong ProductsCostLessTax;

		/// <summary>
		/// Total sum of products including any tax. If delivery is free, this equals TotalCost.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Data("readonly", true)]
		[Data("type", "price")]
		public ulong ProductsCost;

		/// <summary>
		/// Cost excluding tax, including delivery.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Data("readonly", true)]
		[Data("type", "price")]
		public ulong TotalCostLessTax;

		/// <summary>
		/// The total cost in the currency codes native atomic unit, inclusive of any tax and delivery.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Data("readonly", true)]
		[Data("type", "price")]
		public ulong TotalCost;
		
		/// <summary>
		/// Delivery cost excluding tax.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Data("readonly", true)]
		[Data("type", "price")]
		public ulong DeliveryCostLessTax;

		/// <summary>
		/// The delivery cost in the currency codes native atomic unit, inclusive of any tax.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Data("readonly", true)]
		[Data("type", "price")]
		public ulong DeliveryCost;

		/// <summary>
		/// Present if apportionment was calculated based on the tax status of the delivered goods.
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Module(Hide = true)]
		public double? DeliveryApportionment;

		/// <summary>
		/// True if any of the products in this purchase are subs.
		/// </summary>
		[JsonIgnore]
		public bool HasSubscriptions;

		/// <summary>
		/// A field for identifying duplicate purchase requests. Used by the content type.
		/// </summary>
		[JsonIgnore]
		public ulong ContentAntiDuplication;

		/// <summary>
		/// The content type that requested the payment. E.g. "Subscription" or "ShoppingCart".
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Module(Hide = true)]
		public string ContentType;

		/// <summary>
		/// The ID of the content type that requested the payment. E.g. an ID of a particular subscription or shopping cart
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Module(Hide = true)]
		public uint ContentId;

		/// <summary>
		/// If delivery relevant, the delivery addr.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint DeliveryAddressId;

		/// <summary>
		/// If billing relevant, the billing addr.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint BillingAddressId;

		/// <summary>
		/// If delivery relevant, the delivery method.
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint DeliveryOptionId;

		/// <summary>
		/// The ip addess of the client
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Module(Hide = true)]
		public string IpAddress;

		/// <summary>
		/// Gateway response for debug/auditing
		/// </summary>
		[JsonIgnore]
		public JsonString GatewayResponseJson;

		/// <summary>
		/// Gateway response for exposure to end user/admin
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Module(Hide = true)]
		public JsonString GatewayPublicJson;

		/// <summary>
		/// The unique reference created for this purchase
		/// </summary>
		[Meta("title")]
		public string Reference;

		/// <summary>
		/// Customer order reference. Not guaranteed to be unique.
		/// </summary>
		[MaxLength(26)]
		public string CustomerOrderReference;

		/// <summary>
		/// The contact name for the order, defaulted from addresses
		/// </summary>
		[MaxLength(50)]
		public string ContactName;

		/// <summary>
		/// Additional delivery information
		/// </summary>
		[MaxLength(150)]
		public string DeliveryInformation;

		/// <summary>
		/// When was this order delivered
		/// </summary>
		[Permissions(WriteRule = "false")]
		[Module(Hide = true)]
		public DateTime? DeliveredDateUtc;

		/// <summary>
		/// Available during creation of a purchase (before/after create), if the purchase is for physical goods.
		/// Holds the established set of delivery information.
		/// </summary>
		[JsonIgnore]
		public List<Delivery> InitialDeliveryData { get; set; }

		/// <summary>
		/// The raw metadata/content of this purchase, used for free text search
		/// </summary>
		[JsonIgnore]
		public string DescriptionRaw;
	}

}