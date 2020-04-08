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

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
/// <typeparam name="T"></typeparam>
[ApiController]
public partial class AutoController<T> : ControllerBase
	where T : Api.Database.DatabaseRow, new()
{

	/// <summary>
	/// The underlying autoservice used by this controller.
	/// </summary>
	protected AutoService<T> _service;

    /// <summary>
    /// Instanced automatically.
    /// </summary>
    public AutoController()
    {
        // Find the service:
        if (Api.Startup.Services.AutoServices.TryGetValue(typeof(AutoService<T>), out AutoService svc))
		{
			_service = (AutoService<T>)svc;
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
	/// GET /v1/entityTypeName/2/
	/// Returns the data for 1 entity.
	/// </summary>
	[HttpGet("{id}")]
	public virtual async Task<object> Load([FromRoute] int id)
	{
		var context = Request.GetContext();

        try
        {
            var result = await _service.Get(context, id);
            return await _service.EventGroup.Load.Dispatch(context, result, Response);
        }
        catch (PermissionException ex)
        {
            await Events.Logging.Dispatch(context, v1: new Logging() { LogLevel = LOG_LEVEL.Information, Message = $"Access Denied: {ex}" });
            Response.StatusCode = 403;
            return new ErrorResponse() { Message = "Access Denied" };
        }
    }

    /// <summary>
    /// DELETE /v1/entityTypeName/2/
    /// Deletes an entity
    /// </summary>
    [HttpDelete("{id}")]
    public virtual async Task<object> Delete([FromRoute] int id)
    {
        var context = Request.GetContext();
        try
        {
            var result = await _service.Get(context, id);
            result = await _service.EventGroup.Delete.Dispatch(context, result, Response);

			// Future todo - move this event inside the service:
			result = await _service.EventGroup.BeforeDelete.Dispatch(context, result);
			
            if (result == null || !await _service.Delete(context, id))
            {
                // The handlers have blocked this one from happening, or it failed
                return null;
            }

			// Future todo - move this event inside the service:
			result = await _service.EventGroup.AfterDelete.Dispatch(context, result);
			
            result = await _service.EventGroup.Deleted.Dispatch(context, result, Response);
            return result;
        }
        catch (PermissionException ex)
        {
            await Events.Logging.Dispatch(context, v1: new Logging() {LogLevel = LOG_LEVEL.Information, Message = $"Access Denied: {ex}"});
            Response.StatusCode = 403;
            return new ErrorResponse() {Message = "Access Denied"};
        }
    }

    /// <summary>
	/// GET /v1/entityTypeName/list
	/// Lists all entities of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list")]
	public virtual async Task<object> List()
	{
		return await List(null);
	}

	/// <summary>
	/// POST /v1/entityTypeName/list
	/// Lists filtered entities available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("list")]
	public virtual async Task<object> List([FromBody] JObject filters)
	{
        var context = Request.GetContext();
        try
        {
		    var filter = new Filter<T>(filters);

		    filter = await _service.EventGroup.List.Dispatch(context, filter, Response);

		    if (filter == null)
		    {
			    // A handler rejected this request.
			    return null;
		    }

		    var results = await _service.List(context, filter);
		    
		    return  new Set<object>() { Results = results.ConvertAll(c => (object)c) };
        }
        catch (PermissionException ex)
        {
            await Events.Logging.Dispatch(context, v1: new Logging() { LogLevel = LOG_LEVEL.Information, Message = $"Access Denied: {ex}" });
            Response.StatusCode = 403;
            return new ErrorResponse() { Message = "Access Denied" };
        }

    }

    /// <summary>
    /// POST /v1/entityTypeName/
    /// Creates a new entity. Returns the ID.
    /// </summary>
    [HttpPost]
	public virtual async Task<object> Create([FromBody] JObject body)
	{
		var context = Request.GetContext();

		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = new T();

		// If it's revisionable we'll set the user ID now:
		var revisionableEntity = (entity as Api.Users.RevisionRow);

		if (revisionableEntity != null)
		{
			revisionableEntity.UserId = context.UserId;
		}
		
		// Set the actual fields now:
		var notes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Default);

        try
        {
            // Fire off a create event:
            entity = await _service.EventGroup.Create.Dispatch(context, entity, Response) as T;

            if (entity == null)
            {
                // A handler rejected this request.
                if (notes != null)
                {
                    Request.Headers["Api-Notes"] = notes;
                }

                return null;
            }

            entity = await _service.Create(context, entity, async (Context c, T ent) => {

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
			
			});

            if (entity == null)
            {
                // It was blocked or went wrong, typically because of a bad request.
                Response.StatusCode = 400;

                if (notes != null)
                {
                    Request.Headers["Api-Notes"] = notes;
                }

                return null;
            }
			
            if (notes != null)
            {
                Request.Headers["Api-Notes"] = notes;
            }

            // Fire off after create evt:
            entity = await _service.EventGroup.Created.Dispatch(context, entity, Response) as T;

            return entity;
        }
        catch (PermissionException ex)
        {
            await Events.Logging.Dispatch(context, v1: new Logging() {LogLevel = LOG_LEVEL.Information, Message = $"Access Denied: {ex}" });
            Response.StatusCode = 403;
            return new ErrorResponse() {Message = "Access Denied"};
        }
        
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
	private async Task<string> SetFieldsOnObject(T target, Context context, JObject body, JsonFieldGroup fieldGroup = JsonFieldGroup.Any)
	{
        // Get the JSON meta which will indicate exactly which fields are editable by this user (role):
		var availableFields = await _service.GetTypedJsonStructure(context.RoleId);

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
			await field.SetValue(context, target, property.Value);
		}

		return notes;
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/1/
	/// Updates an entity with the given ID.
	/// </summary>
	[HttpPost("{id}")]
	public virtual async Task<object> Update([FromRoute] int id, [FromBody] JObject body)
	{
		var context = Request.GetContext();

        try
        {
            var entity = await _service.Get(context, id);

            if (entity == null)
            {
                // Either not allowed to edit this, or it doesn't exist.
                // Both situations are a 404.
                Response.StatusCode = 404;
                return null;
            }

            // In this case the entity ID is definitely known, so we can run all fields at the same time:
            var notes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Any);

            if (notes != null)
            {
                Request.Headers["Api-Notes"] = notes;
            }

            // Run the request update event:
            entity = await _service.EventGroup.Update.Dispatch(context, entity, Response) as T;

            if (entity == null)
            {
                // A handler rejected this request.
                return null;
            }

            entity = await _service.Update(context, entity);

            if (entity == null)
            {
                // It was blocked or went wrong, typically because of a bad request.
                Response.StatusCode = 400;
                return null;
            }

            // Run the request updated event:
            entity = await _service.EventGroup.Updated.Dispatch(context, entity, Response) as T;

            return entity;
        }
        catch (PermissionException ex)
        {
            await Events.Logging.Dispatch(context, v1: new Logging() {LogLevel = LOG_LEVEL.Information, Message = $"Access Denied: {ex}"});
            Response.StatusCode = 403;
            return new ErrorResponse() {Message = "Access Denied"};
        }
    }

}
