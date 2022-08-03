using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A ShoppingCart contains a list of productQuantities.
	/// A user has a "current" shopping cart associated to them, and when they checkout, the shopping cart is converted to a payment.
	/// </summary>
	public partial class ShoppingCart : VersionedContent<uint>
	{
		/// <summary>
		/// A shopping cart in the "pending payment" or "payment completed" state is immutable.
		/// It could, however, be cloned. This is if someone wants to buy the same thing again for example.
		/// </summary>
		public uint Status;
	}

}