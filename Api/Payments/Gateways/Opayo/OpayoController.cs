using Api.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>
/// Used to handle the opayo webhook.
/// </summary>
[ApiController]
[Route("v1/opayo-gateway")]
public class OpayoController : AutoController
{
	private OpayoService _opayo;

	/// <summary>
	/// Instanced automatically.
	/// </summary>
	/// <param name="opayo"></param>
	public OpayoController(OpayoService opayo)
	{
		_opayo = opayo;
	}

	/// <summary>
	/// Gets the merchant session key for opayo
	/// </summary>
	/// <returns></returns>
	[HttpGet("merchantsessionkey")]
	public async ValueTask<Opayo.Response.MerchantSessionKeyResponse> GetMerchantSessionKey(Context context)
	{
		return await _opayo.GetMerchantSessionKey();
	}

	/// <summary>
	/// Gets the client request ip address
	/// </summary>
	/// <returns></returns>
	[HttpGet("getipaddress")]
	public string GetRequestIP(HttpContext httpContext, Context context)
	{
		return RequestHelper.GetClientIp(httpContext);
	}


	///// <summary>
	///// Updates a purchase based on a webhook event from a opayo payment.
	///// </summary>
	///// <returns></returns>
	//[HttpPost("webhook")]
	//public async ValueTask<PublicMessage?> Webhook(HttpContext httpContext)
	//{
	//	// Get the opayo config:
	//	var opayoConfig = _opayo.Config;

	//	if (opayoConfig == null || string.IsNullOrEmpty(opayoConfig.PaymentEndpointSecret))
	//	{
	//		// Reject.
	//		Console.WriteLine("Attempted to use opayo webhook but opayo is not configured.");
	//		return null;
	//	}

	//	var json = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();

	//	var signatureHeader = httpContext.Request.Headers["Opayo-Signature"];

	//	var opayoEvent = EventUtility.ConstructEvent(json, signatureHeader, opayoConfig.PaymentEndpointSecret);

	//	// Handle the webhook call:
	//	await _opayo.HandleWebhook(opayoEvent);

	//	return new PublicMessage("Handled", "webhook/ok");
	//}


}