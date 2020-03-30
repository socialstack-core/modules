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
public partial class AutoController<T>
{
	/// <summary>
	/// GET /v1/entityTypeName/revision/2/
	/// Returns the data for 1 entity revision.
	/// </summary>
	[HttpGet("revision/{id}")]
	public virtual async Task<T> LoadRevision([FromRoute] int id)
	{
		var context = Request.GetContext();
		var result = await _service.GetRevision(context, id);
		return await _service.EventGroup.RevisionLoad.Dispatch(context, result, Response);
	}

	/// <summary>
	/// DELETE /v1/entityTypeName/revision/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("revision/{id}")]
	public virtual async Task<T> DeleteRevision([FromRoute] int id)
	{
		var context = Request.GetContext();
		var result = await _service.GetRevision(context, id);
		result = await _service.EventGroup.RevisionDelete.Dispatch(context, result, Response);

		if (result == null || !await _service.DeleteRevision(context, id))
		{
			// The handlers have blocked this one from happening, or it failed
			return null;
		}

		return result;
	}

	/// <summary>
	/// GET /v1/entityTypeName/revision/list
	/// Lists all entity revisions of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("revision/list")]
	public virtual async Task<Set<T>> ListRevisions()
	{
		return await ListRevisions(null);
	}

	/// <summary>
	/// POST /v1/entityTypeName/revision/list
	/// Lists filtered entity revisions available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("revision/list")]
	public virtual async Task<Set<T>> ListRevisions([FromBody] JObject filters)
	{
		var context = Request.GetContext();
		var filter = new Filter<T>(filters);

		filter = await _service.EventGroup.RevisionList.Dispatch(context, filter, Response);

		if (filter == null)
		{
			// A handler rejected this request.
			return null;
		}

		var results = await _service.ListRevisions(context, filter);
		return new Set<T>() { Results = results };
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/revision/1
	/// Updates an entity revision with the given ID.
	/// </summary>
	[HttpPost("revision/{id}")]
	public virtual async Task<T> UpdateRevision([FromRoute] int id, [FromBody] JObject body)
	{
		var context = Request.GetContext();

		var entity = await _service.GetRevision(context, id);
		
		/*
		form = await _service.EventGroup.RevisionUpdate.Dispatch(context, form, Response) as U;

		if (form == null || form.Result == null)
		{
			// A handler rejected this request.
			return null;
		}

		entity = await _service.UpdateRevision(context, form.Result);

		if (entity == null)
		{
			Response.StatusCode = 500;
			return null;
		}

		*/

		return entity;
	}

}
