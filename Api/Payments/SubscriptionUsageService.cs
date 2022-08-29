using Api.CanvasRenderer;
using Api.Contexts;
using Api.Payments;
using Api.Startup;
using System.Threading.Tasks;

namespace Api.Payments
{
    /// <summary>
    /// Handles SubscriptionUsage.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class SubscriptionUsageService : AutoService<SubscriptionUsage>
    {
        private readonly SubscriptionService _subscriptions;


        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public SubscriptionUsageService(SubscriptionService subscriptions, ProductService products) : base(Eventing.Events.SubscriptionUsage)
        {
            _subscriptions = subscriptions;
            
            Eventing.Events.SubscriptionUsage.BeforeCreate.AddEventListener(async (Context ctx, SubscriptionUsage content) =>
            {
                var subscription = await _subscriptions.Get(ctx, content.SubscriptionId, DataOptions.IgnorePermissions);

                if (subscription == null)
                {
                    throw new PublicException("Subscription does not exist", "bad_subscription_usage");
                }

                content.UserId = subscription.UserId;

                return content;
            });

            Eventing.Events.ProductQuantity.BeforeAddToPurchase.AddEventListener(async (Context context, ProductQuantity pq, Purchase purchase) => {

                // Is the purchase for a subscription?
                if (purchase.ContentType == "Subscription")
                {
                    // Yes. It's very likely that we have a usage product that was just added.
                    // Check if it is a usage product:
                    var product = await products.Get(context, pq.ProductId, DataOptions.IgnorePermissions);

                    if (product == null)
                    {
                        return pq;
                    }
					
					// Is it a usage based product?
					if(!product.IsBilledByUsage || purchase.ContentAntiDuplication == 0)
					{
						// Either not usage based or is first purchase
						return pq;
					}
					
                    // Get the time period index that the purchase is targeting. The "current" index is the ContentAntiDuplication value.
                    // However, we want all the usages recorded by the previous period so we remove 1 from it:
                    var timePeriod = (uint)(purchase.ContentAntiDuplication - 1);

                    // Clear the quantity:
                    pq.Quantity = 0;

                    // Get usage for the subscription, for the time period that the purchase is for.
                    await Where("SubscriptionId=? and ChargedTimeslotId=?", DataOptions.IgnorePermissions)
                        .Bind(purchase.ContentId)
                        .Bind(timePeriod)
                        .ListAll(context, (Context ctx, SubscriptionUsage usage, int total, object a, object b) => {

                            // Got a usage in this slot. If it has a productId then it may potentially get ignored.
                            var prodQuant = (ProductQuantity)a;

                            if (usage.ProductId != 0 && usage.ProductId != prodQuant.ProductId)
                            {
                                // ignore this usage.
                                return new ValueTask();
                            }

                            // Add it to the quantity.
                            prodQuant.Quantity += usage.MaximumUsageToday;

                            return new ValueTask();
                        }, pq);

                }

                return pq;
            });

        }

        /*
        /// <summary>
        /// Creates a subscription in stripe for the given local subscription
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="usage"></param>
        /// <param name="stripeSecret"></param>
        /// <returns></returns>
        public async Task<Stripe.UsageRecord> ReportStripeUsage(Context ctx, SubscriptionUsage usage, string stripeSecret)
        {
            //create stripe subscription
            StripeConfiguration.ApiKey = stripeSecret;
            var hostname = _frontEndCode.GetPublicUrl().Replace("https://", "").Replace("http://", "");

            var subscription = await _subscriptions.Get(ctx, usage.SubscriptionId, DataOptions.IgnorePermissions);

            if (subscription == null)
            {
                throw new PublicException("Subscription does not exist", "bad_subscription_usage");
            }

            var stripeSubscriptionId = subscription?.StripeSubscriptionId;

            if (string.IsNullOrEmpty(stripeSubscriptionId))
            {
                throw new PublicException("Subscription does not exist on stripe", "bad_subscription");
            }

            var usageOptions = new UsageRecordCreateOptions
            {
                Quantity = usage.MaximumUsageToday,
                Timestamp = usage.DateUTC
            };

            var stripeUsages = new Stripe.UsageRecordService();
            var stripeUsage = await stripeUsages.CreateAsync(stripeSubscriptionId, usageOptions);

            return stripeUsage;
        }
        */

    }
}
