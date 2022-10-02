using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Payments
{
    /// <summary>Handles subscription endpoints.</summary>
    [Route("v1/subscription")]
	public partial class SubscriptionController : AutoController<Subscription>
    {
		/// <summary>
		/// Updates the card in use on a given subscription.
		/// </summary>
        [HttpPost("{subscriptionId}/update-card")]
        public virtual async ValueTask<CardUpdateStatus> UpdateCard([FromRoute] uint subscriptionId, [FromBody] JObject cardUpdate)
        {
            // Get the context (which user is asking)
            var context = await Request.GetContext();
            // Parse the payment method.
            var paymentMethodJson = cardUpdate["paymentMethod"];
            PaymentMethod paymentMethod;
            if (paymentMethodJson == null || paymentMethodJson.Type != JTokenType.Object)
            {
                throw new PublicException("Payment method information missing", "payment_method_required");
            }

            var nameJson = paymentMethodJson["name"];
            var expiryJson = paymentMethodJson["expiry"];
            var issuerJson = paymentMethodJson["issuer"];
            var gatewayTokenJson = paymentMethodJson["gatewayToken"];
            var gatewayIdJson = paymentMethodJson["gatewayId"];
            var gatewayToken = gatewayTokenJson.ToObject<string>();
            var gatewayId = gatewayIdJson.ToObject<long>();
            if (gatewayId <= 0 || gatewayId > uint.MaxValue)
            {
                throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
            }

            var subscription = await _service.Get(context, subscriptionId);
            if (subscription == null)
            {
                // Saving is required if a product is a subscription.
                throw new PublicException("Subscription Id is provided but it did not exist",
                    "subscription_invalid");
            }

            var canSaveCard = nameJson != null && expiryJson != null && issuerJson != null;
            if (!canSaveCard)
            {
                throw new PublicException(
                    "name, expiry and issuer required when adding a new subscription payment method",
                    "payment_method_missing_data");
            }

            // Get the payment gateway:
            var gateway = Services.Get<PaymentGatewayService>().Get((uint)gatewayId);
            if (gateway == null)
            {
                throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
            }

            // Ask the gateway to convert the gateway token if it needs to do so.
            gatewayToken = await gateway.PrepareToken(context, gatewayToken);

            var name = nameJson.ToString();
            var expiryUtc = expiryJson.ToObject<DateTime>();
            var issuer = issuerJson.ToString();
            paymentMethod = await Services.Get<PaymentMethodService>().Create(context, new PaymentMethod()
            {
                Issuer = issuer,
                UserId = context.UserId,
                Name = name,
                ExpiryUtc = expiryUtc,
                LastUsedUtc = DateTime.UtcNow,
                GatewayToken = gatewayToken,
                PaymentGatewayId = gateway.Id
            }, DataOptions.IgnorePermissions);

            var res = await _service.Update(context, subscription,
                (ctx, target, source) => { target.PaymentMethodId = paymentMethod.Id; });
            if (res != null && res.PaymentMethodId == paymentMethod.Id)
            {
                return new CardUpdateStatus { Status = 200 };
            }
            throw new PublicException("Payment method information missing", "payment_method_required");
        }
    }
}