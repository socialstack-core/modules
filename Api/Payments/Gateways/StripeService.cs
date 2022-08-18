using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Stripe;
using Api.Startup;
using Api.Users;
using System;
using Api.Configuration;

namespace Api.Payments
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class StripeService : AutoService
    {
		private StripeGateway _gateway;
		private StripeConfig _config;
		private PurchaseService _purchases;
		private Context _context;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StripeService(PaymentGatewayService gateways, PurchaseService purchases)
        {
			_purchases = purchases;

			// use a developer context:
			_context = new Context(Roles.Developer);

			// Get configuration:
			var stripeConfig = GetConfig<StripeConfig>();
			
			if(stripeConfig.SecretKey == null)
			{
				// Not configured - don't register Stripe.
				return;
			}

			_config = stripeConfig;
			_gateway = new StripeGateway(stripeConfig);

			StripeConfiguration.ApiKey = _config.SecretKey;

			_config.OnChange += () => {

				StripeConfiguration.ApiKey = _config.SecretKey;

				return new ValueTask();
			};

			gateways.Register(_gateway);
		}

		/// <summary>
		/// Create a setup intent.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<string> SetupIntent(Context context)
		{
			var user = context.User;

			if (user == null)
			{
				throw new PublicException("You need to be logged in to perform this action", "no_user");
			}

			var setupIntentService = new SetupIntentService();

			var setupIntent = await setupIntentService.CreateAsync(new SetupIntentCreateOptions
			{
				PaymentMethodTypes = new List<string>
				{
					"card",
				},
				Metadata = new Dictionary<string, string> {
					{ "User Id", user.Id.ToString() }
				}
			});

			return setupIntent.ClientSecret;
		}

		/// <summary>
		/// The current stripe config. It's null if stripe is not registered.
		/// </summary>
		public StripeConfig Config => _config;

		/// <summary>
		/// Called to handle a webhook being triggered.
		/// </summary>
		/// <param name="stripeEvent"></param>
		/// <returns></returns>
		public async ValueTask HandleWebhook(Event stripeEvent)
		{
			// Only interested in payment intent responses, all others can be ignored if they have been configured on stripe
			if (stripeEvent.Data.Object.Object == "payment_intent")
			{
				var intent = (PaymentIntent)stripeEvent.Data.Object;

				if (!intent.Metadata.TryGetValue("PurchaseId", out string purchaseIdStr))
				{
					// Some sort of remote purchase. Ignore it.
					return;
				}

				if (!uint.TryParse(purchaseIdStr, out uint purchaseId))
				{
					// Invalid purchase ID.
					return;
				}

				// Get the purchase:
				var purchase = await _purchases.Get(_context, purchaseId, DataOptions.IgnorePermissions);

				if (purchase == null)
				{
					throw new PublicException("No purchase found for stripe payment intent", "no_purchase");
				}

				if (stripeEvent.Type == Stripe.Events.PaymentIntentPaymentFailed)
				{
					// E.g. card rejected it.
					await _purchases.Update(_context, purchase, (Context c, Purchase toUpdate, Purchase orig) => {
						toUpdate.Status = 500;
					}, DataOptions.IgnorePermissions);
				}
				else if (
					stripeEvent.Type == Stripe.Events.PaymentIntentCreated || 
					stripeEvent.Type == Stripe.Events.PaymentIntentProcessing || 
					stripeEvent.Type == Stripe.Events.PaymentIntentPartiallyFunded
				)
				{
					// Still processing
				}
				else if (stripeEvent.Type == Stripe.Events.PaymentIntentRequiresAction || stripeEvent.Type == Stripe.Events.PaymentIntentCanceled)
				{
					// User caused in some way.
					await _purchases.Update(_context, purchase, (Context c, Purchase toUpdate, Purchase orig) => {
						toUpdate.Status = 400;
					}, DataOptions.IgnorePermissions);

					// todo: inform user that action is required
				}
				else if (stripeEvent.Type == Stripe.Events.PaymentIntentSucceeded)
				{
					// Payment success!
					await _purchases.Update(_context, purchase, (Context c, Purchase toUpdate, Purchase orig) => {
						toUpdate.Status = 202;
					}, DataOptions.IgnorePermissions);
				}
				// ... handle other event types
				else
				{
					Console.WriteLine("Unhandled stripe event type: {0}", stripeEvent.Type);
				}

			}

		}

	}

	/// <summary>
	/// The stripe payment gateway.
	/// </summary>
	public class StripeGateway : PaymentGateway
	{

		/// <summary>
		/// Creates a stripe gateway.
		/// </summary>
		/// <param name="config"></param>
		public StripeGateway(StripeConfig config)
		{
			_config = config;
			Id = 1;
		}

		/// <summary>
		/// The config for the gateway.
		/// </summary>
		private StripeConfig _config;

		private UserService _users;

		private PurchaseService _purchases;

		private PaymentMethodService _paymentMethods;

		private Context _context = new Context(1, 1, 1);

		/// <summary>
		/// Converts intent status to a Purchase.Status ID.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		public uint ConvertStatus(string status)
		{
			if (status == "requires_payment_method" || status == "requires_confirmation" || status == "requires_capture" || status == "canceled")
			{
				// Failed (frontend failure)
				return 400;
			}
			else if (status == "processing")
			{
				// Pending at gateway.
				return 102;
			}
			else if (status == "requires_action")
			{
				// Pending but requires user to go to an action URL.
				return 103;
			}
			else if (status == "succeeded")
			{
				// Payment succeeded.
				return 202;
			}
			else
			{
				// Failed. Unknown error state.
				return 500;
			}
		}

		private Stripe.PaymentIntentService _paymentIntentService;
		private Stripe.PaymentMethodService _paymentMethodService;
		private Stripe.CustomerService _customerService;

		/// <summary>
		/// Checks if a token is valid and if so, returns its info. *Do not* send this full information to the frontend. It is API only.
		/// You *may* send the Name and ExpiryUtc fields only. Using this method triggers a full PCI DSS requirement as it pulls card information to the server.
		/// </summary>
		/// <param name="gatewayToken"></param>
		/// <returns></returns>
		public override async ValueTask<TokenInformation> GetTokenDetails(string gatewayToken)
		{
			if (_paymentMethodService == null)
			{
				_paymentMethodService = new Stripe.PaymentMethodService();
			}

			// Get the method:
			var paymentMethod = await _paymentMethodService.GetAsync(gatewayToken);

			// We'll only use Stripe for cards at the moment
			var isValid = paymentMethod != null && paymentMethod.Card != null;

			if (!isValid)
			{
				return new TokenInformation() { Valid = false };
			}

			// last 4 card digits:
			var last4 = paymentMethod.Card.Last4;
			var expiryDate = new DateTime((int)paymentMethod.Card.ExpYear, (int)paymentMethod.Card.ExpMonth, 1, 0, 0, 0, DateTimeKind.Utc);

			return new TokenInformation()
			{
				Valid = (expiryDate > DateTime.UtcNow), // It's valid if it hasn't expired yet
				Name = last4,
				ExpiryUtc = expiryDate,
				GatewayData = paymentMethod,
			};
		}

		/// <summary>
		/// Prepares the given token. This can be used to, for example, convert a single use token into a multi-use one depending on the gateway.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="gatewayToken"></param>
		/// <returns></returns>
		public override async ValueTask<string> PrepareToken(Context context, string gatewayToken)
		{
			if (_paymentMethodService == null)
			{
				_paymentMethodService = new Stripe.PaymentMethodService();
			}

			if (_customerService == null)
			{
				_customerService = new Stripe.CustomerService();
			}

			// Method must be attached to a customer to be reusable.
			var customer = await _customerService.CreateAsync(new CustomerCreateOptions() {
				Email = context.User == null ? null : context.User.Email
			});

			// Create a reusable payment method from a single use token:
			var stripeMethod = await _paymentMethodService.CreateAsync(new PaymentMethodCreateOptions() {
				Type = "card",
				Card = new PaymentMethodCardOptions()
				{
					Token = gatewayToken
				}
			});

			// Attach to customer:
			await _paymentMethodService.AttachAsync(stripeMethod.Id, new PaymentMethodAttachOptions() {
				Customer = customer.Id
			});

			// Return the customer and payment method ID which will be the new gateway token.
			return customer.Id + "/" + stripeMethod.Id;
		}

		/// <summary>
		/// Asks the payment gateway to complete a purchase.
		/// </summary>
		/// <param name="purchase"></param>
		/// <param name="totalCost"></param>
		/// <param name="paymentMethod"></param>
		/// <returns></returns>
		public override async ValueTask<PurchaseAndAction> ExecutePurchase(Purchase purchase, ProductCost totalCost, PaymentMethod paymentMethod)
		{
			if (_users == null)
			{
				_users = Services.Get<UserService>();
				_purchases = Services.Get<PurchaseService>();
				_paymentMethods = Services.Get<PaymentMethodService>();
			}

			// Get the user:
			var user = await _users.Get(_context, purchase.UserId, DataOptions.IgnorePermissions);

			// Get the payment method:
			if (paymentMethod == null || string.IsNullOrEmpty(paymentMethod.GatewayToken))
			{
				throw new PublicException("The provided payment method is invalid.", "invalid_payment_method");
			}

			// Start creating the payment intent:
			if (_paymentIntentService == null)
			{
				_paymentIntentService = new PaymentIntentService();
			}

			StripeConfiguration.ApiKey = _config.SecretKey;

			if (totalCost.Amount >= long.MaxValue)
			{
				// Long cast overflow check:
				throw new PublicException("Requested quantity is too large.", "substantial_quantity");
			}

			var longAmount = (long)totalCost.Amount;

			// Mark as starting to submit to gateway and add the total cost to it:
			await _purchases.Update(_context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) => {

				// It might have instantly completed or instantly failed. We can find out from the status:
				toUpdate.Status = 101;
				toUpdate.TotalCost = totalCost.Amount;
				toUpdate.CurrencyCode = totalCost.CurrencyCode;

			});

			var gatewayTokenCustomerEnd = paymentMethod.GatewayToken.IndexOf('/');
			var customerId = paymentMethod.GatewayToken.Substring(0, gatewayTokenCustomerEnd);
			var methodId = paymentMethod.GatewayToken.Substring(gatewayTokenCustomerEnd + 1);

			var paymentIntent = await _paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
			{
				Amount = longAmount,
				Customer = customerId,
				Currency = totalCost.CurrencyCode,
				PaymentMethod = methodId,
				Confirm = true,
				ReturnUrl = AppSettings.GetPublicUrl() + "/complete?status=success",
				AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
				{
					Enabled = true,
				},
				ReceiptEmail = user != null && user.Email != null ? user.Email : null,
				Metadata = new Dictionary<string, string> {
					{ "PurchaseId", purchase.Id.ToString() },
					{ "UserId", user == null ? "0" : user.Id.ToString() }
				}
			});

			// Update purchase with Id from payment intent and the total cost:
			purchase = await _purchases.Update(_context, purchase, (Context ctx, Purchase toUpdate, Purchase orig) => {

				// It might have instantly completed or instantly failed. We can find out from the status:
				toUpdate.Status = ConvertStatus(paymentIntent.Status);
				toUpdate.PaymentGatewayInternalId = paymentIntent.Id;

			});

			string action = null;

			if (purchase.Status == 103 && paymentIntent.NextAction != null && paymentIntent.NextAction.Type == "redirect_to_url")
			{
				var redir = paymentIntent.NextAction.RedirectToUrl;

				if (redir != null)
				{
					action = redir.Url;
				}
			}

			return new PurchaseAndAction() {
				Purchase = purchase,
				Action = action
			};
		}

	}

}
