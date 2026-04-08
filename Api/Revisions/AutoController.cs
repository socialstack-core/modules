using Api.Contexts;
using Api.Permissions;
using Api.Revisions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

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
	[OmitIfNoService]
	public virtual async ValueTask<Revision<T,ID>> LoadRevision(Context context, [FromRoute] ID id)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			return null;
		}

		var result = await revisions.Get(context, id);
		return result;
	}

	/// <summary>
	/// GET /v1/entityTypeName/revision/content/2/
	/// Returns the loaded data for a specific entity revision.
	/// </summary>
	[HttpGet("revision/content/{id}")]
	public virtual async ValueTask<T> LoadFromRevision(Context context, [FromRoute] ID id)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			return null;
		}

		var result = await revisions.Get(context, id);

		if (result == null || string.IsNullOrEmpty(result.ContentJson))
		{
			return null;
		}

		var val = _service.FromStoredJson(result.ContentJson);
		val.Id = result.ContentId;
		return val;
	}

	/// <summary>
	/// DELETE /v1/entityTypeName/revision/2/
	/// Deletes an entity
	/// </summary>
	[HttpDelete("revision/{id}")]
	[OmitIfNoService]
	public virtual async ValueTask<Revision<T,ID>> DeleteRevision(Context context, [FromRoute] ID id)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			return null;
		}

		var result = await revisions.Get(context, id);
		
		if (result == null || !await revisions.Delete(context, result))
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
	[OmitIfNoService]
	public virtual ValueTask<ContentStream<Revision<T, ID>, ID>?> RevisionListAll(Context context)
	{
		return RevisionList(context, null);
	}

	/// <summary>
	/// POST /v1/entityTypeName/revision/list
	/// Lists filtered entity revisions available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("revision/list")]
	[OmitIfNoService]
	public virtual ValueTask<ContentStream<Revision<T, ID>, ID>?> RevisionList(Context context, [FromBody] ListFilter filters)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			return new ValueTask<ContentStream<Revision<T, ID>, ID>?>((ContentStream<Revision<T, ID>, ID>?)null);
		}

		var filter = revisions.LoadFilter(filters) as Filter<Revision<T, ID>, ID>;
		
		if (filter == null)
		{
			// A handler rejected this request.
			return new ValueTask<ContentStream<Revision<T, ID>, ID>?>((ContentStream<Revision<T, ID>, ID>?)null);
		}

		return new ValueTask<ContentStream<Revision<T, ID>, ID>?>(revisions.GetResults(filter));
	}
	
	/// <summary>
	/// GET /v1/entityTypeName/publish/1
	/// Publishes the given revision as the new live entry.
	/// </summary>
	[HttpGet("publish/{id}")]
	[OmitIfNoService]
	public virtual async ValueTask<T> PublishRevision(Context context, [FromRoute] ID id)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			return null;
		}

		var entity = await revisions.Get(context, id);
		return await revisions.PublishRevision(context, entity);
	}
	
	/// <summary>
	/// POST /v1/entityTypeName/draft/
	/// Creates a draft.
	/// </summary>
	[HttpPost("draft")]
	[OmitIfNoService]
	public virtual async ValueTask<Revision<T,ID>> CreateDraft(Context context, [FromBody] JObject body)
	{
		var revisions = _service.Revisions;

		if (revisions == null)
		{
			return null;
		}

		// Start building up our object.
		// Most other fields, particularly custom extensions, are handled by autoform.
		var entity = (T)Activator.CreateInstance(_service.InstanceType);

		// If it's user created we'll set the user ID now:
		var userCreated = (entity as Api.Users.UserCreatedContent<ID>);

		if (userCreated != null)
		{
			userCreated.UserId = context.UserId;
		}

		// Often zero.
		var revNumber = (entity as VersionedContent<ID>)?.Revision;

		// Set the fields now:
		await _service.SetFieldsOnObject(entity, context, body);

		var contentJson = await _service.ToStoredJson(entity);

		var now = DateTime.UtcNow;

		return await revisions.Create(context, new Revision<T, ID>() {
			CreatedUtc = now,
			EditedUtc = now,
			UserId = context.UserId,
			ImpersonatorUserId = context.RealUserId,
			ContentJson = contentJson,
			IsDraft = true,
			RevisionNumber = revNumber.GetValueOrDefault(),
			ContentId = entity.Id, // Often zero
			ActionType = 1
		});
	}
}
