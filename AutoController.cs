using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Results;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class AutoController<T> : AutoController<T, uint>
	where T : Content<uint>, new()
{
}

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
[ApiController]
public partial class AutoController<T,ID> : ControllerBase
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>
{

	/// <summary>
	/// The underlying autoservice used by this controller.
	/// </summary>
	protected AutoService<T, ID> _service;

    /// <summary>
    /// Instanced automatically.
    /// </summary>
    public AutoController()
    {
        // Find the service:
        if (Api.Startup.Services.AutoServices.TryGetValue(typeof(AutoService<T, ID>), out AutoService svc))
		{
			_service = (AutoService<T, ID>)svc;
		}
		else
		{
			throw new Exception(
				"Unable to use AutoController for type " + typeof(T).Name + " as it doesn't have an AutoService. " +
				"You must also declare an :AutoService<" + typeof(T).Name + "> as it'll use that for the underlying functionality."
			);
		}

	}

	/// <summary>
	/// Json header
	/// </summary>
	private readonly static string _applicationJson = "application/json";

	/// <summary>
	/// Outputs the given content object whilst considering the field visibility rules of the role in the context.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="content"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	protected async ValueTask OutputJson(Context context, T content, string includes)
	{
		if (content == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}
			return;
		}

		Response.ContentType = _applicationJson;
		await _service.ToJson(context, content, Response.Body, includes);
	}

	/// <summary>
	/// Outputs the given content object list whilst considering the field visibility rules of the role in the context.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="list"></param>
	/// <param name="includes"></param>
	/// <returns></returns>
	protected async ValueTask OutputJson(Context context, ListWithTotal<T> list, string includes)
	{
		if (list == null)
		{
			return;
		}

		Response.ContentType = _applicationJson;
		await _service.ToJson(context, list, Response.Body, includes);
	}

	/// <summary>
	/// Outputs a context update.
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	protected async ValueTask OutputContext(Context context)
	{
		// Regenerate the contextual token:
		context.SendToken(Response);

		Response.ContentType = _applicationJson;
		await Services.Get<ContextService>().ToJson(context, Response.Body);
	}

	/// <summary>
	/// GET /v1/entityTypeName/2/
	/// Returns the data for 1 entity.
	/// </summary>
	[HttpGet("{id}")]
	public virtual async ValueTask Load([FromRoute] ID id, [FromQuery] string includes = null)
	{
		var context = await Request.GetContext();

		id = await _service.EventGroup.EndpointStartLoad.Dispatch(context, id, Response);
		
		var result = await _service.Get(context, id);
		result = await _service.EventGroup.EndpointEndLoad.Dispatch(context, result, Response);

		await OutputJson(context, result, includes);
    }

	/// <summary>
	/// DELETE /v1/entityTypeName/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("{id}")]
    public virtual async ValueTask Delete([FromRoute] ID id, [FromQuery] string includes = null)
	{
		var context = await Request.GetContext();
		var result = await _service.Get(context, id);
		result = await _service.EventGroup.EndpointStartDelete.Dispatch(context, result, Response);

		if (result == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}
			return;
		}

		if (result == null || !await _service.Delete(context, result))
		{
			// The handlers have blocked this one from happening, or it failed
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}
			return;
		}

		result = await _service.EventGroup.EndpointEndDelete.Dispatch(context, result, Response);
		await OutputJson(context, result, includes);
	}

    /// <summary>
	/// GET /v1/entityTypeName/list
	/// Lists all entities of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list")]
	public virtual async ValueTask List([FromQuery] string includes = null)
	{
		await List(null, includes);
	}

	/// <summary>
	/// POST /v1/entityTypeName/list
	/// Lists filtered entities available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("list")]
	public virtual async ValueTask List([FromBody] JObject filters, [FromQuery] string includes = null)
	{
		var context = await Request.GetContext();
		var filter = new Filter<T>(filters);

		filter = await _service.EventGroup.EndpointStartList.Dispatch(context, filter, Response);

		if (filter == null)
		{
			// A handler rejected this request.
			Response.StatusCode = 404;
			return;
		}

		ListWithTotal<T> response;

		if (filter.PageSize != 0 && filters != null && filters["includeTotal"] != null)
		{
			// Get the total number of non-paginated results as well:
			response = await _service.ListWithTotal(context, filter);
		}
		else
		{
			// Not paginated or requestor doesn't care about the total.
			var results = await _service.List(context, filter);

			response = new ListWithTotal<T>()
			{
				Results = results
			};

			if (filter.PageSize == 0)
			{
				// Trivial instance - pagination is off so the total is just the result set length.
				response.Total = results == null ? 0 : results.Count;
			}
		}

		response.Results = await _service.EventGroup.EndpointEndList.Dispatch(context, response.Results, Response);

		await OutputJson(context, response, includes);
	}

    /// <summary>
    /// POST /v1/entityTypeName/
    /// Creates a new entity. Returns the ID. Includes everything by default.
    /// </summary>
    [HttpPost]
	public virtual async ValueTask Create([FromBody] JObject body)
	{
		var context = await Request.GetContext();

		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = new T();

		// If it's revisionable we'll set the user ID now:
		var revisionableEntity = (entity as Api.Users.VersionedContent<ID>);

		if (revisionableEntity != null)
		{
			revisionableEntity.UserId = context.UserId;
		}
		
		// Set the actual fields now:
		var notes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Default);

		// Not permitted to create with a specified ID via the API. Ensure it's 0:
		entity.SetId(default);

		// Fire off a create event:
		entity = await _service.EventGroup.EndpointStartCreate.Dispatch(context, entity, Response) as T;

		if (entity == null)
		{
			// A handler rejected this request.
			if (notes != null)
			{
				Request.Headers["Api-Notes"] = notes;
			}

			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		entity = await _service.CreatePartial(context, entity, DataOptions.Default);
		
		if(entity == null)
		{
			// A handler rejected this request.
			if (notes != null)
			{
				Request.Headers["Api-Notes"] = notes;
			}

			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}
		
		// Set post ID fields:
		var secondaryNotes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.AfterId);

		if (secondaryNotes != null)
		{
			if (notes == null)
			{
				notes = secondaryNotes;
			}
			else
			{
				notes += ", " + secondaryNotes;
			}

		}

		// Complete the call (runs AfterCreate):
		entity = await _service.CreatePartialComplete(context, entity);

		if (entity == null)
		{
			// It was blocked or went wrong, typically because of a bad request.
			Response.StatusCode = 400;

			if (notes != null)
			{
				Request.Headers["Api-Notes"] = notes;
			}

			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}
		
		if (notes != null)
		{
			Request.Headers["Api-Notes"] = notes;
		}

		// Fire off after create evt:
		entity = await _service.EventGroup.EndpointEndCreate.Dispatch(context, entity, Response);

		await OutputJson(context, entity, "*");
	}

	/// <summary>
	/// Sets the fields from the given JSON object on the given target object, based on the user role in the context.
	/// Note that there's 2 sets of fields - a primary set, then also a secondary set which are set only after the ID of the object is known.
	/// E.g. during create, the object is instanced, initial fields are set, it's then actually created, and then the after ID set is run.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="context"></param>
	/// <param name="body"></param>
	/// <param name="fieldGroup"></param>
	protected async ValueTask<string> SetFieldsOnObject(T target, Context context, JObject body, JsonFieldGroup fieldGroup = JsonFieldGroup.Any)
	{
        // Get the JSON meta which will indicate exactly which fields are editable by this user (role):
		var availableFields = await _service.GetTypedJsonStructure(context);

		string notes = null;

		foreach (var property in body.Properties())
		{
			// Attempt to get the available field:
			var field = availableFields.GetField(property.Name, fieldGroup);

			if (field == null)
			{
				// Tell the callee that this field was ignored.
				if (notes != null)
				{
					notes += ", " + property.Name + " was ignored (doesn't exist or no permission)";
				}
				else
				{
					notes = property.Name + " was ignored (doesn't exist or no permission)";
				}

				continue;
			}

			// Try setting the value now:
			if (await field.SetValueIfChanged(context, target, property.Value))
			{
				target.MarkChanged(field.ChangeFlag);
			}
		}

		return notes;
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/1/
	/// Updates an entity with the given ID. Includes everything by default.
	/// </summary>
	[HttpPost("{id}")]
	public virtual async ValueTask Update([FromRoute] ID id, [FromBody] JObject body)
	{
		var context = await Request.GetContext();
		
		var entity = await _service.Get(context, id);
		
		if (entity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}
		
		// Run the request update event (using the original object to be updated):
		entity = await _service.EventGroup.EndpointStartUpdate.Dispatch(context, entity, Response) as T;

		if (entity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		if (!await _service.StartUpdate(context, entity))
		{
			// Can't start update (no permission, typically).
			return;
		}

		// In this case the entity ID is definitely known, so we can run all fields at the same time:
		var notes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Any);

		if (notes != null)
		{
			Request.Headers["Api-Notes"] = notes;
		}

		// Make sure it's the original ID:
		entity.SetId(id);

		if (entity == null)
		{
			// A handler rejected this request.
			return;
		}

		entity = await _service.FinishUpdate(context, entity);

		if (entity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}
		
		// Run the request updated event:
		entity = await _service.EventGroup.EndpointEndUpdate.Dispatch(context, entity, Response) as T;
		await OutputJson(context, entity, "*");
	}

}
