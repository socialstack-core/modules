#nullable enable
using System;
using System.Diagnostics;
using Api.Contexts;
using Api.Database;
using Api.Startup;
using Api.Startup.Routing;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Eventing;

namespace Api.Startup.Health;

/// <summary>
/// Handles healthz and readyz endpoints for container health checks.
/// </summary>
[Route("healthz")]
public partial class HealthzController : AutoController
{
	/// <summary>
	/// Liveness check endpoint. Returns health status information.
	/// Throws PublicException with 503 status if unhealthy.
	/// </summary>
	[HttpGet]
	public async ValueTask<object> Healthz(Context context)
	{
		var now = DateTime.UtcNow;
		var process = Process.GetCurrentProcess();
		var uptime = now - process.StartTime.ToUniversalTime();
		var routerReady = Router.CurrentRouter != null;
		
		// Build response payload
		var checks = new HealthzChecks()
		{
			Ok = routerReady,
			RouterReady = routerReady
		};

		checks = await Events.Healthz.RunChecks.Dispatch(context, checks);

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

		// Throw PublicException with 503 if unhealthy
		if (!checks.Ok)
		{
			throw new PublicException("Service unhealthy", "healthz/service_unhealthy", 503);
		}

		return payload;
	}
}
