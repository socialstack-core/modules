using Api.Contexts;
using Api.PaymentGateways;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Api.Purchases
{
    /// <summary>Handles purchase endpoints.</summary>
    [Route("v1/purchase")]
	public partial class PurchaseController : AutoController<Purchase>
    {
        private string _stripePaymentEndpointSecret;

        /// <summary>
		/// Instanced automatically.
		/// </summary>
        public PurchaseController(PaymentGatewayService paymentGateways) : base()
        {
            var paymentGatewayConfig = paymentGateways.GetConfig<PaymentGatewayConfig>();
            _stripePaymentEndpointSecret = paymentGatewayConfig.StripePaymentEndpointSecret;
        }

        /// <summary>
		/// Updates a purchase based on a webhook event from a stripe payment.
		/// </summary>
		/// <returns></returns>
        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            // use a developer context
            var context = new Context(Roles.Developer);

            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);
                var signatureHeader = Request.Headers["Stripe-Signature"];

                stripeEvent = EventUtility.ConstructEvent(json,signatureHeader, _stripePaymentEndpointSecret);

                //Only interested in payment intent responses, all others can be ignored if they have been configured on stripe
                if (stripeEvent.Data.Object.Object == "payment_intent") 
                { 
                    var intent = (PaymentIntent)stripeEvent.Data.Object;

                    var purchase = await (_service as PurchaseService).Where("ThirdPartyId=?", DataOptions.IgnorePermissions).Bind(intent.Id).First(context);

                    if (purchase == null)
                    {
                        Console.WriteLine("Failed to process stripe webhook event: " + stripeEvent.Type + ". No purchase found for stripe payment intent: " + intent.Id);
                        return StatusCode(500);
                    }

                    var purchaseToUpdate = await (_service as PurchaseService).StartUpdate(context, purchase);

                    if (purchaseToUpdate != null)
                    {
                        if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
                        {
                            purchaseToUpdate.DidPaymentFail = true;
                            purchaseToUpdate.IsPaymentProcessed = false;
                            purchaseToUpdate.IsPaymentProcessed = false;
                        }
                        else if (stripeEvent.Type == Events.PaymentIntentRequiresAction)
                        {
                            purchaseToUpdate.DoesPaymentRequireAction = true;
                            purchaseToUpdate.DidPaymentFail = false;
                            purchaseToUpdate.IsPaymentProcessed = false;
                            // todo: inform user that action is required
                        }
                        else if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                        {
                            purchaseToUpdate.IsPaymentProcessed = true;
                            purchaseToUpdate.DoesPaymentRequireAction = false;
                            purchaseToUpdate.DidPaymentFail = false;
                        }
                        // ... handle other event types
                        else
                        {
                            Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                            return Ok();
                        }

                        // update purchase with Id from payment intent
                    
                        purchase = await (_service as PurchaseService).FinishUpdate(context, purchaseToUpdate, purchase);
                    }

                }
                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return BadRequest();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return StatusCode(500);
            }
        }
    }
}