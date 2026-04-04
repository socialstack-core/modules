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
	/// <param name="purchases"></param>
	/// 
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
}