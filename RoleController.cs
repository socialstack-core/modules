using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Results;
using Api.Eventing;
using Api.Database;
using System.Linq;

namespace Api.Permissions
{
	/// <summary>
	/// Handles an endpoint which describes available endpoints. It's at the root of the API.
	/// </summary>

	[Route("v1/role")]
	[ApiController]
	public partial class AvailableEndpointController : ControllerBase
    {
		/// <summary>
		/// GET /v1/list
		/// Returns meta about what's available from this API. Includes endpoints and content types.
		/// </summary>
		[HttpGet("list")]
		public ListWithTotal<Role> List()
        {
			var roles = Roles.All;
			
			return new ListWithTotal<Role>()
			{
				Results = roles.ToList(),
				Total = roles.Length
			};
			
        }
		
    }

}
