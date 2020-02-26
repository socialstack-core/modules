using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;


/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="U"></typeparam>
[ApiController]
public partial class AutoController<T, U> : ControllerBase
	where T : Api.Users.RevisionRow, new()
	where U : AutoForm<T>
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
		if (Api.Startup.Services.AutoServices.TryGetValue(typeof(AutoService<T>), out object svc))
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
	public virtual async Task<T> Load([FromRoute] int id)
	{
		var context = Request.GetContext();
		var result = await _service.Get(context, id);
		return await _service.EventGroup.Load.Dispatch(context, result, Response);
	}

	/// <summary>
	/// DELETE /v1/entityTypeName/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("{id}")]
	public virtual async Task<T> Delete([FromRoute] int id)
	{
		var context = Request.GetContext();
		var result = await _service.Get(context, id);
		result = await _service.EventGroup.Delete.Dispatch(context, result, Response);

		if (result == null || !await _service.Delete(context, id))
		{
			// The handlers have blocked this one from happening, or it failed
			return null;
		}

		return result;
	}

	/// <summary>
	/// GET /v1/entityTypeName/list
	/// Lists all entities of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list")]
	public virtual async Task<Set<T>> List()
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
	public virtual async Task<Set<T>> List([FromBody] JObject filters)
	{
		var context = Request.GetContext();
		var filter = new Filter<T>(filters);

		filter = await _service.EventGroup.List.Dispatch(context, filter, Response);

		if (filter == null)
		{
			// A handler rejected this request.
			return null;
		}

		var results = await _service.List(context, filter);
		return new Set<T>() { Results = results };
	}

	/// <summary>
	/// POST /v1/entityTypeName/
	/// Creates a new entity. Returns the ID.
	/// </summary>
	[HttpPost]
	public virtual async Task<T> Create([FromBody] U form)
	{
		var context = Request.GetContext();

		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = new T
		{
			UserId = context.UserId
		};

		if (!ModelState.Setup(form, entity))
		{
			return null;
		}

		form = await _service.EventGroup.Create.Dispatch(context, form, Response) as U;

		if (form == null || form.Result == null)
		{
			// A handler rejected this request.
			return null;
		}

		entity = await _service.Create(context, form.Result);

		if (entity == null)
		{
			Response.StatusCode = 500;
			return null;
		}

		return entity;
	}

	/// <summary>
	/// POST /v1/entityTypeName/1/
	/// Updates an entity with the given ID.
	/// </summary>
	[HttpPost("{id}")]
	public virtual async Task<T> Update([FromRoute] int id, [FromBody] U form)
	{
		var context = Request.GetContext();

		var entity = await _service.Get(context, id);

		if (!ModelState.Setup(form, entity))
		{
			return null;
		}

		form = await _service.EventGroup.Update.Dispatch(context, form, Response) as U;

		if (form == null || form.Result == null)
		{
			// A handler rejected this request.
			return null;
		}

		entity = await _service.Update(context, form.Result);

		if (entity == null)
		{
			Response.StatusCode = 500;
			return null;
		}

		return entity;
	}

}
