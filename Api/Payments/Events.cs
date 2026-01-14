using Api.Addresses;
#if PAYMENTS_GUEST_USERS
using Api.GuestUsers;
#endif
using Api.Payments;
using Api.Permissions;
using Api.SocketServerLibrary;
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
		/// Set of events for a coupon.
		/// </summary>
		public static EventGroup<Coupon> Coupon;

		/// <summary>
		/// Set of events for a promotion.
		/// </summary>
		public static EventGroup<Promotion> Promotion;

        /// <summary>
        /// Set of events for a productCategory.
        /// </summary>
        public static EventGroup<ProductCategory> ProductCategory;

		/// <summary>
		/// Set of events for a product attribute.
		/// </summary>
		public static EventGroup<ProductAttribute> ProductAttribute;
		
		/// <summary>
		/// Set of events for a product template.
		/// </summary>
		public static EventGroup<ProductTemplate> ProductTemplate;
		
		/// <summary>
		/// Set of events for a product attribute group.
		/// </summary>
		public static EventGroup<ProductAttributeGroup> ProductAttributeGroup;
		
		/// <summary>
		/// Set of events for a product attribute value.
		/// </summary>
		public static EventGroup<ProductAttributeValue> ProductAttributeValue;

		/// <summary>
		/// Set of events for a PurchaseToken.
		/// </summary>
		public static EventGroup<PurchaseToken> PurchaseToken;

        /// <summary>
        /// After purchase token has been used.
        /// </summary>
        public static EventHandler<PurchaseToken, string> PurchaseTokenAfterUse;        
		
		/// <summary>
		/// Set of events for a purchase.
		/// </summary>
		public static PurchaseEventGroup Purchase;
		
		/// <summary>
		/// Set of events for a price.
		/// </summary>
		public static PriceEventGroup Price;

		/// <summary>
		/// Set of events for a shoppingCart.
		/// </summary>
		public static ShoppingCartEventGroup ShoppingCart;

		/// <summary>
		/// Set of events for a productQuantity.
		/// </summary>
		public static ProductQuantityEventGroup ProductQuantity;
		
		/// <summary>
		/// Set of events for a deliveryOption. This is the info the user chooses and it typically becomes one or more Delivery.
		/// </summary>
		public static DeliveryOptionEventGroup DeliveryOption;

		/// <summary>
		/// Set of events for a delivery.
		/// </summary>
		public static DeliveryEventGroup Delivery;

		/// <summary>
		/// Set of events for a paymentMethod.
		/// </summary>
		public static PaymentMethodEventGroup PaymentMethod;

		/// <summary>
		/// Set of events for a subscription.
		/// </summary>
		public static SubscriptionEventGroup Subscription;
		
		/// <summary>
		/// Set of events for a product.
		/// </summary>
		public static ProductEventGroup Product;
	}

	/// <summary>
	/// Specialised event group for the PaymentMethod event type.
	/// </summary>
	public partial class ShoppingCartEventGroup : EventGroup<ShoppingCart>
	{

		/// <summary>
		/// Called when items are being changed in a cart.
		/// </summary>
		public EventHandler<ShoppingCart, List<CartItemChange>> ChangeItems;

		/// <summary>
		/// Called when writing out the cart contents
		/// </summary>
		public EventHandler<Writer, ShoppingCart, ProductQuantityPricing> OnWriteCartContents;

#if PAYMENTS_GUEST_USERS
		/// <summary>
		/// Called when a guest user is checking out. Use this event to populate the 
		/// guest object based on anything you've stored in the guest details.
		/// </summary>
		public EventHandler<ShoppingCart, PurchaseAndAction, GuestUser> AfterGuestCheckout;
#endif

	}


	/// <summary>
	/// Specialised event group for the PaymentMethod event type.
	/// </summary>
	public partial class PaymentMethodEventGroup : EventGroup<PaymentMethod>
	{

		/// <summary>
		/// Called when checking if BNPL is available to the given context.
		/// </summary>
		public EventHandler<BuyNowPayLater> AuthoriseBuyNowPayLater;

	}
	
	/// <summary>
	/// Specialised event group for the Delivery event type.
	/// </summary>
	public partial class DeliveryEventGroup : EventGroup<Delivery>
	{

		/// <summary>
		/// Called when initially setting up a delivery object for the given purchase.
		/// </summary>
		public EventHandler<Delivery, Purchase> Setup;

	}


	/// <summary>
	/// Specialised event group for the DeliveryOption event type.
	/// </summary>
	public partial class DeliveryOptionEventGroup : EventGroup<DeliveryOption>
	{

		/// <summary>
		/// Called when collecting delivery estimates.
		/// </summary>
		public EventHandler<DeliveryEstimates> Estimate;

	}
	
	/// <summary>
	/// Specialised event group for the Product event type.
	/// </summary>
	public partial class ProductEventGroup : EventGroup<Product>
	{

		/// <summary>
		/// Called when running a search for products.
		/// </summary>
		[Permissions(Check = true)]
		public EventHandler<ProductSearch> BeforeSearch;

		/// <summary>
		/// Called when running a search for products.
		/// </summary>
		public EventHandler<ProductSearch> Search;

		/// <summary>
		/// Called when collecting any important notices about a product. The productQuantity can be null.
		/// Handlers add them to the provided set with .Add
		/// </summary>
		public EventHandler<ProductNoticeSet, ProductQuantity, Product> CollectNotices;

		/// <summary>
		/// Called when pricing for a product is being established.
		/// </summary>
		public EventHandler<List<Price>, Product> Pricing;

	}

	/// <summary>
	/// Specialised event group for the Price type in order to add additional events.
	/// As usual, instanced automatically by the event handler engine.
	/// </summary>
	public partial class PriceEventGroup : EventGroup<Price>
	{

		/// <summary>
		/// Called whilst a tax calculator is being established for the given jurisdiction name and context.
		/// Use this event to provide a custom one instead.
		/// </summary>
		public EventHandler<TaxCalculator, string> ResolveTaxCalculator;

	}

	/// <summary>
	/// Specialised event group for the Purchase type in order to add additional events.
	/// As usual, instanced automatically by the event handler engine.
	/// </summary>
	public partial class PurchaseEventGroup : EventGroup<Purchase>
	{

#if PAYMENTS_GUEST_USERS
		/// <summary>
		/// Called when a guest user is checking out. Use this event to populate the 
		/// guest object based on anything you've stored in the guest details.
		/// </summary>
		public EventHandler<GuestUser, GuestDetails, ShoppingCart>  BeforeCreateGuest;
#endif

		/// <summary>
		/// Called just before a purchase is created during execution. 
		/// This is the first opportunity to modify its billable items and is a good place to add approval mechanisms as you can 
		/// reject the order before it is even created here.
		/// </summary>
		public EventHandler<Purchase, ProductQuantityPricing, PaymentMethod> BeforeExecuteCreate;

		/// <summary>
		/// Called just before a purchase is executed. This is the final opportunity to modify its billable items.
		/// </summary>
		public EventHandler<Purchase> BeforeExecute;

		/// <summary>
		/// Called during checkout. Map any custom fields from a checkout submission to the purchase here.
		/// </summary>
		public EventHandler<Purchase, CheckoutInfo> Checkout;

		/// <summary>
		/// Called after checkout. Allow for other services to override stock confirmation emails.
		/// </summary>
		public EventHandler<bool, string, Purchase> SendConfirmationEmail;


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

	}

	/// <summary>
	/// Event group for Addresses.
	/// </summary>
	public partial class AddressEventGroup : EventGroup<Address>
	{

		/// <summary>
		/// Called when loading up the filter for the cart address book.
		/// </summary>
		public EventHandler<Filter<Address, uint>> GetCartAddressFilter;

	}
}