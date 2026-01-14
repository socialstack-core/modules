using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;


namespace Api.AutoForms
{
	/// <summary>
	/// Handles an endpoint which describes available endpoints. It's at the root of the API.
	/// </summary>

	[Route("v1/autoform")]
	public partial class AutoFormController : AutoController
    {
        private AutoFormService _autoForms;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AutoFormController(
			AutoFormService autoForms
		)
        {
			_autoForms = autoForms;
        }

		/// <summary>
		/// Gets the autoform info for a particular form by type and name. Type is usually content, component or config.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		[HttpGet("{type}/{name}")]
		public async ValueTask<AutoFormInfo> Get(Context context, [FromRoute] string type, [FromRoute] string name)
		{
			return await _autoForms.Get(context, type, name);
		}

		/// <summary>
		/// GET /v1/autoform/all
		/// Returns meta about all content autoforms in this API.
		/// </summary>
		[HttpGet("all")]
		public async ValueTask<AutoFormStructure> AllContentForms(Context context)
        {
			// The result object:
			var set = await _autoForms.AllContentForms(context);

			var structure = new AutoFormStructure()
			{
				Forms = set == null ? null : set.Values,
				ContentTypes = _autoForms.AllContentTypes()
			};

            return structure;
        }
		
    }

}
