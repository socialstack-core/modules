using Api.Database;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Api.Payments
{

	/// <summary>
	/// A ShoppingCart contains a list of productQuantities.
	/// A user has a "current" shopping cart associated to them, and when they checkout, the shopping cart is converted to a payment.
	/// Each time objects in the cart are changed, the editedUtc of the cart itself *must* change too.
	/// </summary>
	[HasVirtualField("coupon", typeof(Coupon), "CouponId")]
	public partial class ShoppingCart : VersionedContent<uint>
	{
		/// <summary>
		/// True if the cart has been checked out. It's immutable at that point.
		/// It could, however, be cloned. This is if someone wants to buy the same thing again for example.
		/// </summary>
		public bool CheckedOut;

		/// <summary>
		/// The cart key used to ensure an anon user can actually update/ load this cart.
		/// </summary>
		[DatabaseField(Length = 20)]
		public string AnonymousCartKey;

		/// <summary>
		/// An applied coupon, if any. Not directly settable but can be included.
		/// </summary>
		public uint CouponId;

		/// <summary>
		/// The established tax jurisdiction for this shopping cart (based on the context at the time the cart was created).
		/// </summary>
		[JsonIgnore]
		public string TaxJurisdiction;

		/// <summary>
		/// Gateway response for exposure to end user/admin
		/// </summary>
		public JsonString GatewayPublicJson;

		/// <summary>
		/// The unique reference created for this basket and hence purchase
		/// </summary>
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

	}
}