using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using Api.Emails;
using Api.Users;

namespace Api.Payments
{
    /// <summary>
    /// Handles subscriptions.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class SubscriptionService : AutoService<Subscription>
    {
        private readonly UserService _users;
        private readonly ProductQuantityService _productQuantities;
        private readonly PurchaseService _purchases;
        private readonly PaymentMethodService _paymentMethods;
        private readonly EmailTemplateService _emails;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public SubscriptionService(UserService users, ProductQuantityService productQuantities,
            PurchaseService purchases, PaymentMethodService paymentMethods, EmailTemplateService emails) : base(
            Events.Subscription)
        {
            _users = users;
            _productQuantities = productQuantities;
            _purchases = purchases;
            _paymentMethods = paymentMethods;
            _emails = emails;

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

            Events.Subscription.BeforeUpdate.AddEventListener(
                async (Context ctx, Subscription toUpdate, Subscription original) =>
                {
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

			config = GetConfig<SubscriptionConfig>();

            Events.Subscription.AfterCreate.AddEventListener(async (context, subscription) =>
            {
                if (config.SendThankYouEmail)
                {
                    _emails.Send(new List<Recipient>()
                    {
                        new(subscription.UserId, subscription.LocaleId)
                    },
                        "thank_you_for_subscribing");
                }

                return subscription;
            });

            InstallEmails(
                new EmailTemplate
                {
                    Name = "Cancelled subscription",
                    Subject = "We're sorry to see you go",
                    Key = "subscription_cancelled",
                    BodyJson =
                        "{\"module\":\"Email/Default\",\"content\":[{\"module\":\"Email/Centered\",\"data\":{}," +
                        "\"content\":[\"Your subscription with us has now been fully cancelled. Thank you for subscribing and we hope to see you again in the future.\"]}]}"
                },
				new EmailTemplate
				{
					Name = "Payment method expires in a month",
					Subject = "Your payment card expires in a month",
					Key = "card_expires_in_a_month",
					BodyJson = "{\"c\":{\"t\":\"Email/Default\",\"d\":{},\"r\":{\"children\":{\"t\":\"Email/Centered\",\"d\":{},\"c\":[{\"s\":\"Your payment card ending \",\"i\":2},{\"t\":\"b\",\"c\":{\"t\":\"UI/Token\",\"d\":{\"mode\":\"customdata\",\"fields\":[\"cardLastFour\"]},\"i\":4},\"i\":6},{\"s\":\" expires in a month.\",\"i\":2},{\"t\":\"p\",\"c\":{\"s\":\"In order to avoid disruptions, please log into your customer dashboard to update your payment details.\",\"i\":7},\"i\":8},{\"t\":\"Email/PrimaryButton\",\"d\":{\"label\":\"Update Billing Information\",\"target\":\"/subscription/${customData.subscriptionId}/update-card\"},\"r\":{\"label\":{\"s\":\"Update Billing Information\",\"i\":6}},\"i\":4},{\"t\":\"p\",\"c\":{\"s\":\"If you have received this in error, please get in touch as soon as possible. \",\"i\":7},\"i\":8}],\"i\":3},\"customLogo\":null},\"i\":4},\"i\":5}"
				},
				new EmailTemplate // Not sent by default. Activated by Subscription config/ SendRenewalEmails
				{
					Name = "Your subscription is renewing soon",
					Subject = "Your subscription is renewing soon",
					Key = "subscription_renews_soon",
					BodyJson = "{\"c\":{\"t\":\"Email/Default\",\"d\":{},\"r\":{\"children\":" +
							   "{\"t\":\"Email/Centered\",\"d\":{},\"c\":[{\"t\":\"p\"," +
							   "\"c\":{\"s\":\"Thank you for being a customer.\",\"i\":6},\"i\":6}," +
							   "{\"t\":\"p\",\"c\":{\"s\":\"We hope you're enjoying the service.\",\"i\":7},\"i\":6},{\"t\":\"p\",\"c\":{\"s\":\"We’re letting you " +
							   "know your subscription will automatically renew soon.\",\"i\":8},\"i\":6}," +
							   "{\"t\":\"p\",\"c\":{\"s\":\"You do not need to do anything, but if you wish to " +
							   "cancel or update your payment details, you can do so from your customer dashboard.\"," +
							   "\"i\":11},\"i\":12},{\"t\":\"p\",\"c\":{\"s\":\"Thank you again!\",\"i\":6},\"i\":7}],\"i\":3},\"customLogo\":null},\"i\":4},\"i\":5}"
				},
				new EmailTemplate // Not sent by default. Activated by Subscription config/ SendSubscriptionReminderAfterDays
				{
					Name = "Not subscribed - Reminder to subscribe",
					Subject = "Looks like you haven't subscribed yet",
					Key = "not_subscribed_reminder",
					BodyJson = "{\"c\":{\"t\":\"Email/Default\",\"d\":{},\"r\":{\"children\":[{\"t\":" +
							   "\"Email/Centered\",\"d\":{},\"c\":[{\"t\":\"p\",\"c\":{\"s\":\"It\u2019s been " +
							   "some time since you created an account with us, but haven\u2019t yet started a " +
							   "subscription.\",\"i\":6},\"i\":6},{\"t\":\"p\"" +
							   ",\"c\":{\"s\":\"We\u2019re here to help you " +
							   "so don\u2019t hesitate to reach out with questions.\",\"i\":6},\"i\":7}],\"i\":3}," +
							   "{\"t\":\"Email/PrimaryButton\",\"d\":{\"label\":\"Get Started\"," +
							   "\"target\":\"/subscribe\"},\"r\":{\"label\":{\"s\":\"Get Started\",\"i\":6}}," +
							   "\"i\":7}],\"customLogo\":null},\"i\":4},\"i\":5}"
				},
				new EmailTemplate // Not sent by default. Activated by Subscription config/ SendThankYouEmail
				{
					Name = "Thank you for subscribing",
					Subject = "Thank you for subscribing",
					Key = "thank_you_for_subscribing",
					BodyJson =
						"{\"c\":{\"t\":\"Email/Default\",\"d\":{},\"r\":{\"children\":[{\"t\":\"Email/Centered\"," +
						"\"d\":{},\"c\":[{\"t\":\"p\",\"c\":{\"s\":\"Your subscription is now " +
						"confirmed and you are ready to go!\",\"i\":6},\"i\":6}],\"i\":3},{\"t\":\"Email/PrimaryButton\",\"d\":{\"label\":\"My subscriptions\"," +
						"\"target\":\"/my-subscriptions\"},\"r\":{\"label\":{\"s\":\"My Subscriptions\",\"i\":6}}," +
						"\"i\":7}],\"customLogo\":null},\"i\":4},\"i\":5}"
				}
			);
            
            // If a subscription payment changes to state 202, fulfil it immediately.
            Events.Purchase.BeforeUpdate.AddEventListener(
                async (Context context, Purchase purchase, Purchase original) =>
                {
                    // Is it for a subscription, or a multi-execution (>1 subscription) and is this a status change?
                    if (purchase.Status == original.Status ||
                        (purchase.ContentType != "Subscription" && !purchase.MultiExecute))
                    {
                        return purchase;
                    }

                    // State change - is it now a successful payment?
                    if (purchase.Status == 202)
                    {
                        // Success! Mark subscription(s) as renewed for the current month.

                        // Is there a coupon on the purchase and does it add extra days?
                        uint extraDays = 0;

                        if (purchase.CouponId != 0)
                        {
                            var coupon = await Services.Get<CouponService>()
                                .Get(context, purchase.CouponId, DataOptions.IgnorePermissions);

                            if (coupon != null && coupon.SubscriptionDelayDays > 0)
                            {
                                // Yes the coupon has some additional days. This typically happens to indicate a free trial after a card was authorised.
                                // Basically we're just delaying the first actual payment.
                                extraDays = coupon.SubscriptionDelayDays;
                            }
                        }

                        if (purchase.ContentType == "Subscription")
                        {
                            var subscription = await Get(context, purchase.ContentId, DataOptions.IgnorePermissions);

                            // Note that we'll only change the status if the payment is for the current timeslot index.
                            ulong timePeriodKey = (ulong)subscription.LastChargeUtc.Ticks;

                            if (timePeriodKey == purchase.ContentAntiDuplication)
                            {
                                await MarkActive(context, subscription, extraDays);
                            }
                        }

                        if (purchase.MultiExecute)
                        {
                            // Get the list of subscriptions on the purchase and mark each as active. A multi-execute only occurs on initial purchase.
                            var mappedSubscriptions = await ListBySource(context, _purchases, purchase.Id,
                                "subscriptions", DataOptions.IgnorePermissions);

                            foreach (var subscription in mappedSubscriptions)
                            {
                                await MarkActive(context, subscription, extraDays);
                            }
                        }
                    }
                    else if (purchase.Status >= 300)
                    {
                        // A permanent failure. The subscription(s) are now going to be deactivated.
                        if (purchase.ContentType == "Subscription")
                        {
                            var subscription = await Get(context, purchase.ContentId, DataOptions.IgnorePermissions);

                            if (subscription.Status == 1)
                            {
                                await MarkInactive(context, subscription);
                            }
                        }

                        if (purchase.MultiExecute)
                        {
                            // Get the list of subscriptions on the purchase and mark each as active. A multi-execute only occurs on initial purchase.
                            var mappedSubscriptions = await ListBySource(context, _purchases, purchase.Id,
                                "subscriptions", DataOptions.IgnorePermissions);

                            foreach (var subscription in mappedSubscriptions)
                            {
                                await MarkInactive(context, subscription);
                            }
                        }
                    }

                    return purchase;
                }, 11); // Just after everything else such that we can overwrite the desired status to immediately fulfil if necessary.

            Events.Automation("subscription_nudge", "0 0 0 ? * * *").AddEventListener(async (context, runInfo) =>
            {
                if (config.SendSubscriptionReminderAfterDays <= 0)
                {
                    return runInfo;
                }

                var daysAgo = -config.SendSubscriptionReminderAfterDays;

				var users = await _users.Where("Role=? and CreatedUtc<? and CreatedUtc>?", DataOptions.IgnorePermissions)
                    .Bind(Roles.Member.Id)
                    .Bind(DateTime.UtcNow.AddDays(daysAgo))
                    .Bind(DateTime.UtcNow.AddDays(daysAgo - 1))
                    .ListAll(context);

                foreach (var user in users)
                {
                    var sub = await Where("UserId=?", DataOptions.IgnorePermissions).Bind(user.Id).First(context);
                    if (sub == null)
                    {
                        _emails.Send(new List<Recipient>()
                            {
                                new(user)
                            },
                            "not_subscribed_reminder");
                    }
                }

                return runInfo;
            });

            Events.Automation("subscription_payments", "0 0 0 ? * * *").AddEventListener(
                async (Context context, Api.Automations.AutomationRunInfo runInfo) =>
                {
                    // Runs daily. Check for subscriptions which require a payment run.
                    // A payment run occurs if we're in a new timeslot from the last time a particular subscription was charged.

                    // Get current date:
                    var date = DateTime.UtcNow;

                    var meta = await Events.Subscription.BeforeBeginDailyProcess.Dispatch(context,
                        new DailySubscriptionMeta()
                        {
                            ProcessDateUtc = date
                        });

                    if (meta.DoNotProcess)
                    {
                        // Halt! The subscription system has declared that it is not ready to be processed yet.
                        Console.WriteLine(
                            "[NOTICE] Subscription system has been told to not process anything today. This is usually because usage stats for the current time period are not ready yet.");
                        return runInfo;
                    }

                    await RenewSubscriptions(date, context);
                    await NotifyCardExpires(date, context);
                    await NotifySubscriptionRenews(date, context);

                    return runInfo;
                });
        }

        private SubscriptionConfig config;

        private async Task NotifySubscriptionRenews(DateTime currentDateUtc, Context context)
        {
            if (!config.SendRenewalEmails)
            {
                return;
            }
            var subsToNotify = await Where("Status<? and NextChargeUtc>? and NextChargeUtc<?",
                    DataOptions.IgnorePermissions)
                .Bind((uint)2)
                .Bind(currentDateUtc.Date.AddDays(5))
                .Bind(currentDateUtc.Date.AddDays(6))
                .ListAll(context);
            foreach (var subscription in subsToNotify)
            {
                _emails.Send(new List<Recipient>
                {
                    new(subscription.UserId, subscription.LocaleId)
                }, "subscription_renews_soon");
            }
        }

        private async Task NotifyCardExpires(DateTime currentDateUtc, Context context)
        {
            var expiringPaymentMethods = await _paymentMethods.Where(
                    "ExpiryUtc>? and ExpiryUtc<? and OneMonthExpiryNotice=?",
                    DataOptions.IgnorePermissions)
                .Bind(currentDateUtc)
                .Bind(currentDateUtc.AddMonths(1))
                .Bind(false)
                .ListAll(context);

            foreach (var paymentMethod in expiringPaymentMethods)
            {
                var subscription = await Where("PaymentMethodId=?", DataOptions.IgnorePermissions)
                    .Bind(paymentMethod.Id)
                    .First(context);

                var recipient = new Recipient(subscription.UserId, subscription.LocaleId)
                {
                    CustomData = new
                    {
                        SubscriptionId = subscription.Id,
                        CardLastFour = paymentMethod.Name
                    }
                };

                _emails.Send(new List<Recipient>
                {
                    recipient
                }, "card_expires_in_a_month");

                await _paymentMethods.Update(context, paymentMethod,
                    (ctx, paymentMethodToUpdate, original) => { paymentMethodToUpdate.OneMonthExpiryNotice = true; },
                    DataOptions.IgnorePermissions);
            }
        }

        private async Task RenewSubscriptions(DateTime date, Context context)
        {
            // Get all subscriptions which are in need of updating:
            var subsToUpdate = await Where("Status<? and NextChargeUtc<?", DataOptions.IgnorePermissions)
                .Bind((uint)2)
                .Bind(date)
                .ListAll(context);

            // Future feature, if large number of subs: could make the automation more frequent (hourly rather than daily) and let it run batches.

            // For each one, charge it.
            foreach (var subscription in subsToUpdate)
            {
                if (subscription.WillCancel)
                {
                    // Cancelling this subscription.
                    // This occurs here such that any duration the user has paid for can be fully utilised.
                    await Update(context, subscription, (Context context, Subscription toUpdate, Subscription orig) =>
                    {
                        // Cancelled (by user)
                        toUpdate.Status = 2;
                        toUpdate.WillCancel = false;
                    }, DataOptions.IgnorePermissions);

                    // Send email:
                    var recipients = new List<Recipient>();
                    var userRecipient = new Recipient(subscription.UserId, subscription.LocaleId);
                    recipients.Add(userRecipient);
                    _emails.Send(recipients, "subscription_cancelled");

#warning TODO: charge overages if there are any. Must discount any pre-paid amounts.
                }
                else
                {
                    await ChargeSubscription(context, subscription, null, true);
                }
            }
        }

        /// <summary>
        /// Marks the given subscription as active. Usually triggered by payments only.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public async ValueTask<Subscription> MarkActive(Context context, Subscription subscription, uint extraDays)
        {
            // This payment is for the latest period of the subscription.
            // Update the subscription and set to active if not already.
            return await Update(context, subscription, (Context ctx, Subscription subToUpdate, Subscription orig) =>
            {
                // Update the charge dates:
                SetUpdatedChargeDates(subToUpdate, extraDays);

                // it's active:
                subToUpdate.Status = 1;
            }, DataOptions.IgnorePermissions);
        }

        /// <summary>
        /// Marks the given subscription as inactive (state 3, paused). Usually triggered by payments only.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public async ValueTask<Subscription> MarkInactive(Context context, Subscription subscription)
        {
            // This payment is for the latest period of the subscription.
            // Update the subscription and set to active if not already.
            return await Update(context, subscription, (Context ctx, Subscription subToUpdate, Subscription orig) =>
            {
                // Paused by payment failure:
                subToUpdate.Status = 3;
            }, DataOptions.IgnorePermissions);
        }

        /// <summary>
        /// Sets the next charge date for a given subscription. Must be called within an update statement.
        /// </summary>
        /// <returns></returns>
        public void SetUpdatedChargeDates(Subscription sub, uint extraDays = 0)
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
                    sub.NextChargeUtc = chargeDateUtc.AddYears(1);
                    break;
                case 3:
                    // Add a week to the charge date.
                    sub.NextChargeUtc = chargeDateUtc.AddDays(7);
                    break;
                default:
                    throw new Exception("Unknown billing frequency " + sub.TimeslotFrequency);
            }

            if (extraDays != 0)
            {
                sub.NextChargeUtc = sub.NextChargeUtc.AddDays(extraDays);
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
        /// <param name="coupon"></param>
        /// <param name="offline">True if the payment is being made offline (without the user present. Most subscription purchases are offline).</param>
        public async ValueTask<PurchaseAndAction> ChargeSubscription(Context context, Subscription subscription,
            Coupon coupon = null, bool offline = false)
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
                    await Update(context, subscription,
                        (Context context, Subscription toUpdate, Subscription orig) =>
                        {
                            SetUpdatedChargeDates(toUpdate);
                        }, DataOptions.IgnorePermissions);

                    return new PurchaseAndAction()
                    {
                        Purchase = purchase
                    };
                }

                if (purchase.Status >= 100 && purchase.Status < 200)
                {
                    // It's in the waiting for gateway state.
                    System.Console.WriteLine(
                        "[WARN] Manual intervention required. Subscription has waited unusually long for payment response. Gateway webhook likely misfired.");

                    return new PurchaseAndAction()
                    {
                        Purchase = purchase
                    };
                }

                // All other status codes indicate permanent failure or not yet submitted to gateway.
                // It is therefore safe to effectively recreate the products on the subscription and go again by first resetting the code.
                purchase = await _purchases.Update(context, purchase,
                    (Context context, Purchase toUpdate, Purchase orig) =>
                    {
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

            // Get the payment method from the subscription. Offline subscription charges can safely use IgnorePermissions.
            PaymentMethod method = null;

            if (offline)
            {
                method = await _paymentMethods.Get(context, purchase.PaymentMethodId, DataOptions.IgnorePermissions);
            }

            // Attempt to fulfil the purchase now:
            return await _purchases.Execute(context, purchase, method, coupon);
        }

        /// <summary>
        /// Gets the products in the given subscription.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public async ValueTask<List<ProductQuantity>> GetProducts(Context context, Subscription subscription)
        {
            return await _productQuantities.Where("SubscriptionId=?", DataOptions.IgnorePermissions)
                .Bind(subscription.Id).ListAll(context);
        }

        /// <summary>
        /// Gets the products in the given subscription.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public async ValueTask<List<ProductQuantity>> GetProducts(Context context, uint subscriptionId)
        {
            return await _productQuantities.Where("SubscriptionId=?", DataOptions.IgnorePermissions)
                .Bind(subscriptionId).ListAll(context);
        }

        /// <summary>
        /// Adds the given product to the subscription.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subscription"></param>
        /// <param name="product"></param>
        /// <param name="quantity">Optional: Often subscription level products are added once.</param>
        /// <returns></returns>
        public async ValueTask<ProductQuantity> AddToSubscription(Context context, Subscription subscription,
            Product product, uint quantity = 1)
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
                await _productQuantities.Update(context, pQuantity,
                    (Context ctx, ProductQuantity toUpdate, ProductQuantity orig) =>
                    {
                        toUpdate.Quantity += quantity;
                    });
            }

            return pQuantity;
        }
    }
}