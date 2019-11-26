using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;


namespace Api.AutoForms
{
	/// <summary>
	/// Handles an endpoint which describes available endpoints. It's at the root of the API.
	/// </summary>

	[Route("v1/autoform")]
	[ApiController]
	public partial class AutoFormController : ControllerBase
    {
        private IAutoFormService _autoForms;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AutoFormController(
			IAutoFormService autoForms
		)
        {
			_autoForms = autoForms;
        }
		
		/// <summary>
		/// GET /v1/autoform
		/// Returns meta about all autoforms in this API.
		/// </summary>
		[HttpGet]
		public AutoFormStructure Get()
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
			var structure = new AutoFormStructure()
			{
				Forms = _autoForms.List(),
				ContentTypes = cTypes
			};

            return structure;
        }
		
    }

}
