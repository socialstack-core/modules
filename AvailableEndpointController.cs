using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		public ApiStructure Get()
        {
			// Get the content types and their IDs:
			var cTypes = new List<ContentType>();

			foreach (var kvp in Database.ContentTypes.TypeMap)
			{
				cTypes.Add(new ContentType()
				{
					Id = Database.ContentTypes.GetId(kvp.Key),
					Name = kvp.Value.Name
				});
			}

			// The result object:
			var structure = new ApiStructure()
			{
				Endpoints = _availableEndpoints.List(),
				ContentTypes = cTypes
			};

            return structure;
        }
		
    }

}
