using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;
using Api.Startup;
using Api.Users;
using Api.SocketServerLibrary;

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class AutoController<T, ID>
{
	/// <summary>
	/// GET /v1/entityTypeName/revision/2/
	/// Returns the data for 1 entity revision.
	/// </summary>
	[HttpGet("revision/{id}")]
	public virtual async ValueTask LoadRevision([FromRoute] ID id, [FromQuery] string includes = null)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			Response.StatusCode = 404;
			return;
		}

		var context = await Request.GetContext();

		id = await revisions.EventGroup.EndpointStartLoad.Dispatch(context, id, Response);
		var result = await _service.Get(context, id);
		result = await revisions.EventGroup.EndpointEndLoad.Dispatch(context, result, Response);

		await OutputJson(context, result, includes);
	}

	/// <summary>
	/// DELETE /v1/entityTypeName/revision/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("revision/{id}")]
	public virtual async ValueTask DeleteRevision([FromRoute] ID id, [FromQuery] string includes = null)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			Response.StatusCode = 404;
			return;
		}

		var context = await Request.GetContext();
		var result = await revisions.Get(context, id);
		result = await revisions.EventGroup.EndpointStartDelete.Dispatch(context, result, Response);

		if (result == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}
			return;
		}

		if (result == null || !await revisions.Delete(context, result))
		{
			// The handlers have blocked this one from happening, or it failed
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}
			return;
		}

		result = await revisions.EventGroup.EndpointEndDelete.Dispatch(context, result, Response);
		await OutputJson(context, result, includes);
	}

	/// <summary>
	/// GET /v1/entityTypeName/revision/list
	/// Lists all entity revisions of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("revision/list")]
	public virtual async ValueTask ListRevisions()
	{
		await ListRevisions(null);
	}

	/// <summary>
	/// POST /v1/entityTypeName/revision/list
	/// Lists filtered entity revisions available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("revision/list")]
	public virtual async ValueTask ListRevisions([FromBody] JObject filters, [FromQuery] string includes = null)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			Response.StatusCode = 404;
			return;
		}

		var context = await Request.GetContext();

		var filter = revisions.LoadFilter(filters) as Filter<T, ID>;
		filter = await revisions.EventGroup.EndpointStartList.Dispatch(context, filter, Response);

		if (filter == null)
		{
			// A handler rejected this request.
			Response.StatusCode = 404;
			return;
		}

		Response.ContentType = _applicationJson;
		var writer = Writer.GetPooled();
		writer.Start(null);
		await revisions.ToJson(context, filter, async (Context ctx, Filter<T, ID> filt, Func<T, int, ValueTask> onResult) => {

			return await revisions.GetResults(ctx, filt, async (Context ctx2, T result, int index, object src, object srcB) => {

				// Passing this in avoids a delegate frame allocation:
				var _onResult = src as Func<T, int, ValueTask>;
				await _onResult(result, index);

			}, onResult, null);

		}, writer, Response.Body, includes);
		writer.Release();
		filter = await revisions.EventGroup.EndpointEndList.Dispatch(context, filter, Response);
		filter.Release();
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/revision/1
	/// Updates an entity revision with the given RevisionId.
	/// </summary>
	[HttpPost("revision/{id}")]
	public virtual async ValueTask UpdateRevision([FromRoute] ID id, [FromBody] JObject body)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			Response.StatusCode = 404;
			return;
		}
		var context = await Request.GetContext();

		var entity = await revisions.Get(context, id);

		if (entity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		// Run the request update event (using the original object to be updated):
		entity = await revisions.EventGroup.EndpointStartUpdate.Dispatch(context, entity, Response) as T;

		if (entity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		if (!await revisions.StartUpdate(context, entity))
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

		entity = await revisions.FinishUpdate(context, entity);

		if (entity == null)
		{
			if (Response.StatusCode == 200)
			{
				Response.StatusCode = 404;
			}

			return;
		}

		// Run the request updated event:
		entity = await revisions.EventGroup.EndpointEndUpdate.Dispatch(context, entity, Response) as T;
		await OutputJson(context, entity, "*");
	}
	
	/// <summary>
	/// GET /v1/entityTypeName/publish/1
	/// Publishes the given revision as the new live entry.
	/// </summary>
	[HttpGet("publish/{id}")]
	public virtual async ValueTask PublishRevision([FromRoute] ID id)
	{
		await PublishRevision(id, null);
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/publish/1
	/// Publishes the given posted object as an extension to the given revision (if body is not null).
	/// </summary>
	[HttpPost("publish/{id}")]
	public virtual ValueTask PublishRevision([FromRoute] ID id, [FromBody] JObject body)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			Response.StatusCode = 404;
			return new ValueTask();
		}

		throw new PublicException("Publishing is incomplete", "incomplete");
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/draft/
	/// Creates a draft.
	/// </summary>
	[HttpPost("draft")]
	public virtual ValueTask CreateDraft([FromBody] JObject body)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			Response.StatusCode = 404;
			return new ValueTask();
		}

		throw new PublicException("Drafts are incomplete", "incomplete");
	}

}
