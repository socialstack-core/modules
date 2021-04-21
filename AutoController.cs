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
	public virtual async ValueTask<T> LoadRevision([FromRoute] ID id)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}

		var context = await Request.GetContext();
		id = await _service.EventGroup.EndpointStartRevisionLoad.Dispatch(context, id, Response);
		var result = await _service.GetRevision(context, id);
		return await _service.EventGroup.EndpointEndRevisionLoad.Dispatch(context, result, Response);
	}

	/// <summary>
	/// DELETE /v1/entityTypeName/revision/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("revision/{id}")]
	public virtual async ValueTask<T> DeleteRevision([FromRoute] ID id)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}
		
		var context = await Request.GetContext();
		var result = await _service.GetRevision(context, id);
		await _service.EventGroup.EndpointStartRevisionDelete.Dispatch(context, id, Response);

		if (result == null || !await _service.DeleteRevision(context, id))
		{
			// The handlers have blocked this one from happening, or it failed
			return null;
		}

		result = await _service.EventGroup.EndpointEndRevisionDelete.Dispatch(context, result, Response);
		return result;
	}

	/// <summary>
	/// GET /v1/entityTypeName/revision/list
	/// Lists all entity revisions of this type available to this user.
	/// </summary>
	/// <returns></returns>
	[HttpGet("revision/list")]
	public virtual async ValueTask<Set<T>> ListRevisions()
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
	public virtual async ValueTask<Set<T>> ListRevisions([FromBody] JObject filters)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}
		
		var context = await Request.GetContext();
		var filter = new Filter<T>(filters);

		filter = await _service.EventGroup.EndpointStartRevisionList.Dispatch(context, filter, Response);

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
	/// Updates an entity revision with the given RevisionId.
	/// </summary>
	[HttpPost("revision/{id}")]
	public virtual async ValueTask<T> UpdateRevision([FromRoute] ID id, [FromBody] JObject body)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}
		
		var context = await Request.GetContext();
		
		var entity = await _service.GetRevision(context, id);

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

		// Make sure it's the same ID:
		entity.SetId(id);

		// Run the request update event:
		entity = await _service.EventGroup.EndpointStartRevisionUpdate.Dispatch(context, entity, Response) as T;

		if (entity == null)
		{
			// A handler rejected this request.
			return null;
		}

		entity = await _service.UpdateRevision(context, entity);

		if (entity == null)
		{
			// It was blocked or went wrong, typically because of a bad request.
			Response.StatusCode = 400;
			return null;
		}
		
		// Run the request updated event:
		entity = await _service.EventGroup.EndpointEndRevisionUpdate.Dispatch(context, entity, Response) as T;
		
		return entity;
	}
	
	/// <summary>
	/// GET /v1/entityTypeName/publish/1
	/// Publishes the given revision as the new live entry.
	/// </summary>
	[HttpGet("publish/{id}")]
	public virtual async ValueTask<T> PublishRevision([FromRoute] ID id, [FromBody] JObject body)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}
		
		var context = await Request.GetContext();
		
		var entity = await _service.GetRevision(context, id);

		if (entity == null)
		{
			// Either not allowed to edit this, or it doesn't exist.
			// Both situations are a 404.
			Response.StatusCode = 404;
			return null;
		}
		
		// Run the request update event:
		entity = await _service.EventGroup.EndpointStartRevisionPublish.Dispatch(context, entity, Response) as T;

		if (entity == null)
		{
			// A handler rejected this request.
			return null;
		}

		entity = await _service.PublishRevision(context, entity);

		if (entity == null)
		{
			// It was blocked or went wrong, typically because of a bad request.
			Response.StatusCode = 400;
			return null;
		}
		
		// Run the request updated event:
		entity = await _service.EventGroup.EndpointEndRevisionPublish.Dispatch(context, entity, Response) as T;
		
		return entity;
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/publish/1
	/// Publishes the given posted object as an extension to the given revision.
	/// </summary>
	[HttpPost("publish/{id}")]
	public virtual async ValueTask<T> PublishAndUpdateRevision([FromRoute] ID id, [FromBody] JObject body)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}
		
		var context = await Request.GetContext();
		
		var entity = await _service.GetRevision(context, id);

		if (entity == null)
		{
			// Either not allowed to edit this, or it doesn't exist.
			// Both situations are a 404.
			Response.StatusCode = 404;
			return null;
		}
		
		var contentId = entity.GetId();
		
		// In this case the entity ID is definitely known, so we can run all fields at the same time:
		var notes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Any);

		if (notes != null)
		{
			Request.Headers["Api-Notes"] = notes;
		}
		
		// Ensure the ID remains unchanged:
		entity.SetId(contentId);
		
		// Run the request update event:
		entity = await _service.EventGroup.EndpointStartRevisionPublish.Dispatch(context, entity, Response) as T;

		if (entity == null)
		{
			// A handler rejected this request.
			return null;
		}

		entity = await _service.PublishRevision(context, entity);

		if (entity == null)
		{
			// It was blocked or went wrong, typically because of a bad request.
			Response.StatusCode = 400;
			return null;
		}
		
		// Run the request updated event:
		entity = await _service.EventGroup.EndpointEndRevisionPublish.Dispatch(context, entity, Response) as T;
		
		return entity;
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/draft/
	/// Updates an entity revision with the given ID.
	/// </summary>
	[HttpPost("draft")]
	public virtual async ValueTask<T> CreateDraft([FromBody] JObject body)
	{
		if (!_service.IsRevisionType())
		{
			Response.StatusCode = 404;
			return null;
		}
		
		var context = await Request.GetContext();

		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = new T();

		// If it's revisionable we'll set the user ID now:
		var revisionableEntity = (entity as Api.Users.VersionedContent<ID>);

		if (revisionableEntity != null)
		{
			revisionableEntity.UserId = context.UserId;

			// Mark it as a draft:
			revisionableEntity.IsDraft = true;
		}
		
		// Set the actual fields now:
		var notes = await SetFieldsOnObject(entity, context, body, JsonFieldGroup.Default);
		
		/*
		// Note: Providing an Id is acceptable here.
		if (entity.Id != 0)
		{
			// Get the entity that this is a draft for:
			var draftOfThisContent = await _service.Get(context, entity.Id);

			if (draftOfThisContent == null)
			{
				// Note: fails if creating another draft of already draft content.
				Response.StatusCode = 404;
				return null;
			}
		}
		*/

		// Fire off a create draft event:
		entity = await _service.EventGroup.EndpointStartDraftCreate.Dispatch(context, entity, Response) as T;
		
		if (entity == null)
		{
			// A handler rejected this request.
			if (notes != null)
			{
				Request.Headers["Api-Notes"] = notes;
			}

			return null;
		}
		
		entity = await _service.CreateDraft(context, entity, async (Context c, T ent) => {

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
		entity = await _service.EventGroup.EndpointEndDraftCreate.Dispatch(context, entity, Response) as T;

		return entity;
	}

}
