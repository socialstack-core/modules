using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;


namespace Api.AvailableEndpoints
{
	/// <summary>
	/// Handles an endpoint which describes available endpoints. It's at the root of the API.
	/// </summary>

	[Route("v1")]
	[ApiController]
	public partial class AvailableEndpointController : ControllerBase
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
		public async ValueTask Uptime()
		{
			if (_upTime == null)
			{
				_upTime = System.Text.Encoding.UTF8.GetBytes("{\"since\": {\"utcTicks\": " + _startTime.Ticks + ", \"utc\": \"" + _startTime.ToString("o") + "\"}}");
			}

			Response.ContentType = _applicationJson;
			await Response.Body.WriteAsync(_upTime);
			await Response.Body.FlushAsync();
		}
		
		/// <summary>
		/// GET /v1/
		/// Returns meta about what's available from this API. Includes endpoints and content types.
		/// </summary>
		[HttpGet]
		public async ValueTask<ApiStructure> Get()
        {
			var context = await Request.GetContext();
			
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("api_home", context);
			}
			
			return await _availableEndpoints.GetStructure(context);
        }
		
    }

}
