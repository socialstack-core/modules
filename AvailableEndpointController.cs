using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Results;
using Api.Eventing;


namespace Api.AvailableEndpoints
{
	/// <summary>
	/// Handles an endpoint which describes available endpoints. It's at the root of the API.
	/// </summary>

	[Route("v1")]
	[ApiController]
	public partial class AvailableEndpointController : ControllerBase
    {
        private IAvailableEndpointService _availableEndpoints;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AvailableEndpointController(
			IAvailableEndpointService availableEndpoints
		)
        {
			_availableEndpoints = availableEndpoints;
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
		
		/// <summary>
		/// GET /v1/apievents
		/// Returns meta about the events in this API
		/// </summary>
		[HttpGet("apievent/list")]
		public Set<EventMeta> EventList()
        {
			// Get the event names:
			var eventTypes = new List<EventMeta>();
			
			foreach (var kvp in Events.All)
			{
				eventTypes.Add(new EventMeta()
				{
					Name = kvp.Value.Name
				});
			}
			
            return new Set<EventMeta>(){
				Results = eventTypes
			};
        }
    }

}
