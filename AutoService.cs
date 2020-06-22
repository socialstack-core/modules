using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;


/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T> where T: DatabaseRow, new(){
	
	/// <summary>
	/// A query which deletes 1 entity revision.
	/// </summary>
	protected Query<T> revisionDeleteQuery;

	/// <summary>
	/// A query which creates 1 entity revision.
	/// </summary>
	protected Query<T> revisionCreateQuery;

	/// <summary>
	/// A query which selects 1 entity revision.
	/// </summary>
	protected Query<T> revisionSelectQuery;

	/// <summary>
	/// A query which lists multiple revisions.
	/// </summary>
	protected Query<T> revisionListQuery;

	/// <summary>
	/// A query which updates 1 entity revision.
	/// </summary>
	protected Query<T> revisionUpdateQuery;

	/// <summary>
	/// A query which clears draft state for any existing revisions of a particular content.
	/// </summary>
	protected Query<T> clearDraftStateQuery;


	/// <summary>Sets up the revision queries.</summary>
	private void SetupRevisionQueries()
	{
		var tableName = typeof(T).TableName() + "_revisions";
		
		revisionDeleteQuery = Query.Delete<T>().SetMainTable(tableName);
		revisionCreateQuery = Query.Insert<T>().SetMainTable(tableName);
		revisionUpdateQuery = Query.Update<T>().SetMainTable(tableName);
		revisionSelectQuery = Query.Select<T>().SetMainTable(tableName);
		revisionListQuery = Query.List<T>().SetMainTable(tableName);
		
		clearDraftStateQuery = Query.Update<T>().SetMainTable(tableName);
		SetRevisionColumns(clearDraftStateQuery);
		clearDraftStateQuery.RemoveAllBut("RevisionIsDraft");
		clearDraftStateQuery.Where().Equals("IsDraft", 1).And().EqualsArg("RevisionOriginalContentId", 0);

		SetRevisionColumns(revisionCreateQuery);
		SetRevisionColumns(revisionUpdateQuery);
		SetRevisionColumns(revisionSelectQuery);
		SetRevisionColumns(revisionListQuery);
	}
	
	/// <summary>
	/// Remaps some of the query fields such that they correctly direct data to and from the entity fields/ database columns.
	/// </summary>
	private void SetRevisionColumns(Query<T> query){
		
		var revisionIdField = typeof(RevisionRow).GetField("_RevisionId", BindingFlags.Instance | BindingFlags.NonPublic);
		var idField = typeof(DatabaseRow).GetField("Id");
		
		// Remap the ID column, because the Id column in the database goes to the RevisionId field always.
		query.IdField = revisionIdField;
		
		var contentIdField = query.GetField("Id");
		if (contentIdField != null)
		{
			contentIdField.TargetField = revisionIdField;
		}

		// Include hidden draft field:
		var isDraftField = typeof(RevisionRow).GetField("_IsDraft", BindingFlags.Instance | BindingFlags.NonPublic);
		query.AddField(new Field(typeof(T), isDraftField, "RevisionIsDraft"));
		
		// Similarly the actual Id field on the entity goes to the column called RevisionOriginalContentId:
		query.AddField(new Field(typeof(T), idField, "RevisionOriginalContentId"));
	}
	
	/// <summary>
	/// Deletes an entity revision by its ID.
	/// Optionally includes uploaded content refs in there too.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<bool> DeleteRevision(Context context, int id)
	{
		if(revisionDeleteQuery == null)
		{
			SetupRevisionQueries();
		}
		
		// Delete the entry:
		await _database.Run(context, revisionDeleteQuery, id);

		// Ok!
		return true;
	}

	/// <summary>
	/// List a filtered set of revisions.
	/// </summary>
	/// <returns></returns>
	public virtual async Task<List<T>> ListRevisions(Context context, Filter<T> filter)
	{
		if(revisionListQuery == null)
		{
			SetupRevisionQueries();
		}
		
		filter = await EventGroup.RevisionBeforeList.Dispatch(context, filter);
		var list = await _database.List(context, revisionListQuery, filter);
		list = await EventGroup.RevisionAfterList.Dispatch(context, list);
		return list;
	}

	/// <summary>
	/// Gets a single entity revision by its ID.
	/// </summary>
	public virtual async Task<T> GetRevision(Context context, int id)
	{
		if(revisionSelectQuery == null)
		{
			SetupRevisionQueries();
		}
		
		return await _database.Select(context, revisionSelectQuery, id);
	}
	
	/// <summary>
	/// Creates a new entity revision.
	/// Note that this is infrequently used - most revisions are made using an optimised copy process.
	/// The BeforeCreateRevision and AfterCreateRevision events are still triggered however.
	/// </summary>
	public virtual async Task<T> CreateRevision(Context context, T entity)
	{
		if(revisionCreateQuery == null)
		{
			SetupRevisionQueries();
		}
		
		entity = await EventGroup.RevisionBeforeCreate.Dispatch(context, entity);

		// Note: The Id field is automatically updated by Run here.
		if (entity == null || !await _database.Run(context, revisionCreateQuery, entity))
		{
			return default(T);
		}

		entity = await EventGroup.RevisionAfterCreate.Dispatch(context, entity);
		return entity;
	}

	/// <summary>
	/// Updates the given entity revision.
	/// </summary>
	public virtual async Task<T> UpdateRevision(Context context, T entity)
	{
		if(revisionUpdateQuery == null)
		{
			SetupRevisionQueries();
		}
		
		entity = await EventGroup.RevisionBeforeUpdate.Dispatch(context, entity);

		if (entity == null || !await _database.Run(context, revisionUpdateQuery, entity, entity.Id))
		{
			return null;
		}

		entity = await EventGroup.RevisionAfterUpdate.Dispatch(context, entity);
		return entity;
	}
	
	/// <summary>
	/// Creates the given entity as a draft. It'll be assigned a content ID like anything else.
	/// </summary>
	public virtual async Task<T> CreateDraft(Context context, T entity, Action<Context, T> postIdCallback)
	{
		if(revisionCreateQuery == null)
		{
			SetupRevisionQueries();
		}

		if (entity.Id == 0)
		{
			// For simplicity for other consuming API's (such as publishing draft content), as well as 
			// so we can track all revisions of draft content, we'll get a content ID.
			// We do that by creating the object in the database, then immediately deleting it.
			await _database.Run(context, createQuery, entity);
			await _database.Run(context, deleteQuery, entity.Id);
		}
		else
		{
			// Clear any existing drafts:
			await _database.Run(context, clearDraftStateQuery, 0, entity.Id);
		}

		entity = await EventGroup.DraftBeforeCreate.Dispatch(context, entity);
		
		// Note: The Id field is automatically updated by Run here.
		if (entity == null || !await _database.Run(context, revisionCreateQuery, entity))
		{
			return default(T);
		}
		
		postIdCallback?.Invoke(context, entity);
		
		entity = await EventGroup.DraftAfterCreate.Dispatch(context, entity);
		return entity;
	}
}
