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
public partial class StdOutController : AutoController
{
	/*
	/// <summary>
	/// Page cache status.
	/// </summary>
	[HttpGet("cachestatus/html")]
	public HtmlCacheStatus HtmlCache(Context context)
	{
		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_cachestat", context);
		}

		return Services.Get<HtmlService>().GetCacheStatus();
	}
	*/

	/// <summary>
	/// Plaintext benchmark.
	/// </summary>
	/// <returns></returns>
	[HttpGet("helloworld")]
	public string PlainTextBenchmark()
	{
		return "Hello, World!";
	}
}

