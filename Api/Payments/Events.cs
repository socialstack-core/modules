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
        /// Set of events for a subscriptionUsage.
        /// </summary>
        public static EventGroup<SubscriptionUsage> SubscriptionUsage;
		
		/// <summary>
		/// Set of events for a purchase.
		/// </summary>
		public static PurchaseEventGroup Purchase;
		
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
		public static ProductQuantityEventGroup ProductQuantity;
		
		/// <summary>
		/// Set of events for a paymentMethod.
		/// </summary>
		public static EventGroup<PaymentMethod> PaymentMethod;

		/// <summary>
		/// Set of events for a subscription.
		/// </summary>
		public static SubscriptionEventGroup Subscription;
		
		/// <summary>
		/// Set of events for a product.
		/// </summary>
		public static EventGroup<Product> Product;
	}

	/// <summary>
	/// Specialised event group for the Purchase type in order to add additional events.
	/// As usual, instanced automatically by the event handler engine.
	/// </summary>
	public partial class PurchaseEventGroup : EventGroup<Purchase>
	{

		/// <summary>
		/// Called just before a purchase is executed. This is the final opportunity to modify its billable items.
		/// </summary>
		public EventHandler<Purchase> BeforeExecute;

	}

	/// <summary>
	/// Specialised event group for the Subscription type in order to add additional events.
	/// As usual, instanced automatically by the event handler engine.
	/// </summary>
	public partial class SubscriptionEventGroup : EventGroup<Subscription>
	{

		/// <summary>
		/// Called just before the daily process is started.
		/// Can be used to prevent it from doing anything if you know the supporting data is not ready yet.
		/// </summary>
		public EventHandler<DailySubscriptionMeta> BeforeBeginDailyProcess;

	}

	/// <summary>
	/// Specialised event group for the ProductQuantity type in order to add additional events.
	/// As usual, instanced automatically by the event handler engine.
	/// </summary>
	public partial class ProductQuantityEventGroup : EventGroup<ProductQuantity>
	{

		/// <summary>
		/// Called just before a given item is about to be added to a purchase for charging.
		/// This event is the best place to the purchase with e.g. usage stats for the given product if the site is using a "use now pay later" mechanism.
		/// </summary>
		public EventHandler<ProductQuantity, Purchase> BeforeAddToPurchase;
		
	}

}