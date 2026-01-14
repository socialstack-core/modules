using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Permissions;
using Api.Pages;
using Api.CanvasRenderer;

namespace Api.Startup;

/// <summary>
/// </summary>
public partial class StdOutController : AutoController
{

	/// <summary>
	/// Attempts to purge V8 engines from the canvas renderer service.
	/// </summary>
	[HttpGet("v8/clear")]
	public void V8Clear(Context context)
	{
		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_v8clear", context);
		}

		Services.Get<CanvasRendererService>().ClearEngineCaches();
	}

}

