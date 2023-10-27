using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Permissions;
using Api.Pages;
using Api.CanvasRenderer;

namespace Api.Startup;

/// <summary>
/// </summary>
public partial class StdOutController : ControllerBase
{

	/// <summary>
	/// V8 status.
	/// </summary>
	[HttpGet("v8/status")]
	public async ValueTask V8Status()
	{
		var context = await Request.GetContext();

		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_v8status", context);
		}

		// Future if needed!
	}

	/// <summary>
	/// Attempts to purge V8 engines from the canvas renderer service.
	/// </summary>
	[HttpGet("v8/clear")]
	public async ValueTask V8Clear()
	{
		var context = await Request.GetContext();

		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_v8clear", context);
		}

		Services.Get<CanvasRendererService>().ClearEngineCaches();
	}

}

