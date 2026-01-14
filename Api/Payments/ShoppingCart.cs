using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;


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
		/// The unique reference created for this basket and hence purchase
		/// </summary>
		public string Reference;

		/// <summary>
		/// Key to allow anonymous users to see address details.
		/// This ensures that an anonymous user cannot create an address or deliver to an incorrect address, instead, they are made using CQC and the user can only pass the address key
		/// </summary>
		public string AnonymousAddressKey;
	}
}