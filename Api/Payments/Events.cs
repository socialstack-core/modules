using Api.Payments;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for a purchase.
		/// </summary>
		public static EventGroup<Purchase> Purchase;
		
		/// <summary>
		/// Set of events for a price.
		/// </summary>
		public static EventGroup<Price> Price;

		/// <summary>
		/// Set of events for a shoppingCart.
		/// </summary>
		public static EventGroup<ShoppingCart> ShoppingCart;

		/// <summary>
		/// Set of events for a productQuantity.
		/// </summary>
		public static EventGroup<ProductQuantity> ProductQuantity;
		
		/// <summary>
		/// Set of events for a paymentMethod.
		/// </summary>
		public static EventGroup<PaymentMethod> PaymentMethod;

		/// <summary>
		/// Set of events for a subscription.
		/// </summary>
		public static EventGroup<Subscription> Subscription;
		
		/// <summary>
		/// Set of events for a product.
		/// </summary>
		public static EventGroup<Product> Product;
	}
}