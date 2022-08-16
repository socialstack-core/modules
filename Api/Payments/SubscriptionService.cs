using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;

namespace Api.Payments
{
	/// <summary>
	/// Handles subscriptions.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SubscriptionService : AutoService<Subscription>
    {
		private readonly ProductQuantityService _productQuantities;
		private readonly PurchaseService _purchases;
		private readonly PaymentMethodService _paymentMethods;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SubscriptionService(ProductQuantityService productQuantities, PurchaseService purchases, PaymentMethodService paymentMethods) : base(Events.Subscription)
		{
			_productQuantities = productQuantities;
			_purchases = purchases;
			_paymentMethods = paymentMethods;

			Events.Subscription.BeforeSettable.AddEventListener((Context ctx, JsonField<Subscription, uint> field) =>
			{
				if (field == null)
				{
					return new ValueTask<JsonField<Subscription, uint>>(field);
				}

				if (field.Name == "Status")
				{
					// Not settable
					field = null;
				}

				return new ValueTask<JsonField<Subscription, uint>>(field);
			});

			Events.Subscription.BeforeUpdate.AddEventListener(async (Context ctx, Subscription toUpdate, Subscription original) => {

				if (toUpdate.PaymentMethodId != original.PaymentMethodId)
				{
					// Can this context access the target one?
					var paymentMethod = await _paymentMethods.Get(ctx, toUpdate.PaymentMethodId);

					if (paymentMethod == null)
					{
						// nope!
						throw new PublicException("Payment method not found", "payment_method_not_found");
					}

				}

				return toUpdate;
			});

			// If a subscription payment changes to state 202, fulfil it immediately.
			Events.Purchase.BeforeUpdate.AddEventListener(async (Context context, Purchase purchase, Purchase original) => {

				// Is it for a subscription and is this a status change?
				if (purchase.ContentType == "Subscription" && purchase.Status != original.Status)
				{
					// State change - is it now a successful payment
					if (purchase.Status == 202)
					{
						// Success! Mark subscription as renewed for the current month.
						var subscription = await Get(context, purchase.ContentId, DataOptions.IgnorePermissions);

						// Note that we'll only change the status if the payment is for the current timeslot index.
						ulong timePeriodKey = (ulong)subscription.LastChargeUtc.Ticks;

						if (timePeriodKey == purchase.ContentAntiDuplication)
						{
							// This payment is for the latest period of the subscription.
							// Update the subscription and set to active if not already.
							await Update(context, subscription, (Context ctx, Subscription subToUpdate, Subscription orig) => {

								// Update the charge dates:
								SetUpdatedChargeDates(subToUpdate);

								// it's active:
								subToUpdate.Status = 1;

							}, DataOptions.IgnorePermissions);
						}
					}
					else if (purchase.Status >= 300)
					{
						// A permanent failure. The subscription is now going to be deactivated.
						var subscription = await Get(context, purchase.ContentId, DataOptions.IgnorePermissions);

						if (subscription.Status == 1)
						{
							await Update(context, subscription, (Context ctx, Subscription subToUpdate, Subscription orig) => {

								// Paused by payment failure:
								subToUpdate.Status = 3;

							}, DataOptions.IgnorePermissions);
						}

					}
				}

				return purchase;
			}, 11); // Just after everything else such that we can overwrite the desired status to immediately fulfil if necessary.

			Events.Automation("subcription_payments", "0 0 0 ? * * *").AddEventListener(async (Context context, Api.Automations.AutomationRunInfo runInfo) => {

				// Runs daily. Check for subscriptions which require a payment run.
				// A payment run occurs if we're in a new timeslot from the last time a particular subscription was charged.
				
				// Get current date:
				var date = DateTime.UtcNow;

				var meta = await Events.Subscription.BeforeBeginDailyProcess.Dispatch(context, new DailySubscriptionMeta() {
					ProcessDateUtc = date
				});

				if (meta.DoNotProcess)
				{
					// Halt! The subscription system has declared that it is not ready to be processed yet.
					Console.WriteLine("[NOTICE] Subscription system has been told to not process anything today. This is usually because usage stats for the current time period are not ready yet.");
					return runInfo;
				}

				// Get all subscriptions which are in need of updating:
				var subsToUpdate = await Where("Status<? and NextChargeUtc<?", DataOptions.IgnorePermissions)
					.Bind((uint)2)
					.Bind(date)
					.ListAll(context);

				// Future feature, if large number of subs: could make the automation more frequent (hourly rather than daily) and let it run batches.

				// For each one, charge it.
				foreach (var subscription in subsToUpdate)
				{
					await ChargeSubscription(context, subscription);
				}

				return runInfo;
			});

		}

		/// <summary>
		/// Sets the next charge date for a given subscription. Must be called within an update statement.
		/// </summary>
		/// <returns></returns>
		public void SetUpdatedChargeDates(Subscription sub)
		{
			DateTime chargeDateUtc = DateTime.UtcNow;

			// Preserve billing date - if the charge date is <24h apart from the requested date, use the prev requested date instead.
			var timeDistance = chargeDateUtc - sub.NextChargeUtc;

			if (timeDistance.TotalMilliseconds < (1000 * 60 * 60 * 24))
			{
				chargeDateUtc = sub.NextChargeUtc;
			}

			switch (sub.TimeslotFrequency)
			{
				case 0:
					// Add a month to the charge date.
					sub.NextChargeUtc = chargeDateUtc.AddMonths(1);
				break;
				case 1:
					// Add a quarter to the charge date.
					sub.NextChargeUtc = chargeDateUtc.AddMonths(3);
				break;
				case 2:
					// Add a year to the charge date.
					sub.NextChargeUtc = chargeDateUtc.AddMonths(3);
				break;
				case 3:
					// Add a week to the charge date.
					sub.NextChargeUtc = chargeDateUtc.AddDays(7);
				break;
				default:
					throw new Exception("Unknown billing frequency " + sub.TimeslotFrequency);
			}

			sub.LastChargeUtc = chargeDateUtc;
		}

		/// <summary>
		/// Gets the current timeslot index for the given timeslot type.
		/// </summary>
		/// <param name="timeslotFrequency">
		/// 0 = The default, it's in months.   (currently the only supported option)
		/// 1 = Quarters
		/// 2 = Years</param>
		/// <returns></returns>
		public uint GetCurrentTimeslotIndex(uint timeslotFrequency)
		{
			// Get current date:
			var date = DateTime.UtcNow;
			var yearIndex = (uint)(date.Year - 2020);
			var monthIndex = (uint)((yearIndex * 12) + (date.Month - 1));

			switch (timeslotFrequency)
			{
				case 0:
					// Months
					return monthIndex;
				case 1:
					// Quarters
					return monthIndex / 4;
				case 2:
					// Years
					return yearIndex;
			}

			return 0;
		}

		/// <summary>
		/// Charge a subscription by raising a purchase now.
		/// Returns a Purchase object which contains a clone of the objects in the cart.
		/// Will throw publicExceptions if the payment failed.
		/// You should however check the purchase.Status for immediate failures as well.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="subscription"></param>
		public async ValueTask<PurchaseAndAction> ChargeSubscription(Context context, Subscription subscription)
		{
			// First, has a purchase been raised for the subscription already?
			ulong timePeriodKey = (ulong)subscription.LastChargeUtc.Ticks;
			
			var purchase = await _purchases.Where(
					"ContentType=? and ContentId=? and ContentAntiDuplication=?",
					DataOptions.IgnorePermissions
			).Bind("Subscription").Bind(subscription.Id).Bind(timePeriodKey).First(context);

			if (purchase != null)
			{
				// Recovery of an existing purchase. We won't create a new object.
				// Based on the status of the purchase we can identify where it got up to in the process.

				if (purchase.Status >= 200 && purchase.Status < 300)
				{
					// It's in the success state. Only thing that should be done is update the subscription's date as it seems like that part skipped.
					await Update(context, subscription, (Context context, Subscription toUpdate, Subscription orig) => {

						SetUpdatedChargeDates(toUpdate);

					}, DataOptions.IgnorePermissions);

					return new PurchaseAndAction() {
						Purchase = purchase
					};
				}

				if (purchase.Status >= 100 && purchase.Status < 200)
				{
					// It's in the waiting for gateway state.
					System.Console.WriteLine("[WARN] Manual intervention required. Subscription has waited unusually long for payment response. Gateway webhook likely misfired.");

					return new PurchaseAndAction()
					{
						Purchase = purchase
					};
				}

				// All other status codes indicate permanent failure or not yet submitted to gateway.
				// It is therefore safe to effectively recreate the products on the subscription and go again by first resetting the code.
				purchase = await _purchases.Update(context, purchase, (Context context, Purchase toUpdate, Purchase orig) => {

					// Clear status code:
					toUpdate.Status = 0;

					// Ensure purchase locale matches that of the sub:
					toUpdate.LocaleId = subscription.LocaleId;

				}, DataOptions.IgnorePermissions);

				// Remove all items from the purchase. We'll recreate them.
				var currentProducts = await _purchases.GetProducts(context, purchase);

				foreach (var qp in currentProducts)
				{
					await _productQuantities.Delete(context, qp, DataOptions.IgnorePermissions);
				}
			}
			else
			{
				// Create a purchase:
				purchase = await _purchases.Create(context, new Purchase()
				{
					ContentType = "Subscription",
					ContentId = subscription.Id,
					PaymentMethodId = subscription.PaymentMethodId,
					ContentAntiDuplication = timePeriodKey,
					LocaleId = subscription.LocaleId,
					UserId = subscription.UserId,
				}, DataOptions.IgnorePermissions);
			}

			// Copy the items from the subscription to the purchase.
			// This prevents any risk of someone manipulating their cart during the fulfilment.
			var inSub = await GetProducts(context, subscription);
			await _purchases.AddProducts(context, purchase, inSub);

			// Attempt to fulfil the purchase now:
			return await _purchases.Execute(context, purchase);
		}

		/// <summary>
		/// Gets the products in the given subscription.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="subscription"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProducts(Context context, Subscription subscription)
		{
			return await _productQuantities.Where("SubscriptionId=?", DataOptions.IgnorePermissions).Bind(subscription.Id).ListAll(context);
		}

		/// <summary>
		/// Gets the products in the given subscription.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="subscriptionId"></param>
		/// <returns></returns>
		public async ValueTask<List<ProductQuantity>> GetProducts(Context context, uint subscriptionId)
		{
			return await _productQuantities.Where("SubscriptionId=?", DataOptions.IgnorePermissions).Bind(subscriptionId).ListAll(context);
		}

		/// <summary>
		/// Adds the given product to the subscription.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="subscription"></param>
		/// <param name="product"></param>
		/// <param name="quantity">Optional: Often subscription level products are added once.</param>
		/// <returns></returns>
		public async ValueTask<ProductQuantity> AddToSubscription(Context context, Subscription subscription, Product product, uint quantity = 1)
		{
			// Check if this product is already in this cart:
			var pQuantity = await _productQuantities
				.Where("ProductId=? and SubscriptionId=?", DataOptions.IgnorePermissions)
				.Bind(product.Id)
				.Bind(subscription.Id)
			.First(context);

			if (pQuantity == null)
			{
				// Create a new one:
				pQuantity = await _productQuantities.Create(context, new ProductQuantity()
				{
					ProductId = product.Id,
					SubscriptionId = subscription.Id,
					Quantity = quantity
				}, DataOptions.IgnorePermissions);
			}
			else
			{
				// Add to the existing one:
				await _productQuantities.Update(context, pQuantity, (Context ctx, ProductQuantity toUpdate, ProductQuantity orig) => {
					toUpdate.Quantity += quantity;
				});
			}

			return pQuantity;
		}

	}

}
