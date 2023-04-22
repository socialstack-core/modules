using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
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
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
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
	/// Outputs the given content object set whilst considering the field visibility rules of the role in the context.
	/// To avoid an IEnumerable allocation, also consider using the non-alloc mechanism inside this function directly on high traffic usage.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="content"></param>
	/// <param name="includes"></param>
	/// <param name="withTotal"></param>
	/// <returns></returns>
	protected async ValueTask OutputJson(Context context, IEnumerable<T> content, string includes, bool withTotal = false)
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

		var writer = Writer.GetPooled();
		writer.Start(null);
		await _service.ToJson(context, content, async (Context context, IEnumerable<T> data, Func<T, int, ValueTask> onResult) => {
			int i = 0;

			foreach (var entry in data)
			{
				await onResult(entry, i++);
			}

			return i;
		}, writer, Response.Body, includes, withTotal);
		writer.Release();
	}

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

		var writer = Writer.GetPooled();
		writer.Start(null);
		await _service.ToJson(context, content, writer, Response.Body, includes);
		writer.Release();
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
	/// GET /v1/entityTypeName/recache
	/// Repopulates the cache for this service (if it is cached, and if you are an admin).
	/// </summary>
	/// <returns></returns>
	[HttpGet("recache")]
	public virtual async ValueTask Recache()
	{
		var context = await Request.GetContext();

		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("recache", context);
		}


		await _service.Recache();
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

		var filter = _service.LoadFilter(filters) as Filter<T, ID>;
		filter = await _service.EventGroup.EndpointStartList.Dispatch(context, filter, Response);

		if (filter == null)
		{
			// A handler rejected this request.
			Response.StatusCode = 404;
			return;
		}

		Response.ContentType = _applicationJson;
		var writer = Writer.GetPooled();
		writer.Start(null);
		await _service.ToJson(context, filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {

			return await _service.GetResults(ctx, filt, async (Context ctx2, T result, int index, object src, object srcB) => {

				var _onResult = src as Func<T, int, ValueTask>;
				result = await _service.EventGroup.EndpointListEntry.Dispatch(context, result, Response);
				await _onResult(result, index);
			}, onResult, null);

		}, writer, Response.Body, includes, filter.IncludeTotal);
		writer.Release();

		filter = await _service.EventGroup.EndpointEndList.Dispatch(context, filter, Response);
		filter.Release();
	}

    /// <summary>
    /// POST /v1/entityTypeName/
    /// Creates a new entity. Returns the ID. Includes everything by default.
    /// </summary>
    [HttpPost]
	public virtual async ValueTask Create([FromBody] JObject body, [FromQuery] string includes = null)
	{
		var context = await Request.GetContext();

		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = new T();

		// If it's user created we'll set the user ID now:
		var userCreated = (entity as Api.Users.UserCreatedContent<ID>);

		if (userCreated != null)
		{
			userCreated.UserId = context.UserId;
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

		// If it has an on object, create the mapping entry now if we have read visibility of the target:
		var on = body["on"];

		if (on != null && on.Type == JTokenType.Object)
		{
			// Get relevant fields:
			var type = on["type"];
			var id = on["id"];
			var map = on["map"];

			// If map is null, we'll use the primary map. First though, attempt to get the actual content type:
			var contentType = ContentTypes.GetType(type.Value<string>());

			if (contentType != null)
			{
				var svc = Services.GetByContentType(contentType);

				if (svc != null)
				{
					var srcObject = await svc.GetObject(context, "Id", id.Value<string>());

					if (srcObject != null)
					{
						// Mapping permitted.
						string mapName;

						if (map == null)
						{
							// "this" service is the one which has a ListAs:
							mapName = _service.GetContentFields().PrimaryMapName;

							if (string.IsNullOrEmpty(mapName))
							{
								throw new PublicException(
									"This type '" + typeof(T).Name + "' doesn't have a primary map name so you'll need to specify a particular map: in your on:{}.",
									"no_map"
								);
							}
						}
						else
						{
							mapName = map.Value<string>();

							if (!ContentFields.GlobalVirtualFields.ContainsKey(mapName.ToLower()))
							{
								throw new PublicException(
									"A map called '" + mapName + "' doesn't exist.",
									"no_map"
								);
							}
						}

						// Create map from srcObject -> entity via the map called MapName. First though, get the mapping service:
						var mappingService = await MappingTypeEngine.GetOrGenerate(svc, _service, mapName);
						await mappingService.CreateMapping(context, srcObject, entity, DataOptions.IgnorePermissions);
					}
				}
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

		await OutputJson(context, entity, includes == null ? "*" : includes);
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
			if (property.Name == "on")
			{
				continue;
			}

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
			await field.SetFieldValue(context, target, property.Value);
		}

		return notes;
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/1/
	/// Updates an entity with the given ID. Includes everything by default.
	/// </summary>
	[HttpPost("{id}")]
	public virtual async ValueTask Update([FromRoute] ID id, [FromBody] JObject body, [FromQuery] string includes = null)
	{
		var context = await Request.GetContext();
		
		var originalEntity = await _service.Get(context, id);
		
		if (originalEntity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		// Run the request update event (using the original object to be updated):
		originalEntity = await _service.EventGroup.EndpointStartUpdate.Dispatch(context, originalEntity, Response) as T;

		if (originalEntity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		var entityToUpdate = await _service.StartUpdate(context, originalEntity);

		if (entityToUpdate == null)
		{
			// Can't start update (no permission, typically - it throws in that scenario).
			return;
		}

		// In this case the entity ID is definitely known, so we can run all fields at the same time:
		var notes = await SetFieldsOnObject(entityToUpdate, context, body, JsonFieldGroup.Any);

		if (notes != null)
		{
			Request.Headers["Api-Notes"] = notes;
		}

		// Make sure it's still the original ID:
		entityToUpdate.SetId(id);

		entityToUpdate = await _service.FinishUpdate(context, entityToUpdate, originalEntity);

		if (entityToUpdate == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		// Run the request updated event:
		entityToUpdate = await _service.EventGroup.EndpointEndUpdate.Dispatch(context, entityToUpdate, Response) as T;
		await OutputJson(context, entityToUpdate, includes == null ? "*" : includes);
	}

}
