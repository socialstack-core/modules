using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		
    }

}
