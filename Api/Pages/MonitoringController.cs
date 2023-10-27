using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Permissions;
using Api.Pages;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Api.Startup;

/// <summary>
/// </summary>
public partial class StdOutController : ControllerBase
{

	/// <summary>
	/// Page cache status.
	/// </summary>
	[HttpGet("cachestatus/html")]
	public async ValueTask<HtmlCacheStatus> HtmlCache()
	{
		var context = await Request.GetContext();

		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_cachestat", context);
		}

		return Services.Get<HtmlService>().GetCacheStatus();
	}

	/// <summary>
	/// Plaintext benchmark.
	/// </summary>
	/// <returns></returns>
	[HttpGet("helloworld")]
	public IActionResult PlainTextBenchmark()
	{
		return new PlainTextActionResult();
	}

	private class PlainTextActionResult : IActionResult
	{
		private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

		public Task ExecuteResultAsync(ActionContext context)
		{
			context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
			context.HttpContext.Response.ContentType = "text/plain";
			context.HttpContext.Response.ContentLength = _helloWorldPayload.Length;
			return context.HttpContext.Response.Body.WriteAsync(_helloWorldPayload, 0, _helloWorldPayload.Length);
		}
	}
}

