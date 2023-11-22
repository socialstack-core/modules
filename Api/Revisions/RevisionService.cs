using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;

namespace Api.Revisions
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class RevisionService : AutoService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RevisionService()
        {
			// Exists as a convenient way to check if revisions are supported.
		}
	}

	/// <summary>
	/// Revision service for a particular type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public class RevisionService<T, ID> : AutoService<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{

		/// <summary>
		/// The parent service.
		/// </summary>
		public AutoService<T, ID> Parent;

		/// <summary>
		/// Instances as revision service for a particular type. See also: aService.Revisions
		/// </summary>
		public RevisionService(AutoService<T, ID> parent) : base(new EventGroup<T, ID>())
		{
			Parent = parent;
		}

		/*
		 
	/// <summary>
	/// A query which deletes 1 entity revision.
	/// </summary>
	protected Query revisionDeleteQuery;

	/// <summary>
	/// A query which creates 1 entity revision.
	/// </summary>
	protected Query revisionCreateQuery;

	/// <summary>
	/// A query which selects 1 entity revision.
	/// </summary>
	protected Query revisionSelectQuery;

	/// <summary>
	/// A query which lists multiple revisions.
	/// </summary>
	protected Query revisionListQuery;

	/// <summary>
	/// A query which updates 1 entity revision.
	/// </summary>
	protected Query revisionUpdateQuery;

	/// <summary>
	/// A query which clears draft state for any existing revisions of a particular content.
	/// </summary>
	protected Query clearDraftStateQuery;

	/// <summary>Sets up the revision queries.</summary>
	private void SetupRevisionQueries()
	{
		var tableName = typeof(T).TableName() + "_revisions";
		
		revisionDeleteQuery = Query.Delete(InstanceType).SetMainTableName(tableName);
		revisionCreateQuery = Query.Insert(InstanceType).SetMainTableName(tableName);
		revisionUpdateQuery = Query.Update(InstanceType).SetMainTableName(tableName);
		revisionSelectQuery = Query.Select(InstanceType).SetMainTableName(tableName);
		revisionListQuery = Query.List(InstanceType).SetMainTableName(tableName);
		
		clearDraftStateQuery = Query.Update(InstanceType).SetMainTableName(tableName);
		SetRevisionColumns(clearDraftStateQuery);
		clearDraftStateQuery.RemoveAllBut("RevisionIsDraft");
		clearDraftStateQuery.Where().Equals(InstanceType, "IsDraft", 1).And().EqualsArg(InstanceType, "RevisionOriginalContentId", 0);

		SetRevisionColumns(revisionCreateQuery);
		SetRevisionColumns(revisionUpdateQuery);
		SetRevisionColumns(revisionSelectQuery);
		SetRevisionColumns(revisionListQuery);
	}
	
	/// <summary>
	/// Remaps some of the query fields such that they correctly direct data to and from the entity fields/ database columns.
	/// </summary>
	private static void SetRevisionColumns(Query query){
		
		var revisionIdField = typeof(VersionedContent<ID>).GetField("_RevisionId", BindingFlags.Instance | BindingFlags.NonPublic);
		var idField = typeof(T).GetField("Id");
		
		// Remap the ID column, because the Id column in the database goes to the RevisionId field always.
		query.IdField = revisionIdField;
		
		var contentIdField = query.GetField("Id");
		if (contentIdField != null)
		{
			contentIdField.TargetField = revisionIdField;
		}

		// Include hidden draft field:
		var isDraftField = typeof(VersionedContent<ID>).GetField("_IsDraft", BindingFlags.Instance | BindingFlags.NonPublic);
		query.AddField(new Field(typeof(T), isDraftField, "RevisionIsDraft"));
		
		// Similarly the actual Id field on the entity goes to the column called RevisionOriginalContentId:
		query.AddField(new Field(typeof(T), idField, "RevisionOriginalContentId"));
	}
	
	/// <summary>
	/// Publishes the given entity, which originated from a revision. The entity content ID may not exist at all.
	/// </summary>
	public virtual async ValueTask<T> PublishRevision(Context context, T entity, DataOptions options = DataOptions.Default)
	{
		if(revisionCreateQuery == null)
		{
			SetupRevisionQueries();
		}

		var id = entity.GetId();

		if (id.Equals(0))
		{
			// Id required.
			return null;
		}

		// Clear any existing drafts:
		await _database.Run(context, clearDraftStateQuery, 0, id);

		var rr = (entity as VersionedContent<ID>);

		if (rr != null)
		{
			// Clear revision ID:
			rr.RevisionId = default;
			rr.IsDraft = false;
		}

		// Does it exist? If yes, call update, otherwise, create it (but with a prespecified ID).
		var existingObject = await Get(context, id, options);

		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		entity = await EventGroup.BeforeRevisionPublish.Dispatch(context, entity);
		context.IgnorePermissions = previousPermState;

		if (existingObject != null)
		{
			// This effectively replaces the complete live row with the revision's data.
			entity = await FinishUpdate(context, entity);
		}
		else
		{
			// Create
			entity = await Create(context, entity, DataOptions.IgnorePermissions);
		}

		entity = await EventGroup.AfterRevisionPublish.Dispatch(context, entity);

		return entity;
	}

	/// <summary>
	/// Creates the given entity as a draft. It'll be assigned a content ID like anything else.
	/// </summary>
	public virtual async ValueTask<T> CreateDraft(Context context, T entity, Action<Context, T> postIdCallback, DataOptions options = DataOptions.Default)
	{
		if(revisionCreateQuery == null)
		{
			SetupRevisionQueries();
		}

		var id = entity.GetId();

		if (id.Equals(0))
		{
			// For simplicity for other consuming API's (such as publishing draft content), as well as 
			// so we can track all revisions of draft content, we'll get a content ID.
			// We do that by creating the object in the database, then immediately deleting it.
			await _database.Run(context, createQuery, entity);
			await _database.Run(context, deleteQuery, id);
		}
		else
		{
			// Clear any existing drafts:
			await _database.Run(context, clearDraftStateQuery, 0, id);
		}

		var previousPermState = context.IgnorePermissions;
		context.IgnorePermissions = options == DataOptions.IgnorePermissions;
		entity = await EventGroup.BeforeDraftCreate.Dispatch(context, entity);
		context.IgnorePermissions = previousPermState;

		// Note: The Id field is automatically updated by Run here.
		if (entity == null || !await _database.Run(context, revisionCreateQuery, entity))
		{
			return default;
		}
		
		postIdCallback?.Invoke(context, entity);
		
		entity = await EventGroup.AfterDraftCreate.Dispatch(context, entity);
		return entity;
	}
		 */

	}

}
