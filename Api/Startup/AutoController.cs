using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

/// <summary>
/// The base of all controllers. Don't use ASP.NET controllers as they will not run!
/// This is because the serverside renderer and the websocket engine mount your 
/// controller methods directly, and thus no actual request happens. This also is used to enforce  
/// field visibility rules always as your controller methods can safely just return content objects.
/// </summary>
[JsonTypeName("AutoControllerBase")]
public class AutoController
{

	/// <summary>
	/// Outputs a context update.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	protected async ValueTask OutputContext(HttpContext httpContext, Context context)
	{
		var response = httpContext.Response;

		// Regenerate the contextual token:
		context.SendToken(response);

		response.ContentType = "application/json";
		await Services.Get<ContextService>().ToJson(context, response.Body);
	}

}

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
/// <typeparam name="T"></typeparam>
[JsonTypeName("AutoControllerInt")]
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
public partial class AutoController<T,ID> : AutoController
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
	/// GET /v1/entityTypeName/2/
	/// Returns the data for 1 entity.
	/// </summary>
	[HttpGet("{id}")]
	public virtual async ValueTask<T> Load(Context context, [FromRoute] ID id)
	{
		var result = await _service.Get(context, id);
		return result;
	}

	/// <summary>
	/// DELETE /v1/entityTypeName/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("{id}")]
	public virtual async ValueTask<T> Delete(Context context, [FromRoute] ID id)
	{
		var result = await _service.Get(context, id);

		if (result == null || !await _service.Delete(context, result))
		{
			// The handlers have blocked this one from happening, or it failed
			return null;
		}

		return result;
	}

	/// <summary>
	/// GET /v1/entityTypeName/cache/invalidate/{id}
	/// Repopulates the referenced item for this service (if it is cached, and if you are an admin).
	/// </summary>
	/// <returns></returns>
	[HttpGet("cache/invalidate/{id}")]
	public virtual async ValueTask InvalidateCachedItem(Context context, [FromRoute] ID id)
	{
		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("cache/invalidate", context);
		}

		await _service.InvalidateCachedItem(id);
	}
	
	/// <summary>
	/// GET /v1/entityTypeName/cache/invalidate
	/// Repopulates the cache for this service (if it is cached, and if you are an admin).
	/// </summary>
	/// <returns></returns>
	[HttpGet("cache/invalidate")]
	public virtual async ValueTask InvalidateCache(Context context)
	{
		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("cache/invalidate", context);
		}

		await _service.InvalidateCache();
	}

	/// <summary>
	/// GET /v1/entityTypeName/list
	/// Lists all entities of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list")]
	public virtual ValueTask<ContentStream<T, ID>?> ListAll(Context context)
	{
		return List(context, null);
	}

	/// <summary>
	/// POST /v1/entityTypeName/list
	/// Lists filtered entities available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("list")]
	public virtual ValueTask<ContentStream<T, ID>?> List(Context context, [FromBody] ListFilter filters)
	{
		var filter = _service.LoadFilter(filters) as Filter<T, ID>;
		
		if (filter == null)
		{
			// A handler rejected this request.
			return new ValueTask<ContentStream<T, ID>?>((ContentStream<T, ID>?)null);
		}

		var streamer = _service.GetResults(filter);
		return new ValueTask<ContentStream<T, ID>?>(streamer);
	}

	/// <summary>
	/// POST /v1/entityTypeName/
	/// Creates a new entity. Returns the ID. Includes everything by default.
	/// </summary>
	[HttpPost]
	[Receives(typeof(PartialContent))]
	public virtual async ValueTask<T> Create(Context context, [FromBody] JObject body)
	{
		return await CreateInternal(_service, context, body);
	}

	/// <summary>
	/// Creates a new entity using the given service, such that revisions can reuse this code directly. 
	/// Returns the ID. Includes everything by default.
	/// </summary>
	/// <param name="service"></param>
	/// <param name="context"></param>
	/// <param name="body"></param>
	/// <param name="setFields"></param>
	/// <returns></returns>
	/// <exception cref="PublicException"></exception>
	protected virtual async ValueTask<T> CreateInternal(AutoService<T, ID> service, Context context, JObject body, Action<Context, T> setFields = null)
	{
		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = (T)Activator.CreateInstance(service.InstanceType);

		// If it's user created we'll set the user ID now:
		var userCreated = (entity as Api.Users.UserCreatedContent<ID>);

		if (userCreated != null)
		{
			userCreated.UserId = context.UserId;
		}
		
		// Set the fields now:
		await service.SetFieldsOnObject(entity, context, body);

		// Set any additional fields if necessary:
		if (setFields != null)
		{
			setFields(context, entity);
		}

		// Not permitted to create with a specified ID via the API. Ensure it's 0:
		entity.SetId(default);

		entity = await service.CreatePartial(context, entity, DataOptions.Default);
		
		if(entity == null)
		{
			return null;
		}
		
		// Complete the call (runs AfterCreate):
		entity = await service.CreatePartialComplete(context, entity);

		if (entity == null)
		{
			return null;
		}

		return entity;
	}

	/// <summary>
	/// POST /v1/entityTypeName/1/
	/// Updates an entity with the given ID. Includes everything by default.
	/// </summary>
	[HttpPost("{id}")]
	[Receives(typeof(PartialContent))]
	public virtual async ValueTask<T> Update(Context context, [FromRoute] ID id, [FromBody] JObject body)
	{
		return await UpdateInternal(_service, context, id, body);
	}

	/// <summary>
	/// Updates an object using a specific service, such that e.g. 
	/// revisions can use a different one whilst reusing the bulk of this functionality.
	/// </summary>
	/// <param name="service"></param>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="body"></param>
	/// <returns></returns>
	protected async ValueTask<T> UpdateInternal(AutoService<T, ID> service, Context context, ID id, JObject body)
	{
		var originalEntity = await service.Get(context, id);

		if (originalEntity == null)
		{
			return null;
		}

		return await UpdateInternal(service, context, id, body, originalEntity, DataOptions.Default);
	}

	/// <summary>
	/// Updates an object using a specific service, such that e.g. 
	/// revisions can use a different one whilst reusing the bulk of this functionality.
	/// In this overload you can pre-provide the original entity from a load call and also specify options on the update itself.
	/// </summary>
	/// <param name="service"></param>
	/// <param name="context"></param>
	/// <param name="id"></param>
	/// <param name="body"></param>
	/// <param name="originalEntity"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	protected async ValueTask<T> UpdateInternal(AutoService<T, ID> service, Context context, ID id, JObject body, T originalEntity, DataOptions options)
	{
		var entityToUpdate = service.StartUpdate(context, originalEntity, options);

		if (entityToUpdate == null)
		{
			// Can't start update (no permission, typically - it throws in that scenario).
			return null;
		}

		// Set all the fields:
		await service.SetFieldsOnObject(entityToUpdate, context, body);

		// Make sure it's still the original ID:
		entityToUpdate.SetId(id);

		entityToUpdate = await service.FinishUpdate(context, entityToUpdate, originalEntity, options);

		return entityToUpdate;
	}

}
