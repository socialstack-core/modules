#nullable enable
using Api.Contexts;
using Api.Eventing;
using Api.Startup.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Api.Startup.Health;

/// <summary>
/// Handles healthz and readyz endpoints for container health checks.
/// </summary>
public partial class HealthzController : AutoController
{
	/// <summary>
	/// Liveness check endpoint. Returns health status information.
	/// Returns 503 status with payload if unhealthy.
	/// </summary>
	[HttpGet("healthz")]
	public async ValueTask Healthz(Context context, HttpContext httpContext, [FromQuery] bool dependencies = false)
	{
		var now = DateTime.UtcNow;
		var process = Process.GetCurrentProcess();
		var uptime = now - process.StartTime.ToUniversalTime();
		var routerReady = Router.CurrentRouter != null;

		var checks = new HealthzChecks()
		{
			Ok = routerReady,
			RouterReady = routerReady
		};

		checks = await Events.Healthz.RunChecks.Dispatch(context, checks);

		if (dependencies)
		{
			checks = await Events.Healthz.RunDependencyChecks.Dispatch(context, checks);
		}

		var payload = new
		{
			status = checks.Ok ? "ok" : "unhealthy",
			timestampUtc = now,
			node = new
			{
				machineName = Environment.MachineName,
				processId = Environment.ProcessId,
				environment = Services.Environment,
				version = typeof(WebServerStartupInfo).Assembly.GetName().Version?.ToString(),
				uptimeSeconds = (long)Math.Floor(uptime.TotalSeconds)
			},
			checks
		};

		var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(payload));

		httpContext.Response.StatusCode = checks.Ok ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
		httpContext.Response.ContentType = "text/json";
		httpContext.Response.ContentLength = body.Length;
		await httpContext.Response.Body.WriteAsync(body, 0, body.Length);
	}

}
