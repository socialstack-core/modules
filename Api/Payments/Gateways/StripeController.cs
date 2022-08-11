using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>
/// Used to handle the stripe webhook.
/// </summary>
[ApiController]
[Route("v1/stripe-gateway")]
public class StripeController : ControllerBase
{
    private StripeService _stripe;

    /// <summary>
    /// Instanced automatically.
    /// </summary>
    /// <param name="stripe"></param>
    public StripeController(StripeService stripe)
    {
        _stripe = stripe;
    }

    /// <summary>
    /// Create a setup intent.
    /// </summary>
    /// <returns></returns>
    [HttpGet("setup")]
    public async ValueTask<StripeIntentResponse> SetupIntent()
    {
        var context = await Request.GetContext();

        var secret = await _stripe.SetupIntent(context);

        return new StripeIntentResponse() {
            ClientSecret = secret
        };
    }

    /// <summary>
    /// Updates a purchase based on a webhook event from a stripe payment.
    /// </summary>
    /// <returns></returns>
    [HttpPost("webhook")]
    public async ValueTask<IActionResult> Webhook()
    {
        // Get the stripe config:
        var stripeConfig = _stripe.Config;

        if (stripeConfig == null || string.IsNullOrEmpty(stripeConfig.PaymentEndpointSecret))
        {
            // Reject.
            Console.WriteLine("Attempted to use stripe webhook but stripe is not configured.");
            return BadRequest();
        }

        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var signatureHeader = Request.Headers["Stripe-Signature"];

        var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, stripeConfig.PaymentEndpointSecret);

        // Handle the webhook call:
        await _stripe.HandleWebhook(stripeEvent);

        return Ok();
    }

    /// <summary>
    /// SetupIntent response
    /// </summary>
    public struct StripeIntentResponse
    {

        /// <summary>
        /// The client secret.
        /// </summary>
        public string ClientSecret;

    }
}