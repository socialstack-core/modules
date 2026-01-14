using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Api.AvailableEndpoints
{
	/// <summary>
	/// Handles an endpoint which describes available endpoints. It's at the root of the API.
	/// </summary>

	[Route("v1")]
	public partial class AvailableEndpointController : AutoController
    {
        private AvailableEndpointService _availableEndpoints;

		private DateTime _startTime;
		private byte[] _upTime;
		
		/// <summary>
		/// Json header
		/// </summary>
		private readonly static string _applicationJson = "application/json";

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AvailableEndpointController(
			AvailableEndpointService availableEndpoints
		)
        {
			_startTime = DateTime.UtcNow;
			_availableEndpoints = availableEndpoints;
        }
		
		/// <summary>
		/// Gets the time (in both ticks and as a timestamp) that the service last started at.
		/// </summary>
		[HttpGet("uptime")]
		[Returns(typeof(UptimeResult))]
		public async ValueTask Uptime(HttpContext httpContext)
		{
			if (_upTime == null)
			{
				_upTime = System.Text.Encoding.UTF8.GetBytes("{\"since\": {\"utcTicks\": " + _startTime.Ticks + ", \"utc\": \"" + _startTime.ToString("o") + "\"}}");
			}

			var response = httpContext.Response;
			response.ContentType = _applicationJson;
			await response.Body.WriteAsync(_upTime);
			await response.Body.FlushAsync();
		}
		
		/// <summary>
		/// GET /v1/
		/// Returns meta about what's available from this API. Includes endpoints and content types.
		/// </summary>
		[HttpGet]
		public ApiStructure Get(Context context)
        {
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("api_home", context);
			}
			
			return _availableEndpoints.GetStructure(context);
        }
		
    }

	/// <summary>
	/// Result from the uptime endpoint.
	/// </summary>
	public struct UptimeResult {
		/// <summary>
		/// System has been up since the given date.
		/// </summary>
		public UptimeSince Since;
	}

	/// <summary>
	/// System uptime since.
	/// </summary>
	public struct UptimeSince {
		/// <summary>
		/// Utc ticks (C#).
		/// </summary>
		public long UtcTicks;

		/// <summary>
		/// Utc timestamp text.
		/// </summary>
		public string Utc;
	}

}
