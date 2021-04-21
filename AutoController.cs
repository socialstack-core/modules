using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;
using Api.Startup;
using Microsoft.Extensions.Logging;
using Api.Database;
using Api.Views;

public partial class AutoController<T>
{

	/// <summary>
	/// GET /v1/entityTypeName/2/viewed/
	/// </summary>
	[HttpGet("{id}/viewed")]
	public virtual async Task<object> MarkViewed([FromRoute] uint id)
	{
		// Get the ctx:
		var context = await Request.GetContext();
		
        try
        {
            
			// Just going to directly create (or update) the viewed row.
			var contentTypeId = ContentTypes.GetId(typeof(T));
			
			await Services.Get<ViewService>().MarkViewed(context, contentTypeId, id);
			
			return new {
				success = true
			};
        }
        catch (PermissionException ex)
        {
            await Events.Logging.Dispatch(context, v1: new Logging() { LogLevel = LOG_LEVEL.Information, Message = $"Access Denied: {ex}" });
            Response.StatusCode = 403;
            return new ErrorResponse() { Message = "Access Denied" };
        }
    }
	
}
