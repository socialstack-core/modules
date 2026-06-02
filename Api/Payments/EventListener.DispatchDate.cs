using System;
using Api.Eventing;
using Api.Startup;

namespace Api.Payments
{
	public partial class EventListeners
	{
		/// <summary>
		/// Handles dispatch dates for deliveries. 
		/// When all deliveries have been delivered, the purchase they're on moves to being complete.
		/// </summary>
		public void InitDispatchDateHook()
		{
			Events.Delivery.BeforeUpdate.AddEventListener(async (ctx, delivery, original) =>
			{
				// exit early if no change is made.
				if (delivery.ActualUtc == original.ActualUtc)
				{
					return delivery;
				}
				
				// we need the delivery service to load the remaining deliveries
				var deliveryService = Services.Get<DeliveryService>();
				_purchaseService ??= Services.Get<PurchaseService>();

				// first though, lets attempt to fetch the purchase, by the delivery
				// ID, this is a one (purchase) to many (delivery) relationship.
				// so multiple delivery IDs can exist on a singular purchase
				// via Mappings.deliveries 
				var purchase = await _purchaseService.GetPurchaseByDelivery(ctx, delivery, DataOptions.IgnorePermissions);
				
				// if for some reason, the purchase cannot be found,
				// an error gets logged, and it exits early.
				if (purchase is null)
				{
					Log.Error("purchase/unexpected", "A purchase linked to this delivery does not exist");
					return delivery;
				}
				
				// load ALL the deliveries. 
				var deliveries = await deliveryService.GetDeliveries(ctx, purchase);
			
				// iterate deliveries, if there's a delivery that
				// hasn't been completed yet, return early.
				DateTime? latestActualUtc = delivery.ActualUtc ?? null;

				foreach (var otherDelivery in deliveries)
				{
					if(otherDelivery.Id == delivery.Id)
					{
						//This is the current delivery so we skip ahead
						continue;
					}

					// if there is a delivery that hasn't 
					// been delivered yet, the code after this 
					// foreach should not be executed, 
					// so we exit early.
					if (otherDelivery.ActualUtc is null)
					{
						return delivery;
					}

					// Compare timestamps to find the latest delivery
					if (latestActualUtc is null || otherDelivery.ActualUtc > latestActualUtc)
					{
						latestActualUtc = otherDelivery.ActualUtc;
					}
				}

				
				// here we can safely assume that all the deliveries
				// have been completed, and thus-for can mark the 
				// purchase as fully delivered
				await _purchaseService.Update(ctx, purchase.Id, (context, current, original) =>
				{
					// change the date to the last delivered. 
					current.DeliveredDateUtc = latestActualUtc;
					current.Status = 200;
				}, DataOptions.IgnorePermissions);
				
				return delivery;
			});
		}
	}
}