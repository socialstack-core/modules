using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Translate
{
    /// <summary>
    /// Handles locale endpoints.
    /// </summary>

    [Route("v1/locale")]
	public partial class LocaleController : AutoController<Locale>
	{
		
		/// <summary>
		/// GET /v1/locale/set/2/
		/// Sets locale by its ID.
		/// </summary>
		[HttpGet("set/{id}")]
		public virtual async ValueTask<object> Set([FromRoute] uint id)
		{
			var context = Request.GetContext();
			
			// Set locale ID:
			context.LocaleId = id;
			
			// Regenerate the contextual token:
			context.SendToken(Response);
			
			// Note: Can also set cookie called "Locale", however, using the context will 
			// allow the locale info to be available to the frontend as well.
			
			return await context.GetPublicContext();
		}

    }

}
