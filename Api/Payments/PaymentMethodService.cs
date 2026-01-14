using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.Startup;
using System;

namespace Api.Payments
{
	/// <summary>
	/// Handles paymentMethods.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PaymentMethodService : AutoService<PaymentMethod>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PaymentMethodService() : base(Events.PaymentMethod)
		{
		}

		/// <summary>
		/// Gets a payment method from the JSON representation.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="paymentMethodJson"></param>
		/// <param name="saveableRequired">
		/// Subscribing to something requires saving the payment method.
		/// </param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		public async ValueTask<PaymentMethod> GetFromJson(Context context, JToken paymentMethodJson, bool saveableRequired)
		{
			if (paymentMethodJson == null)
			{
				// Only permitted if BNPL is.
				var bnpl = await Events.PaymentMethod.AuthoriseBuyNowPayLater.Dispatch(context, new BuyNowPayLater());

				if (!bnpl.Authorized)
				{
					throw new PublicException("Buy now pay later is not available at the moment.", "bnpl/not_authorized");
				}

				// Null is acceptable if BNPL is authd.
				return null;
			}

			// Must of course check ownership if an ID is provided.
			if (paymentMethodJson.Type == JTokenType.Integer)
			{
				// Convert to a long:
				var value = paymentMethodJson.ToObject<long>();

				if (value <= 0 || value > uint.MaxValue)
				{
					throw new PublicException("Payment method ID provided but it did not exist", "payment_method_invalid");
				}

				// -> uint:
				var methodId = (uint)value;

				// Get by that ID - dependent on the permission system permitting this:
				var paymentMethod = await Services.Get<PaymentMethodService>().Get(context, methodId);

				if (paymentMethod == null)
				{
					throw new PublicException("Payment method ID provided but it did not exist", "payment_method_invalid");
				}

				return paymentMethod;
			}

			if (paymentMethodJson.Type != JTokenType.Object)
			{
				throw new PublicException("Payment method ID provided but it was an invalid type", "payment_method_invalid");
			}

			// gatewayToken, gatewayId, save: true|false
			// Saving is required and inferred to be true if any of the products are subscriptions.

			var nameJson = paymentMethodJson["name"];
			var expiryJson = paymentMethodJson["expiry"];
			var issuerJson = paymentMethodJson["issuer"];
			var gatewayTokenJson = paymentMethodJson["gatewayToken"];
			var gatewayIdJson = paymentMethodJson["gatewayId"];
			var saveableJson = paymentMethodJson["canSave"];
			var sessionIdJson = paymentMethodJson["sessionId"];
			var browserDetailsJson = paymentMethodJson["browserDetails"];

			var gatewayToken = gatewayTokenJson.ToObject<string>();
			var gatewayId = gatewayIdJson.ToObject<long>();

			if (gatewayId <= 0 || gatewayId > uint.MaxValue)
			{
				throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
			}

			// optional session id used for payment transaction process with some providers (opayo)
			var sessionId = sessionIdJson.ToObject<string>();

			// the payment details may also include browser details for 3ds
			var browserInfo = browserDetailsJson.ToObject<BrowserInfo>() ?? null;

			// has the user said we can save their details
			bool saveable = saveableJson.ToObject<bool>();

			// if a subscription then must save the details
			if (!saveable && saveableRequired)
			{
				saveable = true;
			}

			if (saveable)
			{
				if (context.UserId == 0)
				{
					throw new PublicException("Unable to save card details for guest user", "payment_unable_save_details");
				}

				var canSaveCard = nameJson != null && expiryJson != null && issuerJson != null;
				if (!canSaveCard)
				{
					throw new PublicException(
						"name, expiry and issuer required when saving card details",
						"payment_method_missing_data");
				}
			}

			if (saveableRequired)
			{
				// Saving is required if a product is a subscription.
				if (!saveable)
				{
					throw new PublicException("name, expiry and issuer required when adding a new subscription payment method", "payment_method_missing_data");
				}
			}

			// Get the payment gateway:
			var gateway = Services.Get<PaymentGatewayService>().Get((uint)gatewayId);

			if (gateway == null)
			{
				throw new PublicException("Gateway ID provided but it did not exist", "gateway_invalid");
			}

			// Ask the gateway to convert the public gateway token if it needs to do so into a private one
			gatewayToken = await gateway.PrepareToken(context, gatewayToken);

			if (saveable)
			{
				var name = nameJson.ToString();
				var expiryUtc = expiryJson.ToObject<DateTime>();
				var issuer = issuerJson.ToString();

				return await Services.Get<PaymentMethodService>().Create(context, new PaymentMethod()
				{
					Issuer = issuer,
					UserId = context.UserId,
					Name = name,
					ExpiryUtc = expiryUtc,
					LastUsedUtc = DateTime.UtcNow,
					GatewayToken = gatewayToken,
					PaymentGatewayId = gateway.Id,
					SessionId = sessionId,
					BrowserInfo = browserInfo
				}, DataOptions.IgnorePermissions);
			}

			// Create a method but don't save it.
			return new PaymentMethod()
			{
				UserId = context.UserId,
				LastUsedUtc = DateTime.UtcNow,
				GatewayToken = gatewayToken,
				PaymentGatewayId = gateway.Id,
				SessionId = sessionId,
				BrowserInfo = browserInfo
			};
		}
	}
}
